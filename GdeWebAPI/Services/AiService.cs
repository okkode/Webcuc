using GdeWebDB.Interfaces;
using GdeWebModels;
using LangChain.Databases;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GdeWebAPI.Services
{
    public class AiService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IHttpClientFactory _f;

        private readonly IConfiguration _cfg;

        private readonly string _apiKey;

        public AiService(IServiceScopeFactory scopeFactory, IHttpClientFactory f, IConfiguration cfg)
        {
            this._scopeFactory = scopeFactory;
            this._f = f;
            this._cfg = cfg;
            this._apiKey = _cfg["OpenAI:ApiKey"] ?? string.Empty;
        }

        /// <summary>
        /// Streames delták (deduplikált) a Responses API-ból.
        /// </summary>
        public async IAsyncEnumerable<string> StreamDeltasAsync(MessageListModel model, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            // input felépítése: system + felhasználói/asszisztens üzenetek
            var messagePrompts = new List<object>();

            var systemMessage = @$"Te egy többnyelven beszélő, segítőkész asszisztens vagy, aki oktatóként működik! Neved: Edu! Feladatod, hogy tanítsd a felhasználókat. 
                Az alábbi szabályok szerint működj:

                1. Ha egy hozzászólás sértő, obszcén, vagy nem kapcsolódik az adatvédelemhez (pl. reklám, politika, trágárság), akkor udvariasan utasítsd el.
                2. A válaszod legyen közérthető, barátságos és rövid, de informatív.
                3. Ne válaszolj politikai, vallási, vagy személyeskedő kérdésre.
                4. A válaszok kerüljenek megosztásra a Facebookon is, ha megfelelnek a moderálásnak.
                5. Ha a szöveget Moderálni kell, akkor a szöveg legvégére illeszd be a következő kódot: '*MODERATE*'
                6. A választ mindig a kérdés nyelvén fogalmazd meg!

                Írásmódodnak informatívnak és logikusnak kell lennie. A mai dátum: {DateTime.Now:yyyy.MM.dd}.";
            var usedModel = "gpt-4o"; // gpt-4.1
            var maxOutputTokens = 2048; // 1024

            if (model.GeneratePrompt)
            {
                // Generálás prompt
                systemMessage = $"Te egy többnyelven beszélő, segítőkész asszisztens vagy! Írásmódodnak informatívnak és logikusnak kell lennie. A mai dátum: {DateTime.Now:yyyy.MM.dd}.";
                usedModel = "gpt-5";
                maxOutputTokens = 32768;

                messagePrompts.Add(new { role = "system", content = systemMessage });
                messagePrompts.Add(new { role = "user", content = masterPrompt });

                // További üzenetek hozzáadása
                foreach (var m in model.MessageList.ToList())
                {
                    var role = string.IsNullOrWhiteSpace(m.Role) ? "user" : m.Role;
                    messagePrompts.Add(new { role, content = m.Message ?? "" });
                }
            }
            else
            {
                if (model.CourseId == 0)
                {
                    // Hagyományos válasz
                    messagePrompts.Add(new { role = "system", content = systemMessage });

                    // További üzenetek hozzáadása
                    foreach (var m in model.MessageList.ToList())
                    {
                        var role = string.IsNullOrWhiteSpace(m.Role) ? "user" : m.Role;
                        messagePrompts.Add(new { role, content = m.Message ?? "" });
                    }
                }
                else
                {
                    messagePrompts.Add(new { 
                        role = "system", 
                        content = systemMessage +
                        @$" 
                            Használd a következő hivatkozásokat a felhasználói kérdés pontos megválaszolásához!
                            Ha nem tudod a választ, vagy közvetlenül nem található a hivatkozásban, akkor keress választ az intertneten!" });

                    // Megkeressük az utolsó user szerepű elemet dinamikusan, és töröljük, mert azt most similarrel küldjük
                    MessageModel lastUserMessage = model.MessageList.ToList()
                        .Last(x => (string)x.Role == "user" || string.IsNullOrWhiteSpace((string)x.Role));
                    var question = lastUserMessage.Message;

                    // Töröljük a listából, mert azt most vectorral adjuk hozzá, a többit pedig hozzáadjuk a promprthoz
                    model.MessageList.Remove(lastUserMessage);

                    // További üzenetek hozzáadása
                    foreach (var m in model.MessageList.ToList())
                    {
                        var role = string.IsNullOrWhiteSpace(m.Role) ? "user" : m.Role;
                        messagePrompts.Add(new { role, content = m.Message ?? "" });
                    }

                    using var scope = _scopeFactory.CreateScope();

                    // Scoped szolgáltatások felvétele scope-ból:
                    var _trainingService = scope.ServiceProvider.GetRequiredService<ITrainingService>();

                    // Keresés vector adatbázisban
                    var provider = new OpenAiProvider(_apiKey);
                    var embeddingModel = new TextEmbeddingV3SmallModel(provider);
                    var llm = new OpenAiLatestFastChatModel(provider);

                    CourseModel result = await _trainingService.GetCourse(new CourseModel() { CourseId = model.CourseId });

                    if (result.Result.Success && !string.IsNullOrEmpty(result.CourseDB))
                    {
                        string file = result.CourseDB.Replace("/vector/", string.Empty);
                        string databasePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "vector", file);
                        string _collectionName = "data";

                        const int searchCount = 5;
                        IReadOnlyCollection<Document> similarDocuments = new List<Document>();

                        using (var vectorDatabase = new LangChain.Databases.Sqlite.SqLiteVectorDatabase(dataSource: databasePath))
                        {
                            if (await vectorDatabase.IsCollectionExistsAsync(_collectionName))
                            {
                                var vectorCollection = await vectorDatabase.GetCollectionAsync(_collectionName);
                                if (vectorCollection is not null)
                                {
                                    EmbeddingRequest embeddingRequest = new EmbeddingRequest() { Strings = new List<String>() { question } };
                                    similarDocuments = await vectorCollection.GetSimilarDocuments(embeddingModel, request: embeddingRequest, searchType: VectorSearchType.MaximumMarginalRelevance, amount: searchCount);
                                }
                            }
                        }

                        if (similarDocuments is not null && similarDocuments.Count > 0)
                        {
                            messagePrompts.Add(new
                            {
                                role = "user",
                                content =
                                @$"Először olvassd el ezeket a hivatkozásokat:
                                '{similarDocuments.AsString()}'

                                A kurzus címe:
                                '{result.CourseTitle}'

                                A kurzus rövid leírása:
                                '{result.CourseDescription}'

                                Most pedig, a hivatkozások vagy a tudásod alapján válaszolj erre a kérdésre: 
                                '{question}'"
                            });
                        }
                    }
                }
            }


            object payload = 
                (usedModel == "gpt-5") ? 
                new
                {
                    model = usedModel,
                    input = messagePrompts.ToArray(),
                    stream = true,
                    max_output_tokens = maxOutputTokens,
                    reasoning = new { effort = "low" },
                    text = new { format = new { type = "text" }, verbosity = "low" },
                    truncation = "auto"
                } 
                
                :
                
                new
                {
                    model = usedModel,
                    input = messagePrompts.ToArray(),
                    stream = true,
                    max_output_tokens = maxOutputTokens
                };

            var http = _f.CreateClient("openai");

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, jsonOpts), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Headers.Accept.Clear();
            req.Headers.Accept.ParseAdd("text/event-stream");

            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync(ct);

                // ha a stream tiltott (org nincs verifikálva) → adjuk vissza egyszerű hiba szövegként
                if ((int)res.StatusCode == 400 && err.Contains("\"param\": \"stream\""))
                {
                    yield return "[Hiba] A szervezet nincs verifikálva a streamhez. Kapcsold ki a streamet, vagy végezd el a Verify Organization-t.";
                    yield break;
                }

                yield return $"[Hiba {res.StatusCode}] {err}";
                yield break;
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // deduplikációs állapot
            var built = new StringBuilder(capacity: 4096);
            string lastChunk = "";

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line is null) break;
                if (line.Length == 0) continue;
                if (!line.StartsWith("data:")) continue;

                var data = line.AsSpan(5).Trim().ToString();
                if (data == "[DONE]") yield break;

                string? toEmit = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("type", out var t) &&
                        t.GetString() == "response.output_text.delta" &&
                        root.TryGetProperty("delta", out var d))
                    {
                        toEmit = d.GetString();
                    }
                    else if (root.TryGetProperty("type", out var t2) && t2.GetString() == "response.completed")
                    {
                        break;
                    }
                    // minden mást ignorálunk
                }
                catch
                {
                    // ha nem JSON, kihagyjuk (tisztább kimenet)
                    continue;
                }

                if (string.IsNullOrEmpty(toEmit)) continue;

                // deduplikáció
                if (toEmit == lastChunk) continue;
                if (toEmit.Length <= 16 && EndsWith(built, toEmit)) continue;
                if (toEmit.Length <= 64 && EndsWithOverlap(built, toEmit)) continue;

                lastChunk = toEmit;
                built.Append(toEmit);
                yield return toEmit;
            }
        }

        // Segéd: a StringBuilder vége egyezik-e egy tail-lel
        private static bool EndsWith(StringBuilder sb, string tail)
        {
            if (tail.Length == 0 || sb.Length < tail.Length) return false;
            for (int i = 0; i < tail.Length; i++)
            {
                if (sb[sb.Length - tail.Length + i] != tail[i]) return false;
            }
            return true;
        }

        // Segéd: gyors átfedés-detektor rövid mintákra
        private static bool EndsWithOverlap(StringBuilder sb, string s)
        {
            int n = s.Length;
            if (n == 0 || sb.Length == 0) return false;

            int maxCheck = Math.Min(n, sb.Length);
            for (int k = 1; k <= maxCheck; k++)
            {
                bool ok = true;
                for (int i = 0; i < k; i++)
                {
                    if (sb[sb.Length - k + i] != s[i]) { ok = false; break; }
                }
                if (ok && n <= k) return true;
            }
            return false;
        }

        /// <summary>
        /// Generates a monthly summary from user notes using OpenAI.
        /// </summary>
        public async Task<string> GenerateMonthlySummaryAsync(string notesContent, int year, int month, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    throw new Exception("OpenAI API kulcs nincs beállítva. Kérlek add hozzá az 'OpenAI:ApiKey' értéket az appsettings.json fájlhoz.");
                }

                var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("hu-HU"));
                
                var systemMessage = @$"Te egy oktatási asszisztens vagy, aki segít a diákoknak összefoglalni tanulmányaikat. 
