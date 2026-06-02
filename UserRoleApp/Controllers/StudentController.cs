using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;

namespace UserRoleApp.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly UserManager<Users> _userManager;
        public StudentController(UserManager<Users> userManager) => _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var me = await _userManager.GetUserAsync(User);
            return View(me);
        }
    }
}
