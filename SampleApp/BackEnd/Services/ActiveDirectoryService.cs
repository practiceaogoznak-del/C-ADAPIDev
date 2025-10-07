using System.DirectoryServices.AccountManagement;
using BackEnd.Models;
using Microsoft.Extensions.Options;

namespace BackEnd.Services;

public interface IActiveDirectoryService
{
    Task<List<ADResource>> GetAllResourcesAsync();
    Task<List<ADUser>> GetAllUsersAsync();
    Task<ADUser?> GetUserAsync(string username);
    Task<List<string>> GetUserGroupsAsync(string username);
    Task AddUserToGroupAsync(string username, string groupName);
    Task RemoveUserFromGroupAsync(string username, string groupName);
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly ADSettings _settings;
    private readonly Random _random = new Random();

    public ActiveDirectoryService(
        IOptions<ADSettings> settings,
        ILogger<ActiveDirectoryService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetNextDomainController()
    {
        if (_settings.PrimaryControllers == null || _settings.PrimaryControllers.Length == 0)
        {
            return _settings.Domain;
        }

        return _settings.PrimaryControllers[_random.Next(_settings.PrimaryControllers.Length)];
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<PrincipalContext, T> action, string operation)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrEmpty(operation);

        Exception? lastException = null;
        
        foreach (var attempt in Enumerable.Range(1, _settings.RetryAttempts))
        {
            try
            {
                var domainController = GetNextDomainController();
                _logger.LogDebug("Attempt {Attempt}: Trying domain controller: {DomainController} for operation: {Operation}", 
                    attempt, domainController, operation);

                using var context = new PrincipalContext(
                    ContextType.Domain,
                    domainController,
                    _settings.Container);

                var result = action(context);
                
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Operation {Operation} succeeded on attempt {Attempt} using domain controller {DomainController}", 
                        operation, attempt, domainController);
                }
                
                return result;
            }
            catch (Exception ex) when (ex is not ArgumentException and not InvalidOperationException)
            {
                lastException = ex;
                _logger.LogWarning(ex, 
                    "Failed on attempt {Attempt} for operation: {Operation}", 
                    attempt, operation);
                
                if (attempt < _settings.RetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(_settings.RetryDelaySeconds * attempt, 30)));
                }
            }
        }

        throw new InvalidOperationException(
            $"Failed to execute {operation} after {_settings.RetryAttempts} attempts", 
            lastException);
    }

    public async Task<List<ADResource>> GetAllResourcesAsync()
    {
        _logger.LogInformation("Getting all AD resources");
        return await ExecuteWithRetryAsync(context =>
        {
            var resources = new List<ADResource>();
            using var groupPrincipal = new GroupPrincipal(context);
            using var searcher = new PrincipalSearcher(groupPrincipal);
            
            foreach (var result in searcher.FindAll().Cast<GroupPrincipal>())
            {
                if (result?.Description == null || !result.Description.StartsWith("Resource:"))
                {
                    continue;
                }

                try
                {
                    var description = result.Description.Substring(9).Trim();
                    var owner = string.Empty;
                    
                    if (result.Description.Contains("Owner:"))
                    {
                        var parts = result.Description.Split("Owner:", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            owner = parts[1].Trim();
                        }
                    }

                    var members = result.Members?
                        .Where(m => m != null)
                        .Select(m => m.SamAccountName ?? string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList() ?? new List<string>();

                    resources.Add(new ADResource
                    {
                        Name = result.Name ?? string.Empty,
                        Description = description,
                        GroupName = result.SamAccountName ?? string.Empty,
                        Owner = owner,
                        Members = members
                    });

                    _logger.LogDebug("Added resource {ResourceName} with {MemberCount} members", 
                        result.Name ?? "Unknown", members.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process resource group {GroupName}", 
                        result.Name ?? "Unknown");
                }
            }

            return resources;
        }, "GetAllResources");
    }

    public async Task<List<ADUser>> GetAllUsersAsync()
    {
        _logger.LogInformation("Getting all AD users");
        return await ExecuteWithRetryAsync(context =>
        {
            var users = new List<ADUser>();
            using var userPrincipal = new UserPrincipal(context);
            using var searcher = new PrincipalSearcher(userPrincipal);
            
            foreach (var result in searcher.FindAll().Cast<UserPrincipal>())
            {
                if (result == null)
                {
                    continue;
                }

                try
                {
                    var department = string.Empty;
                    if (!string.IsNullOrEmpty(result.Description))
                    {
                        var parts = result.Description.Split(';');
                        department = parts.FirstOrDefault() ?? string.Empty;
                    }

                    var groups = result.GetGroups()?
                        .Where(g => g != null)
                        .Select(g => g.SamAccountName ?? string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList() ?? new List<string>();

                    users.Add(new ADUser
                    {
                        SamAccountName = result.SamAccountName ?? string.Empty,
                        DisplayName = result.DisplayName ?? string.Empty,
                        EmployeeId = result.EmployeeId ?? string.Empty,
                        Title = result.Description ?? string.Empty,
                        Department = department,
                        Email = result.EmailAddress ?? string.Empty,
                        PhoneNumber = result.VoiceTelephoneNumber ?? string.Empty,
                        Groups = groups
                    });

                    _logger.LogDebug("Added user {UserName} with {GroupCount} groups", 
                        result.SamAccountName ?? "Unknown", groups.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process user {UserName}", 
                        result.SamAccountName ?? "Unknown");
                }
            }

            return users;
        }, "GetAllUsers");
    }

    public async Task<ADUser?> GetUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("GetUserAsync called with empty username");
            return null;
        }

        _logger.LogInformation("Getting AD user: {Username}", username);
        return await ExecuteWithRetryAsync(context =>
        {
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            
            if (userPrincipal == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            try
            {
                var department = string.Empty;
                if (!string.IsNullOrEmpty(userPrincipal.Description))
                {
                    var parts = userPrincipal.Description.Split(';');
                    department = parts.FirstOrDefault() ?? string.Empty;
                }

                var groups = userPrincipal.GetGroups()?
                    .Where(g => g != null)
                    .Select(g => g.SamAccountName ?? string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList() ?? new List<string>();

                var user = new ADUser
                {
                    SamAccountName = userPrincipal.SamAccountName ?? string.Empty,
                    DisplayName = userPrincipal.DisplayName ?? string.Empty,
                    EmployeeId = userPrincipal.EmployeeId ?? string.Empty,
                    Title = userPrincipal.Description ?? string.Empty,
                    Department = department,
                    Email = userPrincipal.EmailAddress ?? string.Empty,
                    PhoneNumber = userPrincipal.VoiceTelephoneNumber ?? string.Empty,
                    Groups = groups
                };

                _logger.LogDebug("Successfully retrieved user {Username} with {GroupCount} groups",
                    username, groups.Count);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user data for {Username}", username);
                throw;
            }
        }, $"GetUser_{username}");
    }

    public async Task<List<string>> GetUserGroupsAsync(string username)
    {
        _logger.LogInformation("Getting groups for user: {Username}", username);
        return await ExecuteWithRetryAsync(context =>
        {
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            
            if (userPrincipal == null)
            {
                _logger.LogWarning("User not found when getting groups: {Username}", username);
                return new List<string>();
            }

            return userPrincipal.GetGroups()
                .Select(g => g.SamAccountName ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }, $"GetUserGroups_{username}");
    }

    public async Task AddUserToGroupAsync(string username, string groupName)
    {
        _logger.LogInformation("Adding user {Username} to group {GroupName}", username, groupName);
        await ExecuteWithRetryAsync(context =>
        {
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            using var groupPrincipal = GroupPrincipal.FindByIdentity(context, groupName);
            
            if (userPrincipal == null || groupPrincipal == null)
            {
                _logger.LogWarning("User or group not found: User={Username}, Group={GroupName}", 
                    username, groupName);
                return false;
            }

            if (!groupPrincipal.Members.Contains(userPrincipal))
            {
                groupPrincipal.Members.Add(userPrincipal);
                groupPrincipal.Save();
                _logger.LogInformation("Successfully added user {Username} to group {GroupName}", 
                    username, groupName);
            }
            else
            {
                _logger.LogInformation("User {Username} is already a member of group {GroupName}", 
                    username, groupName);
            }

            return true;
        }, $"AddUserToGroup_{username}_{groupName}");
    }

    public async Task RemoveUserFromGroupAsync(string username, string groupName)
    {
        _logger.LogInformation("Removing user {Username} from group {GroupName}", username, groupName);
        await ExecuteWithRetryAsync(context =>
        {
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            using var groupPrincipal = GroupPrincipal.FindByIdentity(context, groupName);
            
            if (userPrincipal == null || groupPrincipal == null)
            {
                _logger.LogWarning("User or group not found: User={Username}, Group={GroupName}", 
                    username, groupName);
                return false;
            }

            if (groupPrincipal.Members.Contains(userPrincipal))
            {
                groupPrincipal.Members.Remove(userPrincipal);
                groupPrincipal.Save();
                _logger.LogInformation("Successfully removed user {Username} from group {GroupName}", 
                    username, groupName);
            }
            else
            {
                _logger.LogInformation("User {Username} is not a member of group {GroupName}", 
                    username, groupName);
            }

            return true;
        }, $"RemoveUserFromGroup_{username}_{groupName}");
    }
}