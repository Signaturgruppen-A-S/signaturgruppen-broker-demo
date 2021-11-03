using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetsBrokerIntegration.NetCore.Models;

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
