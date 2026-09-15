using FieldEvents.Server.Auth;
using FieldEvents.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Controllers;

public record LoginRequest(string UserName, string Password);
public record LoginResponse(string Token, string UserName, string Role);

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var token = tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, user.UserName, user.Role.ToString()));
    }
}
