using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;

namespace EmailService.WebApi.Infrastructure
{
    /// <summary>
    /// Convention to exclude controllers from other assemblies (CMS/TMS) from being exposed
    /// </summary>
    public class ControllerExclusionConvention : IControllerModelConvention
    {
        private readonly Assembly _allowedAssembly;

        public ControllerExclusionConvention(Assembly allowedAssembly)
        {
            _allowedAssembly = allowedAssembly;
        }

        public void Apply(ControllerModel controller)
        {
            // Only allow controllers from the Email Service assembly
            if (controller.ControllerType.Assembly != _allowedAssembly)
            {
                // Mark controller as not visible by removing all actions
                controller.Actions.Clear();
                
                // Also remove from API explorer
                foreach (var action in controller.Actions.ToList())
                {
                    controller.Actions.Remove(action);
                }
            }
        }
    }

    /// <summary>
    /// Application model provider to filter out unwanted controllers
    /// </summary>
    public class EmailServiceApplicationModelProvider : IApplicationModelProvider
    {
        public int Order => -1000; // Run early

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            var emailServiceAssembly = typeof(Controllers.EmailController).Assembly;
            
            // Remove controllers that are not from EmailService assembly
            var controllersToRemove = context.Result.Controllers
                .Where(c => c.ControllerType.Assembly != emailServiceAssembly)
                .ToList();

            foreach (var controller in controllersToRemove)
            {
                context.Result.Controllers.Remove(controller);
            }
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            // Nothing to do here
        }
    }
}
