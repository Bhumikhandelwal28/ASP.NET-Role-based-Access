using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;
using UserRoleApp.ViewModels;

namespace UserRoleApp.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class FacultyController : Controller
    {
        private readonly UserManager<Users> _userManager;

        public FacultyController(UserManager<Users> userManager)
            => _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var me = await _userManager.GetUserAsync(User);
            return View(me);
        }

        public async Task<IActionResult> Students()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> EditStudent(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Student")) return Forbid();

            return View(new EditUserViewModel
            {
                Id         = user.Id,
                FullName   = user.FullName,
                Email      = user.Email!,
                Department = user.Department,
                Role       = "Student"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName   = model.FullName;
            user.Department = model.Department;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction("Students");
        }
    }
}
