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

                    await dbContext.Database.EnsureCreatedAsync();
                    
                    await EnsureRoleExistsAsync(roleManager, UserRole.Admin.ToString());
                    await EnsureRoleExistsAsync(roleManager, UserRole.Doctor.ToString());
                    await EnsureRoleExistsAsync(roleManager, UserRole.Patient.ToString());
                    
                    const string adminEmail = "admin@dentalclinic.com";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    
                    if (adminUser == null)
                    {
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
                        if (!await userManager.IsInRoleAsync(adminUser, UserRole.Admin.ToString()))
                        {
                            await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                            Console.WriteLine("Added existing admin user to Admin role");
                        }
                    }

                    if (!dbContext.LeaveTypes.Any())
                    {
                        var leaveTypes = new List<LeaveType>
                        {
                            new LeaveType
                            {
                                Name = "Annual Leave",
                                IsPaid = true,
                                DefaultDays = 14,
                                Description = "Regular annual leave for vacation"
                            },
                            new LeaveType
                            {
                                Name = "Sick Leave",
                                IsPaid = true,
                                DefaultDays = 10,
                                Description = "Leave for illness and medical reasons"
                            },
                            new LeaveType
                            {
                                Name = "Bereavement Leave",
                                IsPaid = true,
                                DefaultDays = 3,
                                Description = "Leave for the death of a family member"
                            },
                            new LeaveType
                            {
                                Name = "Unpaid Leave",
                                IsPaid = false,
                                DefaultDays = 0, 
                                Description = "Leave without pay for extended absence"
                            }
                        };

                        dbContext.LeaveTypes.AddRange(leaveTypes);
                        await dbContext.SaveChangesAsync();
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
