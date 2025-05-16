using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using geotagger_backend.Models;

namespace geotagger_backend.Helpers;

public static class IdentityExtensions
{
    /// <summary>
    /// Makes sure core Identity columns are never NULL.
    /// </summary>
    public static async Task EnsureCoreIdentityFieldsAsync(
        this ApplicationUser user,
        UserManager<ApplicationUser> manager,
        string email)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(user.UserName))
        { user.UserName = email; changed = true; }

        if (string.IsNullOrWhiteSpace(user.NormalizedUserName))
        { user.NormalizedUserName = email.ToUpperInvariant(); changed = true; }

        if (string.IsNullOrWhiteSpace(user.Email))
        { user.Email = email; changed = true; }

        if (string.IsNullOrWhiteSpace(user.NormalizedEmail))
        { user.NormalizedEmail = email.ToUpperInvariant(); changed = true; }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            // 128-bit random “fake” hash so the column isn’t NULL
            user.PasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            changed = true;
        }

        if (changed)
            await manager.UpdateAsync(user);
    }

    /// <summary>
    /// Updates first/last name and avatar if they changed.
    /// </summary>
    public static async Task UpdateProfileAsync(
        this ApplicationUser user,
        UserManager<ApplicationUser> manager,
        string first, string last, string avatar)
    {
        var changed = false;

        if (user.FirstName != first)
        { user.FirstName = first; changed = true; }

        if (user.LastName != last)
        { user.LastName = last; changed = true; }

        if (user.ProfilePictureUrl != avatar)
        { user.ProfilePictureUrl = avatar; changed = true; }

        if (changed)
            await manager.UpdateAsync(user);
    }
}
