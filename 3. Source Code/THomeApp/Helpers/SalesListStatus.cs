using System.Linq.Expressions;
using THomeApp.Models;

namespace THomeApp.Helpers;

// Draft payment presentation only. Issuance and delivery remain separate business states.
public static class SalesListStatus
{
    public const string PaidLabel = "Đã thanh toán";
    public const string UnpaidLabel = "Chưa thanh toán";

    // One expression serves both SQL filtering and the labels. Payment never issues a draft.
    public static Expression<Func<HoaDon, bool>> Paid { get; } = h =>
        h.TrangThaiThanhToan != "Đã hủy" && h.TongThanhToan > 0
        && h.DaThu - h.DaHoanKhach >= h.TongThanhToan;

    public static Expression<Func<HoaDon, bool>> Unpaid { get; } =
        Expression.Lambda<Func<HoaDon, bool>>(Expression.Not(Paid.Body), Paid.Parameters);

    public static Func<HoaDon, bool> IsPaid { get; } = Paid.Compile();
}
