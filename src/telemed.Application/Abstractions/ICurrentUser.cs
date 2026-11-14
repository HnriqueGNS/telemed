using System;
using System.Collections.Generic;
using System.Text;

namespace telemed.Application.Abstractions
{
    public interface ICurrentUser
    {
        Guid? UserId { get; }
        Guid? ClinicId { get; }
        string? IpAddress { get;  }
    }
}
