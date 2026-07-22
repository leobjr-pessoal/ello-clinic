using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ElloClinic.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ElloClinic.Api.Tests;

public sealed class SecurityAndTenantTests
{
    [Fact]
    public void Password_hash_is_salted_and_verifiable()
    {
        var first = Passwords.Hash("StrongPassword!");
        var second = Passwords.Hash("StrongPassword!");

        Assert.NotEqual(first, second);
        Assert.True(Passwords.Verify("StrongPassword!", first));
        Assert.False(Passwords.Verify("wrong", first));
        Assert.False(Passwords.Verify("anything", "invalid"));
    }

    [Fact]
    public void Token_contains_tenant_role_and_professional_claims()
    {
        var professionalId = Guid.NewGuid();
        var user = new AppUser
        {
            TenantId = Guid.NewGuid(), ProfessionalId = professionalId, Name = "Dra. Ana",
            Email = "ana@ello.local", Role = UserRole.Professional
        };
        var service = new TokenService(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = TestData.JwtKey }).Build());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.Create(user));

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(x => x.Type == "sub").Value);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.Single(x => x.Type == "tenant_id").Value);
        Assert.Equal(professionalId.ToString(), jwt.Claims.Single(x => x.Type == "professional_id").Value);
        Assert.Contains(jwt.Claims, x => x.Value == UserRole.Professional.ToString());
    }

    [Fact]
    public void Token_requires_a_configured_key()
    {
        var service = new TokenService(new ConfigurationBuilder().Build());
        Assert.Throws<InvalidOperationException>(() => service.Create(new AppUser()));
    }

    [Fact]
    public void Tenant_context_reads_valid_claims_and_ignores_invalid_ones()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", tenantId.ToString()), new Claim("sub", userId.ToString())]))
        };
        var context = new TenantContext(new HttpContextAccessor { HttpContext = http });
        Assert.Equal(tenantId, context.TenantId); Assert.Equal(userId, context.UserId);

        http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "bad"), new Claim("sub", "bad")]));
        Assert.Equal(Guid.Empty, context.TenantId); Assert.Null(context.UserId);
    }

    [Fact]
    public async Task Db_context_assigns_tenant_filters_rows_and_updates_timestamp()
    {
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid();
        var database = "tenant-" + Guid.NewGuid();
        await using var seed = Db(new FixedTenant(Guid.Empty), database);
        await seed.Database.EnsureCreatedAsync();
        seed.Patients.AddRange(
            new Patient { TenantId = tenantA, Name = "A" },
            new Patient { TenantId = tenantB, Name = "B" });
        await seed.SaveChangesAsync();

        await using var db = Db(new FixedTenant(tenantA), database);
        var visible = await db.Patients.SingleAsync();
        Assert.Equal("A", visible.Name);
        var added = new Patient { Name = "Novo" }; db.Add(added); await db.SaveChangesAsync();
        Assert.Equal(tenantA, added.TenantId);
        visible.Name = "Atualizado"; await db.SaveChangesAsync();
        Assert.NotNull(visible.UpdatedAt);
    }

    [Fact]
    public async Task Db_context_rejects_tenant_entity_without_tenant()
    {
        await using var db = Db(new FixedTenant(Guid.Empty));
        db.Patients.Add(new Patient { Name = "Sem clínica" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private static ClinicDbContext Db(ITenantContext tenant, string? database = null) => new(
        new DbContextOptionsBuilder<ClinicDbContext>().UseInMemoryDatabase(database ?? "tenant-" + Guid.NewGuid()).Options, tenant);
    private sealed record FixedTenant(Guid TenantId) : ITenantContext { public Guid? UserId => null; }
}
