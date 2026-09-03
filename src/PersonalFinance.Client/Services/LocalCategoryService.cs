using PersonalFinance.Client.Data;
using PersonalFinance.Domain.Entities;
using PersonalFinance.Domain.Enums;

namespace PersonalFinance.Client.Services;

public class LocalCategoryService
{
    private const string StoreName = "categories";

    private readonly IndexedDbService _db;

    public LocalCategoryService(IndexedDbService db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var categories = await _db.GetAllAsync<Category>(StoreName);
        return categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList();
    }

    public async Task<Category> CreateAsync(string name, CategoryType type, string? icon, string? color)
    {
        var category = new Category
        {
            Name = name,
            Type = type,
            Icon = icon,
            Color = color
        };

        await _db.PutAsync(StoreName, category);
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _db.GetAsync<Category>(StoreName, id.ToString());
        if (category is null)
        {
            return;
        }

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _db.PutAsync(StoreName, category);
    }
}
