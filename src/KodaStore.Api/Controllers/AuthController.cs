using System.IdentityModel.Tokens.Jwt;
using KodaStore.Application.DTOs.Auth;
using KodaStore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KodaStore.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtToken;

    public AuthController(IAuthService authService, IJwtTokenService jwtToken)
    {
        _authService = authService;
        _jwtToken = jwtToken;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]

    public async Task<ActionResult<AuthUserResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        if(result.Status == RegisterStatus.Created)
        {
            return StatusCode(StatusCodes.Status201Created, result.User);
        }

        if(result.Status == RegisterStatus.DuplicateEmail)
        {
            return Conflict(new ProblemDetails
            {
               Status = StatusCodes.Status409Conflict,
               Title = "Email Terdaftar" ,
               Detail = "Email sudah digunakan oleh orang lain"
            });
        }

        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>{["registration"] = result.Error})
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Registrasion validation failed"  
        };
        return BadRequest(problem);
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginLimiter")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]

    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LogisAsync(request, ct);
        if(!result.Succeeded || result.User is null)
        {
            return Unauthorized(new ProblemDetails
            {
               Status = StatusCodes.Status401Unauthorized,
               Title = "Invalid Acces",
               Detail = "Email atau password salah" 
            });
        }

        var user = result.User!;
        var issueToken = _jwtToken.IssuedAccessToken(user);

        var response = new LoginResponse(
            Message: "Akun berhasil diverifikasi",
            User: user,
            AccessToken: issueToken.AccessToken,
            TokenType: "Bearer",
            ExpiresAtUtc:  issueToken.ExpiredAtUtc
        );
        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if(!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid user identity"
            });
        }

        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
        var response = new CurrentUserResponse(userId, email);

        return Ok(response);
    }
}