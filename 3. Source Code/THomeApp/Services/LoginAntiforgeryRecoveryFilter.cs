using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace THomeApp.Services;

// Authorization short-circuits before the login action when its form token is stale.
// An always-run result filter can offer a fresh GET without executing that POST.
public sealed class LoginAntiforgeryRecoveryFilter(ILogger<LoginAntiforgeryRecoveryFilter> logger,
    ITempDataDictionaryFactory tempDataFactory, IUrlHelperFactory urlFactory) : IAlwaysRunResultFilter
{
    public const string ErrorKey = "LoginAntiforgeryError";

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not IAntiforgeryValidationFailedResult)
            return;

        // The login controller may not have been instantiated after authorization failed.
        var tempData = tempDataFactory.GetTempData(context.HttpContext);
        tempData[ErrorKey] = "Phiên đăng nhập đã thay đổi hoặc biểu mẫu đã hết hiệu lực. Vui lòng nhập lại thông tin và đăng nhập.";
        // The resource-stage TempData save callback was also skipped by authorization.
        tempData.Save();

        // Antiforgery has already read the form. Do not read credentials or retry the POST.
        var returnUrl = context.HttpContext.Features.Get<IFormFeature>()?.Form?["returnUrl"].FirstOrDefault()
            ?? context.HttpContext.Request.Query["returnUrl"].FirstOrDefault();
        if (returnUrl != null && !urlFactory.GetUrlHelper(context).IsLocalUrl(returnUrl)) returnUrl = null;

        logger.LogWarning("Login POST rejected by antiforgery. Redirecting to a fresh login GET. TraceId: {TraceId}", context.HttpContext.TraceIdentifier);
        context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
