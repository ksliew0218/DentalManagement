using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add PostgreSQL Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add Identity (必须添加 AddDefaultTokenProviders)
builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // ✅ 否则无法使用密码重置、邮箱验证等功能

// ✅ Add MVC and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // 🔹 必须添加，否则 Identity UI 404

var app = builder.Build();

// ✅ Fix: 监听 localhost:9090（或者改为 5000/7000 测试）
app.Urls.Add("http://localhost:9090");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // 🔹 确保启用身份认证
app.UseAuthorization();

// ✅ 设置默认路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ 确保 Identity UI 可用
app.MapRazorPages(); // 🔹 这个必须加，否则 Identity 页面 404！

// ✅ 确保数据库连接正常
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        dbContext.Database.CanConnect();
        Console.WriteLine("✅ Database connection successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}

app.Run();
