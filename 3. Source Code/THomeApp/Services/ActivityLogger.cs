using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using THomeApp.Data;
using THomeApp.Models;

namespace THomeApp.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(string loaiDoiTuong, string noiDung, string? lyDo = null, string? maNV = null, string? duLieuThayDoiJson = null);
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly THomeDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(THomeDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string loaiDoiTuong, string noiDung, string? lyDo = null, string? maNV = null, string? duLieuThayDoiJson = null)
        {
                if (string.IsNullOrEmpty(maNV))
                {
                    maNV = _httpContextAccessor.HttpContext?.User?.FindFirstValue("MaNV") ?? "SYSTEM";
                }

                var log = new LichSuHoatDong
                {
                    ThoiGianThaoTac = DateTime.Now,
                    LoaiDoiTuong = loaiDoiTuong,
                    NoiDungThaoTac = noiDung,
                    LyDo = lyDo,
                    MaNV = maNV,
                    DuLieuThayDoiJson = duLieuThayDoiJson
                };

                _context.LichSuHoatDongs.Add(log);
                await _context.SaveChangesAsync();
        }
    }
}
