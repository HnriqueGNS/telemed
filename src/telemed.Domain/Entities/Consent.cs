using telemed.Domain.Common;

namespace telemed.Domain.Entities;

public class Consent : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
}