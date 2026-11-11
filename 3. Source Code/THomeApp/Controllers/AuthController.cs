using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Helpers;
using THomeApp.Services;

namespace THomeApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly THomeDbContext _context;
        private readonly IActivityLogger _logger;

        public AuthController(THomeDbContext context, IActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                TempData.Remove(LoginAntiforgeryRecoveryFilter.ErrorKey);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ErrorMessage = TempData[LoginAntiforgeryRecoveryFilter.ErrorKey] as string;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ServiceFilter(typeof(LoginAntiforgeryRecoveryFilter))]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Login(string tenDN, string matKhau, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            // Exception flow 4a: Bỏ trống trường thông tin đăng nhập (BR-UC1-01)
            if (string.IsNullOrWhiteSpace(tenDN) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.ErrorMessage = "Không được để trống!";
                return View();
            }

            // BR-UC1-03: Tên đăng nhập không quá 10 kí tự
            if (tenDN.Trim().Length > 10)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập không được quá 10 kí tự!";
                return View();
            }

            var user = await _context.NhanViens.FirstOrDefaultAsync(u => u.TenDN.ToLower() == tenDN.Trim().ToLower());
            if (user == null || !PasswordHelper.VerifyPassword(matKhau, user.MatKhau))
            {
                // 5a. Hệ thống xác thực không hợp lệ
                ViewBag.ErrorMessage = "Đăng nhập không thành công!";
                return View();
            }

            // Chặn tài khoản đã khóa
            if (user.TrangThai == "Đã khóa")
            {
                ViewBag.ErrorMessage = "Tài khoản này đã bị khóa! Vui lòng liên hệ Chủ cửa hàng.";
                return View();
            }

            // Create Claims
            if (!user.MatKhau.StartsWith("PBKDF2$"))
            {
                user.MatKhau = PasswordHelper.HashPassword(matKhau);
                await _context.SaveChangesAsync();
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNV),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim("TenDN", user.TenDN),
                new Claim(ClaimTypes.Role, user.ViTri),
                new Claim("MaNV", user.MaNV),
                new Claim("ViTri", user.ViTri)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            // Log activity
            await _logger.LogAsync("HeThong", $"Đăng nhập thành công vào hệ thống với vai trò {user.ViTri}", "Đăng nhập", user.MaNV);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet, HttpPost]
        public async Task<IActionResult> Logout()
        {
            var maNV = User.FindFirstValue("MaNV");
            if (!string.IsNullOrEmpty(maNV))
            {
                await _logger.LogAsync("HeThong", "Đăng xuất khỏi hệ thống", "Đăng xuất", maNV);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }
    }
}
