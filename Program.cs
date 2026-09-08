global using QigloRestaurant.Models.Entities;
global using QigloRestaurant.Web;
global using QigloRestaurant.Web.Models;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Data;
using QigloRestaurant.Web.Services;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);
var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(dataDirectory.ToUpperInvariant()));
var databaseName = $"QigloRestaurant_{Convert.ToHexString(pathHash[..6])}";
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")!
    .Replace("|DataDirectory|", dataDirectory, StringComparison.OrdinalIgnoreCase)
    .Replace("|DatabaseName|", databaseName, StringComparison.OrdinalIgnoreCase);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "QIGLO.Cart";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddDbContext<DB>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.Name = "QIGLO.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Helper>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IKitchenService, KitchenService>();
builder.Services.AddScoped<IBillingService, BillingService>();
// Table, reservation and walk-in services
builder.Services.AddScoped<ITableAvailabilityService, TableAvailabilityService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
builder.Services.AddScoped<IReceiptEmailService, ReceiptEmailService>();
builder.Services.AddScoped<IAccountEmailService, AccountEmailService>();
builder.Services.AddScoped<IReservationEmailService, ReservationEmailService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Serve wwwroot directly in both Visual Studio and standalone runs. This keeps
// CSS/JS available even when a Debug build is launched outside Development,
// where static-web-asset compression metadata is not enabled.
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    var helper = scope.ServiceProvider.GetRequiredService<Helper>();
    await DbInitializer.InitializeAsync(db, helper);
    // Add reservation and menu demonstration data after the shared seed.
    await ReservationSeedData.SeedAsync(db, helper);
    await MenuDemoCatalog.EnsureSeededAsync(db);
}

app.Run();
