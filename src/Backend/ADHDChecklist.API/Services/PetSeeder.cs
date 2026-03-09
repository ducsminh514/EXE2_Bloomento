using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Services;

public class PetSeeder
{
    private readonly AppDbContext _context;

    public PetSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task SeedAsync()
    {
        // 1. Seed Pet Templates if empty
        if (!await _context.PetTemplates.AnyAsync())
        {
            var mysticalPlant = new PetTemplate
            {
                Id = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                Name = "Cây Tinh Linh (Mystical Plant)",
                Species = "Plant",
                Description = "Một loài thực vật huyền bí có thể cảm nhận và lớn lên cùng sự tập trung của bạn. Nó mang lại cảm giác bình yên và tĩnh lặng.",
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow
            };

            await _context.PetTemplates.AddAsync(mysticalPlant);

            // 2. Seed Evolution Stages for Mystical Plant
            var stages = new List<PetEvolutionStage>
            {
                new PetEvolutionStage
                {
                    Id = Guid.NewGuid(),
                    TemplateId = mysticalPlant.Id,
                    RequiredLevel = 1,
                    EvolutionName = "Hạt Giống (Seedling)",
                    AssetUrl = "/assets/models/pets/plant_stage1.glb" // Placeholder
                },
                new PetEvolutionStage
                {
                    Id = Guid.NewGuid(),
                    TemplateId = mysticalPlant.Id,
                    RequiredLevel = 11,
                    EvolutionName = "Mầm Xanh (Young Plant)",
                    AssetUrl = "/assets/models/pets/plant_stage2.glb" // Placeholder
                },
                new PetEvolutionStage
                {
                    Id = Guid.NewGuid(),
                    TemplateId = mysticalPlant.Id,
                    RequiredLevel = 31,
                    EvolutionName = "Cổ Thụ (Ancient Tree)",
                    AssetUrl = "/assets/models/pets/plant_stage3.glb" // Placeholder
                }
            };

            await _context.PetEvolutionStages.AddRangeAsync(stages);
            await _context.SaveChangesAsync();
        }
    }
}
