using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserRoleApp.Models;

namespace UserRoleApp.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<Users> _userManager;

        public JwtService(IConfiguration config, UserManager<Users> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public async Task<string> GenerateTokenAsync(Users user)
        {
            var roles  = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,  user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,    user.Id),
                new Claim(ClaimTypes.Name,              user.FullName),
                new Claim(ClaimTypes.Email,             user.Email!)
            };
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:DurationInMinutes"]));

            var token = new JwtSecurityToken(
                issuer:             _config["JwtSettings:Issuer"],
                audience:           _config["JwtSettings:Audience"],
                claims:             claims,
                expires:            expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
