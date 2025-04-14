using DentalManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace DentalManagement.Authorization
{
    public class AdminOnlyAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userManager = context.HttpContext.RequestServices.GetService(typeof(UserManager<User>)) as UserManager<User>;
            
            if (userManager == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }
            
            var user = await userManager.GetUserAsync(context.HttpContext.User);
            
            if (user == null || user.Role != UserRole.Admin)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", new { area = "" });
            }
        }
    }
} 