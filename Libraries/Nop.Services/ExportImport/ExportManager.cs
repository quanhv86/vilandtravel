using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport.Help;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Tax;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Nop.Services.ExportImport
{
    /// <summary>
    /// Export manager
    /// </summary>
    public partial class ExportManager : IExportManager
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly IDestinationService _destinationService;
        private readonly ICustomerService _customerService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IPictureService _pictureService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly ProductEditorSettings _productEditorSettings;
        private readonly IProductTemplateService _productTemplateService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICustomerAttributeFormatter _customerAttributeFormatter;

        #endregion

        #region Ctor

        public ExportManager(ICategoryService categoryService,
            IDestinationService destinationService,
            ICustomerService customerService,
            IProductAttributeService productAttributeService,
            IPictureService pictureService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IStoreService storeService,
            IWorkContext workContext,
            ProductEditorSettings productEditorSettings,
            IProductTemplateService productTemplateService,
            ITaxCategoryService taxCategoryService,
            CatalogSettings catalogSettings,
            IGenericAttributeService genericAttributeService,
            ICustomerAttributeFormatter customerAttributeFormatter)
        {
            this._categoryService = categoryService;
            this._destinationService = destinationService;
            this._customerService = customerService;
            this._productAttributeService = productAttributeService;
            this._pictureService = pictureService;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._storeService = storeService;
            this._workContext = workContext;
            this._productEditorSettings = productEditorSettings;
            this._productTemplateService = productTemplateService;
            this._taxCategoryService = taxCategoryService;
            this._catalogSettings = catalogSettings;
            this._genericAttributeService = genericAttributeService;
            this._customerAttributeFormatter = customerAttributeFormatter;
        }

        #endregion

        #region Utilities

        protected virtual void WriteCategories(XmlWriter xmlWriter, int parentCategoryId)
        {
            var categories = _categoryService.GetAllCategoriesByParentCategoryId(parentCategoryId, true);
            if (categories != null && categories.Any())
            {
                foreach (var category in categories)
                {
                    xmlWriter.WriteStartElement("Category");

                    xmlWriter.WriteString("Id", category.Id);

                    xmlWriter.WriteString("Name", category.Name);
                    xmlWriter.WriteString("Description", category.Description);
                    xmlWriter.WriteString("CategoryTemplateId", category.CategoryTemplateId);
                    xmlWriter.WriteString("MetaKeywords", category.MetaKeywords, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("MetaDescription", category.MetaDescription, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("MetaTitle", category.MetaTitle, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("SeName", category.GetSeName(0), IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("ParentCategoryId", category.ParentCategoryId);
                    xmlWriter.WriteString("PictureId", category.PictureId);
                    xmlWriter.WriteString("PageSize", category.PageSize, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("AllowCustomersToSelectPageSize", category.AllowCustomersToSelectPageSize, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("PageSizeOptions", category.PageSizeOptions, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("PriceRanges", category.PriceRanges, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("ShowOnHomePage", category.ShowOnHomePage, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("IncludeInTopMenu", category.IncludeInTopMenu, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("Published", category.Published, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("Deleted", category.Deleted, true);
                    xmlWriter.WriteString("DisplayOrder", category.DisplayOrder);
                    xmlWriter.WriteString("CreatedOnUtc", category.CreatedOnUtc, IgnoreExportCategoryProperty());
                    xmlWriter.WriteString("UpdatedOnUtc", category.UpdatedOnUtc, IgnoreExportCategoryProperty());

                    xmlWriter.WriteStartElement("Products");
                    var productCategories = _categoryService.GetProductCategoriesByCategoryId(category.Id, showHidden: true);
                    foreach (var productCategory in productCategories)
                    {
                        var product = productCategory.Product;
                        if (product != null && !product.Deleted)
                        {
                            xmlWriter.WriteStartElement("ProductCategory");
                            xmlWriter.WriteString("ProductCategoryId", productCategory.Id);
                            xmlWriter.WriteString("ProductId", productCategory.ProductId);
                            xmlWriter.WriteString("ProductName", product.Name);
                            xmlWriter.WriteString("IsFeaturedProduct", productCategory.IsFeaturedProduct);
                            xmlWriter.WriteString("DisplayOrder", productCategory.DisplayOrder);
                            xmlWriter.WriteEndElement();
                        }
                    }
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("SubCategories");
                    WriteCategories(xmlWriter, category.Id);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
            }
        }

        protected virtual void SetCaptionStyle(ExcelStyle style)
        {
            style.Fill.PatternType = ExcelFillStyle.Solid;
            style.Fill.BackgroundColor.SetColor(Color.FromArgb(184, 204, 228));
            style.Font.Bold = true;
        }

        /// <summary>
        /// Returns the path to the image file by ID
        /// </summary>
        /// <param name="pictureId">Picture ID</param>
        /// <returns>Path to the image file</returns>
        protected virtual string GetPictures(int pictureId)
        {
            var picture = _pictureService.GetPictureById(pictureId);
            return _pictureService.GetThumbLocalPath(picture);
        }

        /// <summary>
        /// Returns the list of categories for a product separated by a ";"
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>List of categories</returns>
        protected virtual string GetCategories(Product product)
        {
            string categoryNames = null;
            foreach (var pc in _categoryService.GetProductCategoriesByProductId(product.Id, true))
            {
                categoryNames += pc.Category.Name;
                categoryNames += ";";
            }
            return categoryNames;
        }

        /// <summary>
        /// Returns the list of destination for a product separated by a ";"
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>List of destination</returns>
        protected virtual string GetDestinations(Product product)
        {
            string destinationNames = null;
            foreach (var pm in _destinationService.GetProductDestinationsByProductId(product.Id, true))
            {
                destinationNames += pm.Destination.Name;
                destinationNames += ";";
            }
            return destinationNames;
        }

        /// <summary>
        /// Returns the list of product tag for a product separated by a ";"
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>List of product tag</returns>
        protected virtual string GetProductTags(Product product)
        {
            string productTagNames = null;

            foreach (var productTag in product.ProductTags)
            {
                productTagNames += productTag.Name;
                productTagNames += ";";
            }
            return productTagNames;
        }

        /// <summary>
        /// Returns the three first image associated with the product
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>three first image</returns>
        protected virtual string[] GetPictures(Product product)
        {
            //pictures (up to 3 pictures)
            string picture1 = null;
            string picture2 = null;
            string picture3 = null;
            var pictures = _pictureService.GetPicturesByProductId(product.Id, 3);
            for (var i = 0; i < pictures.Count; i++)
            {
                var pictureLocalPath = _pictureService.GetThumbLocalPath(pictures[i]);
                switch (i)
                {
                    case 0:
                        picture1 = pictureLocalPath;
                        break;
                    case 1:
                        picture2 = pictureLocalPath;
                        break;
                    case 2:
                        picture3 = pictureLocalPath;
                        break;
                }
            }
            return new[] { picture1, picture2, picture3 };
        }

        private bool IgnoreExportPoductProperty(Func<ProductEditorSettings, bool> func)
        {
            var productAdvancedMode = _workContext.CurrentCustomer.GetAttribute<bool>("product-advanced-mode");
            return !productAdvancedMode && !func(_productEditorSettings);
        }

        private bool IgnoreExportCategoryProperty()
        {
            return !_workContext.CurrentCustomer.GetAttribute<bool>("category-advanced-mode");
        }

        private bool IgnoreExportDestinationProperty()
        {
            return !_workContext.CurrentCustomer.GetAttribute<bool>("destination-advanced-mode");
        }

        /// <summary>
        /// Export objects to XLSX
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="properties">Class access to the object through its properties</param>
        /// <param name="itemsToExport">The objects to export</param>
        /// <returns></returns>
        protected virtual byte[] ExportToXlsx<T>(PropertyByName<T>[] properties, IEnumerable<T> itemsToExport)
        {
            using (var stream = new MemoryStream())
            {
                // ok, we can run the real code of the sample now
                using (var xlPackage = new ExcelPackage(stream))
                {
                    // uncomment this line if you want the XML written out to the outputDir
                    //xlPackage.DebugMode = true; 

                    // get handles to the worksheets
                    var worksheet = xlPackage.Workbook.Worksheets.Add(typeof(T).Name);
                    var fWorksheet = xlPackage.Workbook.Worksheets.Add("DataForFilters");
                    fWorksheet.Hidden = eWorkSheetHidden.VeryHidden;

                    //create Headers and format them 
                    var manager = new PropertyManager<T>(properties.Where(p => !p.Ignore));
                    manager.WriteCaption(worksheet, SetCaptionStyle);

                    var row = 2;
                    foreach (var items in itemsToExport)
                    {
                        manager.CurrentObject = items;
                        manager.WriteToXlsx(worksheet, row++, _catalogSettings.ExportImportUseDropdownlistsForAssociatedEntities, fWorksheet: fWorksheet);
                    }

                    xlPackage.Save();
                }
                return stream.ToArray();
            }
        }

        private byte[] ExportProductsToXlsxWithAttributes(PropertyByName<Product>[] properties, IEnumerable<Product> itemsToExport)
        {
            var attributeProperties = new[]
            {
                new PropertyByName<ExportProductAttribute>("AttributeId", p => p.AttributeId),
                new PropertyByName<ExportProductAttribute>("AttributeName", p => p.AttributeName),
                new PropertyByName<ExportProductAttribute>("AttributeTextPrompt", p => p.AttributeTextPrompt),
                new PropertyByName<ExportProductAttribute>("AttributeIsRequired", p => p.AttributeIsRequired),
                new PropertyByName<ExportProductAttribute>("AttributeControlType", p => p.AttributeControlTypeId)
                {
                    DropDownElements = AttributeControlType.TextBox.ToSelectList(useLocalization: false)
                },
                new PropertyByName<ExportProductAttribute>("AttributeDisplayOrder", p => p.AttributeDisplayOrder),
                new PropertyByName<ExportProductAttribute>("ProductAttributeValueId", p => p.Id),
                new PropertyByName<ExportProductAttribute>("ValueName", p => p.Name),
                new PropertyByName<ExportProductAttribute>("AttributeValueType", p => p.AttributeValueTypeId)
                {
                    DropDownElements = AttributeValueType.Simple.ToSelectList(useLocalization: false)
                },
                new PropertyByName<ExportProductAttribute>("AssociatedProductId", p => p.AssociatedProductId),
                new PropertyByName<ExportProductAttribute>("ColorSquaresRgb", p => p.ColorSquaresRgb),
                new PropertyByName<ExportProductAttribute>("ImageSquaresPictureId", p => p.ImageSquaresPictureId),
                new PropertyByName<ExportProductAttribute>("PriceAdjustment", p => p.PriceAdjustment),
                new PropertyByName<ExportProductAttribute>("WeightAdjustment", p => p.WeightAdjustment),
                new PropertyByName<ExportProductAttribute>("Cost", p => p.Cost),
                new PropertyByName<ExportProductAttribute>("CustomerEntersQty", p => p.CustomerEntersQty),
                new PropertyByName<ExportProductAttribute>("Quantity", p => p.Quantity),
                new PropertyByName<ExportProductAttribute>("IsPreSelected", p => p.IsPreSelected),
                new PropertyByName<ExportProductAttribute>("DisplayOrder", p => p.DisplayOrder),
                new PropertyByName<ExportProductAttribute>("PictureId", p => p.PictureId)
            };

            var attributeManager = new PropertyManager<ExportProductAttribute>(attributeProperties);

            using (var stream = new MemoryStream())
            {
                // ok, we can run the real code of the sample now
                using (var xlPackage = new ExcelPackage(stream))
                {
                    // uncomment this line if you want the XML written out to the outputDir
                    //xlPackage.DebugMode = true; 

                    // get handles to the worksheets
                    var worksheet = xlPackage.Workbook.Worksheets.Add(typeof(Product).Name);
                    var fpWorksheet = xlPackage.Workbook.Worksheets.Add("DataForProductsFilters");
                    fpWorksheet.Hidden = eWorkSheetHidden.VeryHidden;
                    var faWorksheet = xlPackage.Workbook.Worksheets.Add("DataForProductAttributesFilters");
                    faWorksheet.Hidden = eWorkSheetHidden.VeryHidden;

                    //create Headers and format them 
                    var manager = new PropertyManager<Product>(properties.Where(p => !p.Ignore));
                    manager.WriteCaption(worksheet, SetCaptionStyle);

                    var row = 2;
                    foreach (var item in itemsToExport)
                    {
                        manager.CurrentObject = item;
                        manager.WriteToXlsx(worksheet, row++, _catalogSettings.ExportImportUseDropdownlistsForAssociatedEntities, fWorksheet: fpWorksheet);

                        var attributes = item.ProductAttributeMappings.SelectMany(pam => pam.ProductAttributeValues.Select(pav => new ExportProductAttribute
                        {
                            AttributeId = pam.ProductAttribute.Id,
                            AttributeName = pam.ProductAttribute.Name,
                            AttributeTextPrompt = pam.TextPrompt,
                            AttributeIsRequired = pam.IsRequired,
                            AttributeControlTypeId = pam.AttributeControlTypeId,
                            AssociatedProductId = pav.AssociatedProductId,
                            AttributeDisplayOrder = pam.DisplayOrder,
                            Id = pav.Id,
                            Name = pav.Name,
                            AttributeValueTypeId = pav.AttributeValueTypeId,
                            ColorSquaresRgb = pav.ColorSquaresRgb,
                            ImageSquaresPictureId = pav.ImageSquaresPictureId,
                            PriceAdjustment = pav.PriceAdjustment,
                            WeightAdjustment = pav.WeightAdjustment,
                            Cost = pav.Cost,
                            CustomerEntersQty = pav.CustomerEntersQty,
                            Quantity = pav.Quantity,
                            IsPreSelected = pav.IsPreSelected,
                            DisplayOrder = pav.DisplayOrder,
                            PictureId = pav.PictureId
                        })).ToList();

                        attributes.AddRange(item.ProductAttributeMappings.Where(pam => !pam.ProductAttributeValues.Any()).Select(pam => new ExportProductAttribute
                        {
                            AttributeId = pam.ProductAttribute.Id,
                            AttributeName = pam.ProductAttribute.Name,
                            AttributeTextPrompt = pam.TextPrompt,
                            AttributeIsRequired = pam.IsRequired,
                            AttributeControlTypeId = pam.AttributeControlTypeId
                        }));

                        if (!attributes.Any())
                            continue;

                        attributeManager.WriteCaption(worksheet, SetCaptionStyle, row, ExportProductAttribute.ProducAttributeCellOffset);
                        worksheet.Row(row).OutlineLevel = 1;
                        worksheet.Row(row).Collapsed = true;

                        foreach (var exportProducAttribute in attributes)
                        {
                            row++;
                            attributeManager.CurrentObject = exportProducAttribute;
                            attributeManager.WriteToXlsx(worksheet, row, _catalogSettings.ExportImportUseDropdownlistsForAssociatedEntities, ExportProductAttribute.ProducAttributeCellOffset, faWorksheet);
                            worksheet.Row(row).OutlineLevel = 1;
                            worksheet.Row(row).Collapsed = true;
                        }

                        row++;
                    }

                    xlPackage.Save();
                }
                return stream.ToArray();
            }
        }

        private string GetCustomCustomerAttributes(Customer customer)
        {
            var selectedCustomerAttributes = customer.GetAttribute<string>(SystemCustomerAttributeNames.CustomCustomerAttributes, _genericAttributeService);
            return _customerAttributeFormatter.FormatAttributes(selectedCustomerAttributes, ";");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Export destination list to xml
        /// </summary>
        /// <param name="destinations">Destinations</param>
        /// <returns>Result in XML format</returns>
        public virtual string ExportDestinationsToXml(IList<Destination> destinations)
        {
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Destinations");
            xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

            foreach (var destination in destinations)
            {
                xmlWriter.WriteStartElement("Destination");

                xmlWriter.WriteString("DestinationId", destination.Id);
                xmlWriter.WriteString("Name", destination.Name);
                xmlWriter.WriteString("Description", destination.Description);
                xmlWriter.WriteString("DestinationTemplateId", destination.DestinationTemplateId);
                xmlWriter.WriteString("MetaKeywords", destination.MetaKeywords, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("MetaDescription", destination.MetaDescription, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("MetaTitle", destination.MetaTitle, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("SEName", destination.GetSeName(0), IgnoreExportDestinationProperty());
                xmlWriter.WriteString("PictureId", destination.PictureId);
                xmlWriter.WriteString("PageSize", destination.PageSize, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("AllowCustomersToSelectPageSize", destination.AllowCustomersToSelectPageSize, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("PageSizeOptions", destination.PageSizeOptions, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("PriceRanges", destination.PriceRanges, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("Published", destination.Published, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("Deleted", destination.Deleted, true);
                xmlWriter.WriteString("DisplayOrder", destination.DisplayOrder);
                xmlWriter.WriteString("CreatedOnUtc", destination.CreatedOnUtc, IgnoreExportDestinationProperty());
                xmlWriter.WriteString("UpdatedOnUtc", destination.UpdatedOnUtc, IgnoreExportDestinationProperty());

                xmlWriter.WriteStartElement("Products");
                var productDestinations = _destinationService.GetProductDestinationsByDestinationId(destination.Id, showHidden: true);
                if (productDestinations != null)
                {
                    foreach (var productDestination in productDestinations)
                    {
                        var product = productDestination.Product;
                        if (product != null && !product.Deleted)
                        {
                            xmlWriter.WriteStartElement("ProductDestination");
                            xmlWriter.WriteString("ProductDestinationId", productDestination.Id);
                            xmlWriter.WriteString("ProductId", productDestination.ProductId);
                            xmlWriter.WriteString("ProductName", product.Name);
                            xmlWriter.WriteString("IsFeaturedProduct", productDestination.IsFeaturedProduct);
                            xmlWriter.WriteString("DisplayOrder", productDestination.DisplayOrder);
                            xmlWriter.WriteEndElement();
                        }
                    }
                }
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Export destinations to XLSX
        /// </summary>
        /// <param name="destinations">Manufactures</param>
        public virtual byte[] ExportDestinationsToXlsx(IEnumerable<Destination> destinations)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Destination>("Id", p => p.Id),
                new PropertyByName<Destination>("Name", p => p.Name),
                new PropertyByName<Destination>("Description", p => p.Description),
                new PropertyByName<Destination>("DestinationTemplateId", p => p.DestinationTemplateId),
                new PropertyByName<Destination>("MetaKeywords", p => p.MetaKeywords, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("MetaDescription", p => p.MetaDescription, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("MetaTitle", p => p.MetaTitle, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("SeName", p => p.GetSeName(0), IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("Picture", p => GetPictures(p.PictureId)),
                new PropertyByName<Destination>("PageSize", p => p.PageSize, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("PageSizeOptions", p => p.PageSizeOptions, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("PriceRanges", p => p.PriceRanges, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("Published", p => p.Published, IgnoreExportDestinationProperty()),
                new PropertyByName<Destination>("DisplayOrder", p => p.DisplayOrder)
            };

            return ExportToXlsx(properties, destinations);
        }

        /// <summary>
        /// Export category list to xml
        /// </summary>
        /// <returns>Result in XML format</returns>
        public virtual string ExportCategoriesToXml()
        {
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Categories");
            xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);
            WriteCategories(xmlWriter, 0);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Export categories to XLSX
        /// </summary>
        /// <param name="categories">Categories</param>
        public virtual byte[] ExportCategoriesToXlsx(IEnumerable<Category> categories)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Category>("Id", p => p.Id),
                new PropertyByName<Category>("Name", p => p.Name),
                new PropertyByName<Category>("Description", p => p.Description),
                new PropertyByName<Category>("CategoryTemplateId", p => p.CategoryTemplateId),
                new PropertyByName<Category>("MetaKeywords", p => p.MetaKeywords, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("MetaDescription", p => p.MetaDescription, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("MetaTitle", p => p.MetaTitle, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("SeName", p => p.GetSeName(0), IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("ParentCategoryId", p => p.ParentCategoryId),
                new PropertyByName<Category>("Picture", p => GetPictures(p.PictureId)),
                new PropertyByName<Category>("PageSize", p => p.PageSize, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("PageSizeOptions", p => p.PageSizeOptions, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("PriceRanges", p => p.PriceRanges, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("ShowOnHomePage", p => p.ShowOnHomePage, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("IncludeInTopMenu", p => p.IncludeInTopMenu, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("Published", p => p.Published, IgnoreExportCategoryProperty()),
                new PropertyByName<Category>("DisplayOrder", p => p.DisplayOrder)
            };
            return ExportToXlsx(properties, categories);
        }

        /// <summary>
        /// Export product list to xml
        /// </summary>
        /// <param name="products">Products</param>
        /// <returns>Result in XML format</returns>
        public virtual string ExportProductsToXml(IList<Product> products)
        {
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Products");
            xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

            foreach (var product in products)
            {
                xmlWriter.WriteStartElement("Product");

                xmlWriter.WriteString("ProductId", product.Id, IgnoreExportPoductProperty(p => p.Id));
                xmlWriter.WriteString("ProductTypeId", product.ProductTypeId, IgnoreExportPoductProperty(p => p.ProductType));
                xmlWriter.WriteString("ParentGroupedProductId", product.ParentGroupedProductId, IgnoreExportPoductProperty(p => p.ProductType));
                xmlWriter.WriteString("VisibleIndividually", product.VisibleIndividually, IgnoreExportPoductProperty(p => p.VisibleIndividually));
                xmlWriter.WriteString("Name", product.Name);
                xmlWriter.WriteString("ShortDescription", product.ShortDescription);
                xmlWriter.WriteString("FullDescription", product.FullDescription);
                xmlWriter.WriteString("AdminComment", product.AdminComment, IgnoreExportPoductProperty(p => p.AdminComment));
                xmlWriter.WriteString("ProductTemplateId", product.ProductTemplateId, IgnoreExportPoductProperty(p => p.ProductTemplate));
                xmlWriter.WriteString("ShowOnHomePage", product.ShowOnHomePage, IgnoreExportPoductProperty(p => p.ShowOnHomePage));
                xmlWriter.WriteString("MetaKeywords", product.MetaKeywords, IgnoreExportPoductProperty(p => p.Seo));
                xmlWriter.WriteString("MetaDescription", product.MetaDescription, IgnoreExportPoductProperty(p => p.Seo));
                xmlWriter.WriteString("MetaTitle", product.MetaTitle, IgnoreExportPoductProperty(p => p.Seo));
                xmlWriter.WriteString("SEName", product.GetSeName(0), IgnoreExportPoductProperty(p => p.Seo));
                xmlWriter.WriteString("AllowCustomerReviews", product.AllowCustomerReviews, IgnoreExportPoductProperty(p => p.AllowCustomerReviews));
                xmlWriter.WriteString("ProductCode", product.ProductCode);
                xmlWriter.WriteString("IsDownload", product.IsDownload, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("DownloadId", product.DownloadId, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("HasSampleDownload", product.HasSampleDownload, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("SampleDownloadId", product.SampleDownloadId, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("HasUserAgreement", product.HasUserAgreement, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("UserAgreementText", product.UserAgreementText, IgnoreExportPoductProperty(p => p.DownloadableProduct));
                xmlWriter.WriteString("IsTaxExempt", product.IsTaxExempt);
                xmlWriter.WriteString("TaxCategoryId", product.TaxCategoryId);
                xmlWriter.WriteString("IsTelecommunicationsOrBroadcastingOrElectronicServices", product.IsTelecommunicationsOrBroadcastingOrElectronicServices, IgnoreExportPoductProperty(p => p.TelecommunicationsBroadcastingElectronicServices));
                xmlWriter.WriteString("ManageInventoryMethodId", product.ManageInventoryMethodId);
                xmlWriter.WriteString("ProductAvailabilityRangeId", product.ProductAvailabilityRangeId, IgnoreExportPoductProperty(p => p.ProductAvailabilityRange));
                xmlWriter.WriteString("StockQuantity", product.StockQuantity);
                xmlWriter.WriteString("DisplayStockAvailability", product.DisplayStockAvailability, IgnoreExportPoductProperty(p => p.DisplayStockAvailability));
                xmlWriter.WriteString("DisplayStockQuantity", product.DisplayStockQuantity, IgnoreExportPoductProperty(p => p.DisplayStockQuantity));
                xmlWriter.WriteString("MinStockQuantity", product.MinStockQuantity, IgnoreExportPoductProperty(p => p.MinimumStockQuantity));
                xmlWriter.WriteString("LowStockActivityId", product.LowStockActivityId, IgnoreExportPoductProperty(p => p.LowStockActivity));
                xmlWriter.WriteString("NotifyAdminForQuantityBelow", product.NotifyAdminForQuantityBelow, IgnoreExportPoductProperty(p => p.NotifyAdminForQuantityBelow));
                xmlWriter.WriteString("BackorderModeId", product.BackorderModeId, IgnoreExportPoductProperty(p => p.Backorders));
                xmlWriter.WriteString("AllowBackInStockSubscriptions", product.AllowBackInStockSubscriptions, IgnoreExportPoductProperty(p => p.AllowBackInStockSubscriptions));
                xmlWriter.WriteString("OrderMinimumQuantity", product.OrderMinimumQuantity, IgnoreExportPoductProperty(p => p.MinimumCartQuantity));
                xmlWriter.WriteString("OrderMaximumQuantity", product.OrderMaximumQuantity, IgnoreExportPoductProperty(p => p.MaximumCartQuantity));
                xmlWriter.WriteString("AllowedQuantities", product.AllowedQuantities, IgnoreExportPoductProperty(p => p.AllowedQuantities));
                xmlWriter.WriteString("AllowAddingOnlyExistingAttributeCombinations", product.AllowAddingOnlyExistingAttributeCombinations, IgnoreExportPoductProperty(p => p.AllowAddingOnlyExistingAttributeCombinations));
                xmlWriter.WriteString("DisableBuyButton", product.DisableBuyButton, IgnoreExportPoductProperty(p => p.DisableBuyButton));
                xmlWriter.WriteString("DisableWishlistButton", product.DisableWishlistButton, IgnoreExportPoductProperty(p => p.DisableWishlistButton)); 
                xmlWriter.WriteString("CallForPrice", product.CallForPrice, IgnoreExportPoductProperty(p => p.CallForPrice));
                xmlWriter.WriteString("Price", product.Price);
                xmlWriter.WriteString("OldPrice", product.OldPrice, IgnoreExportPoductProperty(p => p.OldPrice));
                xmlWriter.WriteString("ProductCost", product.ProductCost, IgnoreExportPoductProperty(p => p.ProductCost));
                xmlWriter.WriteString("CustomerEntersPrice", product.CustomerEntersPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice));
                xmlWriter.WriteString("MinimumCustomerEnteredPrice", product.MinimumCustomerEnteredPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice));
                xmlWriter.WriteString("MaximumCustomerEnteredPrice", product.MaximumCustomerEnteredPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice));
                xmlWriter.WriteString("BasepriceEnabled", product.BasepriceEnabled, IgnoreExportPoductProperty(p => p.PAngV));
                xmlWriter.WriteString("BasepriceAmount", product.BasepriceAmount, IgnoreExportPoductProperty(p => p.PAngV));
                xmlWriter.WriteString("BasepriceUnitId", product.BasepriceUnitId, IgnoreExportPoductProperty(p => p.PAngV));
                xmlWriter.WriteString("BasepriceBaseAmount", product.BasepriceBaseAmount, IgnoreExportPoductProperty(p => p.PAngV));
                xmlWriter.WriteString("BasepriceBaseUnitId", product.BasepriceBaseUnitId, IgnoreExportPoductProperty(p => p.PAngV));
                xmlWriter.WriteString("MarkAsNew", product.MarkAsNew, IgnoreExportPoductProperty(p => p.MarkAsNew));
                xmlWriter.WriteString("MarkAsNewStartDateTimeUtc", product.MarkAsNewStartDateTimeUtc, IgnoreExportPoductProperty(p => p.MarkAsNewStartDate));
                xmlWriter.WriteString("MarkAsNewEndDateTimeUtc", product.MarkAsNewEndDateTimeUtc, IgnoreExportPoductProperty(p => p.MarkAsNewEndDate)); 
                xmlWriter.WriteString("Published", product.Published, IgnoreExportPoductProperty(p => p.Published));
                xmlWriter.WriteString("CreatedOnUtc", product.CreatedOnUtc, IgnoreExportPoductProperty(p => p.CreatedOn));
                xmlWriter.WriteString("UpdatedOnUtc", product.UpdatedOnUtc, IgnoreExportPoductProperty(p => p.UpdatedOn));

                if (!IgnoreExportPoductProperty(p => p.Discounts))
                {
                    xmlWriter.WriteStartElement("ProductDiscounts");
                    var discounts = product.AppliedDiscounts;
                    foreach (var discount in discounts)
                    {
                        xmlWriter.WriteStartElement("Discount");
                        xmlWriter.WriteString("DiscountId", discount.Id);
                        xmlWriter.WriteString("Name", discount.Name);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (!IgnoreExportPoductProperty(p => p.TierPrices))
                {
                    xmlWriter.WriteStartElement("TierPrices");
                    var tierPrices = product.TierPrices;
                    foreach (var tierPrice in tierPrices)
                    {
                        xmlWriter.WriteStartElement("TierPrice");
                        xmlWriter.WriteString("TierPriceId", tierPrice.Id);
                        xmlWriter.WriteString("StoreId", tierPrice.StoreId);
                        xmlWriter.WriteString("CustomerRoleId", tierPrice.CustomerRoleId, defaulValue: "0");
                        xmlWriter.WriteString("Quantity", tierPrice.Quantity);
                        xmlWriter.WriteString("Price", tierPrice.Price);
                        xmlWriter.WriteString("StartDateTimeUtc", tierPrice.StartDateTimeUtc);
                        xmlWriter.WriteString("EndDateTimeUtc", tierPrice.EndDateTimeUtc);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (!IgnoreExportPoductProperty(p => p.ProductAttributes))
                {
                    xmlWriter.WriteStartElement("ProductAttributes");
                    var productAttributMappings =
                        _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
                    foreach (var productAttributeMapping in productAttributMappings)
                    {
                        xmlWriter.WriteStartElement("ProductAttributeMapping");
                        xmlWriter.WriteString("ProductAttributeMappingId", productAttributeMapping.Id);
                        xmlWriter.WriteString("ProductAttributeId", productAttributeMapping.ProductAttributeId);
                        xmlWriter.WriteString("ProductAttributeName", productAttributeMapping.ProductAttribute.Name);
                        xmlWriter.WriteString("TextPrompt", productAttributeMapping.TextPrompt);
                        xmlWriter.WriteString("IsRequired", productAttributeMapping.IsRequired);
                        xmlWriter.WriteString("AttributeControlTypeId", productAttributeMapping.AttributeControlTypeId);
                        xmlWriter.WriteString("DisplayOrder", productAttributeMapping.DisplayOrder);
                        //validation rules
                        if (productAttributeMapping.ValidationRulesAllowed())
                        {
                            if (productAttributeMapping.ValidationMinLength.HasValue)
                            {
                                xmlWriter.WriteString("ValidationMinLength",
                                    productAttributeMapping.ValidationMinLength.Value);
                            }
                            if (productAttributeMapping.ValidationMaxLength.HasValue)
                            {
                                xmlWriter.WriteString("ValidationMaxLength",
                                    productAttributeMapping.ValidationMaxLength.Value);
                            }
                            if (String.IsNullOrEmpty(productAttributeMapping.ValidationFileAllowedExtensions))
                            {
                                xmlWriter.WriteString("ValidationFileAllowedExtensions",
                                    productAttributeMapping.ValidationFileAllowedExtensions);
                            }
                            if (productAttributeMapping.ValidationFileMaximumSize.HasValue)
                            {
                                xmlWriter.WriteString("ValidationFileMaximumSize",
                                    productAttributeMapping.ValidationFileMaximumSize.Value);
                            }
                            xmlWriter.WriteString("DefaultValue", productAttributeMapping.DefaultValue);
                        }
                        //conditions
                        xmlWriter.WriteElementString("ConditionAttributeXml",
                            productAttributeMapping.ConditionAttributeXml);

                        xmlWriter.WriteStartElement("ProductAttributeValues");
                        var productAttributeValues = productAttributeMapping.ProductAttributeValues;
                        foreach (var productAttributeValue in productAttributeValues)
                        {
                            xmlWriter.WriteStartElement("ProductAttributeValue");
                            xmlWriter.WriteString("ProductAttributeValueId", productAttributeValue.Id);
                            xmlWriter.WriteString("Name", productAttributeValue.Name);
                            xmlWriter.WriteString("AttributeValueTypeId", productAttributeValue.AttributeValueTypeId);
                            xmlWriter.WriteString("AssociatedProductId", productAttributeValue.AssociatedProductId);
                            xmlWriter.WriteString("ColorSquaresRgb", productAttributeValue.ColorSquaresRgb);
                            xmlWriter.WriteString("ImageSquaresPictureId", productAttributeValue.ImageSquaresPictureId);
                            xmlWriter.WriteString("PriceAdjustment", productAttributeValue.PriceAdjustment); 
                            xmlWriter.WriteString("Cost", productAttributeValue.Cost);
                            xmlWriter.WriteString("CustomerEntersQty", productAttributeValue.CustomerEntersQty);
                            xmlWriter.WriteString("Quantity", productAttributeValue.Quantity);
                            xmlWriter.WriteString("IsPreSelected", productAttributeValue.IsPreSelected);
                            xmlWriter.WriteString("DisplayOrder", productAttributeValue.DisplayOrder);
                            xmlWriter.WriteString("PictureId", productAttributeValue.PictureId);
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteStartElement("ProductPictures");
                var productPictures = product.ProductPictures;
                foreach (var productPicture in productPictures)
                {
                    xmlWriter.WriteStartElement("ProductPicture");
                    xmlWriter.WriteString("ProductPictureId", productPicture.Id);
                    xmlWriter.WriteString("PictureId", productPicture.PictureId);
                    xmlWriter.WriteString("DisplayOrder", productPicture.DisplayOrder);
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("ProductCategories");
                var productCategories = _categoryService.GetProductCategoriesByProductId(product.Id);
                if (productCategories != null)
                {
                    foreach (var productCategory in productCategories)
                    {
                        xmlWriter.WriteStartElement("ProductCategory");
                        xmlWriter.WriteString("ProductCategoryId", productCategory.Id);
                        xmlWriter.WriteString("CategoryId", productCategory.CategoryId);
                        xmlWriter.WriteString("IsFeaturedProduct", productCategory.IsFeaturedProduct);
                        xmlWriter.WriteString("DisplayOrder", productCategory.DisplayOrder);
                        xmlWriter.WriteEndElement();
                    }
                }
                xmlWriter.WriteEndElement();

                if (!IgnoreExportPoductProperty(p => p.Destinations))
                {
                    xmlWriter.WriteStartElement("ProductDestinations");
                    var productDestinations = _destinationService.GetProductDestinationsByProductId(product.Id);
                    if (productDestinations != null)
                    {
                        foreach (var productDestination in productDestinations)
                        {
                            xmlWriter.WriteStartElement("ProductDestination");
                            xmlWriter.WriteString("ProductDestinationId", productDestination.Id);
                            xmlWriter.WriteString("DestinationId", productDestination.DestinationId);
                            xmlWriter.WriteString("IsFeaturedProduct", productDestination.IsFeaturedProduct);
                            xmlWriter.WriteString("DisplayOrder", productDestination.DisplayOrder);
                            xmlWriter.WriteEndElement();
                        }
                    }
                    xmlWriter.WriteEndElement();
                }

                if (!IgnoreExportPoductProperty(p => p.SpecificationAttributes))
                {
                    xmlWriter.WriteStartElement("ProductSpecificationAttributes");
                    var productSpecificationAttributes = product.ProductSpecificationAttributes;
                    foreach (var productSpecificationAttribute in productSpecificationAttributes)
                    {
                        xmlWriter.WriteStartElement("ProductSpecificationAttribute");
                        xmlWriter.WriteString("ProductSpecificationAttributeId", productSpecificationAttribute.Id);
                        xmlWriter.WriteString("SpecificationAttributeOptionId", productSpecificationAttribute.SpecificationAttributeOptionId);
                        xmlWriter.WriteString("CustomValue", productSpecificationAttribute.CustomValue);
                        xmlWriter.WriteString("AllowFiltering", productSpecificationAttribute.AllowFiltering);
                        xmlWriter.WriteString("ShowOnProductPage", productSpecificationAttribute.ShowOnProductPage);
                        xmlWriter.WriteString("DisplayOrder", productSpecificationAttribute.DisplayOrder);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                if (!IgnoreExportPoductProperty(p => p.ProductTags))
                {
                    xmlWriter.WriteStartElement("ProductTags");
                    var productTags = product.ProductTags;
                    foreach (var productTag in productTags)
                    {
                        xmlWriter.WriteStartElement("ProductTag");
                        xmlWriter.WriteString("Id", productTag.Id);
                        xmlWriter.WriteString("Name", productTag.Name);
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Export products to XLSX
        /// </summary>
        /// <param name="products">Products</param>
        public virtual byte[] ExportProductsToXlsx(IEnumerable<Product> products)
        {
            var properties = new[]
            {
                new PropertyByName<Product>("ProductType", p => p.ProductTypeId, IgnoreExportPoductProperty(p => p.ProductType))
                {
                    DropDownElements = ProductType.SimpleProduct.ToSelectList(useLocalization: false)
                },
                new PropertyByName<Product>("ParentGroupedProductId", p => p.ParentGroupedProductId, IgnoreExportPoductProperty(p => p.ProductType)),
                new PropertyByName<Product>("VisibleIndividually", p => p.VisibleIndividually, IgnoreExportPoductProperty(p => p.VisibleIndividually)),
                new PropertyByName<Product>("Name", p => p.Name),
                new PropertyByName<Product>("ShortDescription", p => p.ShortDescription),
                new PropertyByName<Product>("FullDescription", p => p.FullDescription),
                 
                new PropertyByName<Product>("ProductTemplate", p => p.ProductTemplateId, IgnoreExportPoductProperty(p => p.ProductTemplate))
                {
                    DropDownElements = _productTemplateService.GetAllProductTemplates().Select(pt => pt as BaseEntity).ToSelectList(p => (p as ProductTemplate).Return(pt => pt.Name, String.Empty)),
                }, 
                new PropertyByName<Product>("ShowOnHomePage", p => p.ShowOnHomePage, IgnoreExportPoductProperty(p => p.ShowOnHomePage)),
                new PropertyByName<Product>("MetaKeywords", p => p.MetaKeywords, IgnoreExportPoductProperty(p => p.Seo)),
                new PropertyByName<Product>("MetaDescription", p => p.MetaDescription, IgnoreExportPoductProperty(p => p.Seo)),
                new PropertyByName<Product>("MetaTitle", p => p.MetaTitle, IgnoreExportPoductProperty(p => p.Seo)),
                new PropertyByName<Product>("SeName", p => p.GetSeName(0), IgnoreExportPoductProperty(p => p.Seo)),
                new PropertyByName<Product>("AllowCustomerReviews", p => p.AllowCustomerReviews, IgnoreExportPoductProperty(p => p.AllowCustomerReviews)),
                new PropertyByName<Product>("Published", p => p.Published, IgnoreExportPoductProperty(p => p.Published)),
                new PropertyByName<Product>("ProductCode", p => p.ProductCode),   
                new PropertyByName<Product>("IsDownload", p => p.IsDownload, IgnoreExportPoductProperty(p => p.DownloadableProduct)),
                new PropertyByName<Product>("DownloadId", p => p.DownloadId, IgnoreExportPoductProperty(p => p.DownloadableProduct)), 
                new PropertyByName<Product>("HasSampleDownload", p => p.HasSampleDownload, IgnoreExportPoductProperty(p => p.DownloadableProduct)),
                new PropertyByName<Product>("SampleDownloadId", p => p.SampleDownloadId, IgnoreExportPoductProperty(p => p.DownloadableProduct)),
                new PropertyByName<Product>("HasUserAgreement", p => p.HasUserAgreement, IgnoreExportPoductProperty(p => p.DownloadableProduct)),
                new PropertyByName<Product>("UserAgreementText", p => p.UserAgreementText, IgnoreExportPoductProperty(p => p.DownloadableProduct)), 
                new PropertyByName<Product>("IsTaxExempt", p => p.IsTaxExempt),
                new PropertyByName<Product>("TaxCategory", p => p.TaxCategoryId)
                {
                    DropDownElements = _taxCategoryService.GetAllTaxCategories().Select(tc => tc as BaseEntity).ToSelectList(p => (p as TaxCategory).Return(tc => tc.Name, String.Empty)),
                    AllowBlank = true
                },
                new PropertyByName<Product>("IsTelecommunicationsOrBroadcastingOrElectronicServices", p => p.IsTelecommunicationsOrBroadcastingOrElectronicServices, IgnoreExportPoductProperty(p => p.TelecommunicationsBroadcastingElectronicServices)),
                new PropertyByName<Product>("ManageInventoryMethod", p => p.ManageInventoryMethodId)
                {
                    DropDownElements = ManageInventoryMethod.DontManageStock.ToSelectList(useLocalization: false)
                }, 
                new PropertyByName<Product>("DisplayStockAvailability", p => p.DisplayStockAvailability, IgnoreExportPoductProperty(p => p.DisplayStockAvailability)),
                new PropertyByName<Product>("DisplayStockQuantity", p => p.DisplayStockQuantity, IgnoreExportPoductProperty(p => p.DisplayStockQuantity)),
                new PropertyByName<Product>("MinStockQuantity", p => p.MinStockQuantity, IgnoreExportPoductProperty(p => p.MinimumStockQuantity)),
                new PropertyByName<Product>("LowStockActivity", p => p.LowStockActivityId, IgnoreExportPoductProperty(p => p.LowStockActivity))
                {
                    DropDownElements = LowStockActivity.Nothing.ToSelectList(useLocalization: false)
                },
                new PropertyByName<Product>("NotifyAdminForQuantityBelow", p => p.NotifyAdminForQuantityBelow, IgnoreExportPoductProperty(p => p.NotifyAdminForQuantityBelow)),
               
                new PropertyByName<Product>("AllowBackInStockSubscriptions", p => p.AllowBackInStockSubscriptions, IgnoreExportPoductProperty(p => p.AllowBackInStockSubscriptions)),
                new PropertyByName<Product>("OrderMinimumQuantity", p => p.OrderMinimumQuantity, IgnoreExportPoductProperty(p => p.MinimumCartQuantity)),
                new PropertyByName<Product>("OrderMaximumQuantity", p => p.OrderMaximumQuantity, IgnoreExportPoductProperty(p => p.MaximumCartQuantity)),
                new PropertyByName<Product>("AllowedQuantities", p => p.AllowedQuantities, IgnoreExportPoductProperty(p => p.AllowedQuantities)),
                new PropertyByName<Product>("AllowAddingOnlyExistingAttributeCombinations", p => p.AllowAddingOnlyExistingAttributeCombinations, IgnoreExportPoductProperty(p => p.AllowAddingOnlyExistingAttributeCombinations)),
                
                new PropertyByName<Product>("DisableBuyButton", p => p.DisableBuyButton, IgnoreExportPoductProperty(p => p.DisableBuyButton)),
                new PropertyByName<Product>("DisableWishlistButton", p => p.DisableWishlistButton, IgnoreExportPoductProperty(p => p.DisableWishlistButton)),
                
                
                new PropertyByName<Product>("CallForPrice", p => p.CallForPrice, IgnoreExportPoductProperty(p => p.CallForPrice)),
                new PropertyByName<Product>("Price", p => p.Price),
                new PropertyByName<Product>("OldPrice", p => p.OldPrice, IgnoreExportPoductProperty(p => p.OldPrice)),
                new PropertyByName<Product>("ProductCost", p => p.ProductCost, IgnoreExportPoductProperty(p => p.ProductCost)),
                new PropertyByName<Product>("CustomerEntersPrice", p => p.CustomerEntersPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice)),
                new PropertyByName<Product>("MinimumCustomerEnteredPrice", p => p.MinimumCustomerEnteredPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice)),
                new PropertyByName<Product>("MaximumCustomerEnteredPrice", p => p.MaximumCustomerEnteredPrice, IgnoreExportPoductProperty(p => p.CustomerEntersPrice)),
                new PropertyByName<Product>("BasepriceEnabled", p => p.BasepriceEnabled, IgnoreExportPoductProperty(p => p.PAngV)),
                new PropertyByName<Product>("BasepriceAmount", p => p.BasepriceAmount, IgnoreExportPoductProperty(p => p.PAngV)),
                
                new PropertyByName<Product>("BasepriceBaseAmount", p => p.BasepriceBaseAmount, IgnoreExportPoductProperty(p => p.PAngV)),
             
                new PropertyByName<Product>("MarkAsNew", p => p.MarkAsNew, IgnoreExportPoductProperty(p => p.MarkAsNew)),
                new PropertyByName<Product>("MarkAsNewStartDateTimeUtc", p => p.MarkAsNewStartDateTimeUtc, IgnoreExportPoductProperty(p => p.MarkAsNewStartDate)),
                new PropertyByName<Product>("MarkAsNewEndDateTimeUtc", p => p.MarkAsNewEndDateTimeUtc, IgnoreExportPoductProperty(p => p.MarkAsNewEndDate)), 
                new PropertyByName<Product>("Destinations", GetDestinations, IgnoreExportPoductProperty(p => p.Destinations)),
                new PropertyByName<Product>("ProductTags", GetProductTags, IgnoreExportPoductProperty(p => p.ProductTags)),
                new PropertyByName<Product>("Picture1", p => GetPictures(p)[0]),
                new PropertyByName<Product>("Picture2", p => GetPictures(p)[1]),
                new PropertyByName<Product>("Picture3", p => GetPictures(p)[2])
            };

            var productList = products.ToList();
            var productAdvancedMode = _workContext.CurrentCustomer.GetAttribute<bool>("product-advanced-mode");

            if (_catalogSettings.ExportImportProductAttributes)
            {
                if (productAdvancedMode || _productEditorSettings.ProductAttributes)
                    return ExportProductsToXlsxWithAttributes(properties, productList);
            }

            return ExportToXlsx(properties, productList);
        }

        /// <summary>
        /// Export order list to xml
        /// </summary>
        /// <param name="orders">Orders</param>
        /// <returns>Result in XML format</returns>
        public virtual string ExportOrdersToXml(IList<Order> orders)
        {
         
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Orders");
            xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

            foreach (var order in orders)
            {
                xmlWriter.WriteStartElement("Order");

                xmlWriter.WriteString("OrderId", order.Id);
                xmlWriter.WriteString("OrderGuid", order.OrderGuid);
                xmlWriter.WriteString("StoreId", order.StoreId);
                xmlWriter.WriteString("CustomerId", order.CustomerId);
                xmlWriter.WriteString("OrderStatusId", order.OrderStatusId);
                xmlWriter.WriteString("PaymentStatusId", order.PaymentStatusId); 
                xmlWriter.WriteString("CustomerLanguageId", order.CustomerLanguageId);
                xmlWriter.WriteString("CustomerTaxDisplayTypeId", order.CustomerTaxDisplayTypeId);
                xmlWriter.WriteString("CustomerIp", order.CustomerIp);
                xmlWriter.WriteString("OrderSubtotalInclTax", order.OrderSubtotalInclTax);
                xmlWriter.WriteString("OrderSubtotalExclTax", order.OrderSubtotalExclTax);
                xmlWriter.WriteString("OrderSubTotalDiscountInclTax", order.OrderSubTotalDiscountInclTax);
                xmlWriter.WriteString("OrderSubTotalDiscountExclTax", order.OrderSubTotalDiscountExclTax);
                xmlWriter.WriteString("OrderShippingInclTax", order.OrderShippingInclTax);
                xmlWriter.WriteString("OrderShippingExclTax", order.OrderShippingExclTax);
                xmlWriter.WriteString("PaymentMethodAdditionalFeeInclTax", order.PaymentMethodAdditionalFeeInclTax);
                xmlWriter.WriteString("PaymentMethodAdditionalFeeExclTax", order.PaymentMethodAdditionalFeeExclTax);
                xmlWriter.WriteString("TaxRates", order.TaxRates);
                xmlWriter.WriteString("OrderTax", order.OrderTax);
                xmlWriter.WriteString("OrderTotal", order.OrderTotal);
                xmlWriter.WriteString("RefundedAmount", order.RefundedAmount);
                xmlWriter.WriteString("OrderDiscount", order.OrderDiscount);
                xmlWriter.WriteString("CurrencyRate", order.CurrencyRate);
                xmlWriter.WriteString("CustomerCurrencyCode", order.CustomerCurrencyCode);
                xmlWriter.WriteString("AffiliateId", order.AffiliateId);
                xmlWriter.WriteString("AllowStoringCreditCardNumber", order.AllowStoringCreditCardNumber);
                xmlWriter.WriteString("CardType", order.CardType);
                xmlWriter.WriteString("CardName", order.CardName);
                xmlWriter.WriteString("CardNumber", order.CardNumber);
                xmlWriter.WriteString("MaskedCreditCardNumber", order.MaskedCreditCardNumber);
                xmlWriter.WriteString("CardCvv2", order.CardCvv2);
                xmlWriter.WriteString("CardExpirationMonth", order.CardExpirationMonth);
                xmlWriter.WriteString("CardExpirationYear", order.CardExpirationYear);
                xmlWriter.WriteString("PaymentMethodSystemName", order.PaymentMethodSystemName);
                xmlWriter.WriteString("AuthorizationTransactionId", order.AuthorizationTransactionId);
                xmlWriter.WriteString("AuthorizationTransactionCode", order.AuthorizationTransactionCode);
                xmlWriter.WriteString("AuthorizationTransactionResult", order.AuthorizationTransactionResult);
                xmlWriter.WriteString("CaptureTransactionId", order.CaptureTransactionId);
                xmlWriter.WriteString("CaptureTransactionResult", order.CaptureTransactionResult);
                xmlWriter.WriteString("SubscriptionTransactionId", order.SubscriptionTransactionId);
                xmlWriter.WriteString("PaidDateUtc", order.PaidDateUtc == null ? string.Empty : order.PaidDateUtc.Value.ToString()); 
                xmlWriter.WriteString("CustomValuesXml", order.CustomValuesXml);
                xmlWriter.WriteString("VatNumber", order.VatNumber);
                xmlWriter.WriteString("Deleted", order.Deleted);
                xmlWriter.WriteString("CreatedOnUtc", order.CreatedOnUtc);

                //products
                var orderItems = order.OrderItems;
                  

                if (orderItems.Any())
                {
                    xmlWriter.WriteStartElement("OrderItems");
                    foreach (var orderItem in orderItems)
                    {
                        xmlWriter.WriteStartElement("OrderItem");
                        xmlWriter.WriteElementString("Id", null, orderItem.Id.ToString());
                        xmlWriter.WriteElementString("OrderItemGuid", null, orderItem.OrderItemGuid.ToString());
                        xmlWriter.WriteElementString("ProductId", null, orderItem.ProductId.ToString());

                        var product = orderItem.Product;
                        xmlWriter.WriteElementString("ProductName", null, product.Name);
                        xmlWriter.WriteElementString("UnitPriceInclTax", null, orderItem.UnitPriceInclTax.ToString());
                        xmlWriter.WriteElementString("UnitPriceExclTax", null, orderItem.UnitPriceExclTax.ToString());
                        xmlWriter.WriteElementString("PriceInclTax", null, orderItem.PriceInclTax.ToString());
                        xmlWriter.WriteElementString("PriceExclTax", null, orderItem.PriceExclTax.ToString());
                        xmlWriter.WriteElementString("DiscountAmountInclTax", null, orderItem.DiscountAmountInclTax.ToString());
                        xmlWriter.WriteElementString("DiscountAmountExclTax", null, orderItem.DiscountAmountExclTax.ToString());
                        xmlWriter.WriteElementString("OriginalProductCost", null, orderItem.OriginalProductCost.ToString());
                        xmlWriter.WriteElementString("AttributeDescription", null, orderItem.AttributeDescription);
                        xmlWriter.WriteElementString("AttributesXml", null, orderItem.AttributesXml);
                        xmlWriter.WriteElementString("Quantity", null, orderItem.Quantity.ToString());
                        xmlWriter.WriteElementString("DownloadCount", null, orderItem.DownloadCount.ToString()); 
                        var departureDate = orderItem.DepartureDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.DepartureDateUtc.Value) : String.Empty;
                        xmlWriter.WriteElementString("DepartureDateUtc", null, departureDate);                      
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }

                
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Export orders to XLSX
        /// </summary>
        /// <param name="orders">Orders</param>
        public virtual byte[] ExportOrdersToXlsx(IList<Order> orders)
        {
             
            //property array
            var properties = new[]
            {
                new PropertyByName<Order>("OrderId", p => p.Id),
                new PropertyByName<Order>("StoreId", p => p.StoreId),
                new PropertyByName<Order>("OrderGuid", p => p.OrderGuid),
                new PropertyByName<Order>("CustomerId", p => p.CustomerId),
                new PropertyByName<Order>("OrderStatusId", p => p.OrderStatusId),
                new PropertyByName<Order>("PaymentStatusId", p => p.PaymentStatusId), 
                new PropertyByName<Order>("OrderSubtotalInclTax", p => p.OrderSubtotalInclTax),
                new PropertyByName<Order>("OrderSubtotalExclTax", p => p.OrderSubtotalExclTax),
                new PropertyByName<Order>("OrderSubTotalDiscountInclTax", p => p.OrderSubTotalDiscountInclTax),
                new PropertyByName<Order>("OrderSubTotalDiscountExclTax", p => p.OrderSubTotalDiscountExclTax), 
                new PropertyByName<Order>("PaymentMethodAdditionalFeeInclTax", p => p.PaymentMethodAdditionalFeeInclTax),
                new PropertyByName<Order>("PaymentMethodAdditionalFeeExclTax", p => p.PaymentMethodAdditionalFeeExclTax),
                new PropertyByName<Order>("TaxRates", p => p.TaxRates),
                new PropertyByName<Order>("OrderTax", p => p.OrderTax),
                new PropertyByName<Order>("OrderTotal", p => p.OrderTotal),
                new PropertyByName<Order>("RefundedAmount", p => p.RefundedAmount),
                new PropertyByName<Order>("OrderDiscount", p => p.OrderDiscount),
                new PropertyByName<Order>("CurrencyRate", p => p.CurrencyRate),
                new PropertyByName<Order>("CustomerCurrencyCode", p => p.CustomerCurrencyCode),
                new PropertyByName<Order>("AffiliateId", p => p.AffiliateId),
                new PropertyByName<Order>("PaymentMethodSystemName", p => p.PaymentMethodSystemName), 
                new PropertyByName<Order>("CustomValuesXml", p => p.CustomValuesXml),
                new PropertyByName<Order>("VatNumber", p => p.VatNumber),
                new PropertyByName<Order>("CreatedOnUtc", p => p.CreatedOnUtc.ToOADate()),
                new PropertyByName<Order>("BillingFirstName", p => p.BillingAddress.Return(billingAddress => billingAddress.FirstName, String.Empty)),
                new PropertyByName<Order>("BillingLastName", p => p.BillingAddress.Return(billingAddress => billingAddress.LastName, String.Empty)),
                new PropertyByName<Order>("BillingEmail", p => p.BillingAddress.Return(billingAddress => billingAddress.Email, String.Empty)),
                new PropertyByName<Order>("BillingCompany", p => p.BillingAddress.Return(billingAddress => billingAddress.Company, String.Empty)),
                new PropertyByName<Order>("BillingCountry", p => p.BillingAddress.Return(billingAddress => billingAddress.Country, null).Return(country => country.Name, String.Empty)),
                new PropertyByName<Order>("BillingStateProvince", p => p.BillingAddress.Return(billingAddress => billingAddress.StateProvince, null).Return(stateProvince => stateProvince.Name, String.Empty)),
                new PropertyByName<Order>("BillingCity", p => p.BillingAddress.Return(billingAddress => billingAddress.City, String.Empty)),
                new PropertyByName<Order>("BillingAddress1", p => p.BillingAddress.Return(billingAddress => billingAddress.Address1, String.Empty)),
                new PropertyByName<Order>("BillingAddress2", p => p.BillingAddress.Return(billingAddress => billingAddress.Address2, String.Empty)),
                new PropertyByName<Order>("BillingZipPostalCode", p => p.BillingAddress.Return(billingAddress => billingAddress.ZipPostalCode, String.Empty)),
                new PropertyByName<Order>("BillingPhoneNumber", p => p.BillingAddress.Return(billingAddress => billingAddress.PhoneNumber, String.Empty)),
                new PropertyByName<Order>("BillingFaxNumber", p => p.BillingAddress.Return(billingAddress => billingAddress.FaxNumber, String.Empty)), 
            };

            return ExportToXlsx(properties, orders);
        }

        /// <summary>
        /// Export customer list to XLSX
        /// </summary>
        /// <param name="customers">Customers</param>
        public virtual byte[] ExportCustomersToXlsx(IList<Customer> customers)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Customer>("CustomerId", p => p.Id),
                new PropertyByName<Customer>("CustomerGuid", p => p.CustomerGuid),
                new PropertyByName<Customer>("Email", p => p.Email),
                new PropertyByName<Customer>("Username", p => p.Username),
                new PropertyByName<Customer>("Password", p => _customerService.GetCurrentPassword(p.Id).Return(password => password.Password, null)),
                new PropertyByName<Customer>("PasswordFormatId", p => _customerService.GetCurrentPassword(p.Id).Return(password => password.PasswordFormatId, 0)),
                new PropertyByName<Customer>("PasswordSalt", p => _customerService.GetCurrentPassword(p.Id).Return(password => password.PasswordSalt, null)),
                new PropertyByName<Customer>("IsTaxExempt", p => p.IsTaxExempt),
                new PropertyByName<Customer>("AffiliateId", p => p.AffiliateId), 
                new PropertyByName<Customer>("Active", p => p.Active),
                new PropertyByName<Customer>("IsGuest", p => p.IsGuest()),
                new PropertyByName<Customer>("IsRegistered", p => p.IsRegistered()),
                new PropertyByName<Customer>("IsAdministrator", p => p.IsAdmin()), 
                //attributes
                new PropertyByName<Customer>("FirstName", p => p.GetAttribute<string>(SystemCustomerAttributeNames.FirstName)),
                new PropertyByName<Customer>("LastName", p => p.GetAttribute<string>(SystemCustomerAttributeNames.LastName)),
                new PropertyByName<Customer>("Gender", p => p.GetAttribute<string>(SystemCustomerAttributeNames.Gender)),
                new PropertyByName<Customer>("Company", p => p.GetAttribute<string>(SystemCustomerAttributeNames.Company)),
                new PropertyByName<Customer>("StreetAddress", p => p.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress)),
                new PropertyByName<Customer>("StreetAddress2", p => p.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress2)),
                new PropertyByName<Customer>("ZipPostalCode", p => p.GetAttribute<string>(SystemCustomerAttributeNames.ZipPostalCode)),
                new PropertyByName<Customer>("City", p => p.GetAttribute<string>(SystemCustomerAttributeNames.City)),
                new PropertyByName<Customer>("CountryId", p => p.GetAttribute<int>(SystemCustomerAttributeNames.CountryId)),
                new PropertyByName<Customer>("StateProvinceId", p => p.GetAttribute<int>(SystemCustomerAttributeNames.StateProvinceId)),
                new PropertyByName<Customer>("Phone", p => p.GetAttribute<string>(SystemCustomerAttributeNames.Phone)),
                new PropertyByName<Customer>("Fax", p => p.GetAttribute<string>(SystemCustomerAttributeNames.Fax)),
                new PropertyByName<Customer>("VatNumber", p => p.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber)),
                new PropertyByName<Customer>("VatNumberStatusId", p => p.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId)),
                new PropertyByName<Customer>("TimeZoneId", p => p.GetAttribute<string>(SystemCustomerAttributeNames.TimeZoneId)),
                new PropertyByName<Customer>("AvatarPictureId", p => p.GetAttribute<int>(SystemCustomerAttributeNames.AvatarPictureId)),
                new PropertyByName<Customer>("ForumPostCount", p => p.GetAttribute<int>(SystemCustomerAttributeNames.ForumPostCount)),
                new PropertyByName<Customer>("Signature", p => p.GetAttribute<string>(SystemCustomerAttributeNames.Signature)),
                new PropertyByName<Customer>("CustomCustomerAttributes",  GetCustomCustomerAttributes)
            };

            return ExportToXlsx(properties, customers);
        }

        /// <summary>
        /// Export customer list to xml
        /// </summary>
        /// <param name="customers">Customers</param>
        /// <returns>Result in XML format</returns>
        public virtual string ExportCustomersToXml(IList<Customer> customers)
        {
            var sb = new StringBuilder();
            var stringWriter = new StringWriter(sb);
            var xmlWriter = new XmlTextWriter(stringWriter);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Customers");
            xmlWriter.WriteAttributeString("Version", NopVersion.CurrentVersion);

            foreach (var customer in customers)
            {
                xmlWriter.WriteStartElement("Customer");
                xmlWriter.WriteElementString("CustomerId", null, customer.Id.ToString());
                xmlWriter.WriteElementString("CustomerGuid", null, customer.CustomerGuid.ToString());
                xmlWriter.WriteElementString("Email", null, customer.Email);
                xmlWriter.WriteElementString("Username", null, customer.Username);

                var customerPassword = _customerService.GetCurrentPassword(customer.Id);
                xmlWriter.WriteElementString("Password", null, customerPassword.Return(password => password.Password, null));
                xmlWriter.WriteElementString("PasswordFormatId", null, customerPassword.Return(password => password.PasswordFormatId, 0).ToString());
                xmlWriter.WriteElementString("PasswordSalt", null, customerPassword.Return(password => password.PasswordSalt, null));

                xmlWriter.WriteElementString("IsTaxExempt", null, customer.IsTaxExempt.ToString());
                xmlWriter.WriteElementString("AffiliateId", null, customer.AffiliateId.ToString());  
                xmlWriter.WriteElementString("Active", null, customer.Active.ToString());

                xmlWriter.WriteElementString("IsGuest", null, customer.IsGuest().ToString());
                xmlWriter.WriteElementString("IsRegistered", null, customer.IsRegistered().ToString());
                xmlWriter.WriteElementString("IsAdministrator", null, customer.IsAdmin().ToString()); 

                xmlWriter.WriteElementString("FirstName", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName));
                xmlWriter.WriteElementString("LastName", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName));
                xmlWriter.WriteElementString("Gender", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.Gender));
                xmlWriter.WriteElementString("Company", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.Company));

                xmlWriter.WriteElementString("CountryId", null, customer.GetAttribute<int>(SystemCustomerAttributeNames.CountryId).ToString());
                xmlWriter.WriteElementString("StreetAddress", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress));
                xmlWriter.WriteElementString("StreetAddress2", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.StreetAddress2));
                xmlWriter.WriteElementString("ZipPostalCode", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.ZipPostalCode));
                xmlWriter.WriteElementString("City", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.City));
                xmlWriter.WriteElementString("StateProvinceId", null, customer.GetAttribute<int>(SystemCustomerAttributeNames.StateProvinceId).ToString());
                xmlWriter.WriteElementString("Phone", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.Phone));
                xmlWriter.WriteElementString("Fax", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.Fax));
                xmlWriter.WriteElementString("VatNumber", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.VatNumber));
                xmlWriter.WriteElementString("VatNumberStatusId", null, customer.GetAttribute<int>(SystemCustomerAttributeNames.VatNumberStatusId).ToString());
                xmlWriter.WriteElementString("TimeZoneId", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.TimeZoneId));

                foreach (var store in _storeService.GetAllStores())
                {
                    var newsletter = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                    bool subscribedToNewsletters = newsletter != null && newsletter.Active;
                    xmlWriter.WriteElementString(string.Format("Newsletter-in-store-{0}", store.Id), null, subscribedToNewsletters.ToString());
                }

                xmlWriter.WriteElementString("AvatarPictureId", null, customer.GetAttribute<int>(SystemCustomerAttributeNames.AvatarPictureId).ToString()); 
                xmlWriter.WriteElementString("Signature", null, customer.GetAttribute<string>(SystemCustomerAttributeNames.Signature));

                var selectedCustomerAttributesString = customer.GetAttribute<string>(SystemCustomerAttributeNames.CustomCustomerAttributes, _genericAttributeService);

                if (!string.IsNullOrEmpty(selectedCustomerAttributesString))
                {
                    var selectedCustomerAttributes = new StringReader(selectedCustomerAttributesString);
                    var selectedCustomerAttributesXmlReader = XmlReader.Create(selectedCustomerAttributes);
                    xmlWriter.WriteNode(selectedCustomerAttributesXmlReader, false);
                }

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Export newsletter subscribers to TXT
        /// </summary>
        /// <param name="subscriptions">Subscriptions</param>
        /// <returns>Result in TXT (string) format</returns>
        public virtual string ExportNewsletterSubscribersToTxt(IList<NewsLetterSubscription> subscriptions)
        {
            if (subscriptions == null)
                throw new ArgumentNullException("subscriptions");

            const string separator = ",";
            var sb = new StringBuilder();
            foreach (var subscription in subscriptions)
            {
                sb.Append(subscription.Email);
                sb.Append(separator);
                sb.Append(subscription.Active);
                sb.Append(separator);
                sb.Append(subscription.StoreId);
                sb.Append(Environment.NewLine); //new line
            }
            return sb.ToString();
        }

        /// <summary>
        /// Export states to TXT
        /// </summary>
        /// <param name="states">States</param>
        /// <returns>Result in TXT (string) format</returns>
        public virtual string ExportStatesToTxt(IList<StateProvince> states)
        {
            if (states == null)
                throw new ArgumentNullException("states");

            const string separator = ",";
            var sb = new StringBuilder();
            foreach (var state in states)
            {
                sb.Append(state.Country.TwoLetterIsoCode);
                sb.Append(separator);
                sb.Append(state.Name);
                sb.Append(separator);
                sb.Append(state.Abbreviation);
                sb.Append(separator);
                sb.Append(state.Published);
                sb.Append(separator);
                sb.Append(state.DisplayOrder);
                sb.Append(Environment.NewLine); //new line
            }
            return sb.ToString();
        }

        #endregion
    }
}
