using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using ADHDChecklist.Client.Infrastructure.Services;
using ADHDChecklist.Client.Shared.Models;

namespace ADHDChecklist.Client.Features.Auth.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<GoogleLoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
    Task<bool> VerifyEmailAsync(string userId, string token);
    Task<bool> ResendVerificationEmailAsync(string email);
    Task<bool> RefreshTokenAsync();
    Task LogoutAsync();
    Task<CurrentUser?> GetCurrentUserAsync();
}

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApiClient apiClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<RegisterResponse>(
                "/api/auth/register",
                request
            );

            return response ?? new RegisterResponse(false, "Đăng ký thất bại");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            return new RegisterResponse(false, "Có lỗi xảy ra khi đăng ký");
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<LoginResponse>(
                "/api/auth/login",
                request
            );

            if (response?.Success == true && response.AccessToken != null)
            {
                await SaveTokensAsync(response.AccessToken, response.RefreshToken!);
                await SaveUserInfoAsync(response.User!);

                // Notify auth state changed
                await _authStateProvider.GetAuthenticationStateAsync();

                _logger.LogInformation("User logged in successfully");
            }

            return response ?? new LoginResponse(false, "Đăng nhập thất bại");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return new LoginResponse(false, "Có lỗi xảy ra khi đăng nhập");
        }
    }

    public async Task<GoogleLoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            var response = await _apiClient.PostAsync<GoogleLoginResponse>(
                "/api/auth/google-login",
                request
            );

            if (response?.Success == true && response.AccessToken != null)
            {
                await SaveTokensAsync(response.AccessToken, response.RefreshToken!);
                await SaveUserInfoAsync(response.User!);

                await _authStateProvider.GetAuthenticationStateAsync();

                _logger.LogInformation("User logged in via Google");
            }

            return response ?? new GoogleLoginResponse(false, "Đăng nhập Google thất bại");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google login failed");
            return new GoogleLoginResponse(false, "Có lỗi xảy ra khi đăng nhập Google");
        }
    }

    public async Task<bool> VerifyEmailAsync(string userId, string token)
    {
        try
        {
            var response = await _apiClient.PostAsync<VerifyEmailResponse>(
                "/api/auth/verify-email",
                new { UserId = userId, Token = token }
            );

            return response?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed");
            return false;
        }
    }

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var response = await _apiClient.PostAsync<object>(
                "/api/auth/resend-verification",
                new { Email = email }
            );
            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification email failed");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var response = await _apiClient.PostAsync<RefreshTokenResponse>(
                "/api/auth/refresh-token",
                new RefreshTokenRequest(refreshToken)
            );

            if (response?.Success == true && response.AccessToken != null)
            {
                await SaveTokensAsync(response.AccessToken, response.RefreshToken!);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("accessToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("userInfo");

        _apiClient.ClearAuthToken();

        await _authStateProvider.GetAuthenticationStateAsync();

        _logger.LogInformation("User logged out");
    }

    public async Task<CurrentUser?> GetCurrentUserAsync()
    {
        try
        {
            var userInfo = await _localStorage.GetItemAsync<UserInfo>("userInfo");
            if (userInfo == null) return null;

            return new CurrentUser
            {
                UserId = userInfo.UserId,
                Email = userInfo.Email,
                FullName = userInfo.FullName,
                SubscriptionTier = userInfo.SubscriptionTier,
                IsPremium = userInfo.IsPremium,
                IsEmailVerified = userInfo.IsEmailVerified,
                IsAuthenticated = true
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await _localStorage.SetItemAsync("accessToken", accessToken);
        await _localStorage.SetItemAsync("refreshToken", refreshToken);
        _apiClient.SetAuthToken(accessToken);
    }

    private async Task SaveUserInfoAsync(UserInfo userInfo)
    {
        await _localStorage.SetItemAsync("userInfo", userInfo);
    }
}