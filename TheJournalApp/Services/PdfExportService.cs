using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TheJournalApp.Data.Entities;
using Colors = QuestPDF.Helpers.Colors;

namespace TheJournalApp.Services;

public class PdfExportService
{
    public byte[] GenerateJournalPdf(List<JournalEntry> entries, DateOnly startDate, DateOnly endDate, string username)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .Text($"Journal Export: {username}")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        x.Item().Text($"From {startDate} to {endDate}")
                            .FontSize(14).FontColor(Colors.Grey.Medium);

                        foreach (var entry in entries)
                        {
                            x.Item().Component(new JournalEntryComponent(entry));
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        });

        return document.GeneratePdf();
    }
}

public class JournalEntryComponent : QuestPDF.Infrastructure.IComponent
{
    private readonly JournalEntry _entry;

    public JournalEntryComponent(JournalEntry entry)
    {
        _entry = entry;
    }

    public void Compose(QuestPDF.Infrastructure.IContainer container)
    {
        container.Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(5);

                // Date and Mood
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text($"{_entry.EntryDate:dddd, MMMM d, yyyy}").Bold();
                    row.AutoItem().Text($"{_entry.PrimaryMood?.Emoji} {_entry.PrimaryMood?.Name}").FontSize(14);
                });

                // Tags
                if (_entry.EntryTags != null && _entry.EntryTags.Any())
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Tags: ").Bold();
                        text.Span(string.Join(", ", _entry.EntryTags.Select(et => et.Tag.Name)));
                    });
                }

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Content
                column.Item().Text(_entry.Content);
            });
    }
}
