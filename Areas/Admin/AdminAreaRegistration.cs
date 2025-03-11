using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DentalManagement.Areas.Admin
{
    public class AdminAreaRegistration : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.Namespace?.Contains("Areas.Admin") == true)
            {
                controller.RouteValues["area"] = "Admin";
            }
        }
    }
} 