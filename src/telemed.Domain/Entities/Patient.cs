using telemed.Domain.Common;

namespace telemed.Domain.Entities;

public class Patient : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Cpf { get; set; } = default;
    public DateOnly BirthDate { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}