A mai dátum: {DateTime.Now:yyyy.MM.dd}.
A feladatod, hogy a diák jegyzeteiből készíts egy részletes, strukturált havi összefoglalót {monthName} hónapra.
Az összefoglaló legyen informatív, jól strukturált és segítse a diákot a tanulásban.";

                var userPrompt = @$"A következő jegyzeteket írta egy diák {monthName} hónapban:

{notesContent}

Készíts egy részletes havi összefoglalót a következő részekkel:
1. **Összefoglaló**: Részletes összefoglaló a tanult anyagokról, főbb témákról és fontosabb pontokról. Legyen legalább 300-500 szó.
2. **Mit tanultál ebben a hónapban?**: Konkrét válasz arra, hogy mit tanult a diák ebben a hónapban. Legyen személyes és konkrét.
3. **Mit mutattak be a diákok?**: Ha voltak prezentációk vagy bemutatók, foglald össze azokat. Ha nem voltak, akkor írd, hogy nem voltak bemutatók ebben a hónapban.

A válaszod legyen strukturált, informatív és segítő. Használj HTML formázást (<h2>, <p>, <ul>, <li> stb.) a jobb olvashatóságért.
A válaszod csak az összefoglaló tartalmat tartalmazza, semmi mást.";

                var messagePrompts = new List<object>
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userPrompt }
                };

                var payload = new
                {
                    model = "gpt-4o",
                    messages = messagePrompts.ToArray(),
                    max_tokens = 2000,
                    temperature = 0.7
                };

                var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var http = _f.CreateClient("openai");

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, jsonOpts), Encoding.UTF8, "application/json")
                };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var res = await http.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync(ct);
                    throw new Exception($"OpenAI API error: {res.StatusCode} - {err}");
                }

                var responseContent = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? string.Empty;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                // Log error and return empty string or throw
                throw new Exception($"Error generating monthly summary: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses the AI-generated summary into structured parts.
        /// </summary>
        public (string summary, string whatLearned, string whatPresented) ParseMonthlySummary(string aiResponse)
        {
            try
            {
                var summary = string.Empty;
                var whatLearned = string.Empty;
                var whatPresented = string.Empty;

                // Try to parse structured response
                // Look for sections marked with <h2> or **
                var lines = aiResponse.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
                var currentSection = string.Empty;
                var currentContent = new StringBuilder();

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    if (trimmed.StartsWith("<h2>") || trimmed.StartsWith("##") || trimmed.StartsWith("**"))
                    {
                        // Save previous section
                        if (!string.IsNullOrEmpty(currentSection))
                        {
                            var content = currentContent.ToString().Trim();
                            switch (currentSection.ToLower())
                            {
                                case "összefoglaló":
                                case "summary":
                                    summary = content;
                                    break;
                                case "mit tanultál":
                                case "what learned":
                                case "mit tanultál ebben a hónapban":
                                    whatLearned = content;
                                    break;
                                case "mit mutattak be":
                                case "what presented":
                                case "mit mutattak be a diákok":
                                    whatPresented = content;
                                    break;
                            }
                        }

                        // Extract section name
                        currentSection = trimmed
                            .Replace("<h2>", "")
                            .Replace("</h2>", "")
                            .Replace("##", "")
                            .Replace("**", "")
                            .Trim();
                        currentContent.Clear();
                    }
                    else
                    {
                        currentContent.AppendLine(line);
                    }
                }

                // Save last section
                if (!string.IsNullOrEmpty(currentSection))
                {
                    var content = currentContent.ToString().Trim();
                    switch (currentSection.ToLower())
                    {
                        case "összefoglaló":
                        case "summary":
                            summary = content;
                            break;
                        case "mit tanultál":
                        case "what learned":
                        case "mit tanultál ebben a hónapban":
                            whatLearned = content;
                            break;
                        case "mit mutattak be":
                        case "what presented":
                        case "mit mutattak be a diákok":
                            whatPresented = content;
                            break;
                    }
                }

                // If parsing failed, use the whole response as summary
                if (string.IsNullOrEmpty(summary) && string.IsNullOrEmpty(whatLearned) && string.IsNullOrEmpty(whatPresented))
                {
                    summary = aiResponse;
                }

                return (summary, whatLearned, whatPresented);
            }
            catch
            {
                // If parsing fails, return the whole response as summary
                return (aiResponse, string.Empty, string.Empty);
            }
        }

        string masterPrompt =
                    @"A következő leírás alapján készíts egy részletes, jól strukturált JSON-választ az alábbi séma szerint.
