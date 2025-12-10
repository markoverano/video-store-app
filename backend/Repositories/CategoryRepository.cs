using Microsoft.EntityFrameworkCore;
using VideoStore.Backend.Data;
using VideoStore.Backend.Models;

namespace VideoStore.Backend.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(VideoContext context) : base(context)
        {
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await Context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<Category> GetOrCreateAsync(string name)
        {
            var normalizedName = name.Trim();
            var existingCategory = await GetByNameAsync(normalizedName);

            if (existingCategory != null)
            {
                return existingCategory;
            }

            var newCategory = new Category { Name = normalizedName };
            return await CreateAsync(newCategory);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await Context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Category>> GetCategoriesByIdsAsync(IEnumerable<int> categoryIds)
        {
            return await Context.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();
        }
    }
}
