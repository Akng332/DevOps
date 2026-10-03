using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.Api.Data;
using MusicStore.Api.DTOs;
using MusicStore.Api.Models;
using MusicStore.Api.Services;
using System.Security.Claims;

namespace MusicStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext context, PasswordHasher passwordHasher, JwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request)
    {
        // Проверяем, существует ли уже такой пользователь
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("Пользователь с таким логином уже существует.");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password)
            // Role установится по умолчанию "manager" из вашей модели
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user);
        
        return Ok(new AuthResponseDto 
        { 
            Token = token, 
            Username = user.Username, 
            Role = user.Role 
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Неверный логин или пароль.");

        var token = _jwtTokenService.GenerateToken(user);
        
        return Ok(new AuthResponseDto 
        { 
            Token = token, 
            Username = user.Username, 
            Role = user.Role 
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult> GetMe()
    {
        // Достаем ID пользователя из токена
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        
        if (user == null) 
            return NotFound();

        return Ok(new 
        { 
            user.Id, 
            user.Username, 
            user.Role, 
            user.CreatedAt 
        });
    }
}