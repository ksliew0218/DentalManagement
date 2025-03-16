using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DentalManagement.Areas.Patient
{
    public class PatientAreaRegistration : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.Namespace?.Contains("Areas.Patient") == true)
            {
                controller.RouteValues["area"] = "Patient";
            }
        }
    }
}
