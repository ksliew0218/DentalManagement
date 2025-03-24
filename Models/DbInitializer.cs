using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using DentalManagement.Models;

public class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
    {
        try
        {
            // Check if userManager or roleManager is null and try to get from serviceProvider
            userManager ??= serviceProvider.GetRequiredService<UserManager<User>>();
            roleManager ??= serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (userManager.Users.All(u => u.UserName != "admin@gmail.com"))
            {
                var admin = new User
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Role = UserRole.Admin,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");

                if (result.Succeeded)
                {
                    await EnsureRoleExistsAsync(roleManager, UserRole.Admin.ToString());

                    await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
                    Console.WriteLine("Admin user created successfully");
                }
                else
                {
                    Console.WriteLine("Failed to create Admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists");
                
                // Check if the admin is in the Admin role
                var admin = await userManager.FindByEmailAsync("admin@gmail.com");
                if (admin != null)
                {
                    await EnsureRoleExistsAsync(roleManager, UserRole.Admin.ToString());
                    if (!(await userManager.IsInRoleAsync(admin, UserRole.Admin.ToString())))
                    {
                        await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
                        Console.WriteLine("Added existing admin user to Admin role");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
            Console.WriteLine($"Created role: {roleName}");
        }
    }
}
