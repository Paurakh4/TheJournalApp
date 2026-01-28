using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public interface IMoodService
{
    /// <summary>
    /// Get all active moods
    /// </summary>
    Task<List<Mood>> GetAllMoodsAsync();

    /// <summary>
    /// Get moods by category
    /// </summary>
    Task<List<Mood>> GetMoodsByCategoryAsync(MoodCategory category);

    /// <summary>
    /// Get a specific mood by ID
    /// </summary>
    Task<Mood?> GetMoodByIdAsync(int moodId);

    /// <summary>
    /// Get moods grouped by category
    /// </summary>
    Task<Dictionary<MoodCategory, List<Mood>>> GetMoodsGroupedByCategoryAsync();
}
