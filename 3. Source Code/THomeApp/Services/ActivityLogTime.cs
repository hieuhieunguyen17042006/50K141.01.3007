using THomeApp.Helpers;

namespace THomeApp.Services;

// datetime2 has no offset/Kind. Its storage convention must be explicit, never guessed
// from the web host's clock, browser timezone or an individual row's hour of day.
public sealed class ActivityLogTime
{
    private readonly TimeProvider clock;
    public bool StoresUtc { get; }

    public ActivityLogTime(IConfiguration configuration, TimeProvider? clock = null)
    {
        var storage = configuration["ActivityLog:StorageTimeZone"]?.Trim() ?? VietnamTime.IanaId;
        StoresUtc = string.Equals(storage, "UTC", StringComparison.OrdinalIgnoreCase);
        if(!StoresUtc && storage != VietnamTime.IanaId && storage != VietnamTime.WindowsId)
            throw new InvalidOperationException("ActivityLog:StorageTimeZone phải là UTC, Asia/Ho_Chi_Minh hoặc SE Asia Standard Time, đúng với dữ liệu đã lưu.");
        this.clock = clock ?? TimeProvider.System;
    }

    public DateTime StorageNow => DateTime.SpecifyKind(StoresUtc ? clock.GetUtcNow().UtcDateTime
        : TimeZoneInfo.ConvertTime(clock.GetUtcNow(), VietnamTime.Zone).DateTime, DateTimeKind.Unspecified);

    public DateTimeOffset ToVietnam(DateTime stored) => StoresUtc || stored.Kind == DateTimeKind.Utc
        ? VietnamTime.FromUtc(stored)
        : new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Unspecified), VietnamTime.Zone.GetUtcOffset(DateTime.SpecifyKind(stored, DateTimeKind.Unspecified)));

    public DateTime VietnamDayStartForStorage(DateOnly day)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return StoresUtc ? DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(midnight, VietnamTime.Zone), DateTimeKind.Unspecified) : midnight;
    }

    public DateTime? AfterVietnamDayForStorage(DateOnly day) => day == DateOnly.MaxValue ? null : VietnamDayStartForStorage(day.AddDays(1));
}
