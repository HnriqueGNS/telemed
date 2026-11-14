using System.ComponentModel.DataAnnotations;

namespace telemed.Api.Contracts;

public record RegisterRequest(
    [Required, StringLength(150, MinimumLength = 3)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128)] string Password,
    Guid ClinicId
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);