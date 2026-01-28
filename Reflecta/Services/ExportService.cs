using System.Text;
using Microsoft.EntityFrameworkCore;
using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reflecta.Data;
using Reflecta.Models;
using Reflecta.Services.Interfaces;
using QColors = QuestPDF.Helpers.Colors;
using QContainer = QuestPDF.Infrastructure.IContainer;

namespace Reflecta.Services;

public class ExportService : IExportService
{
    private readonly IDbContextFactory<ReflectaDbContext> _contextFactory;

    public ExportService(IDbContextFactory<ReflectaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ExportResult> ExportEntriesAsync(Guid userId, ExportOptions options)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entries = await context.JournalEntries
            .Include(e => e.PrimaryMood)
            .Include(e => e.SecondaryMoods)
                .ThenInclude(sm => sm.Mood)
            .Include(e => e.EntryTags)
                .ThenInclude(et => et.Tag)
            .Where(e => e.UserId == userId && 
                       e.EntryDate >= options.StartDate && 
                       e.EntryDate <= options.EndDate)
            .OrderBy(e => e.EntryDate)
            .ToListAsync();

        if (!entries.Any())
        {
            return new ExportResult
            {
                Success = false,
                Message = "No entries found for the selected date range"
            };
        }

        byte[] fileData;
        string fileName;
        string contentType;

        switch (options.Format)
        {
            case ExportFormat.Markdown:
                fileData = GenerateMarkdownExport(entries, options);
                fileName = $"reflecta_export_{options.StartDate:yyyy-MM-dd}_to_{options.EndDate:yyyy-MM-dd}.md";
                contentType = "text/markdown";
                break;

            case ExportFormat.PlainText:
                fileData = GeneratePlainTextExport(entries, options);
                fileName = $"reflecta_export_{options.StartDate:yyyy-MM-dd}_to_{options.EndDate:yyyy-MM-dd}.txt";
                contentType = "text/plain";
                break;

            case ExportFormat.PDF:
                fileData = GeneratePdfExport(entries, options);
                fileName = $"reflecta_export_{options.StartDate:yyyy-MM-dd}_to_{options.EndDate:yyyy-MM-dd}.pdf";
                contentType = "application/pdf";
                break;
            
            case ExportFormat.HTML:
            default:
                fileData = GenerateHtmlExport(entries, options);
                fileName = $"reflecta_export_{options.StartDate:yyyy-MM-dd}_to_{options.EndDate:yyyy-MM-dd}.html";
                contentType = "text/html";
                break;
        }

        // Log the export
        await LogExportAsync(userId, options.Format, options.StartDate, options.EndDate, entries.Count, fileData.Length);

        return new ExportResult
        {
            Success = true,
            FileData = fileData,
            FileName = fileName,
            ContentType = contentType,
            EntriesCount = entries.Count
        };
    }

    public async Task<List<ExportLog>> GetExportHistoryAsync(Guid userId, int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ExportLogs
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ExportedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<ExportLog> LogExportAsync(Guid userId, ExportFormat format, DateOnly startDate, DateOnly endDate, int entriesCount, long? fileSizeBytes)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var log = new ExportLog
        {
            UserId = userId,
            ExportFormat = format,
            DateRangeStart = startDate,
            DateRangeEnd = endDate,
            EntriesCount = entriesCount,
            FileSizeBytes = fileSizeBytes,
            ExportedAt = DateTime.UtcNow
        };

        context.ExportLogs.Add(log);
        await context.SaveChangesAsync();

        return log;
    }

