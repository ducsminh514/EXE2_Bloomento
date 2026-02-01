using ADHDChecklist.API.Data;
using ADHDChecklist.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADHDChecklist.API.Services
{
    public class KnowledgeSeeder
    {
        private readonly AppDbContext _context;

        public KnowledgeSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task SeedAsync()
        {
            // 1. Seed Categories if empty
            if (!await _context.KnowledgeCategories.AnyAsync())
            {
                var categories = new List<KnowledgeCategory>
                {
                    new KnowledgeCategory 
                    { 
                        Name = "Về ADHD", 
                        Slug = "about-adhd", 
                        Description = "Tìm hiểu cơ bản về não bộ ADHD",
                        Icon = "🧠",
                        ColorHex = "#8B5CF6", // Violet
                        OrderIndex = 1
                    },
                    new KnowledgeCategory 
                    { 
                        Name = "Chiến lược Tập trung", 
                        Slug = "focus-strategies", 
                        Description = "Các phương pháp Pomodoro, Flow state...",
                        Icon = "⚡",
                        ColorHex = "#F59E0B", // Amber
                        OrderIndex = 2
                    },
                    new KnowledgeCategory 
                    { 
                        Name = "Quản lý Cảm xúc", 
                        Slug = "emotional-regulation", 
                        Description = "Kiểm soát lo âu và bốc đồng",
                        Icon = "❤️",
                        ColorHex = "#EC4899", // Pink
                        OrderIndex = 3
                    },
                    new KnowledgeCategory 
                    { 
                        Name = "Công cụ & Apps", 
                        Slug = "tools-apps", 
                        Description = "Review các công cụ hỗ trợ hữu ích",
                        Icon = "🛠️",
                        ColorHex = "#3B82F6", // Blue
                        OrderIndex = 4
                    }
                };

                _context.KnowledgeCategories.AddRange(categories);
                await _context.SaveChangesAsync();
            }

            // 2. Seed Articles if empty (This will now run even if categories existed)
            if (!await _context.Articles.AnyAsync())
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync();
                
                // Fetch categories from DB to ensure we have IDs
                var categories = await _context.KnowledgeCategories.ToListAsync();
                
                if (adminUser != null && categories.Any())
                {
                    var adhdCat = categories.FirstOrDefault(c => c.Slug == "about-adhd");
                    var focusCat = categories.FirstOrDefault(c => c.Slug == "focus-strategies");

                    var articles = new List<Article>();

                    if (adhdCat != null)
                    {
                        articles.Add(new Article
                        {
                            Title = "ADHD là gì? Hiểu đúng để sống vui",
                            Slug = "adhd-la-gi",
                            Summary = "Khám phá cơ chế hoạt động của bộ não ADHD và tại sao nó không phải là một khiếm khuyết.",
                            Content = @"
                                <h2>Không phải lười biếng, là sự khác biệt về Dopamine</h2>
                                <p>Nhiều người lầm tưởng ADHD là biểu hiện của sự lười biếng hay thiếu kỷ luật. Thực tế, não bộ người ADHD thiếu hụt Dopamine - chất dẫn truyền thần kinh tạo động lực.</p>
                                <h3>Siêu năng lực của ADHD</h3>
                                <p>Khi tìm thấy hứng thú, người ADHD có khả năng siêu tập trung (Hyperfocus) và sáng tạo không giới hạn.</p>
                            ",
                            CategoryId = adhdCat.Id,
                            AuthorId = adminUser.Id,
                            EstimatedReadTimeMinutes = 5,
                            Difficulty = "Easy",
                            IsPublished = true,
                            PublishedAt = DateTime.UtcNow,
                            ViewCount = 120
                        });
                    }

                    if (focusCat != null)
                    {
                        articles.Add(new Article
                        {
                            Title = "Kỹ thuật Pomodoro cho não cá vàng",
                            Slug = "pomodoro-cho-adhd",
                            Summary = "Cách điều chỉnh phương pháp Pomodoro truyền thống để phù hợp với người ADHD.",
                            Content = @"
                                <h2>Tại sao 25 phút là quá dài?</h2>
                                <p>Với người ADHD, 25 phút đôi khi là một cực hình. Hãy thử bắt đầu với 10 hoặc 15 phút.</p>
                                <h3>Quy tắc 5 phút</h3>
                                <p>Hãy tự nhủ: 'Tôi chỉ làm việc này 5 phút thôi'. Thường thì sau 5 phút, bạn sẽ cuốn vào guồng quay và làm tiếp.</p>
                            ",
                            CategoryId = focusCat.Id,
                            AuthorId = adminUser.Id,
                            EstimatedReadTimeMinutes = 3,
                            Difficulty = "Medium",
                            IsPublished = true,
                            PublishedAt = DateTime.UtcNow,
                            ViewCount = 85
                        });
                    }

                    if (articles.Any())
                    {
                        _context.Articles.AddRange(articles);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
