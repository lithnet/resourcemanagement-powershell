using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lithnet.ResourceManagement.Client;

namespace Lithnet.ResourceManagement.Automation
{
    public class RmaSearchPager
    {
        SearchResultPager pager;

        internal RmaSearchPager(SearchResultPager pager)
        {
            this.pager = pager;
        }

        public int TotalCount
        {
            get
            {
                return this.pager.TotalCount;
            }
        }

        public int CurrentIndex
        {
            get
            {
                return this.pager.CurrentIndex;
            }
            set
            {
                this.pager.CurrentIndex = value;
            }
        }

        public int PageSize
        {
            get
            {
                return this.pager.PageSize;
            }
            set
            {
                this.pager.PageSize = value;
            }
        }

        public bool HasMoreItems
        {
            get
            {
                return this.pager.HasMoreItems;
            }
        }
        public RmaObject[] GetNextPage()
        {
            return Nito.AsyncEx.AsyncContext.Run(async () => await this.GetNextPageAsync());
        }

        public async Task<RmaObject[]> GetNextPageAsync()
        {
            List<RmaObject> results = new List<RmaObject>();

            await foreach(ResourceObject resource in this.pager.GetNextPageAsync())
            {
                results.Add(new RmaObject(resource));
            }

            return results.ToArray();
        }
    }
}
