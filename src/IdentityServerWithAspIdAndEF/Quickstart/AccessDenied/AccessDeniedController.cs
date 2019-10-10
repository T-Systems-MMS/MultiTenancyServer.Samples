using IdentityServer4.Quickstart.UI;

namespace MultiTenancyServer.Samples.IdentityServerWithAspIdAndEF.Quickstart.AccessDenied
{
    using Microsoft.AspNetCore.Mvc;

    namespace IdentityServer4.Quickstart.UI
    {
        [SecurityHeaders]
        public class AccessDeniedController : Controller
        {
            public AccessDeniedController()
            {
            }

            public IActionResult Index()
            {
                return View();
            }
        }
    }
}
