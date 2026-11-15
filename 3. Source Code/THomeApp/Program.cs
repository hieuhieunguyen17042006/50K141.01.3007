using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using THomeApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Private machine settings are never committed. Environment/CLI values take precedence.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
if (args.Length > 0) builder.Configuration.AddCommandLine(args);

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection. Cấu hình trong appsettings.Local.json hoặc biến môi trường ConnectionStrings__DefaultConnection theo README.");
builder.Services.AddDbContext<THomeDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<THomeDbContext>();
            var id = context.Principal?.FindFirst("MaNV")?.Value;
            var user = id == null ? null : await db.NhanViens.FindAsync(id);
            if (user == null || user.TrangThai == "Đã khóa" || user.ViTri != context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value)
            {
                context.RejectPrincipal();
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<THomeApp.Services.PaymentQr>();
builder.Services.AddScoped<THomeApp.Services.ProductSales>();
builder.Services.AddScoped<THomeApp.Services.RevenueReports>();

// Add session support
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<THomeApp.Services.IActivityLogger, THomeApp.Services.ActivityLogger>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Controllers with Views
builder.Services.AddScoped<THomeApp.Services.DocumentWriteFilter>();
builder.Services.AddScoped<THomeApp.Services.LoginAntiforgeryRecoveryFilter>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<THomeApp.Services.DocumentWriteFilter>());

var app = builder.Build();

// Initialize DB and Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<THomeDbContext>();
        DbInitializer.Initialize(context, app.Configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo cơ sở dữ liệu.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
