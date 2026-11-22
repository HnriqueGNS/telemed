namespace telemed.Infrastructure.Identity;

public static class SecurityPolicies
{
    public const string ClinicalAccess = "ClinicalAccess";
    
    public static readonly string[] TwoFactorRoles = { Roles.Doctor, Roles.Admin};
}