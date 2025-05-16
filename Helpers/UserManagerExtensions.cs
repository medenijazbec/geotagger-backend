using geotagger_backend.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public static class UserManagerExtensions
{
    public static async Task<bool> IsExternalAsync(this UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var logins = await userManager.GetLoginsAsync(user);
        return logins.Any(); // returns true if the user has at least one external login
    }
}
