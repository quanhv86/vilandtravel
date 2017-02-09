using System.Collections.Generic;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog
{
    public partial class DestinationNavigationModel : BaseNopModel
    {
        public DestinationNavigationModel()
        {
            this.Destinations = new List<DestinationBriefInfoModel>();
        }

        public IList<DestinationBriefInfoModel> Destinations { get; set; }

        public int TotalDestinations { get; set; }
    }

    public partial class DestinationBriefInfoModel : BaseNopEntityModel
    {
        public string Name { get; set; }

        public string SeName { get; set; }
        
        public bool IsActive { get; set; }
    }
}