using Microsoft.AspNetCore.Mvc;

namespace UserRoleApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
