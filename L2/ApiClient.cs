using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string _accessToken;

    public ApiClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task<bool> Authenticate(string username, string password)
    {
        var requestBody = new
        {
            username,
            password
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "http://ppfc.us-east-2.elasticbeanstalk.com/api/authenticate",
            requestContent);


        var responseData = await response.Content.ReadAsStringAsync();

        if (responseData.Contains("error")) return false;

        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseData);
        if (authResponse!.status == "FAILURE") return false;
        _accessToken = authResponse!.accessToken;
        return true;
    }

    public async Task<string> GetSchedule(int limit, int dayNumber, int groupId)
    {
        if (string.IsNullOrEmpty(_accessToken))
        {
            throw new InvalidOperationException("Not authenticated. Please call Authenticate method first.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.GetAsync(
            $"http://ppfc.us-east-2.elasticbeanstalk.com/api/schedule?limit={limit}&dayNumber={dayNumber}&groupId={groupId}");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            throw new HttpRequestException($"Failed to get schedule. Status code: {response.StatusCode}");
        }
    }

    private class AuthResponse
    {
        public string status { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
    }
}
