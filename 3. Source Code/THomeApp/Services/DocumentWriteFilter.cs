using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Models;

namespace THomeApp.Services;

public class DocumentWriteFilter(THomeDbContext db) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsPost(request.Method) || context.HttpContext.User.Identity?.IsAuthenticated != true
            || context.RouteData.Values["controller"]?.ToString() == "Auth")
        { await next(); return; }

        if (context.RouteData.Values["controller"]?.ToString() == "BanHang" && !context.ModelState.IsValid)
        { context.Result = new BadRequestObjectResult(new { success = false, message = "Không hợp lệ!", errors=context.ModelState.Where(p=>p.Value?.Errors.Count>0).ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>"Không hợp lệ! Kiểm tra định dạng dữ liệu.").ToArray()) }); return; }

        string? requestId = request.Headers["X-Request-Id"].FirstOrDefault();
        if (request.HasFormContentType) requestId ??= (await request.ReadFormAsync())["requestId"].FirstOrDefault();
        if (!Guid.TryParse(requestId, out _))
        { context.Result = new BadRequestObjectResult(new { success = false, message = "Thiếu mã thao tác. Vui lòng tải lại trang trước khi lưu." }); return; }

        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        var key = Hash($"{context.HttpContext.User.FindFirstValue("MaNV")}|{context.ActionDescriptor.DisplayName}|{requestId}");
        var payloadHash = Hash(JsonSerializer.Serialize(context.ActionArguments.OrderBy(p => p.Key)));
        await using var transaction = await db.Database.BeginTransactionAsync();
        // All document writers share this transaction-owned SQL lock, including other app instances.
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'THOME:document-write', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, N'Không lấy được khóa lưu chứng từ. Vui lòng thử lại.', 1;");
        var previous = await db.RequestOperations.FindAsync(key);
        if (previous != null)
        {
            if (previous.PayloadHash != payloadHash)
                context.Result = new ConflictObjectResult(new { success = false, message = "Mã thao tác đã được dùng với dữ liệu khác. Tải lại trang để tạo thao tác mới." });
            else if (previous.RedirectUrl != null) context.Result = new RedirectResult(previous.RedirectUrl);
            else context.Result = new ContentResult { Content = previous.JsonResult, ContentType = "application/json; charset=utf-8" };
            return;
        }
        if (context.Controller is Controller before)
            foreach (var message in new[] { "ErrorMessage", "Error", "SuccessMessage", "Success", "WarningMessage" }) before.TempData.Remove(message);
        var executed = await next();
        if (executed.Exception != null) return;
        if (context.Controller is Controller after && (after.TempData.ContainsKey("ErrorMessage") || after.TempData.ContainsKey("Error"))) return;
        var operation = new RequestOperation { Id = key, PayloadHash = payloadHash };
        if (executed.Result is JsonResult json)
        {
            operation.JsonResult = JsonSerializer.Serialize(json.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            using var document = JsonDocument.Parse(operation.JsonResult);
            if (document.RootElement.TryGetProperty("success", out var success) && !success.GetBoolean()) return;
        }
        else if (executed.Result is RedirectToActionResult redirect && context.Controller is Controller controller)
            operation.RedirectUrl = controller.Url.Action(redirect.ActionName, redirect.ControllerName, redirect.RouteValues);
        else return;
        db.RequestOperations.Add(operation);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
