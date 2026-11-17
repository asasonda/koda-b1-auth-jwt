using KodaStore.Application.DTOs.Auth;

namespace KodaStore.Application.Services;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync( RegisterRequest request, CancellationToken ct = default);
    Task<LoginResult> LogisAsync(LoginRequest request, CancellationToken ct=default);
}