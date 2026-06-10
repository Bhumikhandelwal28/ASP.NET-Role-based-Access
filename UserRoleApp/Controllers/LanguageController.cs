using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace UserRoleApp.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult Index(string culture, string returnUrl = "/")
        {
            var allowed = new[] { "en", "de", "fr" };

            // Fallback to English if culture is null or not allowed
            if (string.IsNullOrWhiteSpace(culture) || !Array.Exists(allowed, c => c.Equals(culture, StringComparison.OrdinalIgnoreCase)))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/" // Crucial: Ensures cookie is available site-wide
                }
            );

            // Using built-in Url.IsLocalUrl safe check
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        public IActionResult Reset(string returnUrl = "/")
        {
            Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName, new CookieOptions { Path = "/" });

            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return LocalRedirect(returnUrl);
        }
    }
}