using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ElloClinic.Api;
using Microsoft.IdentityModel.Tokens;

namespace ElloClinic.Api.Tests;

internal static class TestData
{
    public const string JwtKey = "ello-clinic-test-key-with-at-least-32-characters";

    public static string Token(Guid tenantId, UserRole role = UserRole.ClinicAdmin, Guid? professionalId = null)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()), new("tenant_id", tenantId.ToString()),
            new(ClaimTypes.Name, "Test User"), new(ClaimTypes.Email, "test@ello.local"),
            new(ClaimTypes.Role, role.ToString())
        };
        if (professionalId.HasValue) claims.Add(new("professional_id", professionalId.Value.ToString()));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials));
    }

    public static HttpClient AuthorizedClient(ApiFactory factory, Guid tenantId,
        UserRole role = UserRole.ClinicAdmin, Guid? professionalId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(tenantId, role, professionalId));
        return client;
    }
}
