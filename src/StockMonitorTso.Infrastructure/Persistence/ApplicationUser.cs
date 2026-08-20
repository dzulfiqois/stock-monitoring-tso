using Microsoft.AspNetCore.Identity;

namespace StockMonitorTso.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser
{
    public string? ActiveRoleName { get; set; }
}
