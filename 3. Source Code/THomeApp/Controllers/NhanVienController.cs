using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Helpers;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers
{
    [Authorize]
    public class NhanVienController : Controller
    {
        private readonly THomeDbContext _context;
        private readonly IActivityLogger _logger;

        public NhanVienController(THomeDbContext context, IActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // Chỉ Chủ cửa hàng được quản lý nhân viên
        private bool IsOwner() => User.FindFirstValue(ClaimTypes.Role) == "Chủ cửa hàng";
        private string CurrentMaNV() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // ==========================================
        // DANH SÁCH NHÂN VIÊN (UC_6.1)
        // ==========================================
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(string? search = null, string? vaiTro = null, string? trangThai = null)
        {
            if (!IsOwner())
                return Forbid();

            var query = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(nv =>
                    nv.HoTen.ToLower().Contains(s) ||
                    nv.TenDN.ToLower().Contains(s) ||
                    (nv.SDT != null && nv.SDT.ToLower().Contains(s)) ||
                    (nv.CCCD != null && nv.CCCD.ToLower().Contains(s))
                );
            }

            if (!string.IsNullOrWhiteSpace(vaiTro) && vaiTro != "Tất cả")
                query = query.Where(nv => nv.ViTri == vaiTro);

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả")
                query = query.Where(nv => nv.TrangThai == trangThai);

            var list = await query.OrderBy(nv => nv.MaNV).ToListAsync();

            ViewBag.Search = search;
            ViewBag.VaiTro = vaiTro;
            ViewBag.TrangThai = trangThai;

            return View(list);
        }

        // ==========================================
        // HỘP THOẠI HỒ SƠ (UC_6.1, UC_6.2)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuThongTin(EmployeeProfileInput model, string? id = null)
        {
            if (!IsOwner()) return Forbid();
            var employee = string.IsNullOrWhiteSpace(id) ? null : await _context.NhanViens.FindAsync(id);
            if (!string.IsNullOrWhiteSpace(id) && employee == null) return NotFound();
            var isNew = employee == null;
            var isExistingOwner = employee?.ViTri == "Chủ cửa hàng";
            if (isExistingOwner)
            {
                if (model.ViTri != "Chủ cửa hàng") ModelState.AddModelError("ViTri", "Không thể thay đổi quyền Chủ cửa hàng qua hồ sơ nhân viên.");
            }
            else if (model.ViTri is not ("Nhân viên bán hàng" or "Kế toán bán hàng"))
                ModelState.AddModelError("ViTri", "Vui lòng chọn Bán hàng hoặc Kế toán.");
            await ValidateProfile(model.HoTen, model.SDT, model.CCCD, id);
            if (!ModelState.IsValid) return ProfileErrors();

            if (isNew)
            {
                var code = await NextEmployeeCode(matchLogin: true);
                // Keep existing account roles and password hashing. The one-time
                // credential is sent via encrypted TempData, never audit/replay JSON.
                var username = code.ToLowerInvariant();
                var password = "Nv@" + Convert.ToHexString(RandomNumberGenerator.GetBytes(10)).ToLowerInvariant();
                employee = new NhanVien { MaNV = code, TenDN = username, MatKhau = PasswordHelper.HashPassword(password), NgayTao = DateTime.Now, TrangThai = "Đang hoạt động" };
                _context.NhanViens.Add(employee);
                TempData["EmployeeAccountUsername"] = username;
                TempData["EmployeeAccountPassword"] = password;
            }
            employee!.HoTen = model.HoTen.Trim();
            employee.SDT = model.SDT;
            employee.CCCD = model.CCCD;
            employee.DiaChi = model.DiaChi?.Trim();
            employee.ViTri = model.ViTri;
            await _context.SaveChangesAsync();
            await _logger.LogAsync("NhanVien", $"{(isNew ? "Thêm" : "Sửa")} thông tin nhân viên {employee.HoTen} ({employee.MaNV})", null, CurrentMaNV());
            TempData["EmployeeSuccess"] = isNew ? "Thêm thành công." : "Sửa thành công.";
            return Json(new { success = true, message = isNew ? "Thêm thành công." : "Sửa thành công.", id = employee.MaNV, accountCreated = isNew });
        }

        [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ThongTin(string id)
        {
            if (!IsOwner()) return Forbid();
            var employee = await _context.NhanViens.AsNoTracking().Where(n => n.MaNV == id)
                .Select(n => new { n.MaNV, n.HoTen, n.SDT, n.CCCD, n.ViTri, n.DiaChi, n.TrangThai }).FirstOrDefaultAsync();
            return employee == null ? NotFound() : Json(employee);
        }

        private async Task ValidateProfile(string? name, string? phone, string? identity, string? exceptId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("HoTen", "Vui lòng nhập họ và tên nhân viên.");
            if (string.IsNullOrEmpty(phone) || !Regex.IsMatch(phone, @"\A0[0-9]{9}\z"))
                ModelState.AddModelError("SDT", "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng 0.");
            if (string.IsNullOrEmpty(identity) || !Regex.IsMatch(identity, @"\A[0-9]{12}\z"))
                ModelState.AddModelError("CCCD", "CCCD phải gồm đúng 12 chữ số.");
            else if (await _context.NhanViens.AnyAsync(n => n.CCCD == identity && n.MaNV != exceptId))
                ModelState.AddModelError("CCCD", "CCCD đã tồn tại trong danh sách nhân viên.");
        }

        private JsonResult ProfileErrors() => Json(new {
            success = false, message = "Thông tin không hợp lệ!",
            errors = ModelState.Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
        });

        private async Task<string> NextEmployeeCode(bool matchLogin = false)
        {
            var codes = await _context.NhanViens.Select(n => n.MaNV).ToListAsync();
            var next = codes.Where(c => c.StartsWith("NV")).Select(c => int.TryParse(c[2..], out var number) ? number : 0).DefaultIfEmpty(0).Max() + 1;
            if (matchLogin)
            {
                var login = $"nv{next:D3}";
                while (await _context.NhanViens.AnyAsync(n => n.TenDN == login))
                    login = $"nv{++next:D3}";
            }
            return $"NV{next:D3}";
        }

        // ==========================================
        // CẤP TÀI KHOẢN THỦ CÔNG HIỆN CÓ
        // ==========================================
        [HttpGet]
        public IActionResult Them()
        {
            if (!IsOwner()) return Forbid();
            return View(new NhanVien());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Them(NhanVien model, string MatKhauMoi)
        {
            if (!IsOwner()) return Forbid();
            ModelState.Remove("MaNV");
            ModelState.Remove("MatKhau");
            if (model.ViTri != "Chủ cửa hàng" && model.ViTri != "Kế toán bán hàng" && model.ViTri != "Nhân viên bán hàng")
                ModelState.AddModelError("ViTri", "Vai trò không hợp lệ.");

            // Validate mã NV tự động
            var now = DateTime.Now;
            model.MaNV = await NextEmployeeCode();
            await ValidateProfile(model.HoTen, model.SDT, model.CCCD);

            // Kiểm tra trùng tên đăng nhập (BR-UC1-03: ≤10 ký tự)
            if (string.IsNullOrWhiteSpace(model.TenDN) || model.TenDN.Length > 10)
            {
                ModelState.AddModelError("TenDN", "Tên đăng nhập phải từ 1–10 ký tự.");
            }
            else if (await _context.NhanViens.AnyAsync(nv => nv.TenDN == model.TenDN))
            {
                ModelState.AddModelError("TenDN", "Tên đăng nhập đã tồn tại.");
            }

            // Kiểm tra mật khẩu (BR-UC1-02: >8 ký tự, chữ hoa, chữ thường, ký tự đặc biệt)
            var (pwdOk, pwdMsg) = PasswordHelper.ValidatePasswordComplexity(MatKhauMoi);
            if (!pwdOk)
            {
                ModelState.AddModelError("MatKhau", pwdMsg);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ErrorTenDN = ModelState["TenDN"]?.Errors.FirstOrDefault()?.ErrorMessage;
                ViewBag.ErrorMatKhau = ModelState["MatKhau"]?.Errors.FirstOrDefault()?.ErrorMessage;
                return View(model);
            }

            model.MatKhau = PasswordHelper.HashPassword(MatKhauMoi);
            model.NgayTao = now;
            model.TrangThai = "Đang hoạt động";

            _context.NhanViens.Add(model);
            await _context.SaveChangesAsync();

            await _logger.LogAsync("NhanVien", $"Thêm nhân viên {model.HoTen} ({model.MaNV})", null, CurrentMaNV());
            TempData["Success"] = $"Đã thêm nhân viên {model.HoTen} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // SỬA NHÂN VIÊN (UC_6.2)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Sua(string id)
        {
            if (!IsOwner()) return Forbid();
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();
            return View(nv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sua(string id, NhanVien model, string? MatKhauMoi)
        {
            if (!IsOwner()) return Forbid();
            ModelState.Remove("MaNV");
            ModelState.Remove("MatKhau");
            if (model.ViTri != "Chủ cửa hàng" && model.ViTri != "Kế toán bán hàng" && model.ViTri != "Nhân viên bán hàng")
                ModelState.AddModelError("ViTri", "Vai trò không hợp lệ.");
            if (id == CurrentMaNV() && model.ViTri != "Chủ cửa hàng")
                ModelState.AddModelError("ViTri", "Không thể tự bỏ quyền Chủ cửa hàng.");

            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            // Validate tên đăng nhập nếu thay đổi
            await ValidateProfile(model.HoTen, model.SDT, model.CCCD, id);
            if (model.TenDN != nv.TenDN)
            {
                if (string.IsNullOrWhiteSpace(model.TenDN) || model.TenDN.Length > 10)
                    ModelState.AddModelError("TenDN", "Tên đăng nhập phải từ 1–10 ký tự.");
                else if (await _context.NhanViens.AnyAsync(n => n.TenDN == model.TenDN && n.MaNV != id))
                    ModelState.AddModelError("TenDN", "Tên đăng nhập đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(MatKhauMoi))
            {
                var (pwdOk2, pwdMsg2) = PasswordHelper.ValidatePasswordComplexity(MatKhauMoi);
                if (!pwdOk2)
                    ModelState.AddModelError("MatKhauMoi", pwdMsg2);
            }

            if (!ModelState.IsValid)
            {
                model.MaNV = id;
                return View(model);
            }

            // Cập nhật chỉ các trường được phép sửa
            nv.HoTen = model.HoTen;
            nv.SDT = model.SDT;
            nv.DiaChi = model.DiaChi;
            nv.CCCD = model.CCCD;
            nv.ViTri = model.ViTri;
            nv.TenDN = model.TenDN;

            if (!string.IsNullOrWhiteSpace(MatKhauMoi))
            {
                nv.MatKhau = PasswordHelper.HashPassword(MatKhauMoi);
            }

            await _context.SaveChangesAsync();
            await _logger.LogAsync("NhanVien", $"Cập nhật thông tin nhân viên {nv.HoTen} ({nv.MaNV})", null, CurrentMaNV());
            TempData["Success"] = $"Đã cập nhật thông tin nhân viên {nv.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // KHÓA / MỞ KHÓA TÀI KHOẢN (UC_6.3)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThai(string id)
        {
            if (!IsOwner()) return Forbid();

            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            // Không cho khóa chính mình
            if (nv.MaNV == CurrentMaNV())
            {
                if (WantsJson()) return Json(new { success = false, message = "Không thể khóa tài khoản của chính mình." });
                TempData["Error"] = "Không thể khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            if (nv.TrangThai != "Đang hoạt động")
            {
                if (WantsJson()) return Json(new { success = false, message = "Tài khoản đã bị khóa." });
                TempData["Error"] = "Tài khoản đã bị khóa.";
                return RedirectToAction(nameof(Index));
            }
            var oldStatus = nv.TrangThai;
            nv.TrangThai = "Đã khóa";
            await _context.SaveChangesAsync();

            var action = nv.TrangThai == "Đã khóa" ? "Khóa" : "Mở khóa";
            await _logger.LogAsync("NhanVien", $"{action} tài khoản {nv.HoTen} ({nv.MaNV}): {oldStatus} → {nv.TrangThai}", null, CurrentMaNV());
            if (WantsJson())
            {
                TempData["EmployeeSuccess"] = "Khóa tài khoản thành công";
                return Json(new { success = true, message = "Khóa tài khoản thành công", id = nv.MaNV });
            }
            TempData["Success"] = $"Đã {action.ToLower()} tài khoản {nv.HoTen}.";
            return RedirectToAction(nameof(Index));
        }

        private bool WantsJson() => Request.Headers.Accept.ToString().Contains("application/json");

        // ==========================================
        // XEM CHI TIẾT NHÂN VIÊN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ChiTiet(string id)
        {
            if (!IsOwner()) return Forbid();

            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            // Thống kê hoạt động của nhân viên
            var soHoaDon = await _context.HoaDons.CountAsync(h => h.MaNV == id);
            var tongDoanhThu = await _context.HoaDons
                .Where(h => h.MaNV == id && h.TrangThaiThanhToan == "Hoàn thành")
                .SumAsync(h => (decimal?)h.TongThanhToan) ?? 0;
            var soPhieu = await _context.Phieus.CountAsync(p => p.MaNV == id);

            ViewBag.SoHoaDon = soHoaDon;
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.SoPhieu = soPhieu;

            return View(nv);
        }

        // ==========================================
        // API: Kiểm tra trùng tên đăng nhập (AJAX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> KiemTraTenDN(string tenDN, string? maNV = null)
        {
            if (!IsOwner()) return Forbid();
            var exists = await _context.NhanViens.AnyAsync(nv =>
                nv.TenDN == tenDN && (maNV == null || nv.MaNV != maNV));
            return Json(new { exists });
        }
    }
}
