using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class HoaDon
    {
        [Key]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [MaxLength(50)] public string? MaHoaDon { get; set; }
        public DateTime? NgayPhatHanh { get; set; }

        [Required]
        [MaxLength(50)]
        public string LoaiChungTu { get; set; } = "DonTamTinh"; // "DonTamTinh", "HoaDon"

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string TrangThaiThanhToan { get; set; } = "Chưa thanh toán"; // "Chưa thanh toán", "Đã đặt cọc", "Đã thanh toán", "Hoàn thành", "Đã hủy"

        [MaxLength(50)]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt"; // "Tiền mặt", "Chuyển khoản"

        [MaxLength(255)]
        public string? DiaChiGiao { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChiPhiVanChuyen { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienThue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienDatCoc { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TyLeDatCoc { get; set; } = 0; // e.g. 30, 50 (%)

        public DateTime? HanThanhToan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienChietKhau { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTienHang { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongThanhToan { get; set; } = 0; // Tổng phải thu (sau khi tính cước + VAT - CK + CPPS thu thêm - trả/đổi)

        [Column(TypeName = "decimal(18,2)")]
        public decimal DaThu { get; set; } = 0; // Tổng tiền đã thu thực tế (đã bao gồm cọc)

        [Column(TypeName = "decimal(18,2)")]
        public decimal DaHoanKhach { get; set; } = 0; // Tổng tiền đã hoàn trả khách

        public int SoLanGhiDe { get; set; } = 0;

        public bool IsLockedGhiDe { get; set; } = false;

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        // Foreign keys
        [Required]
        [MaxLength(20)]
        public string MaKH { get; set; } = string.Empty;

        [ForeignKey("MaKH")]
        public virtual KhachHang? KhachHang { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }

        public int? MaCK { get; set; }

        [ForeignKey("MaCK")]
        public virtual ChietKhau? ChietKhau { get; set; }

        // Navigations
        public virtual ICollection<CT_HoaDon> ChiTietHoaDons { get; set; } = new List<CT_HoaDon>();
        public virtual ICollection<Phieu> Phieus { get; set; } = new List<Phieu>();
        public virtual ICollection<LanThuTien> LanThuTiens { get; set; } = new List<LanThuTien>();
        public virtual ICollection<DotGiaoHang> DotGiaoHangs { get; set; } = new List<DotGiaoHang>();

        // Business rules calculation properties
        [NotMapped]
        public decimal ThuRong => DaThu - DaHoanKhach;

        [NotMapped]
        public decimal ConNo => Math.Max(TongThanhToan - ThuRong, 0);

        [NotMapped]
        public decimal PhaiHoan => Math.Max(ThuRong - TongThanhToan, 0);
    }
}
