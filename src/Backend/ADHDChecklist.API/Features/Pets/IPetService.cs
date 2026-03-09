using System;
using System.Threading.Tasks;

namespace ADHDChecklist.API.Features.Pets;

public interface IPetService
{
    /// <summary>
    /// Lấy thông tin Pet của User, bao gồm việc tính toán HP/XP theo Lazy Evaluation.
    /// </summary>
    System.Threading.Tasks.Task<PetStatusDto> GetUserPetAsync(Guid userId);

    /// <summary>
    /// Cộng XP cho Pet khi hoàn thành Task/Habit.
    /// </summary>
    System.Threading.Tasks.Task AddXpAsync(Guid userId, int xpAmount);

    /// <summary>
    /// Cập nhật tên cho Pet.
    /// </summary>
    System.Threading.Tasks.Task UpdatePetNameAsync(Guid userId, string newName);

    /// <summary>
    /// Sử dụng vật phẩm phục hồi (Hồi sinh/Chữa bệnh).
    /// </summary>
    System.Threading.Tasks.Task UseRecoveryItemAsync(Guid userId);
}
