using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetsBrokerIntegration.NetCore.Models;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }
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
            await ApiLogoutFromNetseidBroker();

            if (User?.Identity.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task ApiLogoutFromNetseidBroker()
        {
            var httpClient = httpClientFactory.CreateClient();
            var idToken = await HttpContext.GetTokenAsync("id_token");
            if(idToken != null)
            {
                await httpClient.PostAsJsonAsync("https://pp.netseidbroker.dk/op/api/v1/session/logout", new { id_token = idToken });
            }
        }
    }
}
