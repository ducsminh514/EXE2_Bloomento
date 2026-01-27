using ADHDChecklist.API.Entities;
using ADHDChecklist.API.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Identity;
using ADHDChecklist.API.Entities.Common;
namespace ADHDChecklist.API.Features.Users.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileResponse>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UpdateProfileResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return new UpdateProfileResponse(false, "Không tìm thấy người dùng");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return new UpdateProfileResponse(false, "Tên hiển thị không được để trống");
            }

            // Update fields
            user.FullName = request.FullName;
            
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} updated profile successfully", request.UserId);
                return new UpdateProfileResponse(true, "Cập nhật thành công", user.FullName);
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new UpdateProfileResponse(false, $"Lỗi cập nhật: {errors}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", request.UserId);
            return new UpdateProfileResponse(false, "Có lỗi xảy ra khi cập nhật hồ sơ");
        }
    }
}
