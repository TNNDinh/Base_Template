using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Parser CSV tối giản cho module Battle: dòng đầu = header, mỗi dòng sau = 1 record.
    ///     Bỏ dòng trống và dòng bắt đầu bằng '#'. Không hỗ trợ dấu phẩy trong giá trị (data tự tránh).
    /// </summary>
    public class CsvTable
    {
        private readonly Dictionary<string, int> _cols = new Dictionary<string, int>();
        public readonly List<string[]> Rows = new List<string[]>();

        public CsvTable(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var headerParsed = false;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (raw.TrimStart().StartsWith("#")) continue;

                var cells = raw.Split(',');
                if (!headerParsed)
                {
                    for (int i = 0; i < cells.Length; i++) _cols[cells[i].Trim()] = i;
                    headerParsed = true;
                    continue;
                }

                Rows.Add(cells);
            }
        }

        public int ColIndex(string name) => _cols.TryGetValue(name, out var i) ? i : -1;
    }

    /// <summary>1 dòng dữ liệu, truy cập theo tên cột với ép kiểu an toàn.</summary>
    public struct CsvRow
    {
        private readonly CsvTable _table;
        private readonly string[] _cells;

        public CsvRow(CsvTable table, string[] cells)
        {
            _table = table;
            _cells = cells;
        }

        public string Str(string col)
        {
            var i = _table.ColIndex(col);
            return i >= 0 && i < _cells.Length ? _cells[i].Trim() : "";
        }

        public int Int(string col) => int.TryParse(Str(col), out var v) ? v : 0;

        public float Float(string col) =>
            float.TryParse(Str(col), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

        public bool Bool(string col)
        {
            var s = Str(col).ToLowerInvariant();
            return s == "true" || s == "1";
        }

        public T Enum<T>(string col) where T : struct =>
            global::System.Enum.TryParse<T>(Str(col), true, out var v) ? v : default;
    }
}
