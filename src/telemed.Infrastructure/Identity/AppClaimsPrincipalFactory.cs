using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace telemed.Infrastructure.Identity;

public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) {}

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if(user.ClinicId is {} clinicId)
        {
            identity.AddClaim(new Claim("clinic_id", clinicId.ToString()));
        }
        return identity;
    }
}