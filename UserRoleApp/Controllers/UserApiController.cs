using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserRoleApp.Models;
using UserRoleApp.ViewModels;

namespace UserRoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserApiController : ControllerBase
    {
        private readonly UserManager<Users>        _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserApiController(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { user.Id, user.FullName, user.Email, user.Department, user.CreatedAt, Roles = roles });
        }

       
        [HttpGet("students")]
        [Authorize(Policy = "AdminOrFaculty", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            return Ok(students.Select(s => new { s.Id, s.FullName, s.Email, s.Department, s.CreatedAt }));
        }

       
        [HttpGet("faculty")]
        [Authorize(Policy = "AdminOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetFaculty()
        {
            var faculty = await _userManager.GetUsersInRoleAsync("Faculty");
            return Ok(faculty.Select(f => new { f.Id, f.FullName, f.Email, f.Department, f.CreatedAt }));
        }

        [HttpGet("all")]
        [Authorize(Policy = "AdminOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAll()
        {
            var users = _userManager.Users.ToList();
            var result = new List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new { u.Id, u.FullName, u.Email, u.Department, u.CreatedAt, Roles = roles });
            }
            return Ok(result);
        }

       
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrFaculty", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> EditUser(string id, [FromBody] EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            user.FullName   = model.FullName;
            user.Email      = model.Email;
            user.UserName   = model.Email;
            user.Department = model.Department;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok(new { message = "User updated successfully." });
        }

       
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return BadRequest(new { message = "Cannot delete admin accounts." });

            await _userManager.DeleteAsync(user);
            return Ok(new { message = $"User {user.Email} deleted." });
        }

        
        [HttpPut("{id}/role")]
        [Authorize(Policy = "AdminOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest req)
        {
            if (!new[] { "Faculty", "Student" }.Contains(req.NewRole))
                return BadRequest(new { message = "Role must be Faculty or Student." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            var current = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, current);
            await _userManager.AddToRoleAsync(user, req.NewRole);

            return Ok(new { message = $"Role changed to {req.NewRole}." });
        }
    }

    public class ChangeRoleRequest
    {
        public string NewRole { get; set; } = string.Empty;
    }
}
