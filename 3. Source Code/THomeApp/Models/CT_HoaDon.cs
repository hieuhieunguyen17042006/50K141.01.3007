using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class CT_HoaDon
    {
        [Key, Column(Order = 0)]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [Key, Column(Order = 1)]
        [MaxLength(50)]
        public string MaSP { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGiaBan { get; set; }

        [Required]
        public int SoLuong { get; set; }

        public int SoLuongDaTra { get; set; } = 0;

        [ForeignKey("MaHD")]
        public virtual HoaDon? HoaDon { get; set; }

        [ForeignKey("MaSP")]
        public virtual SanPham? SanPham { get; set; }

        [NotMapped]
        public decimal ThanhTien => DonGiaBan * SoLuong;

        [NotMapped]
        public int SoLuongConLai => Math.Max(SoLuong - SoLuongDaTra, 0);
    }
}
