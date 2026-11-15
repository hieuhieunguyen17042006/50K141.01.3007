using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using THomeApp.Services;

namespace THomeApp.Controllers;

[Authorize]
public class HomeController(HomeDashboard dashboard) : Controller
{
    public async Task<IActionResult> Index()
    {
        var role=User.FindFirstValue(ClaimTypes.Role);
        // Redirect staff before any dashboard read, including direct requests to /.
        if(role is "Nhân viên bán hàng" or "Kế toán bán hàng")return RedirectToAction("Index","BanHang");
        if(role!="Chủ cửa hàng")return Forbid();
        ViewBag.UserRole=role;
        ViewBag.Dashboard=await dashboard.ReadAsync(true);
        return View();
    }
}
