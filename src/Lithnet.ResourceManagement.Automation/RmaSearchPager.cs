using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            List<RmaObject> results = new List<RmaObject>();

            foreach(ResourceObject resource in this.pager.GetNextPage())
            {
                results.Add(new RmaObject(resource));
            }

            return results.ToArray();
        }
    }
}
