using System.Collections.Generic;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class DestinationExtensions
    {
        /// <summary>
        /// Returns a ProductDestination that has the specified values
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="destinationId">Destination identifier</param>
        /// <returns>A ProductDestination that has the specified values; otherwise null</returns>
        public static ProductDestination FindProductDestination(this IList<ProductDestination> source,
            int productId, int destinationId)
        {
            foreach (var productDestination in source)
                if (productDestination.ProductId == productId && productDestination.DestinationId == destinationId)
                    return productDestination;

            return null;
        }

    }
}
