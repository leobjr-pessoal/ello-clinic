using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ElloClinic.Api;

public interface ITenantContext { Guid TenantId { get; } Guid? UserId { get; } }
public sealed class TenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public Guid TenantId => Guid.TryParse(accessor.HttpContext?.User.FindFirst("tenant_id")?.Value, out var id) ? id : Guid.Empty;
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirst("sub")?.Value, out var id) ? id : null;
}

public sealed class ClinicDbContext(DbContextOptions<ClinicDbContext> options, ITenantContext tenant) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>(); public DbSet<AppUser> Users => Set<AppUser>(); public DbSet<Unit> Units => Set<Unit>(); public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Specialty> Specialties => Set<Specialty>(); public DbSet<ClinicService> Services => Set<ClinicService>(); public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<Patient> Patients => Set<Patient>(); public DbSet<Appointment> Appointments => Set<Appointment>(); public DbSet<ClinicalEvolution> Evolutions => Set<ClinicalEvolution>();
    public DbSet<AppointmentConfirmation> AppointmentConfirmations => Set<AppointmentConfirmation>();
    public DbSet<TherapeuticPlan> TherapeuticPlans => Set<TherapeuticPlan>();
    public DbSet<Receivable> Receivables => Set<Receivable>(); public DbSet<Payable> Payables => Set<Payable>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique(); b.Entity<AppUser>().HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        b.Entity<Appointment>().HasIndex(x => new { x.TenantId, x.StartsAt, x.EndsAt });
        b.Entity<AppointmentConfirmation>().HasIndex(x => x.TokenHash).IsUnique(); b.Entity<AppointmentConfirmation>().HasIndex(x => new { x.TenantId, x.AppointmentId });
        foreach (var type in b.Model.GetEntityTypes().Where(x => typeof(TenantEntity).IsAssignableFrom(x.ClrType)))
        {
            var p = Expression.Parameter(type.ClrType, "e"); var tenantProp = Expression.Property(p, nameof(TenantEntity.TenantId));
            var current = Expression.Property(Expression.Constant(tenant), nameof(ITenantContext.TenantId));
            type.SetQueryFilter(Expression.Lambda(Expression.Equal(tenantProp, current), p));
            type.AddIndex(type.FindProperty(nameof(TenantEntity.TenantId))!);
        }
        base.OnModelCreating(b);
    }
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>().Where(x => x.State == EntityState.Added))
        {
            if (tenant.TenantId == Guid.Empty && entry.Entity.TenantId == Guid.Empty) throw new InvalidOperationException("Tenant ausente.");
            if (tenant.TenantId != Guid.Empty) entry.Entity.TenantId = tenant.TenantId;
        }
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Modified)) entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        return await base.SaveChangesAsync(ct);
    }
}
