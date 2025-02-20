using DentalManagement.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add PostgreSQL Database Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Fix: Bind to all network interfaces (IPv4 & IPv6) to allow Docker access
app.Urls.Add("http://*:9090");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ This ensures static files like CSS/JS load
app.UseRouting();
app.UseAuthorization();

// ✅ Set up Default Route (Fix Incorrect Mapping)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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