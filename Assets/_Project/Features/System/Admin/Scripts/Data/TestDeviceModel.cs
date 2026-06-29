using Postgrest.Attributes;
using Postgrest.Models;

[Table("test_device")]
public class TestDeviceModel : BaseModel
{
    /// <summary>
    ///     Mã định danh thiết bị test
    /// </summary>
    [Column("device")]
    public string Device { get; set; }
}