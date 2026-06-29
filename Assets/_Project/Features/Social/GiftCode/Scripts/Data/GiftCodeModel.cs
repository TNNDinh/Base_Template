using Postgrest.Attributes;
using Postgrest.Models;

namespace Ezg.Feature.Social.GiftCode
{
    [Table("gift_codes")]
    public class GiftCodeModel : BaseModel
    {
        /// <summary>
        ///     Mã Gift Code
        /// </summary>
        [PrimaryKey("id", true)]
        public string Id { get; set; }

        /// <summary>
        ///     Loại tài nguyên nhận được
        /// </summary>
        [Column("res_type")]
        public string ResType { get; set; }

        /// <summary>
        ///     Id của tài nguyên (nếu có)
        /// </summary>
        [Column("res_id")]
        public string ResId { get; set; }

        /// <summary>
        ///     Số lượng tài nguyên
        /// </summary>
        [Column("res_number")]
        public string ResNumber { get; set; }

        /// <summary>
        ///     Giá trị tùy chỉnh thêm cho tài nguyên
        /// </summary>
        [Column("res_custom_value")]
        public string ResCustomValue { get; set; }

        /// <summary>
        ///     Giới hạn tổng số lần nhập code này
        /// </summary>
        [Column("limit_times")]
        public long LimitTimes { get; set; }

        /// <summary>
        ///     Số lần code này đã được nhập thành công
        /// </summary>
        [Column("claimed_times")]
        public long ClaimedTimes { get; set; }

        /// <summary>
        ///     Trạng thái kích hoạt của code
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; }

        /// <summary>
        ///     Code có bị giới hạn số lần nhập chung không
        /// </summary>
        [Column("is_limit")]
        public bool IsLimit { get; set; }

        /// <summary>
        ///     Người tạo Gift Code
        /// </summary>
        [Column("created_by")]
        public string CreatedBy { get; set; }

        /// <summary>
        ///     Code có thời gian hết hạn không
        /// </summary>
        [Column("have_expired")]
        public bool HaveExpired { get; set; }

        /// <summary>
        ///     Thời điểm hết hạn (thời gian Unix)
        /// </summary>
        [Column("expired_time")]
        public long ExpiredTime { get; set; }

        /// <summary>
        ///     Giới hạn phiên bản game tối thiểu để nhập code
        /// </summary>
        [Column("version_limit")]
        public string VersionLimit { get; set; }
    }
}