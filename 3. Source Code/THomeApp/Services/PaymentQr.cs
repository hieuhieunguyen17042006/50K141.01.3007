using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace THomeApp.Services;
public class PaymentQr(IConfiguration config)
{
    public string BankBin => config["Payment:BankBin"]?.Trim() ?? "";
    public string AccountNumber => config["Payment:AccountNumber"]?.Trim() ?? "";
    public string AccountName => config["Payment:AccountName"]?.Trim() ?? "";
    public bool IsConfigured => Regex.IsMatch(BankBin, @"\A[0-9]{6}\z") && Regex.IsMatch(AccountNumber, @"\A[0-9]{6,19}\z");
    public bool CanPay(decimal amount) => IsConfigured && amount > 0 && amount == decimal.Truncate(amount) && amount <= 9_999_999_999_999m;
    public string Label => LabelFor(1);
    public string LabelFor(decimal amount) => CanPay(amount) ? "Mã QR thanh toán" : "QR tra cứu nội bộ";
    // VietQR memo supports at most 25 characters. Preserve the unique code suffix.
    public string TransferContent(string invoice) => invoice.Length <= 25 ? invoice : "TH" + invoice[^23..];
    public string Payload(string invoice, decimal amount, string lookupUrl)
    {
        if (!CanPay(amount)) return lookupUrl;
        string Field(string tag, string value) => tag + Encoding.UTF8.GetByteCount(value).ToString("D2") + value;
        var beneficiary = Field("00", BankBin) + Field("01", AccountNumber);
        var payload = "000201010212" + Field("38", Field("00", "A000000727") + Field("01", beneficiary) + Field("02", "QRIBFTTA"))
            + "5303704" + Field("54", amount.ToString("0", CultureInfo.InvariantCulture)) + "5802VN" + Field("62", Field("08", TransferContent(invoice))) + "6304";
        ushort crc = 0xffff;
        foreach (var b in Encoding.UTF8.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return payload + crc.ToString("X4");
    }
}
