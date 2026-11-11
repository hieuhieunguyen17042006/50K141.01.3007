using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace THomeApp.Models
{
    public class SanPham
    {
        [Key]
        [MaxLength(50)]
        public string MaSP { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TenSP { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DanhMuc { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DonViTinh { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual ICollection<Gia> LichSuGia { get; set; } = new List<Gia>();
    }
}
