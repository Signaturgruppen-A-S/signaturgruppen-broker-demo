using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    [Route("api/logout")]
    [ApiController]
    [AllowAnonymous]
    public class LogoutController : ControllerBase
    {
        [HttpPost]
        [Route("")]
        public IActionResult BackChannelLogout()
        {
            var logoutToken = HttpContext.Request.Form["logout_token"].ToString();
            Console.WriteLine($"Received logout token (Back-Channel): {logoutToken}");
            return new OkResult();
        }
    }

    
}

