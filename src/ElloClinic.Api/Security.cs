using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace ElloClinic.Api;
public sealed class TokenService(IConfiguration config)
{
    public string Create(AppUser user)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada");
        var claims = new List<Claim> { new("sub", user.Id.ToString()), new("tenant_id", user.TenantId.ToString()), new(ClaimTypes.Name, user.Name), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role.ToString()) };
        if (user.ProfessionalId is not null) claims.Add(new Claim("professional_id", user.ProfessionalId.Value.ToString()));
        var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
public static class Passwords
{
    public static string Hash(string password) { var salt = RandomNumberGenerator.GetBytes(16); var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32); return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}"; }
    public static bool Verify(string password, string encoded) { var p = encoded.Split('.'); if (p.Length != 2) return false; var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(p[0]), 100_000, HashAlgorithmName.SHA256, 32); return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(p[1])); }
}
