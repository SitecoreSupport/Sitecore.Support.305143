using Microsoft.Extensions.DependencyInjection;
using Sitecore.ContentSearch.Data;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.XA.Feature.Search.Attributes;
using Sitecore.XA.Feature.Search.Binder;
using Sitecore.XA.Feature.Search.Filters;
using Sitecore.XA.Feature.Search.Models;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Search;
using Sitecore.XA.Foundation.Search.Models;
using Sitecore.XA.Foundation.Search.Models.Binding;
using Sitecore.XA.Foundation.Search.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;

namespace Sitecore.Support.XA.Feature.Search.Controllers
{
    [JsonFormatter]
    public class SupportSearchController : ApiController
    {
        protected ISearchService SearchService
        {
            get;
            set;
        }

        protected ISortingService SortingService
        {
            get;
            set;
        }

        protected IFacetService FacetService
        {
            get;
            set;
        }

        protected IOrderingParametersParserService SearchParametersParserService
        {
            get;
            set;
        }

        protected IContext Context
        {
            get;
        }

        public SupportSearchController()
        {
            SearchService = ServiceLocator.ServiceProvider.GetService<ISearchService>();
            SortingService = ServiceLocator.ServiceProvider.GetService<ISortingService>();
            FacetService = ServiceLocator.ServiceProvider.GetService<IFacetService>();
            SearchParametersParserService = ServiceLocator.ServiceProvider.GetService<IOrderingParametersParserService>();
            Context = ServiceLocator.ServiceProvider.GetService<IContext>();
        }

