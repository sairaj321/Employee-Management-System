using System.Security.Claims;
using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int ActingUserId
    {
        get
        {
            var claim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    protected int? CallerEmployeeId
    {
        get
        {
            var claim = User.FindFirst("employeeId")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }

    protected string? ClientIpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    protected IActionResult OkResponse<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    protected IActionResult CreatedResponse<T>(string uri, T data, string message = "Resource created successfully")
    {
        return Created(uri, ApiResponse<T>.SuccessResult(data, message));
    }
}

[ApiController]
[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ClientIpAddress, ct);
        return OkResponse(result, "Login successful.");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ClientIpAddress, ct);
        return OkResponse(result, "Token refreshed successfully.");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, ActingUserId, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _authService.ChangePasswordAsync(ActingUserId, dto, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var user = await _authService.GetCurrentUserAsync(ActingUserId, ct);
        return OkResponse(user, "User details retrieved.");
    }

    [HttpGet("users")]
    [Authorize(Policy = "User.Manage")]
    public async Task<IActionResult> GetAllUsers(CancellationToken ct)
    {
        var users = await _authService.GetAllUsersAsync(ct);
        return OkResponse(users, "All users retrieved.");
    }

    [HttpGet("roles")]
    [Authorize]
    public async Task<IActionResult> GetAllRoles(CancellationToken ct)
    {
        var roles = await _authService.GetAllRolesAsync(ct);
        return OkResponse(roles, "Roles retrieved.");
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Role.Manage")]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var permissions = await _authService.GetAllPermissionsAsync(ct);
        return OkResponse(permissions, "Permissions retrieved.");
    }
}
