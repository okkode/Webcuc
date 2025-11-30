using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GdeWebAPI.Services
{
    public class SpeechToTextService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SpeechToTextService> _logger;

        public SpeechToTextService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SpeechToTextService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> TranscribeAsync(Stream audioStream, string language = "hu")
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("OpenAI API key not configured");
                }

                using var content = new MultipartFormDataContent();

                // Audio fájl hozzáadása
                var streamContent = new StreamContent(audioStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                content.Add(streamContent, "file", "audio.mp3");

                // Paraméterek
                content.Add(new StringContent("whisper-1"), "model");
                content.Add(new StringContent(language), "language");
                content.Add(new StringContent("text"), "response_format");

                // API hívás
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/audio/transcriptions",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Whisper API error: {error}");
                    throw new HttpRequestException($"Transcription failed: {response.StatusCode}");
                }

                var result = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Successfully transcribed audio (language: {language})");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transcription");
                throw;
            }
        }

        public async Task<TranscriptionWithTimestamps> TranscribeWithTimestampsAsync(
            Stream audioStream,
            string language = "hu")
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];

                using var content = new MultipartFormDataContent();

                var streamContent = new StreamContent(audioStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                content.Add(streamContent, "file", "audio.mp3");

                content.Add(new StringContent("whisper-1"), "model");
                content.Add(new StringContent(language), "language");
                content.Add(new StringContent("verbose_json"), "response_format");
                content.Add(new StringContent("true"), "timestamp_granularities[]");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/audio/transcriptions",
                    content
                );

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<WhisperVerboseResponse>(json);

                return new TranscriptionWithTimestamps
                {
                    Text = result?.Text ?? string.Empty,
                    Segments = result?.Segments?.Select(s => new TranscriptionSegment
                    {
                        Text = s.Text,
                        Start = TimeSpan.FromSeconds(s.Start),
                        End = TimeSpan.FromSeconds(s.End)
                    }).ToList() ?? new List<TranscriptionSegment>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transcription with timestamps");
                throw;
            }
        }
    }

    // Helper classes
    public class TranscriptionWithTimestamps
    {
        public string Text { get; set; }
        public List<TranscriptionSegment> Segments { get; set; } = new();
    }

    public class TranscriptionSegment
    {
        public string Text { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }

    public class WhisperVerboseResponse
    {
        public string Text { get; set; }
        public List<WhisperSegment> Segments { get; set; }
    }

    public class WhisperSegment
    {
        public string Text { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
    }
}