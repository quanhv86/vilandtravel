using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;


namespace Nop.Services.Catalog
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class ProductExtensions
    {
        /// <summary>
        /// Gets a preferred tier price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="quantity">Quantity</param>
        /// <returns>Price</returns>
        public static decimal? GetPreferredTierPrice(this Product product, Customer customer, int storeId, int quantity)
        {
            if (!product.HasTierPrices)
                return null;

            //get actual tier prices
            var actualTierPrices = product.TierPrices.OrderBy(price => price.Quantity).ToList()
                .FilterByStore(storeId)
                .FilterForCustomer(customer)
                .FilterByDate()
                .RemoveDuplicatedQuantities();

            //get the most suitable tier price based on the passed quantity
            var tierPrice = actualTierPrices.LastOrDefault(price => quantity >= price.Quantity);

            return tierPrice != null ? (decimal?)tierPrice.Price : null;
        }

        /// <summary>
        /// Finds a related product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Related product</returns>
        public static RelatedProduct FindRelatedProduct(this IList<RelatedProduct> source,
            int productId1, int productId2)
        {
            foreach (RelatedProduct relatedProduct in source)
                if (relatedProduct.ProductId1 == productId1 && relatedProduct.ProductId2 == productId2)
                    return relatedProduct;
            return null;
        }

        /// <summary>
        /// Finds a cross-sell product item by specified identifiers
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId1">The first product identifier</param>
        /// <param name="productId2">The second product identifier</param>
        /// <returns>Cross-sell product</returns>
        public static CrossSellProduct FindCrossSellProduct(this IList<CrossSellProduct> source,
            int productId1, int productId2)
        {
            foreach (CrossSellProduct crossSellProduct in source)
                if (crossSellProduct.ProductId1 == productId1 && crossSellProduct.ProductId2 == productId2)
                    return crossSellProduct;
            return null;
        }

        /// <summary>
        /// Formats the stock availability/quantity message
        /// </summary>
        public static string FormatStockMessage(this Product product, string attributesXml,
            ILocalizationService localizationService, IProductAttributeParser productAttributeParser)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            if (localizationService == null)
                throw new ArgumentNullException("localizationService");

            if (productAttributeParser == null)
                throw new ArgumentNullException("productAttributeParser");
             

            string stockMessage = string.Empty;

            switch (product.ManageInventoryMethod)
            {
                case ManageInventoryMethod.ManageStock:
                    {
                        #region Manage stock

                        if (!product.DisplayStockAvailability)
                            return stockMessage;

                        var stockQuantity = product.GetTotalStockQuantity();
                        if (stockQuantity > 0)
                        {
                            stockMessage = product.DisplayStockQuantity ?
                                //display "in stock" with stock quantity
                                string.Format(localizationService.GetResource("Products.Availability.InStockWithQuantity"), stockQuantity) :
                                //display "in stock" without stock quantity
                                localizationService.GetResource("Products.Availability.InStock");
                        }
                        else
                        {
                            //out of stock

                            stockMessage = localizationService.GetResource("Products.Availability.OutOfStock");
                        }

                        #endregion
                    }
                    break;
                case ManageInventoryMethod.ManageStockByAttributes:
                    {
                        #region Manage stock by attributes

                        if (!product.DisplayStockAvailability)
                            return stockMessage;

                        var combination = productAttributeParser.FindProductAttributeCombination(product, attributesXml);
                        if (combination != null)
                        {
                            //combination exists
                            var stockQuantity = combination.StockQuantity;
                            if (stockQuantity > 0)
                            {
                                stockMessage = product.DisplayStockQuantity ?
                                    //display "in stock" with stock quantity
                                    string.Format(localizationService.GetResource("Products.Availability.InStockWithQuantity"), stockQuantity) :
                                    //display "in stock" without stock quantity
                                    localizationService.GetResource("Products.Availability.InStock");
                            }
                            else if (combination.AllowOutOfStockOrders)
                            {
                                stockMessage = localizationService.GetResource("Products.Availability.InStock");
                            }
                            else
                            {

                                stockMessage = localizationService.GetResource("Products.Availability.OutOfStock");
                            }
                        }
                        else
                        {
                            //no combination configured
                            if (product.AllowAddingOnlyExistingAttributeCombinations)
                            { 
                                stockMessage = localizationService.GetResource("Products.Availability.OutOfStock");
                            }
                            else
                            {
                                stockMessage = localizationService.GetResource("Products.Availability.InStock");
                            }
                        }

                        #endregion
                    }
                    break;
                case ManageInventoryMethod.DontManageStock:
                default:
                    return stockMessage;
            }
            return stockMessage;
        }

        /// <summary>
        /// Indicates whether a product tag exists
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Result</returns>
        public static bool ProductTagExists(this Product product,
            int productTagId)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            bool result = product.ProductTags.ToList().Find(pt => pt.Id == productTagId) != null;
            return result;
        }

        /// <summary>
        /// Get a list of allowed quantities (parse 'AllowedQuantities' property)
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Result</returns>
        public static int[] ParseAllowedQuantities(this Product product)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            var result = new List<int>();
            if (!String.IsNullOrWhiteSpace(product.AllowedQuantities))
            {
                product.AllowedQuantities
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .ForEach(qtyStr =>
                    {
                        int qty;
                        if (int.TryParse(qtyStr.Trim(), out qty))
                        {
                            result.Add(qty);
                        }
                    });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Get total quantity
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="useReservedQuantity">
        /// A value indicating whether we should consider "Reserved Quantity" property 
        /// when "multiple warehouses" are used
        /// </param>
        /// <param name="warehouseId">
        /// Warehouse identifier. Used to limit result to certain warehouse.
        /// Used only with "multiple warehouses" enabled.
        /// </param>
        /// <returns>Result</returns>
        public static int GetTotalStockQuantity(this Product product,
            bool useReservedQuantity = true, int warehouseId = 0)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            if (product.ManageInventoryMethod != ManageInventoryMethod.ManageStock)
            {
                //We can calculate total stock quantity when 'Manage inventory' property is set to 'Track inventory'
                return 0;
            }

           

            return product.StockQuantity;
        }
 


        /// <summary>
        /// Gets product code
        /// </summary> 
        private static void GetProductCode(this Product product, string attributesXml, IProductAttributeParser productAttributeParser,
            out string productCode)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            productCode = null; 

            if (!String.IsNullOrEmpty(attributesXml) &&
                product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
            {
                //manage stock by attribute combinations
                if (productAttributeParser == null)
                    throw new ArgumentNullException("productAttributeParser");

                //let's find appropriate record
                var combination = productAttributeParser.FindProductAttributeCombination(product, attributesXml);
                if (combination != null)
                {
                    productCode = combination.ProductCode; 
                }
            }

            if (String.IsNullOrEmpty(productCode))
                productCode = product.ProductCode; 
        }

        /// <summary>
        /// Formats Product Code
        /// </summary> 
        public static string FormatProductCode(this Product product, string attributesXml = null, IProductAttributeParser productAttributeParser = null)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            string productCode;

            product.GetProductCode(attributesXml, productAttributeParser,
                out productCode);

            return productCode;
        }
        /// <summary>
        /// Formats start/end date for rental product
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="date">Date</param>
        /// <returns>Formatted date</returns>
        public static string FormatDepartureDate(this Product product, DateTime date)
        {
            if (product == null)
                throw new ArgumentNullException("product"); 

            return date.ToShortDateString();
        }
         

        /// <summary>
        /// Format base price (PAngV)
        /// </summary> 
        public static string FormatBasePrice(this Product product, decimal? productPrice, ILocalizationService localizationService,
             ICurrencyService currencyService, IWorkContext workContext, IPriceFormatter priceFormatter)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            if (localizationService == null)
                throw new ArgumentNullException("localizationService");
 
            if (currencyService == null)
                throw new ArgumentNullException("currencyService");

            if (workContext == null)
                throw new ArgumentNullException("workContext");

            if (priceFormatter == null)
                throw new ArgumentNullException("priceFormatter");

            if (!product.BasepriceEnabled)
                return null;

            var productAmount = product.BasepriceAmount;
            //Amount in product cannot be 0
            if (productAmount == 0)
                return null;
            var referenceAmount = product.BasepriceBaseAmount;
          

            productPrice = productPrice.HasValue ? productPrice.Value : product.Price;

            decimal basePrice = productPrice.Value /  referenceAmount;
            decimal basePriceInCurrentCurrency = currencyService.ConvertFromPrimaryStoreCurrency(basePrice, workContext.WorkingCurrency);
            string basePriceStr = priceFormatter.FormatPrice(basePriceInCurrentCurrency, true, false);

            var result = string.Format(localizationService.GetResource("Products.BasePrice"),
                basePriceStr, referenceAmount.ToString("G29"));
            return result;
        }
    }
}
