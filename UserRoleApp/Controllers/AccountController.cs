using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;
using UserRoleApp.ViewModels;

namespace UserRoleApp.Controllers
{
    
    public class AccountController : Controller
    {
        private readonly SignInManager<Users>      _signInManager;
        private readonly UserManager<Users>        _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(SignInManager<Users> signInManager,
            UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
            _roleManager   = roleManager;
        }

        [HttpGet] public IActionResult Login() => View();

        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _signInManager.SignOutAsync();
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                var user  = await _userManager.FindByEmailAsync(model.Email);
                var roles = await _userManager.GetRolesAsync(user!);

                if (roles.Contains("Admin"))   return RedirectToAction("Index", "Admin");
                if (roles.Contains("Faculty")) return RedirectToAction("Index", "Faculty");
                if (roles.Contains("Student")) return RedirectToAction("Index", "Student");
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

       
        [HttpGet] public IActionResult Register() => View(new RegisterViewModel());

      
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (model.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Admin accounts cannot be self-registered.");
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            var user = new Users
            {
                FullName           = model.FullName,
                UserName           = model.Email,
                Email              = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                NormalizedEmail    = model.Email.ToUpper(),
                Department         = model.Department,
                SecurityStamp      = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            await _userManager.AddToRoleAsync(user, model.Role);
            await _signInManager.SignInAsync(user, false);

            if (model.Role == "Faculty") return RedirectToAction("Index", "Faculty");
            return RedirectToAction("Index", "Student");
        }

     
        [HttpGet] public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

       
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email)
                    ?? await _userManager.FindByNameAsync(model.Email);

            if (user == null) { ModelState.AddModelError("", "User not found."); return View(model); }

            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (result.Succeeded) return RedirectToAction("Login");
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }


        public IActionResult AccessDenied() => View();
    }
}
