using Postgrest.Attributes;
using Postgrest.Models;

[Table("administrator")]
public class AdminModel : BaseModel
{
    /// <summary>
    ///     Id của admin
    /// </summary>
    [PrimaryKey("id")]
    public string Id { get; set; }

    /// <summary>
    ///     Email của admin
    /// </summary>
    [Column("email")]
    public string Email { get; set; }

    /// <summary>
    ///     Có phải là tài khoản test không
    /// </summary>
    [Column("is_test")]
    public bool IsTest { get; set; }
}