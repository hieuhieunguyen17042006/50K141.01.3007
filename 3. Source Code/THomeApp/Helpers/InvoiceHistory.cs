using System.Text.Json;
using THomeApp.Models;
namespace THomeApp.Helpers;

public static class InvoiceHistory
{
    public static decimal? PreviousTotal(HoaDon invoice)
    {
        if(invoice.SoLanGhiDe<=0) return null;
        var revision=invoice.Phieus.Where(p=>p.LoaiPhieu=="GhiDe"&&p.TrangThai=="Hiệu lực"&&p.SoLanGhiDeHienTai==invoice.SoLanGhiDe)
            .OrderByDescending(p=>p.NgayLap).FirstOrDefault();
        if(string.IsNullOrWhiteSpace(revision?.DuLieuGocJson))return null;
        try {using var doc=JsonDocument.Parse(revision.DuLieuGocJson);return doc.RootElement.ValueKind==JsonValueKind.Object&&doc.RootElement.TryGetProperty("TongThanhToan",out var amount)&&amount.ValueKind==JsonValueKind.Number&&amount.TryGetDecimal(out var total)?total:null;}
        catch(JsonException){return null;}
    }
}
