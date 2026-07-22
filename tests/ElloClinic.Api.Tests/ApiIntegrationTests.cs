using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElloClinic.Api;

namespace ElloClinic.Api.Tests;

public sealed class ApiIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Public_health_trial_and_login_flows_work()
    {
        await factory.ResetAsync();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/public/trials",
            new { clinicName = "A", name = "B", email = "bad", phone = "", password = "123", plan = "enterprise" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var invalidPlan = await client.PostAsJsonAsync("/api/public/trials",
            new { clinicName = "Clínica Boa", name = "Admin Geral", email = "admin@boa.test", phone = "", password = "Password1", plan = "enterprise" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPlan.StatusCode);

        var created = await client.PostAsJsonAsync("/api/public/trials",
            new { clinicName = "Clínica Árvore", name = "Admin Geral", email = "ADMIN@BOA.TEST", phone = "11999999999", password = "Password1", plan = "starter" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trial = await Json(created); Assert.Equal("clinica-arvore", trial.GetProperty("tenant").GetProperty("slug").GetString());
        Assert.False(string.IsNullOrWhiteSpace(trial.GetProperty("token").GetString()));

        var duplicate = await client.PostAsJsonAsync("/api/public/trials",
            new { clinicName = "Outra Clínica", name = "Outro Admin", email = "admin@boa.test", phone = "", password = "Password1", plan = "starter" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var secondSlug = await client.PostAsJsonAsync("/api/public/trials",
            new { clinicName = "Clínica Árvore", name = "Segundo Admin", email = "segundo@boa.test", phone = "", password = "Password1", plan = "professional" });
        Assert.Equal(HttpStatusCode.Created, secondSlug.StatusCode);
        Assert.Equal("clinica-arvore-2", (await Json(secondSlug)).GetProperty("tenant").GetProperty("slug").GetString());

        var badTenant = await client.PostAsJsonAsync("/api/auth/login", new { tenant = "inexistente", email = "admin@boa.test", password = "Password1" });
        Assert.Equal(HttpStatusCode.Unauthorized, badTenant.StatusCode);
        var badPassword = await client.PostAsJsonAsync("/api/auth/login", new { tenant = "clinica-arvore", email = "admin@boa.test", password = "errada" });
        Assert.Equal(HttpStatusCode.Unauthorized, badPassword.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { tenant = "clinica-arvore", email = "admin@boa.test", password = "Password1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_complete_clinic_workflow()
    {
        await factory.ResetAsync(); var tenantId = Guid.NewGuid(); await factory.SeedTenantAsync(tenantId);
        using var client = TestData.AuthorizedClient(factory, tenantId);

        var specialty = await PostId(client, "/api/specialties", new { name = "Psicologia", color = "#123456", active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/specialties/{specialty}", new { name = "Psicologia clínica", color = "#654321", active = true })).StatusCode);
        var service = await PostId(client, "/api/services", new { specialtyId = specialty, name = "Consulta", durationMinutes = 50, price = 200, requiresRoom = true, active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/services/{service}", new { specialtyId = specialty, name = "Sessão", durationMinutes = 60, price = 220, requiresRoom = true, active = true })).StatusCode);
        var unit = await PostId(client, "/api/units", new { name = "Matriz", address = "Rua 1", active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/units/{unit}", new { name = "Matriz Centro", address = "Rua 2", active = true })).StatusCode);
        var room = await PostId(client, "/api/rooms", new { unitId = unit, name = "Sala 1", active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/rooms/{room}", new { unitId = unit, name = "Sala Azul", active = true })).StatusCode);
        var patient = await PostId(client, "/api/patients", new { name = "Paciente Um", phone = "11999999999", email = "paciente@test", active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/patients/{patient}", new { name = "Paciente Editado", phone = "11888888888", email = "editado@test", notes = "Observação", active = true })).StatusCode);
        var professional = await PostId(client, "/api/professionals", new { specialtyId = specialty, name = "Dra. Ana", email = "ana@test", defaultSplitPercent = 40, color = "#112233", active = true });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/professionals/{professional}", new { specialtyId = specialty, name = "Dra. Ana Silva", email = "ana@test", phone = "1100000000", council = "CRP", registrationNumber = "123", defaultSplitPercent = 45, color = "#223344", active = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/professionals/{professional}/access", new { email = "ana@test", password = "123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/professionals/{professional}/access", new { email = "ANA@TEST", password = "Password1" })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/catalog")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/professionals")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/patients?search=editado")).StatusCode);

        var start = DateTimeOffset.UtcNow.AddDays(2); var end = start.AddHours(1);
        var invalidPeriod = await client.PostAsJsonAsync("/api/appointments", Appointment(patient, professional, service, unit, room, start, start));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPeriod.StatusCode);
        var appointment = await PostId(client, "/api/appointments", Appointment(patient, professional, service, unit, room, start, end));
        var conflict = await client.PostAsJsonAsync("/api/appointments", Appointment(patient, professional, service, unit, room, start.AddMinutes(10), end));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/appointments?from={Uri.EscapeDataString(start.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(end.AddDays(1).ToString("O"))}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/appointments/{appointment}", Appointment(patient, professional, service, unit, room, start.AddHours(1), end.AddHours(1), "Confirmed"))).StatusCode);

        var linkResponse = await client.PostAsJsonAsync($"/api/appointments/{appointment}/confirmation-link", new { });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var path = (await Json(linkResponse)).GetProperty("path").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/api/public/appointments/confirmation/" + path.Split('/').Last())).StatusCode);
        var publicClient = factory.CreateClient();
        var confirmation = await publicClient.PostAsJsonAsync("/api/public/appointments/confirmation/" + path.Split('/').Last() + "/respond", new { response = "Confirmed", respondentName = "Paciente", message = "Confirmado" });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await publicClient.PostAsJsonAsync("/api/public/appointments/confirmation/" + path.Split('/').Last() + "/respond", new { response = "Confirmed" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/appointments/{appointment}/status", new { status = "Completed" })).StatusCode);
        var evolution = await PostId(client, "/api/evolutions", new { patientId = patient, appointmentId = appointment, professionalId = professional, objective = "Objetivo", activities = "Atividades", response = "Boa", conduct = "Continuar", finalized = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/evolutions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/evolutions/{evolution}", new { objective = "Novo", activities = "Novas", response = "Ótima", conduct = "Alta", finalized = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/evolutions/{evolution}", new { objective = "Não", activities = "Não", response = "Não", conduct = "Não", finalized = false })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/medical-records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/medical-records/{patient}")).StatusCode);
        var plan = await PostId(client, "/api/therapeutic-plans", new { patientId = patient, professionalId = professional, specialtyId = specialty, startDate = "2026-01-01", mainComplaint = "Queixa", generalGoals = "Objetivo", specificGoals = "Meta", recommendedFrequency = "Semanal", status = "Active" });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/therapeutic-plans/{plan}", new { mainComplaint = "Nova", generalGoals = "Novo", specificGoals = "Nova", recommendedFrequency = "Quinzenal", status = "Completed", endDate = "2026-02-01", closingReason = "Alta" })).StatusCode);

        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-2).ToString("O")); var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(10).ToString("O"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/reports?from={from}&to={to}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/reports?from={to}&to={from}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/finance")).StatusCode);
        var payable = await PostId(client, "/api/payables", new { description = "Aluguel", category = "Fixo", amount = 1000, dueDate = "2026-08-01", status = "Pending" });
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/payables/{payable}", new { description = "Aluguel pago", category = "Fixo", amount = 1000, dueDate = "2026-08-01", status = "Paid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/payables/{payable}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/appointments/{appointment}")).StatusCode);
    }

    [Fact]
    public async Task Professional_is_restricted_to_own_clinical_data()
    {
        await factory.ResetAsync(); var tenant = Guid.NewGuid(); await factory.SeedTenantAsync(tenant); var ownProfessional = Guid.NewGuid(); var otherProfessional = Guid.NewGuid();
        using var admin = TestData.AuthorizedClient(factory, tenant);
        var specialty = await PostId(admin, "/api/specialties", new { name = "Fono", active = true });
        var unit = await PostId(admin, "/api/units", new { name = "Matriz", active = true });
        var service = await PostId(admin, "/api/services", new { specialtyId = specialty, name = "Sessão", durationMinutes = 40, price = 100, requiresRoom = false, active = true });
        var patient = await PostId(admin, "/api/patients", new { name = "Paciente", active = true });
        await PostWithId(admin, "/api/professionals", new { id = ownProfessional, specialtyId = specialty, name = "Próprio", email = "own@test", defaultSplitPercent = 50, active = true });
        await PostWithId(admin, "/api/professionals", new { id = otherProfessional, specialtyId = specialty, name = "Outro", email = "other@test", defaultSplitPercent = 30, active = true });
        var appointment = await PostId(admin, "/api/appointments", Appointment(patient, ownProfessional, service, unit, null, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(1), "Completed"));

        using var professional = TestData.AuthorizedClient(factory, tenant, UserRole.Professional, ownProfessional);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.PostAsJsonAsync("/api/patients", new { name = "Proibido" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.GetAsync("/api/finance")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.GetAsync("/api/reports?from=2026-01-01&to=2026-02-01")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await professional.GetAsync("/api/patients")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await professional.GetAsync("/api/medical-records")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await professional.GetAsync($"/api/medical-records/{patient}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.PostAsJsonAsync("/api/evolutions", new { patientId = patient, appointmentId = appointment, professionalId = otherProfessional, objective = "x", activities = "x", response = "x", conduct = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.PostAsJsonAsync("/api/therapeutic-plans", new { patientId = patient, professionalId = otherProfessional, specialtyId = specialty, startDate = "2026-01-01", mainComplaint = "x", generalGoals = "x", specificGoals = "x", recommendedFrequency = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await professional.PostAsJsonAsync("/api/appointments", Appointment(patient, otherProfessional, service, unit, null, DateTimeOffset.UtcNow.AddDays(3), DateTimeOffset.UtcNow.AddDays(3).AddHours(1)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await professional.GetAsync("/api/professional/me/settlements")).StatusCode);
    }

    [Fact]
    public async Task Missing_resources_and_invalid_public_links_return_expected_statuses()
    {
        await factory.ResetAsync(); var tenant = Guid.NewGuid(); using var client = TestData.AuthorizedClient(factory, tenant);
        var missing = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/patients/{missing}", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/patients/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/professionals/{missing}/access", new { email = "x@test", password = "Password1" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/appointments/{missing}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/appointments/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync($"/api/appointments/{missing}/status", new { status = "Completed" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/appointments/{missing}/confirmation-link", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync("/api/public/appointments/confirmation/invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().PostAsJsonAsync("/api/public/appointments/confirmation/invalid/respond", new { response = "bad" })).StatusCode);
    }

    private static object Appointment(Guid patient, Guid professional, Guid service, Guid unit, Guid? room,
        DateTimeOffset starts, DateTimeOffset ends, string status = "PreScheduled") =>
        new { patientId = patient, professionalId = professional, serviceId = service, unitId = unit,
            roomId = room, startsAt = starts, endsAt = ends, status, amount = 200, notes = "Teste" };

    private static async Task<Guid> PostId(HttpClient client, string url, object value)
    {
        var response = await client.PostAsJsonAsync(url, value); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("id").GetGuid();
    }

    private static async Task PostWithId(HttpClient client, string url, object value)
    {
        var response = await client.PostAsJsonAsync(url, value); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();
}
