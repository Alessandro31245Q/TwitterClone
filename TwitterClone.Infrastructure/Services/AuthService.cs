using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TwitterClone.Application.DTOs.Auth;
using TwitterClone.Application.Interfaces;
using TwitterClone.Domain.Entities;

namespace TwitterClone.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }
    public async Task<string> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser is not null) throw new InvalidOperationException("Ya existe una cuenta con ese correo electrónico.");

        var existingUsername = await _userManager.FindByNameAsync(registerDto.Username);
        if (existingUsername is not null) throw new InvalidOperationException("El nombre de usuario ya está en uso.");

        var user = new ApplicationUser
        {
            UserName      = registerDto.Username,
            Email         = registerDto.Email,
            DisplayName   = registerDto.DisplayName ?? registerDto.Username,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error al crear el usuario: {errors}");
        }

        return GenerateJwtToken(user);
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email) ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!passwordValid) throw new UnauthorizedAccessException("Credenciales inválidas.");

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
        var issuer = jwtSettings["Issuer"]    ?? throw new InvalidOperationException("JWT Issuer no configurado.");
        var audience = jwtSettings["Audience"]  ?? throw new InvalidOperationException("JWT Audience no configurado.");
        var expiresInMinutes = int.TryParse(jwtSettings["ExpiresInMinutes"], out var mins) ? mins : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("username", user.UserName!),
            new("displayName", user.DisplayName ?? user.UserName!)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials:credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
