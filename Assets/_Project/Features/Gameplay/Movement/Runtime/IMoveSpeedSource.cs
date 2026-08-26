namespace Ezg.Feature.Movement
{
    /// <summary>
    /// Nguồn tốc độ di chuyển, đọc lại mỗi tick.
    /// <para>
    /// Tách riêng để sau này cắm hiệu ứng làm chậm, tăng tốc, hay tốc độ theo cấp quái
    /// mà không phải sửa <see cref="IPathFollower"/>.
    /// </para>
    /// </summary>
    public interface IMoveSpeedSource
    {
        /// <summary>Tốc độ hiện tại, world unit trên giây. Không âm.</summary>
        float CurrentSpeed { get; }
    }
}
