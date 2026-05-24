using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using backend.Data;
using backend.Models;
using backend.Utilities;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
            {
                return BadRequest("Username is already taken.");
            }

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Role = dto.Role,
                RestaurantId = dto.RestaurantId,
                ParentOwnerId = dto.ParentOwnerId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            string restaurantName = "";
            if (user.RestaurantId.HasValue)
            {
                var rest = await _context.Restaurants.FindAsync(user.RestaurantId.Value);
                restaurantName = rest?.Name ?? "";
            }

            return Ok(new AuthResponseDto
            {
                Token = token,
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                RestaurantId = user.RestaurantId,
                RestaurantName = restaurantName
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(user);

            string restaurantName = "";
            if (user.RestaurantId.HasValue)
            {
                var rest = await _context.Restaurants.FindAsync(user.RestaurantId.Value);
                restaurantName = rest?.Name ?? "";
            }

            return Ok(new AuthResponseDto
            {
                Token = token,
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                RestaurantId = user.RestaurantId,
                RestaurantName = restaurantName
            });
        }

        // Fetch user listing by creator (for staff/managers management)
        [HttpGet("users-by-owner/{ownerId}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersByOwner(int ownerId)
        {
            return await _context.Users
                .Where(u => u.ParentOwnerId == ownerId)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? "superSecretKey12345678901234567890";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            string restaurantName = "";
            if (user.RestaurantId.HasValue)
            {
                var rest = _context.Restaurants.Find(user.RestaurantId.Value);
                restaurantName = rest?.Name ?? "";
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", user.FullName),
                    new Claim("RestaurantId", user.RestaurantId.HasValue ? user.RestaurantId.Value.ToString() : ""),
                    new Claim("RestaurantName", restaurantName)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "pos-api",
                Audience = _configuration["Jwt:Audience"] ?? "pos-client"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
