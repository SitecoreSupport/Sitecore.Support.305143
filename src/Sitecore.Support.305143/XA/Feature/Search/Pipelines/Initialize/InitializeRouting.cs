using System.Linq;
using System.Web.Http;
using System.Web.Routing;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.Pipelines;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.SitecoreExtensions.Session;

namespace Sitecore.Support.XA.Feature.Search.Pipelines.Initialize
{
    public class InitializeRouting
    {
        public void Process(PipelineArgs args)
        {
            foreach (string item in (from s in ServiceLocator.ServiceProvider.GetService<ISiteInfoResolver>().Sites
                select s.VirtualFolder.Trim('/')).Distinct())
            {
                string str = (item.Length > 0) ? (item + "/") : item;
                RouteTable.Routes.MapHttpRoute(str + "supportsxa", str + "sxa/{controller}/{action}", new { controller = "SupportSearch", action = "Suggestions" }).RouteHandler = new SessionHttpControllerRouteHandler();
            }
        }
    }
}