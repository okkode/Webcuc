using Blazored.LocalStorage;
using GdeWeb.Interfaces;
using GdeWebModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace GdeWeb.Services
{
    public class NoteService : INoteService
    {
        private readonly HttpClient httpClient;
        private readonly ILocalStorageService localStorageService;

        public NoteService(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            this.httpClient = httpClient;
            this.localStorageService = localStorageService;
        }

        private async Task<T> SendGetRequest<T>(string endpoint, bool requireAuth = true)
        {
            HttpRequestMessage request;
            if (requireAuth)
            {
                var accessToken = await localStorageService.GetItemAsync<string>("token");
                if (accessToken == null)
                    throw new HttpRequestException("Hiba történt: Token nem található!");

                request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("AccessToken", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var response = await httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }

        private async Task<T> SendPostRequest<T>(string endpoint, object data, bool requireAuth = true)
        {
            HttpRequestMessage request;
            if (requireAuth)
            {
                var accessToken = await localStorageService.GetItemAsync<string>("token");
                if (accessToken == null)
                    throw new HttpRequestException("Hiba történt: Token nem található!");

                var jsonString = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
                request.Headers.Add("AccessToken", accessToken);
            }
            else
            {
                var jsonString = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            }

            var response = await httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }

        private async Task<T> SendPutRequest<T>(string endpoint, object data, bool requireAuth = true)
        {
            HttpRequestMessage request;
            if (requireAuth)
            {
                var accessToken = await localStorageService.GetItemAsync<string>("token");
                if (accessToken == null)
                    throw new HttpRequestException("Hiba történt: Token nem található!");

                var jsonString = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content };
                request.Headers.Add("AccessToken", accessToken);
            }
            else
            {
                var jsonString = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                request = new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = content };
            }

            var response = await httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }

        private async Task<T> SendDeleteRequest<T>(string endpoint, bool requireAuth = true)
        {
            HttpRequestMessage request;
            if (requireAuth)
            {
                var accessToken = await localStorageService.GetItemAsync<string>("token");
                if (accessToken == null)
                    throw new HttpRequestException("Hiba történt: Token nem található!");

                request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Add("AccessToken", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var response = await httpClient.SendAsync(request);
            return await HandleResponse<T>(response);
        }

        private async Task<T> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response is null)
                throw new HttpRequestException("Üres válasz érkezett a szervertől.");

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Hiba történt: {response.StatusCode}, Üzenet: {jsonResponse}");

            var result = JsonConvert.DeserializeObject<T>(jsonResponse);

            if (result is null)
                throw new JsonException("A szerver válasza érvénytelen vagy üres JSON volt.");

            return result;
        }

        // Note operations
        public async Task<NoteModel> GetNote(int noteId)
        {
            try
            {
                return await SendGetRequest<NoteModel>($"api/Note/{noteId}");
            }
            catch (HttpRequestException ex)
            {
                return new NoteModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }

        public async Task<NoteListModel> GetUserNotes()
        {
            try
            {
                return await SendGetRequest<NoteListModel>("api/Note/user");
            }
            catch (HttpRequestException ex)
            {
                return new NoteListModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }

        public async Task<NoteListModel> GetCourseNotes(int courseId)
        {
            try
            {
                return await SendGetRequest<NoteListModel>($"api/Note/course/{courseId}");
            }
            catch (HttpRequestException ex)
            {
                return new NoteListModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }

        public async Task<ResultModel> AddNote(NoteModel note)
        {
            try
            {
                return await SendPostRequest<ResultModel>("api/Note", note);
            }
            catch (HttpRequestException ex)
            {
                return new ResultModel { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ResultModel> ModifyNote(NoteModel note)
        {
            try
            {
                return await SendPutRequest<ResultModel>("api/Note", note);
            }
            catch (HttpRequestException ex)
            {
                return new ResultModel { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<ResultModel> DeleteNote(int noteId)
        {
            try
            {
                return await SendDeleteRequest<ResultModel>($"api/Note/{noteId}");
            }
            catch (HttpRequestException ex)
            {
                return new ResultModel { Success = false, ErrorMessage = ex.Message };
            }
        }

        // Monthly Summary operations
        public async Task<MonthlySummaryModel> GetMonthlySummary(int year, int month)
        {
            try
            {
                return await SendGetRequest<MonthlySummaryModel>($"api/Summary/{year}/{month}");
            }
            catch (HttpRequestException ex)
            {
                return new MonthlySummaryModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }

        public async Task<MonthlySummaryListModel> GetUserMonthlySummaries()
        {
            try
            {
                return await SendGetRequest<MonthlySummaryListModel>("api/Summary/user");
            }
            catch (HttpRequestException ex)
            {
                return new MonthlySummaryListModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }

        public async Task<MonthlySummaryModel> GenerateMonthlySummary(int year, int month)
        {
            try
            {
                return await SendPostRequest<MonthlySummaryModel>($"api/Summary/generate/{year}/{month}", null);
            }
            catch (HttpRequestException ex)
            {
                return new MonthlySummaryModel { Result = new ResultModel { Success = false, ErrorMessage = ex.Message } };
            }
        }
    }
}

