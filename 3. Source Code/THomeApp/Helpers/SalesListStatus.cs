using System.Linq.Expressions;
using THomeApp.Models;

namespace THomeApp.Helpers;

// Presentation grouping only: do not replace the document's business status.
public static class SalesListStatus
{
    public const string CompletedLabel = "Đã hoàn thành";
    public const string IncompleteLabel = "Chưa hoàn thành";

    // A paid draft still needs issuance; debt/refunds still need settlement.
    // Use one expression for SQL filtering and the in-memory table labels.
    public static Expression<Func<HoaDon, bool>> Completed { get; } = h =>
        h.LoaiChungTu == "HoaDon" && h.TrangThaiThanhToan == "Hoàn thành"
        && h.DaThu - h.DaHoanKhach == h.TongThanhToan;

    public static Expression<Func<HoaDon, bool>> Incomplete { get; } =
        Expression.Lambda<Func<HoaDon, bool>>(Expression.Not(Completed.Body), Completed.Parameters);

    public static Func<HoaDon, bool> IsCompleted { get; } = Completed.Compile();
}
