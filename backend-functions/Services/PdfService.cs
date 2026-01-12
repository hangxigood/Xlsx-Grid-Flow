using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Logging;
using XlsxGridFlow.Functions.Models;
using XlsxGridFlow.Functions.Utilities;

namespace XlsxGridFlow.Functions.Services;

/// <summary>
/// Service for generating PDF reports
/// </summary>
public class PdfService
{
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;
        // Set QuestPDF license for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a PDF report for a session
    /// </summary>
    public byte[] GenerateReport(SessionState session)
    {
        _logger.LogInformation("Generating PDF report for session {SessionId}", session.SessionId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text("Xlsx-Grid-Flow Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Cover Page Section
                        column.Item().Element(CoverPage);

                        // Data Table Section
                        column.Item().Element(DataTable);

                        // Audit Trail Section
                        column.Item().PageBreak();
                        column.Item().Element(AuditTrail);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });

            void CoverPage(IContainer container)
            {
                container.Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("Session Metadata").SemiBold().FontSize(16);
                    
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Filename:");
                        row.RelativeItem().Text(session.Filename);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Session ID:");
                        row.RelativeItem().Text(session.SessionId);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Export Date:");
                        row.RelativeItem().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Current Version:");
                        row.RelativeItem().Text(session.Version.ToString());
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total Changes:");
                        row.RelativeItem().Text(session.ChangeLog.Count.ToString());
                    });
                });
            }

            void DataTable(IContainer container)
            {
                container.Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("Current Data State").SemiBold().FontSize(16);

                    column.Item().Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40); // Row ID
                            foreach (var colDef in session.ColumnDefs)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Row").SemiBold();
                            foreach (var colDef in session.ColumnDefs)
                            {
                                header.Cell().Element(CellStyle).Text(colDef.HeaderName).SemiBold();
                            }
                        });

                        // Create a lookup for merged cells
                        var mergedCellLookup = MergedCellHelper.CreateMergedCellLookup(session.MergedCells);

                        // Data rows
                        foreach (var row in session.CurrentSnapshot)
                        {
                            // Row ID column (always rendered)
                            table.Cell().Element(CellStyle).Text(row.RowId.ToString());
                            
                            // Data columns
                            for (int colIndex = 0; colIndex < session.ColumnDefs.Count; colIndex++)
                            {
                                var colDef = session.ColumnDefs[colIndex];
                                var cellKey = $"{row.RowId},{colIndex + 1}"; // colIndex is 0-based, Excel cols are 1-based

                                // Check if this cell is part of a merged range
                                if (mergedCellLookup.TryGetValue(cellKey, out var mergedCell))
                                {
                                    // Only render the top-left cell of a merged range
                                    if (MergedCellHelper.IsTopLeftOfMergedRange(row.RowId, colIndex + 1, mergedCell))
                                    {
                                        var value = row.Cells.TryGetValue(colDef.Field, out var v) 
                                            ? v?.ToString() ?? "" 
                                            : "";

                                        // Apply row and column span
                                        table.Cell()
                                            .RowSpan(MergedCellHelper.GetRowSpan(mergedCell))
                                            .ColumnSpan(MergedCellHelper.GetColumnSpan(mergedCell))
                                            .Element(CellStyle)
                                            .Text(value);
                                    }
                                    // Skip cells that are covered by a merged range
                                }
                                else
                                {
                                    // Normal cell - not merged
                                    var value = row.Cells.TryGetValue(colDef.Field, out var v) 
                                        ? v?.ToString() ?? "" 
                                        : "";
                                    table.Cell().Element(CellStyle).Text(value);
                                }
                            }
                        }
                    });
                });
            }

            void AuditTrail(IContainer container)
            {
                container.Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("Audit Trail").SemiBold().FontSize(16);

                    if (!session.ChangeLog.Any())
                    {
                        column.Item().Text("No changes recorded.");
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);  // Version
                            columns.RelativeColumn(2);   // Timestamp
                            columns.ConstantColumn(50);  // Cell Ref
                            columns.RelativeColumn();    // Old Value
                            columns.RelativeColumn();    // New Value
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Ver").SemiBold();
                            header.Cell().Element(CellStyle).Text("Timestamp").SemiBold();
                            header.Cell().Element(CellStyle).Text("Cell").SemiBold();
                            header.Cell().Element(CellStyle).Text("Old Value").SemiBold();
                            header.Cell().Element(CellStyle).Text("New Value").SemiBold();
                        });

                        // Audit entries (exclude version 0 and 1 - initial states)
                        foreach (var entry in session.ChangeLog
                            .Where(e => e.Version > 1)
                            .OrderBy(e => e.Version)
                            .ThenBy(e => e.Timestamp))
                        {
                            table.Cell().Element(CellStyle).Text(entry.Version.ToString());
                            table.Cell().Element(CellStyle).Text(entry.Timestamp).FontSize(8);
                            table.Cell().Element(CellStyle).Text(entry.CellReference);
                            table.Cell().Element(CellStyle).Text(entry.OldValue?.ToString() ?? "null");
                            table.Cell().Element(CellStyle).Text(entry.NewValue?.ToString() ?? "null");
                        }
                    });
                });
            }

            IContainer CellStyle(IContainer container)
            {
                return container
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(5);
            }
        });

        var pdfBytes = document.GeneratePdf();
        
        _logger.LogInformation("Generated PDF report ({Size} bytes) for session {SessionId}", 
            pdfBytes.Length, session.SessionId);

        return pdfBytes;
    }
}
