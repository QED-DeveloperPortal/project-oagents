using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace Microsoft.AI.DevTeam;
public class GithubAuthService
{
    private readonly GithubOptions _githubSettings;
    private readonly ILogger<GithubAuthService> _logger;

    public GithubAuthService(IOptions<GithubOptions> ghOptions, ILogger<GithubAuthService> logger)
    {
        _githubSettings = ghOptions.Value;
        _logger = logger;
    }

    public string GenerateJwtToken(string appId, string appKey, int minutes)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(appKey);
        var securityKey = new RsaSecurityKey(rsa);

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var iat = new DateTimeOffset(now).ToUnixTimeSeconds();
        var exp = new DateTimeOffset(now.AddMinutes(minutes)).ToUnixTimeSeconds();

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Iat, iat.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, exp.ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: appId,
            claims: claims,
            expires: DateTime.Now.AddMinutes(10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public GitHubClient GetGitHubClient()
    {
        var x = "1";
        try
        {
            var jwtToken = GenerateJwtToken(_githubSettings.AppId.ToString(), _githubSettings.AppKey, 10);
            x = "2";
            var appClient = new GitHubClient(new ProductHeaderValue("SK-DEV-APP"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };
            x = "3";
            _logger.LogInformation($"LOGGING INSTALLATIONID");
            _logger.LogInformation($"InstallationId: {_githubSettings.InstallationId}");
            var response = appClient.GitHubApps.CreateInstallationToken(_githubSettings.InstallationId).Result;
            x = "4";
            return new GitHubClient(new ProductHeaderValue($"SK-DEV-APP-Installation{_githubSettings.InstallationId}"))
            {
                Credentials = new Credentials(response.Token)
            };
            x = "5";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting GitHub client {x}");
             throw;
        }
    }
}