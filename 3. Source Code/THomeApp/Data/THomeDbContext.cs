using Microsoft.EntityFrameworkCore;
using THomeApp.Models;

namespace THomeApp.Data
{
    public class THomeDbContext : DbContext
    {
        public THomeDbContext(DbContextOptions<THomeDbContext> options) : base(options)
        {
        }

        public DbSet<NhanVien> NhanViens { get; set; } = null!;
        public DbSet<KhachHang> KhachHangs { get; set; } = null!;
        public DbSet<SanPham> SanPhams { get; set; } = null!;
        public DbSet<Gia> Gias { get; set; } = null!;
        public DbSet<ChietKhau> ChietKhaus { get; set; } = null!;
        public DbSet<HoaDon> HoaDons { get; set; } = null!;
        public DbSet<CT_HoaDon> CT_HoaDons { get; set; } = null!;
        public DbSet<Phieu> Phieus { get; set; } = null!;
        public DbSet<CT_Phieu> CT_Phieus { get; set; } = null!;
        public DbSet<DotGiaoHang> DotGiaoHangs { get; set; } = null!;
        public DbSet<CT_DotGiaoHang> CT_DotGiaoHangs { get; set; } = null!;
        public DbSet<LanThuTien> LanThuTiens { get; set; } = null!;
        public DbSet<LichSuHoatDong> LichSuHoatDongs { get; set; } = null!;
        public DbSet<YeuCauMoKhoa> YeuCauMoKhoas { get; set; } = null!;
        public DbSet<KiemKeTienMat> KiemKeTienMats { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CT_Phieu>().HasOne<CT_HoaDon>().WithMany()
                .HasForeignKey(c => new { c.MaHDGoc, c.MaSPGoc })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CT_Phieu>().HasOne<CT_Phieu>().WithMany()
                .HasForeignKey(c => c.SourceVoucherLineId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HoaDon>().HasIndex(h => h.MaHoaDon).IsUnique().HasFilter("[MaHoaDon] IS NOT NULL");

            // Composite Key for CT_HoaDon
            modelBuilder.Entity<CT_HoaDon>()
                .HasKey(c => new { c.MaHD, c.MaSP });

            modelBuilder.Entity<CT_HoaDon>()
                .HasOne(c => c.HoaDon)
                .WithMany(h => h.ChiTietHoaDons)
                .HasForeignKey(c => c.MaHD)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CT_HoaDon>()
                .HasOne(c => c.SanPham)
                .WithMany()
                .HasForeignKey(c => c.MaSP)
                .OnDelete(DeleteBehavior.Restrict);

            // HoaDon relationships
            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.NhanVien)
                .WithMany()
                .HasForeignKey(h => h.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.KhachHang)
                .WithMany()
                .HasForeignKey(h => h.MaKH)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.ChietKhau)
                .WithMany()
                .HasForeignKey(h => h.MaCK)
                .OnDelete(DeleteBehavior.SetNull);

            // Phieu & CT_Phieu
            modelBuilder.Entity<CT_Phieu>()
                .HasOne(c => c.Phieu)
                .WithMany(p => p.ChiTietPhieus)
                .HasForeignKey(c => c.MaPhieu)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CT_Phieu>()
                .HasOne(c => c.SanPham)
                .WithMany()
                .HasForeignKey(c => c.MaSP)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Phieu>()
                .HasOne(p => p.HoaDon)
                .WithMany(h => h.Phieus)
                .HasForeignKey(p => p.MaHD)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Phieu>()
                .HasOne(p => p.NhanVien)
                .WithMany()
                .HasForeignKey(p => p.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            // LanThuTien
            modelBuilder.Entity<LanThuTien>()
                .HasOne(l => l.HoaDon)
                .WithMany(h => h.LanThuTiens)
                .HasForeignKey(l => l.MaHD)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LanThuTien>()
                .HasOne(l => l.NhanVienThu)
                .WithMany()
                .HasForeignKey(l => l.MaNVThu)
                .OnDelete(DeleteBehavior.Restrict);

            // DotGiaoHang
            modelBuilder.Entity<DotGiaoHang>()
                .HasOne(d => d.HoaDon)
                .WithMany(h => h.DotGiaoHangs)
                .HasForeignKey(d => d.MaHD)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CT_DotGiaoHang>()
                .HasOne(c => c.DotGiaoHang)
                .WithMany(d => d.ChiTietGiaoHangs)
                .HasForeignKey(c => c.MaDotGiao)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CT_DotGiaoHang>()
                .HasOne(c => c.SanPham)
                .WithMany()
                .HasForeignKey(c => c.MaSP)
                .OnDelete(DeleteBehavior.Restrict);

            // SanPham -> Gia
            modelBuilder.Entity<Gia>()
                .HasOne(g => g.SanPham)
                .WithMany(s => s.LichSuGia)
                .HasForeignKey(g => g.MaSP)
                .OnDelete(DeleteBehavior.Cascade);

            // LichSuHoatDong
            modelBuilder.Entity<LichSuHoatDong>()
                .HasOne(l => l.NhanVien)
                .WithMany()
                .HasForeignKey(l => l.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            // YeuCauMoKhoa
            modelBuilder.Entity<YeuCauMoKhoa>()
                .HasOne(y => y.HoaDon)
                .WithMany()
                .HasForeignKey(y => y.MaHD)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<YeuCauMoKhoa>()
                .HasOne(y => y.NhanVien)
                .WithMany()
                .HasForeignKey(y => y.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<YeuCauMoKhoa>()
                .HasOne(y => y.NhanVienDuyet)
                .WithMany()
                .HasForeignKey(y => y.MaNVDuyet)
                .OnDelete(DeleteBehavior.Restrict);

            // KiemKeTienMat
            modelBuilder.Entity<KiemKeTienMat>()
                .HasOne(k => k.NhanVien)
                .WithMany()
                .HasForeignKey(k => k.MaNV)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique indexes
            modelBuilder.Entity<NhanVien>()
                .HasIndex(n => n.TenDN)
                .IsUnique();

            modelBuilder.Entity<KhachHang>()
                .HasIndex(k => k.SDT)
                .IsUnique();
        }

        public DbSet<RequestOperation> RequestOperations { get; set; } = null!;
    }
}
