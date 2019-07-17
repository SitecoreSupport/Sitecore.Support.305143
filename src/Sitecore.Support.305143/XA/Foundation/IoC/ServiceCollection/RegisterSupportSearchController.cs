using System.Web.Http.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.XA.Feature.Search.Repositories;

namespace Sitecore.Support.XA.Foundation.IoC.ServiceCollection
{
    public class RegisterSupportSearchController : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IHttpController, Sitecore.Support.XA.Feature.Search.Controllers.SupportSearchController>();
            serviceCollection.AddTransient<ISearchBoxRepository, Sitecore.Support.XA.Feature.Search.Repositories.SearchBoxRepository>();
        }
    }
}