using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RecipeWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Thêm c?u hình Session
builder.Services.AddDistributedMemoryCache(); // B? nh? cache cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Th?i gian h?t h?n session
    options.Cookie.HttpOnly = true; // Ng?n JavaScript truy c?p cookie
    options.Cookie.IsEssential = true; // ??m b?o cookie c?n thi?t cho ?ng d?ng
});

// Thêm Authentication v?i Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

// Thêm DbContext cho RecipeWeb
builder.Services.AddDbContext<RecipeDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("RECIPEWEB"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm Middleware cho Session
app.UseSession();

app.UseAuthentication(); // ??m b?o xác th?c tr??c khi ?y quy?n
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
