using System;
using System.Collections.Generic;
using Nop.Core.Domain.Catalog;

namespace Nop.Core.Domain.Orders
{
    /// <summary>
    /// Represents an order item
    /// </summary>
    public partial class OrderItem : BaseEntity
    {
       

        /// <summary>
        /// Gets or sets the order item identifier
        /// </summary>
        public Guid OrderItemGuid { get; set; }
         
        public int OrderId { get; set; }
         
        public int ProductId { get; set; }
         
        public int Quantity { get; set; }
         
        public decimal UnitPriceInclTax { get; set; }
         
        public decimal UnitPriceExclTax { get; set; }
         
        public decimal PriceInclTax { get; set; }
         
        public decimal PriceExclTax { get; set; }
         
        public decimal DiscountAmountInclTax { get; set; }
         
        public decimal DiscountAmountExclTax { get; set; } 

        public decimal OriginalProductCost { get; set; } 

        public string AttributeDescription { get; set; } 

        public string AttributesXml { get; set; } 

        public int DownloadCount { get; set; }  

        public DateTime? DepartureDateUtc { get; set; }  

        public virtual Order Order { get; set; } 

        public virtual Product Product { get; set; }
         
    }
}
