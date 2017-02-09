using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Services.Customers;
using Nop.Services.Events;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Destination service
    /// </summary>
    public partial class DestinationService : IDestinationService
    {
        #region Constants
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : destination ID
        /// </remarks>
        private const string DESTINATIONS_BY_ID_KEY = "Nop.destination.id-{0}";
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// {1} : destination ID
        /// {2} : page index
        /// {3} : page size
        /// {4} : current customer ID
        /// {5} : store ID
        /// </remarks>
        private const string PRODUCTDESTINATIONS_ALLBYDESTINATIONID_KEY = "Nop.productdestination.allbydestinationid-{0}-{1}-{2}-{3}-{4}-{5}";
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// {1} : product ID
        /// {2} : current customer ID
        /// {3} : store ID
        /// </remarks>
        private const string PRODUCTDESTINATIONS_ALLBYPRODUCTID_KEY = "Nop.productdestination.allbyproductid-{0}-{1}-{2}-{3}";
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string DESTINATIONS_PATTERN_KEY = "Nop.destination.";
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string PRODUCTDESTINATIONS_PATTERN_KEY = "Nop.productdestination.";

        #endregion

        #region Fields

        private readonly IRepository<Destination> _destinationRepository;
        private readonly IRepository<ProductDestination> _productDestinationRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<AclRecord> _aclRepository;
        private readonly IRepository<StoreMapping> _storeMappingRepository;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="destinationRepository">Category repository</param>
        /// <param name="productDestinationRepository">ProductCategory repository</param>
        /// <param name="productRepository">Product repository</param>
        /// <param name="aclRepository">ACL record repository</param>
        /// <param name="storeMappingRepository">Store mapping repository</param>
        /// <param name="workContext">Work context</param>
        /// <param name="storeContext">Store context</param>
        /// <param name="catalogSettings">Catalog settings</param>
        /// <param name="eventPublisher">Event published</param>
        public DestinationService(ICacheManager cacheManager,
            IRepository<Destination> destinationRepository,
            IRepository<ProductDestination> productDestinationRepository,
            IRepository<Product> productRepository,
            IRepository<AclRecord> aclRepository,
            IRepository<StoreMapping> storeMappingRepository,
            IWorkContext workContext,
            IStoreContext storeContext,
            CatalogSettings catalogSettings,
            IEventPublisher eventPublisher)
        {
            this._cacheManager = cacheManager;
            this._destinationRepository = destinationRepository;
            this._productDestinationRepository = productDestinationRepository;
            this._productRepository = productRepository;
            this._aclRepository = aclRepository;
            this._storeMappingRepository = storeMappingRepository;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._catalogSettings = catalogSettings;
            this._eventPublisher = eventPublisher;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Deletes a destination
        /// </summary>
        /// <param name="destination">Destination</param>
        public virtual void DeleteDestination(Destination destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            
            destination.Deleted = true;
            UpdateDestination(destination);

            //event notification
            _eventPublisher.EntityDeleted(destination);
        }

        /// <summary>
        /// Gets all destinations
        /// </summary>
        /// <param name="destinationName">Destination name</param>
        /// <param name="storeId">Store identifier; 0 if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Destinations</returns>
        public virtual IPagedList<Destination> GetAllDestinations(string destinationName = "",
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue, 
            bool showHidden = false)
        {
            var query = _destinationRepository.Table;
            if (!showHidden)
                query = query.Where(m => m.Published);
            if (!String.IsNullOrWhiteSpace(destinationName))
                query = query.Where(m => m.Name.Contains(destinationName));
            query = query.Where(m => !m.Deleted);
            query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);

            if ((storeId > 0 && !_catalogSettings.IgnoreStoreLimitations) || (!showHidden && !_catalogSettings.IgnoreAcl))
            {
                if (!showHidden && !_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = _workContext.CurrentCustomer.GetCustomerRoleIds();
                    query = from m in query
                            join acl in _aclRepository.Table
                            on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into m_acl
                            from acl in m_acl.DefaultIfEmpty()
                            where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                            select m;
                }
                if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
                {
                    //Store mapping
                    query = from m in query
                            join sm in _storeMappingRepository.Table
                            on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into m_sm
                            from sm in m_sm.DefaultIfEmpty()
                            where !m.LimitedToStores || storeId == sm.StoreId
                            select m;
                }
                //only distinct destinations (group by ID)
                query = from m in query
                        group m by m.Id
                            into mGroup
                            orderby mGroup.Key
                            select mGroup.FirstOrDefault();
                query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id);
            }

            return new PagedList<Destination>(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets a destination
        /// </summary>
        /// <param name="destinationId">Destination identifier</param>
        /// <returns>Destination</returns>
        public virtual Destination GetDestinationById(int destinationId)
        {
            if (destinationId == 0)
                return null;
            
            string key = string.Format(DESTINATIONS_BY_ID_KEY, destinationId);
            return _cacheManager.Get(key, () => _destinationRepository.GetById(destinationId));
        }

        /// <summary>
        /// Inserts a destination
        /// </summary>
        /// <param name="destination">Destination</param>
        public virtual void InsertDestination(Destination destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            _destinationRepository.Insert(destination);

            //cache
            _cacheManager.RemoveByPattern(DESTINATIONS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTDESTINATIONS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityInserted(destination);
        }

        /// <summary>
        /// Updates the destination
        /// </summary>
        /// <param name="destination">Destination</param>
        public virtual void UpdateDestination(Destination destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            _destinationRepository.Update(destination);

            //cache
            _cacheManager.RemoveByPattern(DESTINATIONS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTDESTINATIONS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityUpdated(destination);
        }
        

        /// <summary>
        /// Deletes a product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        public virtual void DeleteProductDestination(ProductDestination productDestination)
        {
            if (productDestination == null)
                throw new ArgumentNullException("productDestination");

            _productDestinationRepository.Delete(productDestination);

            //cache
            _cacheManager.RemoveByPattern(DESTINATIONS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTDESTINATIONS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityDeleted(productDestination);
        }

        /// <summary>
        /// Gets product destination collection
        /// </summary>
        /// <param name="destinationId">Destination identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product destination collection</returns>
        public virtual IPagedList<ProductDestination> GetProductDestinationsByDestinationId(int destinationId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            if (destinationId == 0)
                return new PagedList<ProductDestination>(new List<ProductDestination>(), pageIndex, pageSize);

            string key = string.Format(PRODUCTDESTINATIONS_ALLBYDESTINATIONID_KEY, showHidden, destinationId, pageIndex, pageSize, _workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id);
            return _cacheManager.Get(key, () =>
            {
                var query = from pm in _productDestinationRepository.Table
                            join p in _productRepository.Table on pm.ProductId equals p.Id
                            where pm.DestinationId == destinationId &&
                                  !p.Deleted &&
                                  (showHidden || p.Published)
                            orderby pm.DisplayOrder, pm.Id
                            select pm;

                if (!showHidden && (!_catalogSettings.IgnoreAcl || !_catalogSettings.IgnoreStoreLimitations))
                {
                    if (!_catalogSettings.IgnoreAcl)
                    {
                        //ACL (access control list)
                        var allowedCustomerRolesIds = _workContext.CurrentCustomer.GetCustomerRoleIds();
                        query = from pm in query
                                join m in _destinationRepository.Table on pm.DestinationId equals m.Id
                                join acl in _aclRepository.Table
                                on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into m_acl
                                from acl in m_acl.DefaultIfEmpty()
                                where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                                select pm;
                    }
                    if (!_catalogSettings.IgnoreStoreLimitations)
                    {
                        //Store mapping
                        var currentStoreId = _storeContext.CurrentStore.Id;
                        query = from pm in query
                                join m in _destinationRepository.Table on pm.DestinationId equals m.Id
                                join sm in _storeMappingRepository.Table
                                on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into m_sm
                                from sm in m_sm.DefaultIfEmpty()
                                where !m.LimitedToStores || currentStoreId == sm.StoreId
                                select pm;
                    }

                    //only distinct destinations (group by ID)
                    query = from pm in query
                            group pm by pm.Id
                            into pmGroup
                            orderby pmGroup.Key
                            select pmGroup.FirstOrDefault();
                    query = query.OrderBy(pm => pm.DisplayOrder).ThenBy(pm => pm.Id);
                }

                var productDestinations = new PagedList<ProductDestination>(query, pageIndex, pageSize);
                return productDestinations;
            });
        }

        /// <summary>
        /// Gets a product destination mapping collection
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product destination mapping collection</returns>
        public virtual IList<ProductDestination> GetProductDestinationsByProductId(int productId, bool showHidden = false)
        {
            if (productId == 0)
                return new List<ProductDestination>();

            string key = string.Format(PRODUCTDESTINATIONS_ALLBYPRODUCTID_KEY, showHidden, productId, _workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id);
            return _cacheManager.Get(key, () =>
            {
                var query = from pm in _productDestinationRepository.Table
                            join m in _destinationRepository.Table on pm.DestinationId equals m.Id
                            where pm.ProductId == productId &&
                                !m.Deleted &&
                                (showHidden || m.Published)
                            orderby pm.DisplayOrder, pm.Id
                            select pm;


                if (!showHidden && (!_catalogSettings.IgnoreAcl || !_catalogSettings.IgnoreStoreLimitations))
                {
                    if (!_catalogSettings.IgnoreAcl)
                    {
                        //ACL (access control list)
                        var allowedCustomerRolesIds = _workContext.CurrentCustomer.GetCustomerRoleIds();
                        query = from pm in query
                                join m in _destinationRepository.Table on pm.DestinationId equals m.Id
                                join acl in _aclRepository.Table
                                on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into m_acl
                                from acl in m_acl.DefaultIfEmpty()
                                where !m.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                                select pm;
                    }

                    if (!_catalogSettings.IgnoreStoreLimitations)
                    {
                        //Store mapping
                        var currentStoreId = _storeContext.CurrentStore.Id;
                        query = from pm in query
                                join m in _destinationRepository.Table on pm.DestinationId equals m.Id
                                join sm in _storeMappingRepository.Table
                                on new { c1 = m.Id, c2 = "Destination" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into m_sm
                                from sm in m_sm.DefaultIfEmpty()
                                where !m.LimitedToStores || currentStoreId == sm.StoreId
                                select pm;
                    }

                    //only distinct destinations (group by ID)
                    query = from pm in query
                            group pm by pm.Id
                            into mGroup
                            orderby mGroup.Key
                            select mGroup.FirstOrDefault();
                    query = query.OrderBy(pm => pm.DisplayOrder).ThenBy(pm => pm.Id);
                }

                var productDestinations = query.ToList();
                return productDestinations;
            });
        }
        
        /// <summary>
        /// Gets a product destination mapping 
        /// </summary>
        /// <param name="productDestinationId">Product destination mapping identifier</param>
        /// <returns>Product destination mapping</returns>
        public virtual ProductDestination GetProductDestinationById(int productDestinationId)
        {
            if (productDestinationId == 0)
                return null;

            return _productDestinationRepository.GetById(productDestinationId);
        }

        /// <summary>
        /// Inserts a product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        public virtual void InsertProductDestination(ProductDestination productDestination)
        {
            if (productDestination == null)
                throw new ArgumentNullException("productDestination");

            _productDestinationRepository.Insert(productDestination);

            //cache
            _cacheManager.RemoveByPattern(DESTINATIONS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTDESTINATIONS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityInserted(productDestination);
        }

        /// <summary>
        /// Updates the product destination mapping
        /// </summary>
        /// <param name="productDestination">Product destination mapping</param>
        public virtual void UpdateProductDestination(ProductDestination productDestination)
        {
            if (productDestination == null)
                throw new ArgumentNullException("productDestination");

            _productDestinationRepository.Update(productDestination);

            //cache
            _cacheManager.RemoveByPattern(DESTINATIONS_PATTERN_KEY);
            _cacheManager.RemoveByPattern(PRODUCTDESTINATIONS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityUpdated(productDestination);
        }


        /// <summary>
        /// Get destination IDs for products
        /// </summary>
        /// <param name="productIds">Products IDs</param>
        /// <returns>Destination IDs for products</returns>
        public virtual IDictionary<int, int[]> GetProductDestinationIds(int[] productIds)
        {
            var query = _productDestinationRepository.Table;

            return query.Where(p => productIds.Contains(p.ProductId))
                .Select(p => new {p.ProductId, p.DestinationId}).ToList()
                .GroupBy(a => a.ProductId)
                .ToDictionary(items => items.Key, items => items.Select(a => a.DestinationId).ToArray());
        }


        /// <summary>
        /// Returns a list of names of not existing destinations
        /// </summary>
        /// <param name="destinationNames">The names of the destinations to check</param>
        /// <returns>List of names not existing destinations</returns>
        public virtual string[] GetNotExistingDestinations(string[] destinationNames)
        {
            if (destinationNames == null)
                throw new ArgumentNullException("destinationNames");

            var query = _destinationRepository.Table;
            var queryFilter = destinationNames.Distinct().ToArray();
            var filter = query.Select(m => m.Name).Where(m => queryFilter.Contains(m)).ToList();
            return queryFilter.Except(filter).ToArray();
        }

        #endregion
    }
}
