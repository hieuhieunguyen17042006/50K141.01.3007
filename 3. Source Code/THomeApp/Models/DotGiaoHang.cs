using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class DotGiaoHang
    {
        [Key]
        public int MaDotGiao { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [ForeignKey("MaHD")]
        public virtual HoaDon? HoaDon { get; set; }

        public int DotThu { get; set; } = 1;

        public DateTime NgayGiao { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? NguoiGiao { get; set; }

        [Required]
        [MaxLength(50)]
        public string TrangThaiGiao { get; set; } = "Chờ giao"; // "Chờ giao", "Đang giao", "Đã giao thành công"

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public virtual ICollection<CT_DotGiaoHang> ChiTietGiaoHangs { get; set; } = new List<CT_DotGiaoHang>();
    }

    public class CT_DotGiaoHang
    {
        [Key]
        public int Id { get; set; }

        public int MaDotGiao { get; set; }

        [ForeignKey("MaDotGiao")]
        public virtual DotGiaoHang? DotGiaoHang { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaSP { get; set; } = string.Empty;

        [ForeignKey("MaSP")]
        public virtual SanPham? SanPham { get; set; }

        [Required]
        public int SoLuongGiao { get; set; }
    }
}
