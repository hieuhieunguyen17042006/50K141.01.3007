using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class ChietKhau
    {
        [Key]
        public int MaCK { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MocTichLuy { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TyLeChietKhau { get; set; } // 2.00, 3.00 (%)

        public bool DangApDung { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