    private byte[] GenerateMarkdownExport(List<JournalEntry> entries, ExportOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Reflecta Journal Export");
        sb.AppendLine();
        sb.AppendLine($"**Date Range:** {options.StartDate:MMMM d, yyyy} - {options.EndDate:MMMM d, yyyy}");
        sb.AppendLine($"**Total Entries:** {entries.Count}");
        sb.AppendLine($"**Total Words:** {entries.Sum(e => e.WordCount):N0}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.AppendLine($"## {entry.EntryDate:dddd, MMMM d, yyyy}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(entry.Title))
            {
                sb.AppendLine($"### {entry.Title}");
                sb.AppendLine();
            }

            if (options.IncludeMoods)
            {
                sb.AppendLine($"**Mood:** {entry.PrimaryMood.Emoji} {entry.PrimaryMood.Name}");
                
                var secondaryMoods = entry.SecondaryMoods.Select(sm => $"{sm.Mood.Emoji} {sm.Mood.Name}").ToList();
                if (secondaryMoods.Any())
                {
                    sb.AppendLine($"**Also feeling:** {string.Join(", ", secondaryMoods)}");
                }
                sb.AppendLine();
            }

            if (options.IncludeTags && entry.EntryTags.Any())
            {
                var tags = entry.EntryTags.Select(et => $"`{et.Tag.Name}`").ToList();
                sb.AppendLine($"**Tags:** {string.Join(" ", tags)}");
                sb.AppendLine();
            }

            sb.AppendLine(entry.Content);
            sb.AppendLine();

            if (options.IncludeWordCount)
            {
                sb.AppendLine($"*{entry.WordCount} words*");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] GeneratePlainTextExport(List<JournalEntry> entries, ExportOptions options)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("REFLECTA JOURNAL EXPORT");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"Date Range: {options.StartDate:MMMM d, yyyy} - {options.EndDate:MMMM d, yyyy}");
        sb.AppendLine($"Total Entries: {entries.Count}");
        sb.AppendLine($"Total Words: {entries.Sum(e => e.WordCount):N0}");
        sb.AppendLine();
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine();

        foreach (var entry in entries)
        {
            sb.AppendLine($"DATE: {entry.EntryDate:dddd, MMMM d, yyyy}");

            if (!string.IsNullOrEmpty(entry.Title))
            {
                sb.AppendLine($"TITLE: {entry.Title}");
            }

            if (options.IncludeMoods)
            {
                sb.AppendLine($"MOOD: {entry.PrimaryMood.Name}");
                
                var secondaryMoods = entry.SecondaryMoods.Select(sm => sm.Mood.Name).ToList();
                if (secondaryMoods.Any())
                {
                    sb.AppendLine($"ALSO FEELING: {string.Join(", ", secondaryMoods)}");
                }
            }

            if (options.IncludeTags && entry.EntryTags.Any())
            {
                var tags = entry.EntryTags.Select(et => et.Tag.Name).ToList();
                sb.AppendLine($"TAGS: {string.Join(", ", tags)}");
            }

            sb.AppendLine();
            sb.AppendLine(entry.Content);
            sb.AppendLine();

            if (options.IncludeWordCount)
            {
                sb.AppendLine($"({entry.WordCount} words)");
            }

            sb.AppendLine("-".PadRight(50, '-'));
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private byte[] GeneratePdfExport(List<JournalEntry> entries, ExportOptions options)
    {
        QuestPdfBootstrapper.EnsureInitialized();

        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QColors.Grey.Darken4));
                    
                    // Header
                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Reflecta Journal Export")
                                    .FontSize(24)
                                    .Bold()
                                    .FontColor(QColors.Green.Darken2);
                                
                                col.Item().Text($"{options.StartDate:MMMM d, yyyy} - {options.EndDate:MMMM d, yyyy}")
                                    .FontSize(12)
                                    .FontColor(QColors.Grey.Darken1);
                            });
                            
                            row.ConstantItem(100).Column(col =>
                            {
                                col.Item().AlignRight().Text($"{entries.Count} Entries")
                                    .FontSize(14)
                                    .Bold();
                                col.Item().AlignRight().Text($"{entries.Sum(e => e.WordCount):N0} Words")
                                    .FontSize(10)
                                    .FontColor(QColors.Grey.Darken1);
                            });
                        });
                        
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(QColors.Grey.Lighten2);
                    });
                    
                    // Content
                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        foreach (var entry in entries)
                        {
                            column.Item().Border(1).BorderColor(QColors.Grey.Lighten2).Background(QColors.Grey.Lighten5)
                                .Padding(15)
                                .Column(entryColumn =>
                                {
                                    // Date header
                                    entryColumn.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(entry.EntryDate.ToString("dddd, MMMM d, yyyy"))
                                            .FontSize(14)
                                            .Bold()
                                            .FontColor(QColors.Grey.Darken3);
                                        
                                        if (options.IncludeWordCount)
                                        {
                                            row.ConstantItem(80).AlignRight().Text($"{entry.WordCount} words")
                                                .FontSize(9)
                                                .FontColor(QColors.Grey.Darken1);
                                        }
                                    });
                                    
                                    // Title if present
                                    if (!string.IsNullOrEmpty(entry.Title))
                                    {
                                        entryColumn.Item().PaddingTop(5).Text(entry.Title)
                                            .FontSize(16)
                                            .SemiBold();
                                    }
                                    
                                    // Mood badge
                                    if (options.IncludeMoods && entry.PrimaryMood != null)
                                    {
                                        entryColumn.Item().PaddingTop(8).Row(row =>
                                        {
                                            var moodColor = entry.PrimaryMood.Category switch
                                            {
                                                MoodCategory.Positive => QColors.Green.Lighten3,
                                                MoodCategory.Negative => QColors.Red.Lighten3,
                                                _ => QColors.Blue.Lighten3
                                            };
                                            
                                            row.ConstantItem(120).Background(moodColor).Padding(5)
                                                .Text($"{entry.PrimaryMood.Emoji} {entry.PrimaryMood.Name}")
                                                .FontSize(10);
                                            
                                            // Secondary moods
                                            foreach (var sm in entry.SecondaryMoods.Take(3))
                                            {
                                                row.ConstantItem(5); // spacer
                                                row.ConstantItem(100).Background(QColors.Grey.Lighten3).Padding(5)
                                                    .Text($"{sm.Mood.Emoji} {sm.Mood.Name}")
                                                    .FontSize(9);
                                            }
                                        });
                                    }
                                    
                                    // Tags
                                    if (options.IncludeTags && entry.EntryTags.Any())
                                    {
                                        entryColumn.Item().PaddingTop(8).Row(row =>
                                        {
                                            foreach (var tag in entry.EntryTags.Take(5))
                                            {
                                                row.ConstantItem(80).Background(QColors.Green.Lighten4).Padding(4)
                                                    .Text(tag.Tag.Name)
                                                    .FontSize(9)
                                                    .FontColor(QColors.Green.Darken3);
                                                row.ConstantItem(5); // spacer
                                            }
                                        });
                                    }
                                    
                                    // Content
                                    entryColumn.Item().PaddingTop(12).Text(entry.Content ?? "")
                                        .FontSize(11)
                                        .LineHeight(1.5f);
                                });
                            
                            column.Item().PaddingVertical(10);
                        }
                    });
                    
                    // Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Generated by Reflecta • ")
                                .FontSize(9)
                                .FontColor(QColors.Grey.Darken1);
                            text.Span(DateTime.Now.ToString("MMMM d, yyyy"))
                                .FontSize(9)
                                .FontColor(QColors.Grey.Darken1);
                        });
                        
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ")
                                .FontSize(9);
                            text.CurrentPageNumber()
                                .FontSize(9);
                            text.Span(" of ")
                                .FontSize(9);
                            text.TotalPages()
                                .FontSize(9);
                        });
                    });
                });
            });
            
            return document.GeneratePdf();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PDF generation failed: {ex.Message}", ex);
        }
    }

    private byte[] GenerateHtmlExport(List<JournalEntry> entries, ExportOptions options)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        
        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("    <title>Reflecta Journal Export</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: 'Segoe UI', system-ui, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; background: #1a1a1a; color: #e0e0e0; }");
        sb.AppendLine("        h1 { color: #B8F4B8; }");
        sb.AppendLine("        h2 { color: #fff; border-bottom: 1px solid #333; padding-bottom: 10px; }");
        sb.AppendLine("        .entry { background: #2a2a2a; padding: 20px; border-radius: 12px; margin-bottom: 20px; }");
        sb.AppendLine("        .mood { display: inline-block; background: #333; padding: 4px 12px; border-radius: 20px; margin-right: 8px; }");
        sb.AppendLine("        .tag { display: inline-block; background: #B8F4B8; color: #000; padding: 2px 8px; border-radius: 12px; margin-right: 4px; font-size: 0.85em; }");
        sb.AppendLine("        .meta { color: #888; font-size: 0.9em; margin-bottom: 15px; }");
        sb.AppendLine("        .content { line-height: 1.6; }");
        sb.AppendLine("        .word-count { color: #666; font-size: 0.85em; margin-top: 15px; }");
        sb.AppendLine("        .summary { background: #2a2a2a; padding: 15px; border-radius: 12px; margin-bottom: 30px; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <h1>📓 Reflecta Journal Export</h1>");
        sb.AppendLine("    <div class=\"summary\">");
        sb.AppendLine($"        <p><strong>Date Range:</strong> {options.StartDate:MMMM d, yyyy} - {options.EndDate:MMMM d, yyyy}</p>");
        sb.AppendLine($"        <p><strong>Total Entries:</strong> {entries.Count}</p>");
        sb.AppendLine($"        <p><strong>Total Words:</strong> {entries.Sum(e => e.WordCount):N0}</p>");
        sb.AppendLine("    </div>");

        foreach (var entry in entries)
        {
            sb.AppendLine("    <div class=\"entry\">");
            sb.AppendLine($"        <h2>{entry.EntryDate:dddd, MMMM d, yyyy}</h2>");

            if (!string.IsNullOrEmpty(entry.Title))
            {
                sb.AppendLine($"        <h3>{entry.Title}</h3>");
            }

            sb.AppendLine("        <div class=\"meta\">");
            
            if (options.IncludeMoods)
            {
                sb.AppendLine($"            <span class=\"mood\">{entry.PrimaryMood.Emoji} {entry.PrimaryMood.Name}</span>");
                
                foreach (var secondaryMood in entry.SecondaryMoods)
                {
                    sb.AppendLine($"            <span class=\"mood\">{secondaryMood.Mood.Emoji} {secondaryMood.Mood.Name}</span>");
                }
            }

            if (options.IncludeTags && entry.EntryTags.Any())
            {
                sb.AppendLine("            <br><br>");
                foreach (var tag in entry.EntryTags)
                {
                    sb.AppendLine($"            <span class=\"tag\">{tag.Tag.Name}</span>");
                }
            }

            sb.AppendLine("        </div>");
            
            var htmlContent = Markdown.ToHtml(entry.Content, pipeline);
            sb.AppendLine($"        <div class=\"content\">{htmlContent}</div>");

            if (options.IncludeWordCount)
            {
                sb.AppendLine($"        <p class=\"word-count\">{entry.WordCount} words</p>");
            }

            sb.AppendLine("    </div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