        [ActionName("Results")]
        [RegisterSearchEvent]
        public ResultSet GetResults([ModelBinder(BinderType = typeof(QueryModelBinder))] QueryModel model)
        {
            try
            {
                Timer timer;
                string indexName;
                Timer timer2;
                int count;
                Timer timer4;
                IEnumerable<Result> results;
                using (timer = new Timer())
                {
                    IQueryable<ContentPage> query = SearchService.GetQuery(new SearchQueryModel
                    {
                        Coordinates = model.Coordinates,
                        ItemID = model.ItemID,
                        Languages = model.Languages,
                        Query = model.Query,
                        ScopesIDs = model.ScopesIDs,
                        Site = model.Site
                    }, out indexName);
                    using (timer2 = new Timer())
                    {
                        count = query.Count();
                    }
                    IEnumerable<Item> items = GetItems(model.Sortings, model.Offset, model.PageSize, model.Site, query, model.Coordinates, count);
                    Item variant = (!ID.IsNullOrEmpty(model.VariantID)) ? Context.Database.GetItem(model.VariantID) : null;
                    using (timer4 = new Timer())
                    {
                        if (SearchService.IsGeolocationRequest)
                        {
                            Unit unit = SearchParametersParserService.GetUnits(model.Sortings, model.Site);
                            results = (from i in items
                                       select new GeospatialResult(i, variant, model.Coordinates, unit)).ToList();
                        }
                        else
                        {
                            results = (from i in items
                                       select new Result(i, variant)).ToList();
                        }
                    }
                }
                return new ResultSet(timer.Msec, timer2.Msec, timer4.Msec, model.Signature, indexName, count, results);
            }
            catch (Exception ex)
            {
                Log.Warn("Results endpoint exception", ex, this);
                throw new HttpResponseException(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message)
                });
            }
        }

        [ActionName("Facets")]
        public FacetSet GetFacets([ModelBinder(BinderType = typeof(FacetsModelBinder))] FacetsModel model)
        {
            try
            {
                Timer queryTimer = new Timer();
                Timer timer;
                string indexName;
                IList<Facet> facets;
                using (timer = new Timer())
                {
                    IQueryable<ContentPage> query = SearchService.GetQuery(new SearchQueryModel
                    {
                        Site = model.Site,
                        Query = model.Query,
                        ItemID = model.ItemID,
                        Coordinates = model.Coordinates,
                        Languages = model.Languages,
                        ScopesIDs = model.ScopesIDs
                    }, out indexName);
                    IList<Item> facetItems = FacetService.GetFacetItems(model.Facets, model.Site);
                    object list2;
                    if (!facetItems.Any())
                    {
                        IList<Facet> list = new List<Facet>();
                        list2 = list;
                    }
                    else
                    {
                        list2 = FacetService.GetFacets(query, facetItems, model.Languages, out queryTimer);
                    }
                    facets = (IList<Facet>)list2;
                }
                return new FacetSet(timer.Msec, queryTimer.Msec, model.Signature, indexName, facets);
            }
            catch (Exception ex)
            {
                Log.Warn("Facets endpoint exception", ex, this);
                throw new HttpResponseException(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message)
                });
            }
        }

        [ActionName("Suggestions")]
        public SuggestionsSet GetSuggestions([ModelBinder(BinderType = typeof(QueryModelBinder))]QueryModel model)
        {
            if ((ConfigurationManager.AppSettings.AllKeys.Contains("search:define") ? ConfigurationManager.AppSettings["search:define"] : string.Empty) != "Solr")
            {
                return ConvertResults(GetResults(model));
            }
            return SearchSuggestions(new SuggesterModel
            {
                ContextItemID = model.ItemID,
                Term = model.Query
            });
        }

        protected virtual SuggestionsSet SearchSuggestions(SuggesterModel model)
        {
            Timer timer;
            SuggestionsSet suggestionsSet;
            Timer queryTimer;
            string indexName;
            using (timer = new Timer())
            {
                suggestionsSet = new SuggestionsSet();
                foreach (Suggestion suggestion in ServiceLocator.ServiceProvider.GetService<ISuggester>().GetSuggestions(model, out queryTimer, out indexName))
                {
                    suggestionsSet.Results.Add(suggestion);
                }
            }
            suggestionsSet.TotalTime = timer.Msec;
            suggestionsSet.QueryTime = queryTimer.Msec;
            suggestionsSet.Index = indexName;
            return suggestionsSet;
        }

        protected virtual SuggestionsSet ConvertResults(ResultSet set)
        {
            SuggestionsSet suggestionsSet = new SuggestionsSet
            {
                TotalTime = set.TotalTime,
                Index = set.Index,
                QueryTime = set.QueryTime,
                Signature = set.Signature
            };
            foreach (Result result in set.Results)
            {
                suggestionsSet.Results.Add(new Suggestion
                {
                    Term = result.Html
                });
            }
            return suggestionsSet;
        }

        protected virtual IEnumerable<Item> GetItems(IEnumerable<string> sortings, int e, int p, string site, IQueryable<ContentPage> query, Coordinate center, int count)
        {
            if (SortingService.GetSortingDirection(sortings) == SortingDirection.Random)
            {
                return GetRandomItems(p, query, count);
            }
            query = SortingService.Order(query, sortings, center, site);
            query = query.Skip(e);
            query = query.Take(p);
            return Enumerable.Where(from r in query
                                    select r.GetItem(), (Item i) => i != null);
        }

        protected virtual IEnumerable<Item> GetRandomItems(int p, IQueryable<ContentPage> query, int count)
        {
            Dictionary<int, Item> dictionary = new Dictionary<int, Item>();
            int num = Math.Min(count, p);
            Random randomize = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < num; i++)
            {
                int uniqueIndex = GetUniqueIndex(count, randomize, dictionary);
                Item item = query.ElementAt(uniqueIndex).GetItem();
                dictionary.Add(uniqueIndex, item);
            }
            return from entry in dictionary
                   select entry.Value;
        }

        protected virtual int GetUniqueIndex(int count, Random randomize, Dictionary<int, Item> randomResults)
        {
            int num = randomize.Next(count);
            bool flag = false;
            while (randomResults.ContainsKey(num))
            {
                if (!flag)
                {
                    if (num + 1 == count)
                    {
                        flag = true;
                    }
                    else
                    {
                        num++;
                    }
                }
                else if (num - 1 >= 0)
                {
                    num--;
                }
            }
            return num;
        }
    }
}