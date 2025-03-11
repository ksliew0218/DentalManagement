using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using DentalManagement.Areas.Admin;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity with more complete options
builder.Services.AddDefaultIdentity<User>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add MVC with support for areas
builder.Services.AddControllersWithViews(options => 
{
    options.Conventions.Add(new AdminAreaRegistration());
});

// Add Razor Pages support (required for Identity)
builder.Services.AddRazorPages();

// Configure application cookie options
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

var app = builder.Build();

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:80";
app.Urls.Add(urls);

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();  
app.UseAuthorization();   

// Add area route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Add default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages (required for Identity)
app.MapRazorPages();  

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    // var userManager = services.GetRequiredService<UserManager<User>>();  
    // var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();  

    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
        {
            Console.WriteLine("✅ Database connection successful!");

            // await DbInitializer.Initialize(services, userManager, roleManager);
        }
        else
        {
            Console.WriteLine("❌ Failed to connect to the database.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}

app.Run();
