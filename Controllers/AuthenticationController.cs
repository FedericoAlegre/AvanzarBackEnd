using AvanzarBackEnd.Data;
using AvanzarBackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AvanzarBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly string secretKey;
        public readonly AppDbContext _context;

        public AuthenticationController(IConfiguration configuration, AppDbContext context)
        {
            secretKey = configuration.GetSection("settings").GetSection("secretKey").ToString()!;
            _context = context;
        }

        [HttpPost]
        [Route("/login")]
        public async Task<ActionResult> Login([FromBody] User user)
        {
            try
            {
                this._context.Database.BeginTransaction();
                User? dbUser = await _context.Users.FirstOrDefaultAsync(x => x.Username!.Equals(user.Username));
                if (dbUser != null)
                {

                    if (BCrypt.Net.BCrypt.Verify(user.password, dbUser.HashedPassword))
                    {
                        var keyBytes = Encoding.ASCII.GetBytes(secretKey);
                        var claims = new ClaimsIdentity();
                        claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Username!));
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = claims,
                            Expires = DateTime.UtcNow.AddMinutes(30),
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                        };
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

                        string createdToken = tokenHandler.WriteToken(tokenConfig);

                        this._context.Database.CommitTransaction();
                        return StatusCode(StatusCodes.Status200OK, new { token = createdToken });
                    }

                    this._context.Database.CommitTransaction();
                    return BadRequest($"Incorrect Password");
                }

                this._context.Database.CommitTransaction();
                return BadRequest($"User with Username '{user.Username}' does not exist");
            }
            catch (Exception ex)
            {

                this._context.Database.CommitTransaction();
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("/singin")]
        public IActionResult Save([FromBody] User user)
        {

            try
            {
                if (user != null)
                {
                    user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(user.password);
                    user.FechaCreacion = DateTime.Now;
                    this._context.Users.Add(user);
                    this._context.SaveChanges();
                }


                return StatusCode(StatusCodes.Status200OK, new { message = "OK" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
