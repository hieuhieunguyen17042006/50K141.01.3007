namespace THomeApp.Helpers;

public static class VietnamTime
{
    public const string IanaId = "Asia/Ho_Chi_Minh";
    public const string WindowsId = "SE Asia Standard Time";
    public static TimeZoneInfo Zone { get; } = FindZone();
    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateTimeOffset FromUtc(DateTime value) => TimeZoneInfo.ConvertTime(
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)), Zone);

    private static TimeZoneInfo FindZone()
    {
        foreach(var id in new[] { IanaId, WindowsId })
            if(TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
        throw new TimeZoneNotFoundException($"Không tìm thấy múi giờ {IanaId} / {WindowsId}.");
    }
}
