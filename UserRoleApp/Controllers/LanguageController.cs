using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace UserRoleApp.Controllers
{
    public class LanguageController : Controller
    {
        public IActionResult Index(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }

            string returnUrl = Request.Headers.Referer.ToString();

            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return Redirect(returnUrl);
        }
    }
}