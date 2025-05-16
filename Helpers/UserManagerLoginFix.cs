using Microsoft.AspNetCore.Identity;

namespace geotagger_backend.Helpers
{
    public static class UserManagerLoginFix
    {
        public static async Task EnsureProviderDisplayNameAsync<TUser>(
                this UserManager<TUser> userManager,
                TUser user,
                ExternalLoginInfo info)
            where TUser : class
        {
            var logins = await userManager.GetLoginsAsync(user);
            var login = logins.FirstOrDefault(l =>
                           l.LoginProvider == info.LoginProvider &&
                           l.ProviderKey == info.ProviderKey);

            if (login != null && string.IsNullOrEmpty(login.ProviderDisplayName))
            {
                // 1) remove the bad row …
                await userManager.RemoveLoginAsync(
                        user, login.LoginProvider, login.ProviderKey);

                // 2) … and add a clean one
                var fixedLogin = new UserLoginInfo(
                        info.LoginProvider,            // e.g. "Google"
                        info.ProviderKey,
                        info.LoginProvider);           // never null/empty

                await userManager.AddLoginAsync(user, fixedLogin);
            }
        }
    }

}
