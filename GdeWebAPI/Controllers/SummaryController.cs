using GdeWebAPI.Middleware;
using GdeWebAPI.Services;
using GdeWebDB.Interfaces;
using GdeWebModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace GdeWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly IAuthService _authService;
        private readonly ILogService _logService;
        private readonly AiService _aiService;

        public SummaryController(INoteService noteService, IAuthService authService, ILogService logService, AiService aiService)
        {
            _noteService = noteService;
            _authService = authService;
            _logService = logService;
            _aiService = aiService;
        }

        /// <summary>
        /// Get monthly summary for a specific year and month
        /// </summary>
        [HttpGet("{year}/{month}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<MonthlySummaryModel>> GetMonthlySummary(int year, int month)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var summary = await _noteService.GetMonthlySummary(userId, year, month);
            if (!summary.Result.Success)
                return NotFound(summary.Result);

            return Ok(summary);
        }

        /// <summary>
        /// Get all monthly summaries for the current user
        /// </summary>
        [HttpGet("user")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<MonthlySummaryListModel>> GetUserMonthlySummaries()
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var summaries = await _noteService.GetUserMonthlySummaries(userId);
            return Ok(summaries);
        }

        /// <summary>
        /// Manually generate a monthly summary for a specific year and month
        /// </summary>
        [HttpPost("generate/{year}/{month}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<MonthlySummaryModel>> GenerateMonthlySummary(int year, int month)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            try
            {
                // Check if summary already exists
                var existingSummary = await _noteService.GetMonthlySummary(userId, year, month);
                if (existingSummary.Result.Success)
                {
                    return BadRequest(new ResultModel 
                    { 
                        Success = false, 
                        ErrorMessage = $"Már létezik összefoglaló {year}.{month:00} hónapra." 
                    });
                }

                // Get all notes from the specified month
                var allNotes = await _noteService.GetUserNotes(userId);
                if (!allNotes.Result.Success || allNotes.NoteList.Count == 0)
                {
                    return BadRequest(new ResultModel 
                    { 
                        Success = false, 
                        ErrorMessage = "Nincsenek jegyzetek az összefoglaló generálásához." 
                    });
                }

                // Filter notes from the specified month
                var monthNotes = allNotes.NoteList
                    .Where(n => n.CreationDate.Year == year && n.CreationDate.Month == month)
                    .ToList();

                if (monthNotes.Count == 0)
                {
                    return BadRequest(new ResultModel 
                    { 
                        Success = false, 
                        ErrorMessage = $"Nincsenek jegyzetek {year}.{month:00} hónapra." 
                    });
                }

                // Combine all notes content
                var notesContent = string.Join("\n\n---\n\n", 
                    monthNotes.Select(n => $"**{n.NoteTitle}**\n{n.NoteContent}"));

                // Generate summary using AI
                var aiResponse = await _aiService.GenerateMonthlySummaryAsync(notesContent, year, month);
                
                // Parse the response
                var (summary, whatLearned, whatPresented) = _aiService.ParseMonthlySummary(aiResponse);

                // Save summary
                var summaryModel = new MonthlySummaryModel
                {
                    UserId = userId,
                    Year = year,
                    Month = month,
                    Summary = summary,
                    WhatLearned = whatLearned,
                    WhatPresented = whatPresented,
                    CreationDate = DateTime.UtcNow,
                    ModificationDate = DateTime.UtcNow
                };

                var result = await _noteService.AddOrUpdateMonthlySummary(summaryModel);
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                // Return the created summary
                var createdSummary = await _noteService.GetMonthlySummary(userId, year, month);
                return Ok(createdSummary);
            }
            catch (Exception ex)
            {
                await _logService.WriteLogToFile(ex, "GenerateMonthlySummary");
                return StatusCode(500, new ResultModel 
                { 
                    Success = false, 
                    ErrorMessage = $"Hiba történt az összefoglaló generálása során: {ex.Message}" 
                });
            }
        }
    }
}

