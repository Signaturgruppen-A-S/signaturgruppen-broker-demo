using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    [Authorize]
    public class SecureController : Controller
    {
        public IActionResult Claims()
        {
            return View();
        }
    }
}