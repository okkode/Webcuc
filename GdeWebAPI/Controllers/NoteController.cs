using GdeWebAPI.Middleware;
using GdeWebDB.Interfaces;
using GdeWebModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace GdeWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly IAuthService _authService;
        private readonly ILogService _logService;

        public NoteController(INoteService noteService, IAuthService authService, ILogService logService)
        {
            _noteService = noteService;
            _authService = authService;
            _logService = logService;
        }

        /// <summary>
        /// Get a specific note by ID
        /// </summary>
        [HttpGet("{noteId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NoteModel>> GetNote(int noteId)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var note = await _noteService.GetNote(new NoteModel { NoteId = noteId });
            if (!note.Result.Success)
                return NotFound(note.Result);

            // Verify ownership
            if (note.UserId != userId)
                return Unauthorized();

            return Ok(note);
        }

        /// <summary>
        /// Get all notes for the current user
        /// </summary>
        [HttpGet("user")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NoteListModel>> GetUserNotes()
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var notes = await _noteService.GetUserNotes(userId);
            return Ok(notes);
        }

        /// <summary>
        /// Get all notes for a specific course
        /// </summary>
        [HttpGet("course/{courseId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NoteListModel>> GetCourseNotes(int courseId)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var notes = await _noteService.GetCourseNotes(userId, courseId);
            return Ok(notes);
        }

        /// <summary>
        /// Create a new note
        /// </summary>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ResultModel>> AddNote([FromBody] NoteModel note)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            note.UserId = userId;
            var result = await _noteService.AddNote(note);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Update an existing note
        /// </summary>
        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ResultModel>> ModifyNote([FromBody] NoteModel note)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            note.UserId = userId;
            var result = await _noteService.ModifyNote(note);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Delete a note
        /// </summary>
        [HttpDelete("{noteId}")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ResultModel>> DeleteNote(int noteId)
        {
            if (!HttpContext.Request.Headers.TryGetValue("AccessToken", out var accessTokenHeader))
                return Unauthorized();

            var userId = Utilities.Utilities.GetUserIdFromToken(accessTokenHeader);
            var userGuid = Utilities.Utilities.GetUserGuidFromToken(accessTokenHeader);
            var userValid = await _authService.UserValidation(userId, userGuid);
            if (!userValid.Success) return Unauthorized();

            var result = await _noteService.DeleteNote(new NoteModel { NoteId = noteId, UserId = userId });
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}

