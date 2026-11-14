using telemed.Domain.Common;

namespace telemed.Domain.Entities;

public class Doctor : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Crm { get; set; } = default!;
    public string CrmState { get; set; } = default!;
    public string Specialty { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}