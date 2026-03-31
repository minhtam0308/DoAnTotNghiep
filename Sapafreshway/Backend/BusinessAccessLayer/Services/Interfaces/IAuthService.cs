using BusinessAccessLayer.DTOs.Auth;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IAuthService 
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);
    
    }
}
