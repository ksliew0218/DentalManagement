using Microsoft.EntityFrameworkCore;
using DentalManagement.Models;
using Microsoft.AspNetCore.Http;    
using Microsoft.AspNetCore.Identity;
using System;
using DentalManagement.Services;
using Microsoft.Extensions.Logging;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure email services
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<EmailTemplateService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<AppointmentReminderService>();

// Configure Stripe and payment services
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<IPaymentService, PaymentService>();

// Configure Identity with complete options
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add MVC with support for multiple areas
builder.Services.AddControllersWithViews();

// Add Razor Pages support (required for Identity)
builder.Services.AddRazorPages();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register application services
builder.Services.AddScoped<LeaveManagementService>();

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Configure application cookie options
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure Stripe API key
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Value;

// Use session
app.UseSession();

// Ensure application runs on the configured URL
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://0.0.0.0:80";
app.Urls.Add(urls);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add area routes for both `Admin` and `Patient`
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Add default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Pages (required for Identity)
app.MapRazorPages();

// Database Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        if (dbContext.Database.CanConnect())
        {
            logger.LogInformation("✅ Database connection successful!");
            await DentalManagement.Models.DbInitializer.InitializeAsync(services);
            logger.LogInformation("Database initialized!");
        }
        else
        {
            logger.LogError("❌ Failed to connect to the database.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database connection failed: {Message}", ex.Message);
    }
}

app.Run();