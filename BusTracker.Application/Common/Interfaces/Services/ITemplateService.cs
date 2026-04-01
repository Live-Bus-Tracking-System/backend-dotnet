using System.Threading.Tasks;

namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface ITemplateService
    {
        Task<string> RenderTemplateAsync<T>(string templateName, T model);
    }
}
