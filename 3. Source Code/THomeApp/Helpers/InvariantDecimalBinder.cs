using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace THomeApp.Helpers;

// HTML number controls submit a dot decimal separator in every browser locale.
public sealed class InvariantDecimalBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (string.IsNullOrWhiteSpace(value.FirstValue)) context.Result = ModelBindingResult.Success(null);
        else if (decimal.TryParse(value.FirstValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            context.Result = ModelBindingResult.Success(number);
        else context.ModelState.AddModelError(context.ModelName, "Vui lòng nhập số hợp lệ, không dùng dấu phân cách hàng nghìn.");
        return Task.CompletedTask;
    }
}
