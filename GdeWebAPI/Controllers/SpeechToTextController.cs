using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GdeWebAPI.Services;
using GdeWebModels;
using System.Text;

namespace GdeWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SpeechToTextController : ControllerBase
    {
        private readonly SpeechToTextService _speechService;
        private readonly TextCleaningService _cleaningService;
        private readonly ILogger<SpeechToTextController> _logger;

        public SpeechToTextController(
            SpeechToTextService speechService,
            TextCleaningService cleaningService,
            ILogger<SpeechToTextController> logger)
        {
            _speechService = speechService;
            _cleaningService = cleaningService;
            _logger = logger;
        }

        /// <summary>
        /// Real-time audio chunk transcription (for note taking)
        /// Frontend beszél → Audio chunk → Backend → Szöveg vissza
        /// </summary>
        [HttpPost("transcribe-chunk")]
        [RequestSizeLimit(5242880)] // 5MB per chunk
        public async Task<ActionResult<TranscriptionChunkResponse>> TranscribeAudioChunk(
            [FromForm] IFormFile audioChunk,
            [FromForm] string language = "hu",
            [FromForm] bool cleanText = true)
        {
            try
            {
                if (audioChunk == null || audioChunk.Length == 0)
                {
                    return BadRequest("No audio data provided");
                }

                _logger.LogInformation($"Transcribing audio chunk: {audioChunk.Length} bytes");

                // 1. Whisper API - Transcription
                string rawText;
                using (var stream = audioChunk.OpenReadStream())
                {
                    rawText = await _speechService.TranscribeAsync(stream, language);
                }

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    return Ok(new TranscriptionChunkResponse
                    {
                        Text = "",
                        IsFinal = false,
                        Language = language
                    });
                }

                // 2. Szöveg tisztítás (opcionális)
                string finalText = rawText;
                if (cleanText)
                {
                    var cleaningResult = _cleaningService.CleanText(rawText, new TextCleaningConfigModel
                    {
                        Language = language,
                        RemoveFillerWords = true,
                        RemoveExtraWhitespace = true
                    });
                    finalText = cleaningResult.CleanedText;
                }

                // 3. Válasz visszaküldése
                return Ok(new TranscriptionChunkResponse
                {
                    Text = finalText,
                    RawText = rawText,
                    IsFinal = true,
                    Language = language,
                    WordCount = finalText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                    ProcessedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio chunk transcription failed");
                return StatusCode(500, new
                {
                    error = "Transcription failed",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Teljes jegyzet generálás hangfelvételből (opcionális, ha később kell)
        /// </summary>
        [HttpPost("transcribe-note")]
        [RequestSizeLimit(26214400)] // 25MB
        public async Task<ActionResult<TranscriptionResponseModel>> TranscribeFullNote(
            [FromForm] IFormFile audioFile,
            [FromForm] string language = "hu",
            [FromForm] string? noteTitle = null)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    return BadRequest("No audio file provided");
                }

                _logger.LogInformation($"Transcribing full note: {audioFile.FileName}");

                // 1. Whisper API
                string rawText;
                using (var stream = audioFile.OpenReadStream())
                {
                    rawText = await _speechService.TranscribeAsync(stream, language);
                }

                // 2. Tisztítás
                var cleaningResult = _cleaningService.CleanText(rawText, new TextCleaningConfigModel
                {
                    Language = language
                });

                // 3. Válasz
                var response = new TranscriptionResponseModel
                {
                    TranscriptionId = Guid.NewGuid().ToString(),
                    RawText = rawText,
                    CleanedText = cleaningResult.CleanedText,
                    Metadata = new TranscriptionMetadata
                    {
                        Language = language,
                        WordCount = cleaningResult.Statistics.CleanedWordCount,
                        OriginalFileName = audioFile.FileName,
                        FileSizeBytes = audioFile.Length
                    },
                    CreatedAt = DateTime.UtcNow,
                    Status = TranscriptionStatus.Completed
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full note transcription failed");
                return StatusCode(500, new { error = "Transcription failed" });
            }
        }

        /// <summary>
        /// Supported languages
        /// </summary>
        [HttpGet("supported-languages")]
        [AllowAnonymous]
        public ActionResult<List<LanguageOption>> GetSupportedLanguages()
        {
            return Ok(new List<LanguageOption>
            {
                new LanguageOption { Code = "hu", Name = "Magyar" },
                new LanguageOption { Code = "en", Name = "English" },
                new LanguageOption { Code = "de", Name = "Deutsch" },
                new LanguageOption { Code = "es", Name = "Español" },
                new LanguageOption { Code = "fr", Name = "Français" },
                new LanguageOption { Code = "it", Name = "Italiano" }
            });
        }
    }

    // Response model a chunk transcription-höz
    public class TranscriptionChunkResponse
    {
        public string Text { get; set; }
        public string? RawText { get; set; }
        public bool IsFinal { get; set; }
        public string Language { get; set; }
        public int WordCount { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class LanguageOption
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}