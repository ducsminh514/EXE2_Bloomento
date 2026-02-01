using System.ComponentModel.DataAnnotations;

namespace ADHDChecklist.API.Entities.Common
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
