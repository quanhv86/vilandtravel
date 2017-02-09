using System;
using System.Collections.Generic;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Catalog
{
    /// <summary>
    /// Represents a product
    /// </summary>
    public partial class Product : BaseEntity, ILocalizedEntity, ISlugSupported, IAclSupported, IStoreMappingSupported
    {
        private ICollection<ProductCategory> _productCategories;
        private ICollection<ProductDestination> _productDestinations;
        private ICollection<ProductPicture> _productPictures;
        private ICollection<ProductReview> _productReviews;
        private ICollection<ProductSpecificationAttribute> _productSpecificationAttributes;
        private ICollection<ProductTag> _productTags;
        private ICollection<ProductAttributeMapping> _productAttributeMappings;
        private ICollection<ProductAttributeCombination> _productAttributeCombinations;
        private ICollection<TierPrice> _tierPrices;
        private ICollection<Discount> _appliedDiscounts; 


        /// <summary>
        /// Gets or sets the product type identifier
        /// </summary>
        public int ProductTypeId { get; set; }
       
        public int ParentGroupedProductId { get; set; }
       
        public bool VisibleIndividually { get; set; }
         
        public string Name { get; set; }
      
        public string ShortDescription { get; set; }
        
        public string FullDescription { get; set; }
         
        public string AdminComment { get; set; }
         
        public int ProductTemplateId { get; set; } 
         
        public bool ShowOnHomePage { get; set; }
         
        public string MetaKeywords { get; set; }
         
        public string MetaDescription { get; set; }
         
        public string MetaTitle { get; set; }
       
        public bool AllowCustomerReviews { get; set; }
        
        public int ApprovedRatingSum { get; set; }
         
        public int NotApprovedRatingSum { get; set; }
       
        public int ApprovedTotalReviews { get; set; }
        
        public int NotApprovedTotalReviews { get; set; }
         
        public bool SubjectToAcl { get; set; }
         
        public bool LimitedToStores { get; set; }
         
        public string ProductCode { get; set; }
         
        public bool IsDownload { get; set; }
      
        public int DownloadId { get; set; }
       
        public bool HasSampleDownload { get; set; }
        
        public int SampleDownloadId { get; set; }
        
        public bool HasUserAgreement { get; set; }
         
        public string UserAgreementText { get; set; } 
  
        public bool IsTaxExempt { get; set; }
       
        public int TaxCategoryId { get; set; }
         
        public bool IsTelecommunicationsOrBroadcastingOrElectronicServices { get; set; }
         
        public int ManageInventoryMethodId { get; set; }
       
        public int ProductAvailabilityRangeId { get; set; }
      
        public int StockQuantity { get; set; }
         
        public bool DisplayStockAvailability { get; set; }
        
        public bool DisplayStockQuantity { get; set; }
    
        public int MinStockQuantity { get; set; }
     
        public int LowStockActivityId { get; set; }
      
        public int NotifyAdminForQuantityBelow { get; set; }
         
        public int BackorderModeId { get; set; }
        
        public bool AllowBackInStockSubscriptions { get; set; }
       
        public int OrderMinimumQuantity { get; set; }
        
        public int OrderMaximumQuantity { get; set; }
       
        public string AllowedQuantities { get; set; }
         
        public bool AllowAddingOnlyExistingAttributeCombinations { get; set; }
     
        public bool DisableBuyButton { get; set; }
        
        public bool DisableWishlistButton { get; set; }
         
        public bool CallForPrice { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal OldPrice { get; set; }
       
        public decimal ProductCost { get; set; }
         
        public bool CustomerEntersPrice { get; set; }
         
        public decimal MinimumCustomerEnteredPrice { get; set; }
        
        public decimal MaximumCustomerEnteredPrice { get; set; }
         
        public bool BasepriceEnabled { get; set; }
          
        public decimal BasepriceAmount { get; set; }
         
        public int BasepriceUnitId { get; set; }
         
        public decimal BasepriceBaseAmount { get; set; }
         
        public int BasepriceBaseUnitId { get; set; }
         
        public bool MarkAsNew { get; set; }
      
        public DateTime? MarkAsNewStartDateTimeUtc { get; set; }
      
        public DateTime? MarkAsNewEndDateTimeUtc { get; set; }
         
        public bool HasTierPrices { get; set; } 

        public bool HasDiscountsApplied { get; set; }
          
        public DateTime? AvailableStartDateTimeUtc { get; set; }
         
        public DateTime? AvailableEndDateTimeUtc { get; set; }
         
        public int DisplayOrder { get; set; }
        
        public bool Published { get; set; }
      
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the date and time of product creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
        /// <summary>
        /// Gets or sets the date and time of product update
        /// </summary>
        public DateTime UpdatedOnUtc { get; set; }






        /// <summary>
        /// Gets or sets the product type
        /// </summary>
        public ProductType ProductType
        {
            get
            {
                return (ProductType)this.ProductTypeId;
            }
            set
            {
                this.ProductTypeId = (int)value;
            }
        }
 
 
         
        /// <summary>
        /// Gets or sets the low stock activity
        /// </summary>
        public LowStockActivity LowStockActivity
        {
            get
            {
                return (LowStockActivity)this.LowStockActivityId;
            }
            set
            {
                this.LowStockActivityId = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating how to manage inventory
        /// </summary>
        public ManageInventoryMethod ManageInventoryMethod
        {
            get
            {
                return (ManageInventoryMethod)this.ManageInventoryMethodId;
            }
            set
            {
                this.ManageInventoryMethodId = (int)value;
            }
        }
  

        /// <summary>
        /// Gets or sets the collection of ProductCategory
        /// </summary>
        public virtual ICollection<ProductCategory> ProductCategories
        {
            get { return _productCategories ?? (_productCategories = new List<ProductCategory>()); }
            protected set { _productCategories = value; }
        }

        /// <summary>
        /// Gets or sets the collection of ProductDestination
        /// </summary>
        public virtual ICollection<ProductDestination> ProductDestinations
        {
            get { return _productDestinations ?? (_productDestinations = new List<ProductDestination>()); }
            protected set { _productDestinations = value; }
        }

        /// <summary>
        /// Gets or sets the collection of ProductPicture
        /// </summary>
        public virtual ICollection<ProductPicture> ProductPictures
        {
            get { return _productPictures ?? (_productPictures = new List<ProductPicture>()); }
            protected set { _productPictures = value; }
        }

        /// <summary>
        /// Gets or sets the collection of product reviews
        /// </summary>
        public virtual ICollection<ProductReview> ProductReviews
        {
            get { return _productReviews ?? (_productReviews = new List<ProductReview>()); }
            protected set { _productReviews = value; }
        }

        /// <summary>
        /// Gets or sets the product specification attribute
        /// </summary>
        public virtual ICollection<ProductSpecificationAttribute> ProductSpecificationAttributes
        {
            get { return _productSpecificationAttributes ?? (_productSpecificationAttributes = new List<ProductSpecificationAttribute>()); }
            protected set { _productSpecificationAttributes = value; }
        }

        /// <summary>
        /// Gets or sets the product tags
        /// </summary>
        public virtual ICollection<ProductTag> ProductTags
        {
            get { return _productTags ?? (_productTags = new List<ProductTag>()); }
            protected set { _productTags = value; }
        }

        /// <summary>
        /// Gets or sets the product attribute mappings
        /// </summary>
        public virtual ICollection<ProductAttributeMapping> ProductAttributeMappings
        {
            get { return _productAttributeMappings ?? (_productAttributeMappings = new List<ProductAttributeMapping>()); }
            protected set { _productAttributeMappings = value; }
        }

        /// <summary>
        /// Gets or sets the product attribute combinations
        /// </summary>
        public virtual ICollection<ProductAttributeCombination> ProductAttributeCombinations
        {
            get { return _productAttributeCombinations ?? (_productAttributeCombinations = new List<ProductAttributeCombination>()); }
            protected set { _productAttributeCombinations = value; }
        }

        /// <summary>
        /// Gets or sets the tier prices
        /// </summary>
        public virtual ICollection<TierPrice> TierPrices
        {
            get { return _tierPrices ?? (_tierPrices = new List<TierPrice>()); }
            protected set { _tierPrices = value; }
        }

        /// <summary>
        /// Gets or sets the collection of applied discounts
        /// </summary>
        public virtual ICollection<Discount> AppliedDiscounts
        {
            get { return _appliedDiscounts ?? (_appliedDiscounts = new List<Discount>()); }
            protected set { _appliedDiscounts = value; }
        }
        
       
    }
}