using System.Threading.Tasks;
using DentalManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DentalManagement.Filters
{
    public class AccountActiveFilter : IAsyncActionFilter
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountActiveFilter(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null && !user.IsActive)
                {
                    // Sign the user out
                    await _signInManager.SignOutAsync();
                    
                    // Redirect to a deactivated account page
                    context.Result = new RedirectToPageResult("/Account/AccountDeactivated", new { area = "Identity" });
                    return;
                }
            }

            await next();
        }
    }
}