using Microsoft.AspNetCore.Razor.TagHelpers;

namespace THomeApp.TagHelpers;

[HtmlTargetElement("form", Attributes = "method")]
public class RequestIdTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.Equals(output.Attributes["method"]?.Value?.ToString(), "post", StringComparison.OrdinalIgnoreCase))
            output.PostContent.AppendHtml($"<input type=\"hidden\" name=\"requestId\" value=\"{Guid.NewGuid()}\" />");
    }
}
