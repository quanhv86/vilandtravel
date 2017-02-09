using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Destination service
    /// </summary>
    public partial interface IDestinationService
    {
        /// <summary>
        /// Deletes a destination
        /// </summary>
        /// <param name="destination">Destination</param>
        void DeleteDestination(Destination destination);
        
        /// <summary>
        /// Gets all destinations
        /// </summary> 
        IPagedList<Destination> GetAllDestinations(string destinationName = "",
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false);

        /// <summary>
        /// Gets a destination
        /// </summary> 
        Destination GetDestinationById(int destinationId);

        /// <summary>
        /// Inserts a destination
        /// </summary>
        /// <param name="destination">Destination</param>
        void InsertDestination(Destination destination);

        /// <summary>
        /// Updates the destination
        /// </summary>
        /// <param name="destination">Destination</param>
        void UpdateDestination(Destination destination);
        

        /// <summary>
        /// Deletes a product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        void DeleteProductDestination(ProductDestination productDestination);
        
        /// <summary>
        /// Gets product destination collection
        /// </summary> 
        IPagedList<ProductDestination> GetProductDestinationsByDestinationId(int destinationId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

        /// <summary>
        /// Gets a product destination mapping collection
        /// </summary> 
        IList<ProductDestination> GetProductDestinationsByProductId(int productId, bool showHidden = false);
        
        /// <summary>
        /// Gets a product destination mapping 
        /// </summary> 
        ProductDestination GetProductDestinationById(int productDestinationId);

        /// <summary>
        /// Inserts a product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        void InsertProductDestination(ProductDestination productDestination);

        /// <summary>
        /// Updates the product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        void UpdateProductDestination(ProductDestination productDestination);

        /// <summary>
        /// Get destination IDs for products
        /// </summary>
        /// <param name="productIds">Products IDs</param>
        /// <returns>Destination IDs for products</returns>
        IDictionary<int, int[]> GetProductDestinationIds(int[] productIds);

        /// <summary>
        /// Returns a list of names of not existing destinations
        /// </summary>
        /// <param name="destinationNames">The names of the destinations to check</param>
        /// <returns>List of names not existing destinations</returns>
        string[] GetNotExistingDestinations(string[] destinationNames);
    }
}
