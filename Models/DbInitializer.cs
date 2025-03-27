using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DentalManagement.Models
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                    // Create database if it doesn't exist
                    await dbContext.Database.EnsureCreatedAsync();
                    
                    // Ensure all roles exist
                    await EnsureRoleExistsAsync(roleManager, UserRole.Admin.ToString());
                    await EnsureRoleExistsAsync(roleManager, UserRole.Doctor.ToString());
                    await EnsureRoleExistsAsync(roleManager, UserRole.Patient.ToString());
                    
                    // Ensure admin account exists
                    const string adminEmail = "admin@dentalclinic.com";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    
                    if (adminUser == null)
                    {
                        // Create default admin account
                        adminUser = new User
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            IsActive = true,
                            Role = UserRole.Admin
                        };
                        
                        var result = await userManager.CreateAsync(adminUser, "Admin@123");
                        
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                            Console.WriteLine("Default admin account created");
                        }
                        else
                        {
                            Console.WriteLine("Failed to create default admin account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        // Ensure the existing admin user is in the Admin role
                        if (!await userManager.IsInRoleAsync(adminUser, UserRole.Admin.ToString()))
                        {
                            await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
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
}
