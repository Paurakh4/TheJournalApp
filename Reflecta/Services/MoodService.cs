using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class MoodService : IMoodService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;

    public MoodService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Mood>> GetAllMoodsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Moods
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Mood>> GetMoodsByCategoryAsync(MoodCategory category)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Moods
            .Where(m => m.IsActive && m.Category == category)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Mood?> GetMoodByIdAsync(int moodId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Moods
            .FirstOrDefaultAsync(m => m.Id == moodId && m.IsActive);
    }

    public async Task<Dictionary<MoodCategory, List<Mood>>> GetMoodsGroupedByCategoryAsync()
    {
        var moods = await GetAllMoodsAsync();
        
        return moods
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
