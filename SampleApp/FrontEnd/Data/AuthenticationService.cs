using System.Net.Http.Json;

namespace FrontEnd.Data;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AuthenticationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token);

    public async Task<string?> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(username, password));
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return result?.Token;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}