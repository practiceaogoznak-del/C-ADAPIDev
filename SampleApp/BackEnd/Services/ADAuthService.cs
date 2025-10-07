using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BackEnd.Services;

public interface IAuthService
{
    Task<(bool success, string token)> AuthenticateAsync(string username, string password);
    Task<List<string>> GetUserRolesAsync(string username);
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class ADAuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly string _domain;
    private readonly string _container;

    public ADAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        _domain = configuration["ActiveDirectory:Domain"] ?? throw new ArgumentNullException(nameof(configuration));
        _container = configuration["ActiveDirectory:Container"] ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<(bool success, string token)> AuthenticateAsync(string username, string password)
    {
        bool isValid = await ValidateCredentialsAsync(username, password);
        
        if (!isValid)
        {
            return (false, string.Empty);
        }

        var roles = await GetUserRolesAsync(username);
        var token = GenerateJwtToken(username, roles);

        return (true, token);
    }

    public async Task<List<string>> GetUserRolesAsync(string username)
    {
        var roles = new List<string>();

        await Task.Run(() =>
        {
            using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
            using var user = UserPrincipal.FindByIdentity(context, username);

            if (user != null)
            {
                var groups = user.GetGroups();
                foreach (var group in groups)
                {
                    if (group.Name.StartsWith("Role_"))
                    {
                        roles.Add(group.Name.Substring(5));
                    }
                }
            }
        });

        return roles;
    }

    private async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain, _domain, _container);
                return context.ValidateCredentials(username, password);
            }
            catch
            {
                return false;
            }
        });
    }

    private string GenerateJwtToken(string username, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}