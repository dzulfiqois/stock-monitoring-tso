using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace StockMonitorTso.Infrastructure.Excel;

/// <summary>
/// Reader .xlsx minimal menggunakan stdlib (ZIP + XML) — tanpa openpyxl/pandas.
/// Baca: sharedStrings + worksheet, hasil sebagai Dictionary[refCell → nilai string].
/// </summary>
public static class XlsxReader
{
    private const string NsSpreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string NsRelationship = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string NsPackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static IReadOnlyDictionary<string, string> ReadSheet(string filePath, string sheetName)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);

        var workbook = LoadXml(archive, "xl/workbook.xml");
        var sheetEntry = workbook.Descendants(XName.Get("sheet", NsSpreadsheet))
            .FirstOrDefault(s => (string?)s.Attribute("name") == sheetName)
            ?? throw new InvalidOperationException($"Sheet '{sheetName}' tidak ditemukan.");
        var rid = (string?)sheetEntry.Attribute(XName.Get("id", NsRelationship))
            ?? throw new InvalidOperationException("Sheet tanpa r:id.");

        var workbookRels = LoadXml(archive, "xl/_rels/workbook.xml.rels");
        var relationship = workbookRels.Descendants(XName.Get("Relationship", NsPackageRel))
            .First(r => (string?)r.Attribute("Id") == rid);
        var target = (string?)relationship.Attribute("Target")
            ?? throw new InvalidOperationException("Relasi tanpa Target.");
        var sheetPath = target.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : "xl/" + target;

        var shared = ReadSharedStrings(archive);
        var cells = ParseCells(archive, sheetPath, shared);
        return cells;
    }

    private static XDocument LoadXml(System.IO.Compression.ZipArchive archive, string entryPath)
    {
        using var stream = archive.GetEntry(entryPath)?.Open()
            ?? throw new InvalidOperationException($"Entry '{entryPath}' tidak ada.");
        return XDocument.Load(stream, LoadOptions.None);
    }

    private static IReadOnlyList<string> ReadSharedStrings(System.IO.Compression.ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var doc = XDocument.Load(stream, LoadOptions.None);
        var strings = new List<string>();
        foreach (var si in doc.Descendants(XName.Get("si", NsSpreadsheet)))
        {
            var text = new StringBuilder();
            foreach (var t in si.Descendants(XName.Get("t", NsSpreadsheet)))
            {
                text.Append(t.Value);
            }

            strings.Add(text.ToString());
        }

        return strings;
    }

    private static Dictionary<string, string> ParseCells(
        System.IO.Compression.ZipArchive archive,
        string sheetPath,
        IReadOnlyList<string> shared)
    {
        using var stream = archive.GetEntry(sheetPath)?.Open()
            ?? throw new InvalidOperationException($"Sheet entry '{sheetPath}' tidak ada.");
        var doc = XDocument.Load(stream, LoadOptions.None);
        var cells = new Dictionary<string, string>();

        foreach (var c in doc.Descendants(XName.Get("c", NsSpreadsheet)))
        {
            var reference = (string?)c.Attribute("reference") ?? (string?)c.Attribute("r");
            if (reference is null)
            {
                continue;
            }

            var type = (string?)c.Attribute("t");
            var valueElement = c.Descendants(XName.Get("v", NsSpreadsheet)).FirstOrDefault();
            if (valueElement is null)
            {
                continue;
            }

            var raw = valueElement.Value;
            var value = type switch
            {
                "s" => int.TryParse(raw, out var index) && index >= 0 && index < shared.Count ? shared[index] : raw,
                "b" => raw == "1" ? "true" : "false",
                _ => raw,
            };

            cells[reference] = NormalizeNumber(value);
        }

        return cells;
    }

    private static string NormalizeNumber(string raw)
    {
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            return raw.Replace(',', '.');
        }

        return raw;
    }
}
