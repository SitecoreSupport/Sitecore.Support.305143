using System.Web.Script.Serialization;
using Sitecore.XA.Feature.Search.Enums;
using Sitecore.XA.Foundation.Multisite;
using Sitecore.XA.Foundation.RenderingVariants.Repositories;
using Sitecore.XA.Foundation.Search.Services;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;

namespace Sitecore.Support.XA.Feature.Search.Repositories
{
    public class SearchBoxRepository : Sitecore.XA.Feature.Search.Repositories.SearchBoxRepository
    {
        public SearchBoxRepository(ISiteInfoResolver siteInfoResolver, IVariantsRepository variantsRepository, IScopeService scopeService) : base(siteInfoResolver, variantsRepository, scopeService)
        {
        }

        protected override string GetJsonDataProperties()
        {
            string text = Rendering.Parameters["SearchSignature"];
            var obj = new
            {
                endpoint = HomeUrl + "/sxa/search/results/",
                suggestionEndpoint = HomeUrl + "/sxa/SupportSearch/suggestions/",
                suggestionsMode = Rendering.Parameters["SuggestionsMode"],
                resultPage = GetSearchResultPageUrl(),
                targetSignature = Rendering.Parameters["TargetSignature"],
                v = (VariantsRepository.VariantItem?.ID.ToString() ?? string.Empty),
                s = ScopeService.GetScopes(PageContext.Current, Rendering.Parameters["Scope"]),
                p = Rendering.Parameters.ParseInt("MaxPredictiveResultsCount"),
                l = ((DefaultLanguageFilter == SearchBoxLanguageFiltering.CurrentLanguage) ? base.Context.Language.Name : string.Empty),
                languageSource = DefaultLanguageFilter.ToString(),
                searchResultsSignature = ((!string.IsNullOrWhiteSpace(text)) ? text.ToLowerInvariant() : string.Empty),
                itemid = base.Context.Item.ID.ToString()
            };
            return new JavaScriptSerializer().Serialize(obj);
        }
    }
}