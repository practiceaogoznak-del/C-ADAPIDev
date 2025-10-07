using System.Net.Http.Json;

namespace FrontEnd.Data;

public class ADResource
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}

public class ADUser
{
    public string SamAccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = new();
}

public class AccessRequestClient
{
    private readonly HttpClient _httpClient;

    public AccessRequestClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ADResource>> GetResourcesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ADResource>>("api/accessrequest/resources") 
            ?? new List<ADResource>();
    }

    public async Task<ADUser?> GetUserAsync(string username)
    {
        return await _httpClient.GetFromJsonAsync<ADUser>($"api/accessrequest/users/{username}");
    }

    public async Task<List<string>> GetUserGroupsAsync(string username)
    {
        return await _httpClient.GetFromJsonAsync<List<string>>($"api/accessrequest/users/{username}/groups") 
            ?? new List<string>();
    }
}