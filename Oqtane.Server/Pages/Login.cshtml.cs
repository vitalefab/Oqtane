using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oqtane.Enums;
using Oqtane.Extensions;
using Oqtane.Infrastructure;
using Oqtane.Managers;
using Oqtane.Security;
using Oqtane.Shared;

namespace Oqtane.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _identityUserManager;
        private readonly SignInManager<IdentityUser> _identitySignInManager;
        private readonly IUserManager _userManager;
        private readonly ILogManager _logger;

        public LoginModel(UserManager<IdentityUser> identityUserManager, SignInManager<IdentityUser> identitySignInManager, IUserManager userManager, ILogManager logger)
        {
            _identityUserManager = identityUserManager;
            _identitySignInManager = identitySignInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync(string username, string password, bool remember, string returnurl)
        {
            if (!User.Identity.IsAuthenticated && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                bool validuser = false;
                IdentityUser identityuser = await _identityUserManager.FindByNameAsync(username);
                if (identityuser != null)
                {
                    var result = await _identitySignInManager.CheckPasswordSignInAsync(identityuser, password, true);
                    if (result.Succeeded)
                    {
                        var alias = HttpContext.GetAlias();
                        var user = _userManager.GetUser(identityuser.UserName, alias.SiteId);
                        if (user != null && !user.IsDeleted && UserSecurity.ContainsRole(user.Roles, RoleNames.Registered))
                        {
                            validuser = true;
                        }
                    }
                }

                if (validuser)
                {
                    // note that .NET Identity uses a hardcoded ApplicationScheme of "Identity.Application" in SignInAsync
                    await _identitySignInManager.SignInAsync(identityuser, remember);
                    _logger.Log(LogLevel.Information, this, LogFunction.Security, "Login Successful For User {Username}", username);
                }
                else
                {
                    _logger.Log(LogLevel.Error, this, LogFunction.Security, "Login Failed For User {Username}", username);
                }
            }
            else
            {
                _logger.Log(LogLevel.Error, this, LogFunction.Security, "Unauthorized Attempt To Login User {Username}", username);
            }

            if (returnurl == null)
            {
                returnurl = "";
            }
            else
            {
                returnurl = WebUtility.UrlDecode(returnurl);
            }
            if (!returnurl.StartsWith("/"))
            {
                returnurl = "/" + returnurl;
            }

            return LocalRedirect(Url.Content("~" + returnurl));
        }
    }
}