A bemenet:
Téma: [itt a témakör vagy szöveg], időtartam: [pl. 30 másodperc, 45 másodperc, 1 perc stb.], jelenetek: [minimum 3, minumum 5, minimum 10, stb.], kérdések: [3 darab, 5 darab, 10 darab, stb.], nyelv: [magyar / angol / más]
A feladat:
A megadott témakör, időtartam és nyelv alapján hozz létre egy látványos, oktató vagy figyelemfelkeltő AI-videó promptot.
A kimenet egyetlen JSON objektum legyen, az alábbi mezőkkel:
{
  ""title"": ""[Figyelemfelkeltő videócím (az időtartam ne szerepeljen benne) az adott nyelven, maximum 50 karakterben]"",
  ""description"": ""[Rövid, informatív, inspiráló leírás az adott nyelven, maximum 100 karakterben]"",
  ""content"": ""[Rendkívül részletes, legalább két A4-es oldalnyi, tudományos alapokon nyugvó, oktató jellegű szöveg HTML formátumban. A tartalom térjen ki: 
  • a pontos ismertetésre, hogy miről beszélünk, 
  • múltbeli ismeretekre és történeti előzményekre, 
  • jelenkori kutatásokra és bizonyítékokra, 
  • a témához kapcsolódó tudományágakra vagy tudományokra, 
  • az emberi életre gyakorolt hatásokra (ha releváns), 
  • gyakorlati alkalmazásokra és mérési / számítási módszerekre (ha releváns), 
  • megfigyelési lehetőségekre, 
  • ismert tudósokhoz vagy felfedezésekhez való kapcsolódásokra, 
  • matematikájára, fontos képletekre (ha releváns), 
  • releváns könyv- és forrásajánlásokra (szerző + cím), 
  • és további érdekes tudományos, társadalmi vagy etikai aspektusokra. 
  Legyen jól tagolt, HTML szerkezetű (<h1>, <h2>, <p>, <ol>, <li>, <b> stb. elemekkel).]""
  ""movie"": [
    {
      ""scene"": 1,
      ""time"": ""0–X mp"",
      ""visuals"": ""[Részletes vizuális leírás, színek, mozgások, kameranézetek, dinamikus látványelemek, az oktatási célhoz igazítva]"",
      ""narration"": ""[Rövid, természetes hangvételű, videó-felolvasásra alkalmas narráció az adott nyelven]""
    }
    // További jelenetek, az időtartam alapján automatikusan elosztva
  ],
  ""music"": {
    ""style"": "" [Zenei stílus, pl. cinematic, tech, ambient, inspirational, educational]"",
    ""tempo"": ""[BPM vagy tempóleírás]"",
    ""mood"": ""[Érzelmi hangulat: pl. inspiráló, nyugodt, felfedező, kíváncsi]""
  },
  ""quiz"": [
    {
      ""question"": ""[Kérdés a leírás (content) alapján]"",
      ""answers"": [
        {""text"": ""[válasz 1]"", ""correct"": false},
        {""text"": ""[válasz 2]"", ""correct"": true},
        {""text"": ""[válasz 3]"", ""correct"": false},
        {""text"": ""[válasz 4]"", ""correct"": false}
      ]
    }
    // 3–5-10 kérdés
  ],
  ""keywords"": ""[3 releváns kulcsszó vesszővel elválasztva]""
}
Szabályok:
•  A JSON minden eleme az adott nyelven készüljön.
•  Az időtartam alapján automatikusan oszd el a jeleneteket egyenletesen.
•  A narráció legyen rövid, természetes, dinamikus, jól felolvasható.
•  A content mező legalább két A4-es oldalnyi szöveget tartalmazzon.
•  A quiz kérdések a „description” és „content” anyagára épüljenek, tanulási célra.
•  A teljes kimenet csak a JSON objektumot tartalmazza, semmi mást.
•  A JSON legyen formailag érvényes, jól strukturált és szintaktikailag helyes.
•  A tartalom oktatásra, videókészítésre, narrációra és kutatási szemléltetésre is alkalmas legyen.
•  A szöveg hangneme legyen tudományos, mégis közérthető, vizuálisan is inspiráló, hogy AI-videó vagy oktatóanyag alapjaként is működjön.
";
    }
}
