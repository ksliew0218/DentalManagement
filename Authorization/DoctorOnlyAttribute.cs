using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace DentalManagement.Authorization
{
    public class DoctorOnlyAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Get the user manager from the context
            var userManager = context.HttpContext.RequestServices.GetService(typeof(UserManager<User>)) as UserManager<User>;
            
            if (userManager == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }
            
            // Get the current user
            var user = await userManager.GetUserAsync(context.HttpContext.User);
            
            // Check if the user is a doctor
            if (user == null || user.Role != UserRole.Doctor)
            {
                // If not a doctor, redirect to access denied page
                // Explicitly set area to empty string to ensure we don't stay in the Doctor area
                context.Result = new RedirectToActionResult("AccessDenied", "Home", new { area = "" });
            }
        }
    }
} 