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
        private readonly RoleManager<IdentityRole> _roleManager; 

        
        public FacultyController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var me = await _userManager.GetUserAsync(User);
            return View(me);
        }

        public async Task<IActionResult> Students()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            
            var activeStudents = students.Where(s => !s.IsDeletionPending).ToList();
            return View(activeStudents);
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
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Department = user.Department,
                Role = "Student"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(EditUserViewModel model)
        {
            ModelState.Remove(nameof(model.Email));
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Student")) return Forbid();

            user.FullName = model.FullName;
            user.Department = model.Department;

           
            user.UpdatedBy = User.Identity?.Name;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
               
                return View(model);
            }

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction("Students");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDeletion(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            
            user.IsDeletionPending = true;
            user.DeletionRequestReason = string.IsNullOrEmpty(reason) ? "No reason provided by faculty." : reason;
            user.UpdatedBy = User.Identity?.Name; 

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Could not submit removal request.";
                return RedirectToAction("Students");
            }

            TempData["Success"] = $"Removal request for {user.FullName} submitted to Admin.";
            return RedirectToAction("Students");
        }

        [HttpGet]
        public IActionResult AddStudent()
        {
            return View(new Users());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(Users student)
        {
            if (ModelState.IsValid)
            {
                student.UserName = student.Email;
                student.CreatedAt = DateTime.Now;
                student.EmailConfirmed = true;
                student.CreatedBy = User.Identity?.Name; 
                student.UpdatedBy = User.Identity?.Name;


                string cleanName = student.FullName.Replace(" ", "");
                cleanName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanName);
                string tempPassword = $"{cleanName}@123";

                var result = await _userManager.CreateAsync(student, tempPassword);

                if (result.Succeeded)
                {
                    
                    if (!await _roleManager.RoleExistsAsync("Student"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    }

                    
                    await _userManager.AddToRoleAsync(student, "Student");

                    TempData["Success"] = $"Student added successfully! Temp Password: {tempPassword}";
                    return RedirectToAction("Students");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(student);
        }
    }
}