using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

/// <summary>
///     Cầu nối tên hiển thị cho <see cref="Resource" />. Tách khỏi model lõi (Ezg.Core) để
///     model không phụ thuộc EnumBase; extension này sống ở project (Assembly-CSharp).
/// </summary>
public static class ResourceNameExtensions
{
    /// <summary>Tên hiển thị của resType theo catalog EnumBase.ResourceTypes của project.</summary>
    public static string GetName(this Resource resource)
    {
        return EnumBase.ResourceTypes.GetName(resource.resType);
    }
}