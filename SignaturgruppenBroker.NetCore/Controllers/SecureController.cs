using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    [Authorize]
    public class SecureController : Controller
    {
        private readonly IMemoryCache memoryCache;

        public SecureController(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public IActionResult Claims()
        {
            if (memoryCache.TryGetValue("pades", out string padesB64))
            {
                ViewBag.Pades = padesB64;
            }

            return View();
        }
    }
}