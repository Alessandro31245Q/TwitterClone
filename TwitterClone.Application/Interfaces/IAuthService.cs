using TwitterClone.Application.DTOs.Auth;

namespace TwitterClone.Application.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDto registerDto);
    Task<string> LoginAsync(LoginDto loginDto);
}
