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

                return action(context);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, 
                    "Failed on attempt {Attempt} for operation: {Operation}", 
                    attempt, operation);
                
                if (attempt < _settings.RetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.RetryDelaySeconds));
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
                if (result.Description?.StartsWith("Resource:") == true)
                {
                    resources.Add(new ADResource
                    {
                        Name = result.Name ?? string.Empty,
                        Description = result.Description.Substring(9).Trim(),
                        GroupName = result.SamAccountName ?? string.Empty,
                        Owner = result.Description.Contains("Owner:") 
                            ? result.Description.Split("Owner:")[1].Trim() 
                            : string.Empty,
                        Members = result.Members.Select(m => m.SamAccountName ?? string.Empty).ToList()
                    });
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
                users.Add(new ADUser
                {
                    SamAccountName = result.SamAccountName ?? string.Empty,
                    DisplayName = result.DisplayName ?? string.Empty,
                    EmployeeId = result.EmployeeId ?? string.Empty,
                    Title = result.Description ?? string.Empty,
                    Department = result.Description?.Split(";").FirstOrDefault() ?? string.Empty,
                    Email = result.EmailAddress ?? string.Empty,
                    PhoneNumber = result.VoiceTelephoneNumber ?? string.Empty,
                    Groups = result.GetGroups().Select(g => g.SamAccountName ?? string.Empty).ToList()
                });
            }

            return users;
        }, "GetAllUsers");
    }

    public async Task<ADUser?> GetUserAsync(string username)
    {
        _logger.LogInformation("Getting AD user: {Username}", username);
        return await ExecuteWithRetryAsync(context =>
        {
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            
            if (userPrincipal == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            return new ADUser
            {
                SamAccountName = userPrincipal.SamAccountName ?? string.Empty,
                DisplayName = userPrincipal.DisplayName ?? string.Empty,
                EmployeeId = userPrincipal.EmployeeId ?? string.Empty,
                Title = userPrincipal.Description ?? string.Empty,
                Department = userPrincipal.Description?.Split(";").FirstOrDefault() ?? string.Empty,
                Email = userPrincipal.EmailAddress ?? string.Empty,
                PhoneNumber = userPrincipal.VoiceTelephoneNumber ?? string.Empty,
                Groups = userPrincipal.GetGroups().Select(g => g.SamAccountName ?? string.Empty).ToList()
            };
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
            using var searcher = new PrincipalSearcher(groupPrincipal);
            
            foreach (var result in searcher.FindAll().Cast<GroupPrincipal>())
            {
                if (result.Description?.StartsWith("Resource:") == true)
                {
                    resources.Add(new ADResource
                    {
                        Name = result.Name,
                        Description = result.Description.Substring(9).Trim(),
                        GroupName = result.SamAccountName,
                        Owner = result.Description.Contains("Owner:") 
                            ? result.Description.Split("Owner:")[1].Trim() 
                            : string.Empty,
                        Members = result.Members.Select(m => m.SamAccountName).ToList()
                    });
                }
            }
        });

        return resources;
    }

    public async Task<List<ADUser>> GetAllUsersAsync()
    {
        var users = new List<ADUser>();
        
        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var userPrincipal = new UserPrincipal(context);
            using var searcher = new PrincipalSearcher(userPrincipal);
            
            foreach (var result in searcher.FindAll().Cast<UserPrincipal>())
            {
                users.Add(new ADUser
                {
                    SamAccountName = result.SamAccountName,
                    DisplayName = result.DisplayName,
                    EmployeeId = result.EmployeeId,
                    Title = result.Description ?? string.Empty,
                    Department = result.Description?.Split(";").FirstOrDefault() ?? string.Empty,
                    Email = result.EmailAddress ?? string.Empty,
                    PhoneNumber = result.VoiceTelephoneNumber ?? string.Empty,
                    Groups = result.GetGroups().Select(g => g.SamAccountName).ToList()
                });
            }
        });

        return users;
    }

    public async Task<ADUser?> GetUserAsync(string username)
    {
        ADUser? user = null;
        
        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            
            if (userPrincipal != null)
            {
                user = new ADUser
                {
                    SamAccountName = userPrincipal.SamAccountName,
                    DisplayName = userPrincipal.DisplayName,
                    EmployeeId = userPrincipal.EmployeeId,
                    Title = userPrincipal.Description ?? string.Empty,
                    Department = userPrincipal.Description?.Split(";").FirstOrDefault() ?? string.Empty,
                    Email = userPrincipal.EmailAddress ?? string.Empty,
                    PhoneNumber = userPrincipal.VoiceTelephoneNumber ?? string.Empty,
                    Groups = userPrincipal.GetGroups().Select(g => g.SamAccountName).ToList()
                };
            }
        });

        return user;
    }

    public async Task<List<string>> GetUserGroupsAsync(string username)
    {
        var groups = new List<string>();
        
        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            
            if (userPrincipal != null)
            {
                groups.AddRange(userPrincipal.GetGroups().Select(g => g.SamAccountName));
            }
        });

        return groups;
    }

    public async Task AddUserToGroupAsync(string username, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            using var groupPrincipal = GroupPrincipal.FindByIdentity(context, groupName);
            
            if (userPrincipal != null && groupPrincipal != null)
            {
                if (!groupPrincipal.Members.Contains(userPrincipal))
                {
                    groupPrincipal.Members.Add(userPrincipal);
                    groupPrincipal.Save();
                }
            }
        });
    }

    public async Task RemoveUserFromGroupAsync(string username, string groupName)
    {
        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var userPrincipal = UserPrincipal.FindByIdentity(context, username);
            using var groupPrincipal = GroupPrincipal.FindByIdentity(context, groupName);
            
            if (userPrincipal != null && groupPrincipal != null)
            {
                if (groupPrincipal.Members.Contains(userPrincipal))
                {
                    groupPrincipal.Members.Remove(userPrincipal);
                    groupPrincipal.Save();
                }
            }
        });
    }
}