using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Catalog
{
    public partial class DestinationListModel : BaseNopModel
    {
        public DestinationListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Catalog.Destinations.List.SearchDestinationName")]
        [AllowHtml]
        public string SearchDestinationName { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Destinations.List.SearchStore")]
        public int SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}