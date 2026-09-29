using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EMS.Infrastructure.Security;

public class JwtTokenGenerator : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string AccessToken, int ExpiresIn) GenerateAccessToken(User user, Employee? employee, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "super_secret_jwt_key_ems_enterprise_2026_dev_prod_token";
        var issuer = _configuration["Jwt:Issuer"] ?? "EMS.API";
        var audience = _configuration["Jwt:Audience"] ?? "EMS.Client";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var min) ? min : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("userId", user.Id.ToString())
        };

        if (employee != null)
        {
            claims.Add(new Claim("employeeId", employee.Id.ToString()));
            claims.Add(new Claim("employeeCode", employee.EmployeeCode));
            claims.Add(new Claim("firstName", employee.FirstName));
            claims.Add(new Claim("lastName", employee.LastName));
            if (employee.DepartmentId > 0)
                claims.Add(new Claim("departmentId", employee.DepartmentId.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permissions", perm));
        }

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiryMinutes * 60);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
