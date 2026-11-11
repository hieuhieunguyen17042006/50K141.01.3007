using System.Globalization;
using THomeApp.Services;

namespace THomeApp.Models;

public sealed class RevenueReport
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string Period => From == To ? $"ngày {From:dd/MM/yyyy}" : $"từ {From:dd/MM/yyyy} đến {To:dd/MM/yyyy}";
    public decimal Gross { get; init; }
    public decimal Discount { get; init; }
    public decimal Cost { get; init; }
    public decimal AfterDiscount => Gross - Discount;
    public decimal Net => AfterDiscount - Cost;
    public decimal Cash { get; init; }
    public decimal Bank { get; init; }
    public decimal Debt { get; init; }
    public decimal Vat { get; init; }
    public int LegacyWithoutReceipts { get; init; }
    public bool HasData { get; init; }
    public List<HoaDon> Invoices { get; init; } = [];
    public List<LanThuTien> Receipts { get; init; } = [];
    public List<Phieu> Vouchers { get; init; } = [];
    public static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
    public List<(string Label, decimal Amount)> Summary =>
    [
        ("Tổng doanh thu", Gross), ("Chiết khấu", Discount), ("Chi phí", Cost), ("Doanh thu thuần", Net),
        ("Tổng tiền mặt", Cash), ("Tổng tiền chuyển khoản", Bank), ("Công nợ phải thu", Debt),
        ("Tổng doanh thu sau chiết khấu", AfterDiscount), ("VAT của hóa đơn phát hành trong kỳ", Vat)
    ];
    public const string Basis = "Doanh thu ghi nhận khi phát hành hóa đơn (chưa VAT), gồm phí thu thêm và điều chỉnh đổi trả có hiệu lực trong kỳ. Chi phí là khoản cửa hàng chi. Tiền mặt/chuyển khoản là tiền thực thu trong kỳ, gồm cọc đã xác nhận, trừ tiền thực hoàn; không ghi nhận cọc thêm vào doanh thu. Công nợ tính từ các đơn đã phát hành hoặc đã thu tiền, với các lần thu/hoàn/đổi trả đến cuối kỳ. Các chỉ tiêu có cơ sở thời gian khác nhau nên không cộng tiền thu và công nợ thành doanh thu. Giá trị hóa đơn/chiết khấu dùng phiên bản đang lưu; hệ thống chưa có sổ chốt để tái dựng đầy đủ lịch sử ghi đè/hủy.";
}

public sealed class ProductStatistics
{
    public string Criterion { get; init; } = "ca2";
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public List<ProductSale> ByRevenue { get; init; } = [];
    public List<ProductSale> ByQuantity { get; init; } = [];
}

public sealed record ProductRanking(string Title, string Key, List<ProductSale> Rows);
