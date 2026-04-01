using BusTracker.Application.Common.Interfaces.Services;
using HandlebarsDotNet;
using System.Reflection;

namespace BusTracker.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Resolves templates from the Infrastructure layer's own output directory.
    /// Callers pass only the template filename (e.g. "WelcomeEmail.html");
    /// this service anchors the full path to the Infrastructure assembly location.
    /// </summary>
    public class HandlebarsTemplateService : ITemplateService
    {
        private static readonly string TemplatesRoot = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Templates");

        public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
        {
            var templatePath = Path.Combine(TemplatesRoot, templateName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Email template '{templateName}' not found. Expected path: {templatePath}");
            }

            var htmlTemplate = await File.ReadAllTextAsync(templatePath);
            var compiledTemplate = Handlebars.Compile(htmlTemplate);
            return compiledTemplate(model);
        }
    }
}
