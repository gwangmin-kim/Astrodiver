namespace Astrodiver.Editor.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper;

    /// <summary>Uses the same CSV parser as Unity Localization (quoted multiline cells included).</summary>
    public sealed class TextCsvDocument
    {
        public sealed class Row
        {
            public int Number;
            public string Key;
            public long Id;
            public Dictionary<string, string> Cells;
        }

        public string[] Headers { get; private set; }
        public List<Row> Rows { get; } = new();

        public static TextCsvDocument Parse(string text)
        {
            using var input = new StringReader(text.TrimStart('\uFEFF'));
            using var reader = new CsvReader(input, CultureInfo.InvariantCulture);
            if (!reader.Read()) throw new FormatException("CSV is empty.");
            reader.ReadHeader();
            var document = new TextCsvDocument { Headers = reader.Context.HeaderRecord };
            if (document.Headers.Any(string.IsNullOrWhiteSpace) ||
                document.Headers.Distinct(StringComparer.Ordinal).Count() != document.Headers.Length)
                throw new FormatException("CSV has empty or duplicate headers.");
            foreach (string required in new[] { "Key", "Id", "Comment", "Smart" })
                if (!document.Headers.Contains(required)) throw new FormatException($"Missing column: {required}");

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<long>();
            int number = 1;
            while (reader.Read())
            {
                number++;
                if (reader.Context.Record.Length != document.Headers.Length)
                    throw new FormatException($"Row {number}: column count does not match header.");
                var cells = document.Headers.ToDictionary(h => h, h => reader.GetField(h), StringComparer.Ordinal);
                string key = cells["Key"];
                if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || !keys.Add(key))
                    throw new FormatException($"Row {number}: empty, padded or duplicate Key '{key}'.");
                long id = 0;
                if (!string.IsNullOrEmpty(cells["Id"]) &&
                    (!long.TryParse(cells["Id"], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id < 0))
                    throw new FormatException($"Row {number}: Id must be a nonnegative integer stored as text.");
                if (id != 0 && !ids.Add(id)) throw new FormatException($"Row {number}: duplicate Id {id}.");
                if (!bool.TryParse(cells["Smart"], out _))
                    throw new FormatException($"Row {number}: Smart must be true or false.");
                document.Rows.Add(new Row { Number = number, Key = key, Id = id, Cells = cells });
            }
            return document;
        }

        public string Write()
        {
            using var output = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = new CsvWriter(output, CultureInfo.InvariantCulture))
            {
                foreach (string header in Headers) writer.WriteField(header);
                writer.NextRecord();
                foreach (Row row in Rows)
                {
                    foreach (string header in Headers)
                        writer.WriteField(header == "Id" ? row.Id.ToString(CultureInfo.InvariantCulture) :
                            header == "Key" ? row.Key : row.Cells[header]);
                    writer.NextRecord();
                }
            }
            return output.ToString();
        }
    }
}
