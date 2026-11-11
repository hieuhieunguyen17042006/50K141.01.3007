using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class Gia
    {
        [Key]
        public int MaGia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        public DateTime NgayApDung { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string MaSP { get; set; } = string.Empty;

        [ForeignKey("MaSP")]
        public virtual SanPham? SanPham { get; set; }
    }
}
