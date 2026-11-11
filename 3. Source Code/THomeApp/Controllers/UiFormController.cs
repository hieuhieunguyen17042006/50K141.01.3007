using Microsoft.AspNetCore.Mvc;

namespace THomeApp.Controllers;

public abstract class UiFormController : Controller
{
    protected bool WantsJson => Request.Headers.Accept.ToString().Contains("application/json");
    protected IActionResult FormErrors(string action, string? controller = null)
    {
        if (WantsJson) return Json(new { success=false, message="Thông tin không hợp lệ!", errors=ModelState.Where(p=>p.Value?.Errors.Count>0)
            .ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>string.IsNullOrEmpty(e.ErrorMessage)?"Thông tin không hợp lệ.":e.ErrorMessage).ToArray()) });
        TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage));
        return RedirectToAction(action, controller);
    }
    protected IActionResult FormSuccess(string message, string action, object? route = null)
    {
        TempData["CatalogSuccess"] = message;
        if (WantsJson) return Json(new {success=true,message,redirect=Url.Action(action,route)});
        return RedirectToAction(action,route);
    }
    protected void RequireReason(string? reason, string key="LyDo")
    {
        if (string.IsNullOrWhiteSpace(reason)) ModelState.AddModelError(key,"Vui lòng nhập lý do thao tác.");
        else if (reason.Length>500) ModelState.AddModelError(key,"Lý do không được quá 500 ký tự.");
    }
}
