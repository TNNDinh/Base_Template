public static class ThemeTypeExtensions
{
    /// <summary>Chuyển ThemeType enum sang tên bundle tương ứng (Scene_01 → "scene_1").</summary>
    public static string ToBundleName(this ThemeType theme)
    {
        var s = theme.ToString();
        var idx = s.IndexOf('_');
        if (idx < 0) return s.ToLower();
        var prefix = s.Substring(0, idx).ToLower();
        var num = int.Parse(s.Substring(idx + 1));
        return $"{prefix}_{num}";
    }
}