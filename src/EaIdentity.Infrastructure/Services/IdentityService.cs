using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EaIdentity.Application.Services;
using EaIdentity.Domain;
using EaIdentity.Infrastructure.Data;
using EaIdentity.Infrastructure.Identity;
using EaIdentity.Infrastructure.Options;
using LanguageExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EaIdentity.Infrastructure.Services;

internal sealed class IdentityService : IIdentityService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtSettings _jwtSettings;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly ApplicationDbContext _context;

    public IdentityService(UserManager<IdentityUser> userManager,
        JwtSettings jwtSettings,
        TokenValidationParameters tokenValidationParameters,
        ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
        _tokenValidationParameters = tokenValidationParameters;
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<AuthenticationResult> RegisterAsync(string email, string password, CancellationToken ct)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return new AuthenticationResult(null, null, false, new[] { "User with this email address already exists" });
        }

        var newUserId = Guid.NewGuid();
        var newUser = new IdentityUser
        {
            Id = newUserId.ToString(),
            Email = email,
            UserName = email
        };

        var createdUser = await _userManager.CreateAsync(newUser, password);

        if (!createdUser.Succeeded)
        {
            return new AuthenticationResult(null, null, false, createdUser.Errors.Select(x => x.Description));
        }

        return await GenerateAuthenticationResultForUserAsync(newUser);
    }

    public async Task<AuthenticationResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new AuthenticationResult(null, null, false, new[] { "User does not exist" });
        }
        var userHasValidPassword = await _userManager.CheckPasswordAsync(user, password);
        if (!userHasValidPassword)
        {
            return new AuthenticationResult(null, null, false, new[] { "User/password combination is wrong" });
        }
        return await GenerateAuthenticationResultForUserAsync(user);
    }
    public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken, CancellationToken ct)
    {
        var validatedTokenOption = GetPrincipalFromToken(token);
        return await validatedTokenOption.MatchAsync<ClaimsPrincipal, AuthenticationResult>(
            Some: async principal => await ValidateTokenAndGenerateAuthenticationResultAsync(principal, refreshToken),
            None: () => new AuthenticationResult(null, null, false, new[] { "Invalid Token" })
        );
    }
    private Option<ClaimsPrincipal> GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var tokenValidationParameters = _tokenValidationParameters.Clone();
            tokenValidationParameters.ValidateLifetime = false;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
            if (IsJwtWithValidSecurityAlgorithm(validatedToken))
            {
                return Option<ClaimsPrincipal>.Some(principal);
            }
            return Option<ClaimsPrincipal>.None;
        }
        catch
        {
            return Option<ClaimsPrincipal>.None;
        }
    }
    private async Task<AuthenticationResult> ValidateTokenAndGenerateAuthenticationResultAsync(ClaimsPrincipal principal, string refreshToken)
    {
        var expiryDateUnix =
            long.Parse(principal.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
        var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(expiryDateUnix);
        if (expiryDateTimeUtc > DateTime.UtcNow)
        {
            return new AuthenticationResult(null, null, false, new[] { "This token hasn't expired yet" });
        }
        var jti = principal.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
        var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);
        if (storedRefreshToken == null)
        {
            return new AuthenticationResult(null, null, false, new[] { "This refresh token does not exist" });
        }
        if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
        {
            return new AuthenticationResult(null, null, false, new[] { "This refresh token has expired" });
        }
        if (storedRefreshToken.Invalidated)
        {
            return new AuthenticationResult(null, null, false, new[] { "This refresh token has been invalidated" });
        }
        if (storedRefreshToken.Used)
        {
            return new AuthenticationResult(null, null, false, new[] { "This refresh token has been used" });
        }
        if (storedRefreshToken.JwtId != jti)
        {
            return new AuthenticationResult(null, null, false, new[] { "This refresh token does not match this JWT" });
        }
        storedRefreshToken.Used = true;
        _context.RefreshTokens.Update(storedRefreshToken);
        await _context.SaveChangesAsync();
        var user = await _userManager.FindByIdAsync(principal.Claims.Single(x => x.Type == "id").Value);
        if (user is null)
        {
            return new AuthenticationResult(null, null, false, new[] { "User not found" });
        }
        return await GenerateAuthenticationResultForUserAsync(user);
    }
    private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
    {
        return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
               jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                   StringComparison.InvariantCultureIgnoreCase);
    }
    private async Task<AuthenticationResult> GenerateAuthenticationResultForUserAsync(IdentityUser user)
    {
        if (user.Email is null)
        {
            return new AuthenticationResult(null, null, false, new[] { "User does not have an email address" });
        }
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
        var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("id", user.Id)
            };
        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole));
            var role = await _roleManager.FindByNameAsync(userRole);
            if (role is null)
            {
                continue;
            }
            var roleClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var roleClaim in roleClaims)
            {
                if (claims.Contains(roleClaim))
                {
                    continue;
                }
                claims.Add(roleClaim);
            }
        }
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = token.Id,
            UserId = user.Id,
            CreationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMonths(6)
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return new AuthenticationResult(tokenHandler.WriteToken(token), refreshToken.Token, true, Array.Empty<string>());
    }
}