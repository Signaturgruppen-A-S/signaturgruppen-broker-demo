using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace NetsBrokerIntegration.NetCore.Controllers
{
    public class RequestUriController : Controller
    {
        private readonly IDistributedCache distributedCache;

        public RequestUriController(IDistributedCache distributedCache)
        {
            this.distributedCache = distributedCache;
        }

        [AllowAnonymous]
        [Route("requestobject/{key}")]
        public async Task<IActionResult> GetRequestObject(string key)
        {
            var requestJwt = await distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(requestJwt))
            {
                return new NotFoundResult();
            }
            return Content(requestJwt);
        }
    }
}
