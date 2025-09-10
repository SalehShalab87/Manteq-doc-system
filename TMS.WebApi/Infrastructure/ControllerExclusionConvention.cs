using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace TMS.WebApi.Infrastructure
{
    /// <summary>
    /// Custom convention to exclude CMS controllers from being exposed in TMS API
    /// </summary>
    public class ControllerExclusionConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            // Exclude controllers from CMS.WebApi namespace
            if (controller.ControllerType.Namespace != null && 
                controller.ControllerType.Namespace.StartsWith("CMS.WebApi.Controllers"))
            {
                // Hide from API explorer (Swagger)
                controller.ApiExplorer.IsVisible = false;
                
                // Remove all actions to prevent routing
                controller.Actions.Clear();
                
                // Alternatively, we could modify the route to make it inaccessible
                // controller.ControllerName = "__EXCLUDED__" + controller.ControllerName;
            }
        }
    }
}
