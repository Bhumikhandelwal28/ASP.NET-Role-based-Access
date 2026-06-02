using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;
using UserRoleApp.Services;
using UserRoleApp.ViewModels;

namespace UserRoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

  
   
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Users>        _userManager;
        private readonly SignInManager<Users>      _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService                _jwtService;

        public AuthController(UserManager<Users> userManager, SignInManager<Users> signInManager,
            RoleManager<IdentityRole> roleManager, JwtService jwtService)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _roleManager   = roleManager;
            _jwtService    = jwtService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            var ok = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!ok.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var token = await _jwtService.GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token,
                expiry  = DateTime.UtcNow.AddMinutes(60),
                userId  = user.Id,
                email   = user.Email,
                name    = user.FullName,
                roles
            });
        }

        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterApiViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (model.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Admin accounts cannot be self-registered." });

            if (!new[] { "Faculty", "Student" }.Any(r => r.Equals(model.Role, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { message = "Role must be 'Faculty' or 'Student'." });

            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "Email already registered." });

            var user = new Users
            {
                FullName           = model.FullName,
                UserName           = model.Email,
                Email              = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                NormalizedEmail    = model.Email.ToUpper(),
                Department         = model.Department,
                EmailConfirmed     = true,
                SecurityStamp      = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            if (!await _roleManager.RoleExistsAsync(model.Role))
                await _roleManager.CreateAsync(new IdentityRole(model.Role));

            await _userManager.AddToRoleAsync(user, model.Role);

            return Ok(new { message = $"Registered successfully as {model.Role}.", email = user.Email });
        }
    }
}
