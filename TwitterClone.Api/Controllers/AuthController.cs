using Microsoft.AspNetCore.Mvc;
using TwitterClone.Application.DTOs.Auth;
using TwitterClone.Application.Interfaces;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>
    /// Registers a new user and returns a JWT token.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var token = await _authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created, new AuthResponseDto(token));
    }

    /// <summary>
    /// Authenticates an existing user and returns a JWT token.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        return Ok(new AuthResponseDto(token));
    }
}
