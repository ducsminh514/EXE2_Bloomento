using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Services;
namespace ADHDChecklist.API.Features.Auth.Login
{
    // ============================================
    // REQUEST & RESPONSE
    // ============================================
    public record LoginCommand(
        string Email,
        string Password,
        bool RememberMe = false
    ) : IRequest<LoginResponse>;

    public record LoginResponse(
        bool Success,
        string Message,
        string? AccessToken = null,
        string? RefreshToken = null,
        UserInfo? User = null,
        bool RequireEmailVerification = false
    );

    public record UserInfoGg(
        string UserId,
        string Email,
        string FullName,
        string SubscriptionTier,
        bool IsPremium,
        string Role
    );

    public record UserInfo(
        string UserId,
        string Email,
        string FullName,
        string SubscriptionTier,
        bool IsPremium,
        bool IsEmailVerified,
        string Role
    );

}
