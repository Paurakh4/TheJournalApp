using Reflecta.Models;

namespace Reflecta.Services.Interfaces;

public interface ITagService
{
    /// <summary>
    /// Get all available tags (system + user's custom tags)
    /// </summary>
    Task<List<Tag>> GetAllTagsAsync(Guid userId);

    /// <summary>
    /// Get system tags only
    /// </summary>
    Task<List<Tag>> GetSystemTagsAsync();

    /// <summary>
    /// Get user's custom tags only
    /// </summary>
    Task<List<Tag>> GetUserTagsAsync(Guid userId);

    /// <summary>
    /// Create a new custom tag
    /// </summary>
    Task<(bool Success, string Message, Tag? Tag)> CreateTagAsync(Guid userId, string name, string? color = null);

    /// <summary>
    /// Update a custom tag
    /// </summary>
    Task<(bool Success, string Message)> UpdateTagAsync(Guid tagId, Guid userId, string name, string? color);

    /// <summary>
    /// Delete a custom tag
    /// </summary>
    Task<(bool Success, string Message)> DeleteTagAsync(Guid tagId, Guid userId);

    /// <summary>
    /// Get tag by ID
    /// </summary>
    Task<Tag?> GetTagByIdAsync(Guid tagId);

    /// <summary>
    /// Get most used tags for a user
    /// </summary>
    Task<List<(Tag Tag, int UsageCount)>> GetTopTagsAsync(Guid userId, int count = 10);
}
