using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;
using UserRoleApp.ViewModels;

namespace UserRoleApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<Users>        _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var list  = new List<(Users user, IList<string> roles)>();
            foreach (var u in users)
                list.Add((u, await _userManager.GetRolesAsync(u)));
            return View(list);
        }

        public async Task<IActionResult> Students()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            return View(students);
        }

        public async Task<IActionResult> Faculty()
        {
            var faculty = await _userManager.GetUsersInRoleAsync("Faculty");
            return View(faculty);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            return View(new EditUserViewModel
            {
                Id         = user.Id,
                FullName   = user.FullName,
                Email      = user.Email!,
                Department = user.Department,
                Role       = roles.FirstOrDefault() ?? ""
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName   = model.FullName;
            user.Email      = model.Email;
            user.UserName   = model.Email;
            user.Department = model.Department;

            await _userManager.UpdateAsync(user);

           
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains("Admin") && !string.IsNullOrEmpty(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["Error"] = "Cannot delete Admin accounts.";
                return RedirectToAction("Index");
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeletion(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            
            await _userManager.DeleteAsync(user);

            TempData["Success"] = "User account permanently deleted.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDeletion(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            
            user.IsDeletionPending = false;
            user.DeletionRequestReason = null;
            user.UpdatedBy = User.Identity?.Name; 

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Removal request rejected. Student remains active.";
            return RedirectToAction("Index");
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
       
        [HttpGet]
        public IActionResult AddFaculty()
        {
            return View(new Users());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFaculty(UserRoleApp.Models.Users faculty)
        {
            if (ModelState.IsValid)
            {
                faculty.UserName = faculty.Email;
                faculty.CreatedAt = DateTime.Now;
                faculty.EmailConfirmed = true;

                
                faculty.CreatedBy = User.Identity?.Name;
                faculty.UpdatedBy = User.Identity?.Name;

                
                string cleanName = faculty.FullName.Replace(" ", "");
                cleanName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanName);
                string tempPassword = $"{cleanName}@123";

                
                var result = await _userManager.CreateAsync(faculty, tempPassword);

                if (result.Succeeded)
                {
                    
                    if (!await _roleManager.RoleExistsAsync("Faculty"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Faculty"));
                    }

                    
                    await _userManager.AddToRoleAsync(faculty, "Faculty");

                    TempData["Success"] = $"Faculty member added successfully! Default Password: {tempPassword}";
                    return RedirectToAction("Index"); 
                }

              
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(faculty);
        }
    }
}
