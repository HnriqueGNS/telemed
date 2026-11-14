using Microsoft.AspNetCore.Identity;

namespace telemed.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid? ClinicId { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public static class Roles
    {
        public const string Patient = "Patient";
        public const string Doctor = "Doctor";
        public const string Reception = "Reception";
        public const string Admin = "Admin";
        public static readonly string[] All = { Patient, Doctor, Reception, Admin };
    }
}
