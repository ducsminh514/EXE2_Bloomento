using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ADHDChecklist.API.Entities.Enums;
using System.Threading.Tasks;
namespace ADHDChecklist.API.Features.Pets;

public class PetService : IPetService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private const string MasterDataCacheKey = "PetMasterData";

    public PetService(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PetStatusDto> GetUserPetAsync(Guid userId)
    {
        var userPet = await _context.UserPets
            .FirstOrDefaultAsync(up => up.UserId == userId);

        if (userPet == null) return new PetStatusDto();

        // 1. Lazy Evaluation cho HP & Persist
        await CalculateAndPersistLazyHp(userPet);

        // 2. Lấy Master Data từ Cache (Triệt tiêu N+1 Query)
        var masterData = await GetMasterDataAsync();
        var template = masterData.FirstOrDefault(t => t.Id == userPet.TemplateId);
        
        // Tìm stage phù hợp với level hiện tại
        var stage = template?.EvolutionStages
            .OrderByDescending(s => s.RequiredLevel)
            .FirstOrDefault(s => s.RequiredLevel <= userPet.CurrentLevel);

        // 3. Lấy thông tin Coins từ Inventory
        var coins = await _context.UserInventories
            .Where(ui => ui.UserId == userId)
            .Select(ui => ui.Coins)
            .FirstOrDefaultAsync();

        // Công thức Tuyến tính (Linear): Mỗi 100 XP là 1 Level
        int relativeXp = userPet.TotalXp % 100;
        if (userPet.TotalXp > 0 && relativeXp == 0) relativeXp = 0; // Đã sang level mới, thanh XP reset về 0

        return new PetStatusDto
    {
            Pet = new UserPetDto
            {
                Id = userPet.Id,
                CustomName = userPet.CustomName,
                TotalXp = relativeXp, // XP tiến trình trong level hiện tại
                CurrentLevel = userPet.CurrentLevel,
                CurrentHealth = userPet.CurrentHealth,
                State = (int)userPet.State,
                AssetUrl = stage?.AssetUrl ?? "",
                EvolutionName = stage?.EvolutionName ?? "Unknown",
                XpToNextLevel = 100 // Mỗi cấp luôn cần 100 XP
            },
            Coins = coins
        };
    }

    private async System.Threading.Tasks.Task CalculateAndPersistLazyHp(UserPet pet)
    {
        var now = DateTime.UtcNow;
        
        // 1. Logic Hibernate tự động bảo vệ tâm lý (Sau 48h không làm việc)
        if ((now - pet.LastActionAt).TotalDays >= 2.0 && pet.State != PetState.Hibernate)
        {
            await _context.UserPets
                .Where(up => up.Id == pet.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(up => up.State, PetState.Hibernate)
                    .SetProperty(up => up.LastHpUpdateAt, now));
            
            pet.State = PetState.Hibernate;
            pet.LastHpUpdateAt = now;
            return;
        }

        if (pet.State == PetState.Hibernate) return;

        // 2. Trừ HP Nguyên tử (The Root-Cause Fix 3)
        // Sử dụng SQL Expression để trừ HP dựa trên thời gian thực tế trôi qua
        // Công thức: hpLoss = (now - LastHpUpdateAt).TotalHours * (10/24)
        double hoursDiff = (now - pet.LastHpUpdateAt).TotalHours;
        int hpToSubtract = (int)(hoursDiff * (10.0 / 24.0));

        if (hpToSubtract > 0)
        {
            await _context.UserPets
                .Where(up => up.Id == pet.Id && up.State != PetState.Hibernate)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(up => up.CurrentHealth, up => Math.Max(0, up.CurrentHealth - hpToSubtract))
                    .SetProperty(up => up.LastHpUpdateAt, now)
                    // Cập nhật State dựa trên HP mới ngay trong DB
                    .SetProperty(up => up.State, up => 
                        (up.CurrentHealth - hpToSubtract) <= 0 ? PetState.Hibernate :
                        (up.CurrentHealth - hpToSubtract) < 30 ? PetState.Sick : PetState.Healthy));
            
            // Đồng bộ lại object local để GetUserPetAsync trả về đúng
            pet.CurrentHealth = Math.Max(0, pet.CurrentHealth - hpToSubtract);
            pet.LastHpUpdateAt = now;
            pet.State = pet.CurrentHealth <= 0 ? PetState.Hibernate : (pet.CurrentHealth < 30 ? PetState.Sick : PetState.Healthy);
        }
    }

    public async System.Threading.Tasks.Task AddXpAsync(Guid userId, int xpAmount)
    {
        var now = DateTime.UtcNow;
        
        // 1. Tải bản ghi Pet (Sử dụng Tracking)
        var pet = await _context.UserPets.FirstOrDefaultAsync(up => up.UserId == userId);
        
        // 2. Tự động nhận nuôi nếu chưa có (The Root-Cause Fix 5: Auto-Adoption)
        if (pet == null)
        {
            if (xpAmount <= 0) return; // Không tạo pet nếu trừ điểm hoặc điểm = 0

            // Lấy Template đầu tiên từ DB để đảm bảo Foreign Key luôn hợp lệ
            var defaultTemplate = await _context.PetTemplates.FirstOrDefaultAsync();
            if (defaultTemplate == null)
            {
                // Fallback: Nếu Seeder chưa chạy kịp, dùng ID mặc định nhưng log lại
                // Ở môi trường Production, Seeder nên được chạy qua Migration hoặc Startup
                _cache.Set("PetSeederError", "No templates found in DB during auto-adoption", TimeSpan.FromMinutes(5));
                return; 
            }

            pet = new UserPet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TemplateId = defaultTemplate.Id,
                CustomName = "Linh Vật Của Tôi",
                TotalXp = 0,
                CurrentLevel = 1,
                CurrentHealth = 100,
                State = PetState.Healthy,
                LastActionAt = now,
                LastHpUpdateAt = now,
                CreatedAt = now
            };
            _context.UserPets.Add(pet);

            // Đảm bảo Inventory tồn tại
            var inventory = await _context.UserInventories.FirstOrDefaultAsync(ui => ui.UserId == userId);
            if (inventory == null)
            {
                _context.UserInventories.Add(new UserInventory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Coins = 0,
                    ResurrectionPotionCount = 0,
                    UpdatedAt = now
                });
            }
        }

        // 3. Cập nhật XP và logic Trạng thái
        pet.TotalXp = Math.Max(0, pet.TotalXp + xpAmount);
        pet.CurrentLevel = (pet.TotalXp / 100) + 1;
        pet.LastActionAt = now;

        // Đánh thức nếu đang Hibernate và có XP dương
        if (pet.State == PetState.Hibernate && xpAmount > 0)
        {
            pet.State = PetState.Healthy;
            pet.CurrentHealth = Math.Max(pet.CurrentHealth, 10);
        }

        await _context.SaveChangesAsync();
    }

    private int CalculateLevelFromXp(int totalXp)
    {
        // Công thức: Level = 1 + floor(sqrt(XP / 100))
        // Ví dụ: 100 XP -> Lv2, 400 XP -> Lv3, 900 XP -> Lv4...
        // Tạo độ khó tăng dần để người dùng có cảm giác chinh phục mốc cao.
        if (totalXp <= 0) return 1;
        return (int)Math.Floor(Math.Sqrt(totalXp / 100.0)) + 1;
    }

    public async System.Threading.Tasks.Task UpdatePetNameAsync(Guid userId, string newName)
    {
        var pet = await _context.UserPets.FirstOrDefaultAsync(up => up.UserId == userId);
        if (pet != null)
        {
            pet.CustomName = newName;
            await _context.SaveChangesAsync();
        }
    }

    public async System.Threading.Tasks.Task UseRecoveryItemAsync(Guid userId)
    {
        // 1. Atomic Update cho Inventory (Chống Race Condition trừ vật phẩm)
        // Chỉ trừ nếu số lượng > 0
        int affectedRows = await _context.UserInventories
            .Where(ui => ui.UserId == userId && ui.ResurrectionPotionCount > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ui => ui.ResurrectionPotionCount, ui => ui.ResurrectionPotionCount - 1));

        if (affectedRows > 0)
        {
            // 2. Chữa lành Pet (Nguyên tử)
            await _context.UserPets
                .Where(up => up.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(up => up.CurrentHealth, 100)
                    .SetProperty(up => up.State, PetState.Healthy)
                    .SetProperty(up => up.LastActionAt, DateTime.UtcNow));
        }
    }

    #region Private Helpers

    // Phương thức cũ dùng void/private đã được thay thế bằng CalculateAndPersistLazyHp async ở trên.

    private async Task<List<PetTemplate>> GetMasterDataAsync()
    {
        return await _cache.GetOrCreateAsync(MasterDataCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return await _context.PetTemplates
                .Include(t => t.EvolutionStages)
                .ToListAsync();
        }) ?? new List<PetTemplate>();
    }

    private int CalculateXpToNextLevel(int currentLevel)
    {
        // Hệ thống Tuyến tính: Luôn cần 100 XP để lên cấp tiếp theo
        return 100;
    }

    #endregion
}
