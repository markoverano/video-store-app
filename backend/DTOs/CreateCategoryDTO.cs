using System.ComponentModel.DataAnnotations;

namespace VideoStore.Backend.DTOs
{
    public class CreateCategoryDTO
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}
