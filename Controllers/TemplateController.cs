using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using LMSDashboard.Models;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/template")]
public class TemplateController : ControllerBase
{
    /// <summary>Download a pre-formatted Excel upload template with valid enum values pre-filled as a reference sheet.</summary>
    [HttpGet("download")]
    public IActionResult DownloadTemplate()
    {
        using var wb = new XLWorkbook();

        // ── Sheet 1: Template (the sheet users fill in) ───────────────────
        var ws = wb.Worksheets.Add("ContentItems");

        // Headers
        var headers = new[] { "Title", "Type", "Track", "Difficulty", "Notes" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // 3 sample rows to guide the user
        var samples = new (string Title, string Type, string Track, string Difficulty, string Notes)[]
        {
            ("Introduction to Python Variables", "Reading", "Foundation", "Easy", "Week 1 content"),
            ("Python Functions Quiz", "Quiz", "B1", "Medium", ""),
            ("Advanced OOP Lecture", "PPT", "Advanced", "Hard", "Slides from May session"),
        };

        for (int r = 0; r < samples.Length; r++)
        {
            var (title, type, track, diff, notes) = samples[r];
            ws.Cell(r + 2, 1).Value = title;
            ws.Cell(r + 2, 2).Value = type;
            ws.Cell(r + 2, 3).Value = track;
            ws.Cell(r + 2, 4).Value = diff;
            ws.Cell(r + 2, 5).Value = notes;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        // ── Sheet 2: Valid Values (read-only reference) ───────────────────
        var ref_ws = wb.Worksheets.Add("Valid Values (Reference)");

        void WriteRefColumn(int col, string heading, IEnumerable<string> values)
        {
            ref_ws.Cell(1, col).Value = heading;
            ref_ws.Cell(1, col).Style.Font.Bold = true;
            ref_ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
            ref_ws.Cell(1, col).Style.Font.FontColor = XLColor.White;
            int row = 2;
            foreach (var v in values)
                ref_ws.Cell(row++, col).Value = v;
        }

        WriteRefColumn(1, "Type", Enum.GetNames<ContentType>());
        WriteRefColumn(2, "Track", Enum.GetNames<Track>());
        WriteRefColumn(3, "Difficulty", Enum.GetNames<Difficulty>());
        ref_ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "LMS_Upload_Template.xlsx");
    }
}
