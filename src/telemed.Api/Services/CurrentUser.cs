using System.Security.Claims;
using telemed.Application.Abstractions;

namespace telemed.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public Guid? ClinicId => Guid.TryParse(Principal?.FindFirstValue("clinic_id"), out var id) ? id : null;
    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}