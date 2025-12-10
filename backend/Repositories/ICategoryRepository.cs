using VideoStore.Backend.Models;

namespace VideoStore.Backend.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetByNameAsync(string name);
        Task<Category> GetOrCreateAsync(string name);
        Task<bool> ExistsByNameAsync(string name);
        Task<IEnumerable<Category>> GetCategoriesByIdsAsync(IEnumerable<int> categoryIds);
    }
}
