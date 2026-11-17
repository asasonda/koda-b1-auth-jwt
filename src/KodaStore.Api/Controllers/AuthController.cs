using KodaStore.Application.DTOs.Auth;
using KodaStore.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KodaStore.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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

    [HttpPost]
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
        var response = new LoginResponse("Akun berhasil diverifikasi", result.User);

        return Ok(response);

    }
}