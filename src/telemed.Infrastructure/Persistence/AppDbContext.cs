using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using telemed.Application.Abstractions;
using telemed.Domain.Common;
using telemed.Domain.Entities;
using telemed.Infrastructure.Identity;
using telemed.Infrastructure.Security;

namespace telemed.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFieldEncryptor _encryptor;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser, IFieldEncryptor encryptor) : base(options)
    {
        _currentUser = currentUser;
        _encryptor = encryptor;
    }

    private Guid? CurrentClinicId => _currentUser.ClinicId;

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Consent> Consents => Set<Consent>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        var encrypted = new EncryptedStringConverter(_encryptor);

        b.Entity<Patient>().HasIndex(p => p.UserId).IsUnique();
        b.Entity<Patient>().HasIndex(p => new { p.ClinicId, p.CpfHash }).IsUnique();
        b.Entity<Patient>().Property(p => p.Cpf).HasConversion(encrypted);
        b.Entity<Patient>().Property(p => p.Phone).HasConversion(encrypted);

        b.Entity<Doctor>().HasIndex(d => new { d.Crm, d.CrmState }).IsUnique();
        b.Entity<Consent>().HasIndex(c => new { c.UserId, c.Type });

        b.Entity<IdentityUserToken<Guid>>().Property(t => t.Value).HasConversion(encrypted);

        //Isolamento entre as clínicas
        b.Entity<Patient>().HasQueryFilter(e => CurrentClinicId == null || e.ClinicId == CurrentClinicId);
        b.Entity<Doctor>().HasQueryFilter(e => CurrentClinicId == null || e.ClinicId == CurrentClinicId);
        b.Entity<Consent>().HasQueryFilter(e => CurrentClinicId == null || e.ClinicId == CurrentClinicId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach(var entry in ChangeTracker.Entries<ITenantEntity>()
        .Where(e => e.State == EntityState.Added && e.Entity.ClinicId == Guid.Empty))
        {
            if(_currentUser.ClinicId is { } clinicId)
            {
                entry.Entity.ClinicId = clinicId;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}