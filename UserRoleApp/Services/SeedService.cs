using Microsoft.AspNetCore.Identity;
using UserRoleApp.Models;

namespace UserRoleApp.Services
{
    public class SeedService
    {
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly ILogger<SeedService> _logger;

        public SeedService(UserManager<Users> userManager, RoleManager<IdentityRole> roleManager,
            IConfiguration config, ILogger<SeedService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Seed all roles
            foreach (var role in new[] { "Admin", "Faculty", "Student" })
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    _logger.LogInformation("Role '{Role}' created.", role);
                }
            }

            // Seed admin user from appsettings
            var adminEmail    = _config["AdminSeed:Email"]!;
            var adminPassword = _config["AdminSeed:Password"]!;
            var adminName     = _config["AdminSeed:FullName"]!;

            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new Users
                {
                    FullName           = adminName,
                    UserName           = adminEmail,
                    NormalizedUserName = adminEmail.ToUpper(),
                    Email              = adminEmail,
                    NormalizedEmail    = adminEmail.ToUpper(),
                    EmailConfirmed     = true,
                    SecurityStamp      = Guid.NewGuid().ToString()
                };

                var result = await _userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                    _logger.LogInformation("Admin seeded: {Email}", adminEmail);
                }
                else
                    _logger.LogError("Admin seed failed: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
