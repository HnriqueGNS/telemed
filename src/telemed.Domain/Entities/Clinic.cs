namespace telemed.Domain.Entities;

public class Clinic
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Cnpj { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}