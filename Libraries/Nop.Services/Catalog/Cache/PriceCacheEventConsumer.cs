﻿using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Services.Events;

namespace Nop.Services.Catalog.Cache
{
    /// <summary>
    /// Price cache event consumer (used for caching of prices)
    /// </summary>
    public partial class PriceCacheEventConsumer: 
        //settings
        IConsumer<EntityUpdated<Setting>>,
        //categories
        IConsumer<EntityInserted<Category>>,
        IConsumer<EntityUpdated<Category>>,
        IConsumer<EntityDeleted<Category>>,
        //destinations
        IConsumer<EntityInserted<Destination>>,
        IConsumer<EntityUpdated<Destination>>,
        IConsumer<EntityDeleted<Destination>>,
        //product categories
        IConsumer<EntityInserted<ProductCategory>>,
        IConsumer<EntityUpdated<ProductCategory>>,
        IConsumer<EntityDeleted<ProductCategory>>,
        //product destinations
        IConsumer<EntityInserted<ProductDestination>>,
        IConsumer<EntityUpdated<ProductDestination>>,
        IConsumer<EntityDeleted<ProductDestination>>,
        //products
        IConsumer<EntityInserted<Product>>,
        IConsumer<EntityUpdated<Product>>,
        IConsumer<EntityDeleted<Product>>,
        //tier prices
        IConsumer<EntityInserted<TierPrice>>,
        IConsumer<EntityUpdated<TierPrice>>,
        IConsumer<EntityDeleted<TierPrice>>,
        //orders
        IConsumer<EntityInserted<Order>>,
        IConsumer<EntityUpdated<Order>>,
        IConsumer<EntityDeleted<Order>>
    {
        /// <summary>
        /// Key for product prices
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : overridden product price
        /// {2} : additional charge
        /// {3} : include discounts (true, false)
        /// {4} : quantity
        /// {5} : roles of the current user
        /// {6} : current store ID
        /// </remarks>
        public const string PRODUCT_PRICE_MODEL_KEY = "Nop.totals.productprice-{0}-{1}-{2}-{3}-{4}-{5}-{6}";
        public const string PRODUCT_PRICE_PATTERN_KEY = "Nop.totals.productprice";

        /// <summary>
        /// Key for category IDs of a product
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : roles of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string PRODUCT_CATEGORY_IDS_MODEL_KEY = "Nop.totals.product.categoryids-{0}-{1}-{2}";
        public const string PRODUCT_CATEGORY_IDS_PATTERN_KEY = "Nop.totals.product.categoryids";

        /// <summary>
        /// Key for destination IDs of a product
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : roles of the current user
        /// {2} : current store ID
        /// </remarks>
        public const string PRODUCT_DESTINATION_IDS_MODEL_KEY = "Nop.totals.product.destinationids-{0}-{1}-{2}";
        public const string PRODUCT_DESTINATION_IDS_PATTERN_KEY = "Nop.totals.product.destinationids";

        private readonly ICacheManager _cacheManager;

        public PriceCacheEventConsumer()
        {
            //TODO inject static cache manager using constructor
            this._cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("nop_cache_static");
        }

        //settings
        public void HandleEvent(EntityUpdated<Setting> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }

        //categories
        public void HandleEvent(EntityInserted<Category> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Category> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Category> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }

        //destinations
        public void HandleEvent(EntityInserted<Destination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Destination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Destination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }

        //product categories
        public void HandleEvent(EntityInserted<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_CATEGORY_IDS_PATTERN_KEY);
        }

        //product destinations
        public void HandleEvent(EntityInserted<ProductDestination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<ProductDestination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<ProductDestination> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCT_DESTINATION_IDS_PATTERN_KEY);
        }

        //products
        public void HandleEvent(EntityInserted<Product> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Product> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Product> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }

        //tier prices
        public void HandleEvent(EntityInserted<TierPrice> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<TierPrice> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<TierPrice> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }

        //orders
        public void HandleEvent(EntityInserted<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_PRICE_PATTERN_KEY);
        }
    }
}