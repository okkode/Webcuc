using GdeWebDB.Entities;
using GdeWebDB.Interfaces;
using GdeWebDB.Utilities;
using GdeWebModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GdeWebDB.Services
{
    public class NoteService : INoteService
    {
        private readonly GdeDbContext _db;
        private readonly ILogService _log;

        public NoteService(GdeDbContext db, ILogService logService)
        {
            _db = db;
            _log = logService;
        }

        // -------- NOTE OPERATIONS --------

        public async Task<NoteModel> GetNote(NoteModel p)
        {
            try
            {
                var note = await _db.A_NOTE
                    .AsNoTracking()
                    .Where(x => x.NOTEID == p.NoteId)
                    .Select(x => new NoteModel
                    {
                        NoteId = x.NOTEID,
                        UserId = x.USERID,
                        CourseId = x.COURSEID,
                        NoteTitle = x.NOTETITLE,
                        NoteContent = x.NOTECONTENT,
                        CreationDate = x.CREATIONDATE,
                        ModificationDate = x.MODIFICATIONDATE,
                        Result = new ResultModel { Success = true }
                    })
                    .FirstOrDefaultAsync();

                return note ?? new NoteModel { Result = ResultTypes.NotFound };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "GetNote");
                return new NoteModel { Result = ResultTypes.UnexpectedError };
            }
        }

        public async Task<NoteListModel> GetUserNotes(int userId)
        {
            try
            {
                var list = await _db.A_NOTE
                    .AsNoTracking()
                    .Where(x => x.USERID == userId)
                    .OrderByDescending(x => x.MODIFICATIONDATE)
                    .Select(x => new NoteModel
                    {
                        NoteId = x.NOTEID,
                        UserId = x.USERID,
                        CourseId = x.COURSEID,
                        NoteTitle = x.NOTETITLE,
                        NoteContent = x.NOTECONTENT,
                        CreationDate = x.CREATIONDATE,
                        ModificationDate = x.MODIFICATIONDATE
                    })
                    .ToListAsync();

                return new NoteListModel
                {
                    NoteList = list,
                    Count = list.Count,
                    Result = new ResultModel { Success = true }
                };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "GetUserNotes");
                return new NoteListModel { Result = ResultTypes.UnexpectedError };
            }
        }

        public async Task<NoteListModel> GetCourseNotes(int userId, int courseId)
        {
            try
            {
                var list = await _db.A_NOTE
                    .AsNoTracking()
                    .Where(x => x.USERID == userId && x.COURSEID == courseId)
                    .OrderByDescending(x => x.MODIFICATIONDATE)
                    .Select(x => new NoteModel
                    {
                        NoteId = x.NOTEID,
                        UserId = x.USERID,
                        CourseId = x.COURSEID,
                        NoteTitle = x.NOTETITLE,
                        NoteContent = x.NOTECONTENT,
                        CreationDate = x.CREATIONDATE,
                        ModificationDate = x.MODIFICATIONDATE
                    })
                    .ToListAsync();

                return new NoteListModel
                {
                    NoteList = list,
                    Count = list.Count,
                    Result = new ResultModel { Success = true }
                };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "GetCourseNotes");
                return new NoteListModel { Result = ResultTypes.UnexpectedError };
            }
        }

        public async Task<ResultModel> AddNote(NoteModel p)
        {
            try
            {
                var now = DateTime.UtcNow;

                var entity = new Note
                {
                    USERID = p.UserId,
                    COURSEID = p.CourseId,
                    NOTETITLE = p.NoteTitle,
                    NOTECONTENT = p.NoteContent ?? string.Empty,
                    CREATIONDATE = now,
                    MODIFICATIONDATE = now
                };

                _db.A_NOTE.Add(entity);
                await _db.SaveChangesAsync();

                return new ResultModel { Success = true, ErrorMessage = entity.NOTEID.ToString() };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "AddNote");
                return ResultTypes.UnexpectedError;
            }
        }

        public async Task<ResultModel> ModifyNote(NoteModel p)
        {
            try
            {
                var note = await _db.A_NOTE.FirstOrDefaultAsync(x => x.NOTEID == p.NoteId);
                if (note == null) return ResultTypes.NotFound;

                // Verify ownership
                if (note.USERID != p.UserId)
                    return new ResultModel { Success = false, ErrorMessage = "Unauthorized" };

                note.NOTETITLE = p.NoteTitle;
                note.NOTECONTENT = p.NoteContent ?? string.Empty;
                note.MODIFICATIONDATE = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return new ResultModel { Success = true };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "ModifyNote");
                return ResultTypes.UnexpectedError;
            }
        }

        public async Task<ResultModel> DeleteNote(NoteModel p)
        {
            try
            {
                var note = await _db.A_NOTE.FirstOrDefaultAsync(x => x.NOTEID == p.NoteId);
                if (note == null) return ResultTypes.NotFound;

                // Verify ownership
                if (note.USERID != p.UserId)
                    return new ResultModel { Success = false, ErrorMessage = "Unauthorized" };

                _db.A_NOTE.Remove(note);
                await _db.SaveChangesAsync();

                return new ResultModel { Success = true };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "DeleteNote");
                return ResultTypes.UnexpectedError;
            }
        }

        // -------- MONTHLY SUMMARY OPERATIONS --------

        public async Task<MonthlySummaryModel> GetMonthlySummary(int userId, int year, int month)
        {
            try
            {
                var summary = await _db.A_MONTHLY_SUMMARY
                    .AsNoTracking()
                    .Where(x => x.USERID == userId && x.YEAR == year && x.MONTH == month)
                    .Select(x => new MonthlySummaryModel
                    {
                        SummaryId = x.SUMMARYID,
                        UserId = x.USERID,
                        Year = x.YEAR,
                        Month = x.MONTH,
                        Summary = x.SUMMARY,
                        WhatLearned = x.WHATLEARNED,
                        WhatPresented = x.WHATPRESENTED,
                        CreationDate = x.CREATIONDATE,
                        ModificationDate = x.MODIFICATIONDATE,
                        Result = new ResultModel { Success = true }
                    })
                    .FirstOrDefaultAsync();

                return summary ?? new MonthlySummaryModel { Result = ResultTypes.NotFound };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "GetMonthlySummary");
                return new MonthlySummaryModel { Result = ResultTypes.UnexpectedError };
            }
        }

        public async Task<MonthlySummaryListModel> GetUserMonthlySummaries(int userId)
        {
            try
            {
                var list = await _db.A_MONTHLY_SUMMARY
                    .AsNoTracking()
                    .Where(x => x.USERID == userId)
                    .OrderByDescending(x => x.YEAR)
                    .ThenByDescending(x => x.MONTH)
                    .Select(x => new MonthlySummaryModel
                    {
                        SummaryId = x.SUMMARYID,
                        UserId = x.USERID,
                        Year = x.YEAR,
                        Month = x.MONTH,
                        Summary = x.SUMMARY,
                        WhatLearned = x.WHATLEARNED,
                        WhatPresented = x.WHATPRESENTED,
                        CreationDate = x.CREATIONDATE,
                        ModificationDate = x.MODIFICATIONDATE
                    })
                    .ToListAsync();

                return new MonthlySummaryListModel
                {
                    SummaryList = list,
                    Count = list.Count,
                    Result = new ResultModel { Success = true }
                };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "GetUserMonthlySummaries");
                return new MonthlySummaryListModel { Result = ResultTypes.UnexpectedError };
            }
        }

        public async Task<ResultModel> AddOrUpdateMonthlySummary(MonthlySummaryModel p)
        {
            try
            {
                var now = DateTime.UtcNow;
                var existing = await _db.A_MONTHLY_SUMMARY
                    .FirstOrDefaultAsync(x => x.USERID == p.UserId && x.YEAR == p.Year && x.MONTH == p.Month);

                if (existing != null)
                {
                    // Update existing
                    existing.SUMMARY = p.Summary;
                    existing.WHATLEARNED = p.WhatLearned;
                    existing.WHATPRESENTED = p.WhatPresented;
                    existing.MODIFICATIONDATE = now;
                }
                else
                {
                    // Create new
                    var entity = new MonthlySummary
                    {
                        USERID = p.UserId,
                        YEAR = p.Year,
                        MONTH = p.Month,
                        SUMMARY = p.Summary,
                        WHATLEARNED = p.WhatLearned,
                        WHATPRESENTED = p.WhatPresented,
                        CREATIONDATE = now,
                        MODIFICATIONDATE = now
                    };
                    _db.A_MONTHLY_SUMMARY.Add(entity);
                }

                await _db.SaveChangesAsync();
                return new ResultModel { Success = true };
            }
            catch (Exception ex)
            {
                await _log.WriteLogToFile(ex, "AddOrUpdateMonthlySummary");
                return ResultTypes.UnexpectedError;
            }
        }
    }
}

