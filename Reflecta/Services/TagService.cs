using Microsoft.EntityFrameworkCore;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;

namespace Reflecta.Services;

public class TagService : ITagService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;

    public TagService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Tag>> GetAllTagsAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(t => t.IsActive && (t.IsSystem || t.UserId == userId))
            .OrderBy(t => t.IsSystem ? 0 : 1) // System tags first
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetSystemTagsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(t => t.IsActive && t.IsSystem)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetUserTagsAsync(Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Tags
            .Where(t => t.IsActive && !t.IsSystem && t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, Tag? Tag)> CreateTagAsync(Guid userId, string name, string? color = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Tag name is required", null);

        name = name.Trim();

        // Check if tag already exists for user or as system tag
        var existingTag = await context.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower() && 
                                      (t.IsSystem || t.UserId == userId));

        if (existingTag != null)
            return (false, "A tag with this name already exists", null);

        var tag = new Tag
        {
            Name = name,
            Color = color ?? GenerateRandomColor(),
            IsSystem = false,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        return (true, "Tag created successfully", tag);
    }

    public async Task<(bool Success, string Message)> UpdateTagAsync(Guid tagId, Guid userId, string name, string? color)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag == null)
            return (false, "Tag not found");

        if (tag.IsSystem)
            return (false, "System tags cannot be modified");

        if (tag.UserId != userId)
            return (false, "You can only modify your own tags");

        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            
            // Check for duplicate name
            var duplicateExists = await context.Tags
                .AnyAsync(t => t.Id != tagId && 
                              t.Name.ToLower() == name.ToLower() && 
                              (t.IsSystem || t.UserId == userId));

            if (duplicateExists)
                return (false, "A tag with this name already exists");

            tag.Name = name;
        }

        if (color != null)
        {
            tag.Color = color;
        }

        await context.SaveChangesAsync();
        return (true, "Tag updated successfully");
    }

    public async Task<(bool Success, string Message)> DeleteTagAsync(Guid tagId, Guid userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tag = await context.Tags
            .Include(t => t.EntryTags)
            .FirstOrDefaultAsync(t => t.Id == tagId);

        if (tag == null)
            return (false, "Tag not found");

        if (tag.IsSystem)
            return (false, "System tags cannot be deleted");

        if (tag.UserId != userId)
            return (false, "You can only delete your own tags");

        // Soft delete - just mark as inactive
        tag.IsActive = false;
        await context.SaveChangesAsync();

        return (true, "Tag deleted successfully");
    }

    public async Task<Tag?> GetTagByIdAsync(Guid tagId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.IsActive);
    }

    public async Task<List<(Tag Tag, int UsageCount)>> GetTopTagsAsync(Guid userId, int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tagUsage = await context.EntryTags
            .Where(et => et.Entry.UserId == userId && et.Tag.IsActive)
            .GroupBy(et => et.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(count)
            .ToListAsync();

        var tagIds = tagUsage.Select(t => t.TagId).ToList();
        var tags = await context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        return tagUsage
            .Where(t => tags.ContainsKey(t.TagId))
            .Select(t => (tags[t.TagId], t.Count))
            .ToList();
    }

    private static string GenerateRandomColor()
    {
        var random = new Random();
        var colors = new[]
        {
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7",
            "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9"
        };
        return colors[random.Next(colors.Length)];
    }
}
