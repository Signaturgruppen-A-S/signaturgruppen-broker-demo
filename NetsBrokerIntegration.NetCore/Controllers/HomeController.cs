using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetsBrokerIntegration.NetCore.Models;
using NetsBrokerIntegration.NetCore.Extensions;
using System.Linq;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult LoggedInSuccess() 
        {
            return View();
        }

        public async Task<IActionResult> TwoFactor()
        {
            if (User.Identity.IsAuthenticated && User.CanStepUp())
            {
                await HttpContext.SignOutAsync();
                var idp = User.Claims.First(c => c.Type == "idp").Value;

                if (idp == "mitid")
                {
                    return RedirectToAction(nameof(LoggedInSuccess), new
                    {
                        mitid_loa_value = User.GetStepUpTwoFactorClaim(),
                        idp_values = idp,
                        enable_step_up = true
                    });
                }
            }

            return RedirectToAction(nameof(LoggedInSuccess));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(AuthenticationErrorResponse authenticationErrorResponse)
        {
            return View(authenticationErrorResponse);
        }

        public async Task<IActionResult> Logout()
        {
            if (User?.Identity.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
                return SignOut(new AuthenticationProperties { RedirectUri = "/Home/Index" }, "oidc");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
