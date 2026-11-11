using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class Phieu
    {
        [Key]
        [MaxLength(50)]
        public string MaPhieu { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LoaiPhieu { get; set; } = string.Empty; // "ChiPhiPhatSinh", "DoiHang", "TraHang", "GhiDe", "ThuTien"

        public DateTime NgayLap { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(500)]
        public string LyDo { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; } = 0;

        [MaxLength(50)]
        public string? LoaiChiPhiPhatSinh { get; set; } // "ThuThemKhach", "CuaHangChi"

        [MaxLength(50)]
        public string? HinhThucXuLyDoiTra { get; set; } // "CanTruCongNo", "HoanTienTrucTiep", "KhachTraThem"

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienChenhLech { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienThucHoan { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")] public decimal TienCanTru { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal TienPhaiHoan { get; set; }

        [MaxLength(50)]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt"; // "Tiền mặt", "Chuyển khoản"

        public int SoLanGhiDeHienTai { get; set; } = 0;

        public string? DuLieuGocJson { get; set; }

        public string? DuLieuMoiJson { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        [MaxLength(50)]
        public string TrangThai { get; set; } = "Hiệu lực"; // "Hiệu lực", "Đã hủy"

        [Required]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [ForeignKey("MaHD")]
        public virtual HoaDon? HoaDon { get; set; }

        public virtual ICollection<CT_Phieu> ChiTietPhieus { get; set; } = new List<CT_Phieu>();
    }
}
