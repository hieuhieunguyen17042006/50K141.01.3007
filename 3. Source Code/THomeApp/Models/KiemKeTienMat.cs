using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class KiemKeTienMat
    {
        [Key]
        public int Id { get; set; }

        public DateTime NgayKiemKe { get; set; } = DateTime.Today;

        [Required]
        [MaxLength(50)]
        public string CaLamViec { get; set; } = "Cuối ngày"; // "Ca sáng", "Ca chiều", "Cuối ngày"

        [Required]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienMatPhanMem { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TienMatKiemDem { get; set; }

        [NotMapped]
        public decimal ChenhLech => TienMatKiemDem - TienMatPhanMem;

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
