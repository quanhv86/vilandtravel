using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.ExportImport.Help;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Tax;
using OfficeOpenXml;

namespace Nop.Services.ExportImport
{
    /// <summary>
    /// Import manager
    /// </summary>
    public partial class ImportManager : IImportManager
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICategoryService _categoryService;
        private readonly IDestinationService _destinationService;
        private readonly IPictureService _pictureService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IStoreContext _storeContext;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IEncryptionService _encryptionService;
        private readonly IDataProvider _dataProvider;
        private readonly MediaSettings _mediaSettings;
        private readonly IProductTemplateService _productTemplateService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IProductTagService _productTagService;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public ImportManager(IProductService productService,
            ICategoryService categoryService,
            IDestinationService destinationService,
            IPictureService pictureService,
            IUrlRecordService urlRecordService,
            IStoreContext storeContext,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            IEncryptionService encryptionService,
            IDataProvider dataProvider,
            MediaSettings mediaSettings,
            IProductTemplateService productTemplateService,
            ITaxCategoryService taxCategoryService,
            IProductAttributeService productAttributeService,
            CatalogSettings catalogSettings,
            IProductTagService productTagService,
            IWorkContext workContext,
            ILocalizationService localizationService)
        {
            this._productService = productService;
            this._categoryService = categoryService;
            this._destinationService = destinationService;
            this._pictureService = pictureService;
            this._urlRecordService = urlRecordService;
            this._storeContext = storeContext;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._encryptionService = encryptionService;
            this._dataProvider = dataProvider;
            this._mediaSettings = mediaSettings;
            this._productTemplateService = productTemplateService;
            this._taxCategoryService = taxCategoryService;
            this._productAttributeService = productAttributeService;
            this._catalogSettings = catalogSettings;
            this._productTagService = productTagService;
            this._workContext = workContext;
            this._localizationService = localizationService;

        }

        #endregion

        #region Utilities

        protected virtual int GetColumnIndex(string[] properties, string columnName)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            if (columnName == null)
                throw new ArgumentNullException("columnName");

            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return i + 1; //excel indexes start from 1
            return 0;
        }

        protected virtual string ConvertColumnToString(object columnValue)
        {
            if (columnValue == null)
                return null;

            return Convert.ToString(columnValue);
        }

        protected virtual string GetMimeTypeFromFilePath(string filePath)
        {
            var mimeType = MimeMapping.GetMimeMapping(filePath);

            //little hack here because MimeMapping does not contain all mappings (e.g. PNG)
            if (mimeType == MimeTypes.ApplicationOctetStream)
                mimeType = MimeTypes.ImageJpeg;

            return mimeType;
        }

        /// <summary>
        /// Creates or loads the image
        /// </summary>
        /// <param name="picturePath">The path to the image file</param>
        /// <param name="name">The name of the object</param>
        /// <param name="picId">Image identifier, may be null</param>
        /// <returns>The image or null if the image has not changed</returns>
        protected virtual Picture LoadPicture(string picturePath, string name, int? picId = null)
        {
            if (String.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
                return null;

            var mimeType = GetMimeTypeFromFilePath(picturePath);
            var newPictureBinary = File.ReadAllBytes(picturePath);
            var pictureAlreadyExists = false;
            if (picId != null)
            {
                //compare with existing product pictures
                var existingPicture = _pictureService.GetPictureById(picId.Value);

                var existingBinary = _pictureService.LoadPictureBinary(existingPicture);
                //picture binary after validation (like in database)
                var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                if (existingBinary.SequenceEqual(validatedPictureBinary) ||
                    existingBinary.SequenceEqual(newPictureBinary))
                {
                    pictureAlreadyExists = true;
                }
            }

            if (pictureAlreadyExists) return null;

            var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType,
                _pictureService.GetPictureSeName(name));
            return newPicture;
        }

        protected virtual void ImportProductImagesUsingServices(IList<ProductPictureMetadata> productPictureMetadata)
        {
            foreach (var product in productPictureMetadata)
            {
                foreach (var picturePath in new[] { product.Picture1Path, product.Picture2Path, product.Picture3Path })
                {
                    if (String.IsNullOrEmpty(picturePath))
                        continue;

                    var mimeType = GetMimeTypeFromFilePath(picturePath);
                    var newPictureBinary = File.ReadAllBytes(picturePath);
                    var pictureAlreadyExists = false;
                    if (!product.IsNew)
                    {
                        //compare with existing product pictures
                        var existingPictures = _pictureService.GetPicturesByProductId(product.ProductItem.Id);
                        foreach (var existingPicture in existingPictures)
                        {
                            var existingBinary = _pictureService.LoadPictureBinary(existingPicture);
                            //picture binary after validation (like in database)
                            var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                            if (!existingBinary.SequenceEqual(validatedPictureBinary) &&
                                !existingBinary.SequenceEqual(newPictureBinary))
                                continue;
                            //the same picture content
                            pictureAlreadyExists = true;
                            break;
                        }
                    }

                    if (pictureAlreadyExists)
                        continue;
                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.ProductItem.Name));
                    product.ProductItem.ProductPictures.Add(new ProductPicture
                    {
                        //EF has some weird issue if we set "Picture = newPicture" instead of "PictureId = newPicture.Id"
                        //pictures are duplicated
                        //maybe because entity size is too large
                        PictureId = newPicture.Id,
                        DisplayOrder = 1,
                    });
                    _productService.UpdateProduct(product.ProductItem);
                }
            }
        }

        protected virtual void ImportProductImagesUsingHash(IList<ProductPictureMetadata> productPictureMetadata, IList<Product> allProductsByProductCode)
        {
            //performance optimization, load all pictures hashes
            //it will only be used if the images are stored in the SQL Server database (not compact)
            var takeCount = _dataProvider.SupportedLengthOfBinaryHash() - 1;
            var productsImagesIds = _productService.GetProductsImagesIds(allProductsByProductCode.Select(p => p.Id).ToArray());
            var allPicturesHashes = _pictureService.GetPicturesHash(productsImagesIds.SelectMany(p => p.Value).ToArray());

            foreach (var product in productPictureMetadata)
            {
                foreach (var picturePath in new[] { product.Picture1Path, product.Picture2Path, product.Picture3Path })
                {
                    if (String.IsNullOrEmpty(picturePath))
                        continue;

                    var mimeType = GetMimeTypeFromFilePath(picturePath);
                    var newPictureBinary = File.ReadAllBytes(picturePath);
                    var pictureAlreadyExists = false;
                    if (!product.IsNew)
                    {
                        var newImageHash = _encryptionService.CreateHash(newPictureBinary.Take(takeCount).ToArray());
                        var newValidatedImageHash = _encryptionService.CreateHash(_pictureService.ValidatePicture(newPictureBinary, mimeType).Take(takeCount).ToArray());

                        var imagesIds = productsImagesIds.ContainsKey(product.ProductItem.Id)
                            ? productsImagesIds[product.ProductItem.Id]
                            : new int[0];

                        pictureAlreadyExists = allPicturesHashes.Where(p => imagesIds.Contains(p.Key)).Select(p => p.Value).Any(p => p == newImageHash || p == newValidatedImageHash);
                    }

                    if (pictureAlreadyExists)
                        continue;
                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.ProductItem.Name));
                    product.ProductItem.ProductPictures.Add(new ProductPicture
                    {
                        //EF has some weird issue if we set "Picture = newPicture" instead of "PictureId = newPicture.Id"
                        //pictures are duplicated
                        //maybe because entity size is too large
                        PictureId = newPicture.Id,
                        DisplayOrder = 1,
                    });
                    _productService.UpdateProduct(product.ProductItem);
                }
            }
        }

        protected virtual IList<PropertyByName<T>> GetPropertiesByExcelCells<T>(ExcelWorksheet worksheet)
        {
            var properties = new List<PropertyByName<T>>();
            var poz = 1;
            while (true)
            {
                try
                {
                    var cell = worksheet.Cells[1, poz];

                    if (cell == null || cell.Value == null || string.IsNullOrEmpty(cell.Value.ToString()))
                        break;

                    poz += 1;
                    properties.Add(new PropertyByName<T>(cell.Value.ToString()));
                }
                catch
                {
                    break;
                }
            }

            return properties;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Import products from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportProductsFromXlsx(Stream stream)
        {
            //var start = DateTime.Now;
            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                //the columns
                var properties = GetPropertiesByExcelCells<Product>(worksheet);

                var manager = new PropertyManager<Product>(properties);

                var attributProperties = new[]
                   {
                        new PropertyByName<ExportProductAttribute>("AttributeId"),
                        new PropertyByName<ExportProductAttribute>("AttributeName"),
                        new PropertyByName<ExportProductAttribute>("AttributeTextPrompt"),
                        new PropertyByName<ExportProductAttribute>("AttributeIsRequired"),
                        new PropertyByName<ExportProductAttribute>("AttributeControlType")
                        {
                            DropDownElements = AttributeControlType.TextBox.ToSelectList(useLocalization: false)
                        },
                        new PropertyByName<ExportProductAttribute>("AttributeDisplayOrder"), 
                        new PropertyByName<ExportProductAttribute>("ProductAttributeValueId"),
                        new PropertyByName<ExportProductAttribute>("ValueName"),
                        new PropertyByName<ExportProductAttribute>("AttributeValueType")
                        {
                            DropDownElements = AttributeValueType.Simple.ToSelectList(useLocalization: false)
                        },
                        new PropertyByName<ExportProductAttribute>("AssociatedProductId"),
                        new PropertyByName<ExportProductAttribute>("ColorSquaresRgb"),
                        new PropertyByName<ExportProductAttribute>("ImageSquaresPictureId"),
                        new PropertyByName<ExportProductAttribute>("PriceAdjustment"),
                        new PropertyByName<ExportProductAttribute>("WeightAdjustment"),
                        new PropertyByName<ExportProductAttribute>("Cost"),
                        new PropertyByName<ExportProductAttribute>("CustomerEntersQty"),
                        new PropertyByName<ExportProductAttribute>("Quantity"),
                        new PropertyByName<ExportProductAttribute>("IsPreSelected"),
                        new PropertyByName<ExportProductAttribute>("DisplayOrder"),
                        new PropertyByName<ExportProductAttribute>("PictureId")
                    };

                var managerProductAttribute = new PropertyManager<ExportProductAttribute>(attributProperties);

                var endRow = 2;
                var allCategoriesNames = new List<string>();
                var allSku = new List<string>();

                var tempProperty = manager.GetProperty("Categories");
                var categoryCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                tempProperty = manager.GetProperty("ProductCode");
                var skuCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                var allDestinationsNames = new List<string>();
                tempProperty = manager.GetProperty("Destinations");
                var destinationCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                manager.SetSelectList("ProductType", ProductType.SimpleProduct.ToSelectList(useLocalization: false));
                manager.SetSelectList("DownloadActivationType", DownloadActivationType.Manually.ToSelectList(useLocalization: false));
                manager.SetSelectList("ManageInventoryMethod", ManageInventoryMethod.DontManageStock.ToSelectList(useLocalization: false));
                manager.SetSelectList("LowStockActivity", LowStockActivity.Nothing.ToSelectList(useLocalization: false));
                manager.SetSelectList("ProductTemplate", _productTemplateService.GetAllProductTemplates().Select(pt => pt as BaseEntity).ToSelectList(p => (p as ProductTemplate).Return(pt => pt.Name, String.Empty)));
                manager.SetSelectList("TaxCategory", _taxCategoryService.GetAllTaxCategories().Select(tc => tc as BaseEntity).ToSelectList(p => (p as TaxCategory).Return(tc => tc.Name, String.Empty)));

                var allAttributeIds = new List<int>();
                var attributeIdCellNum = managerProductAttribute.GetProperty("AttributeId").PropertyOrderPosition + ExportProductAttribute.ProducAttributeCellOffset;

                var countProductsInFile = 0;

                //find end of data
                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[endRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    if (new[] { 1, 2 }.Select(cellNum => worksheet.Cells[endRow, cellNum]).All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString())) && worksheet.Row(endRow).OutlineLevel == 0)
                    {
                        var cellValue = worksheet.Cells[endRow, attributeIdCellNum].Value;
                        try
                        {
                            var aid = cellValue.Return(Convert.ToInt32, -1);

                            var productAttribute = _productAttributeService.GetProductAttributeById(aid);

                            if (productAttribute != null)
                                worksheet.Row(endRow).OutlineLevel = 1;
                        }
                        catch (FormatException)
                        {
                            if (cellValue.Return(cv => cv.ToString(), String.Empty) == "AttributeId")
                                worksheet.Row(endRow).OutlineLevel = 1;
                        }
                    }

                    if (worksheet.Row(endRow).OutlineLevel != 0)
                    {
                        managerProductAttribute.ReadFromXlsx(worksheet, endRow, ExportProductAttribute.ProducAttributeCellOffset);
                        if (!managerProductAttribute.IsCaption)
                        {
                            var aid = worksheet.Cells[endRow, attributeIdCellNum].Value.Return(Convert.ToInt32, -1);
                            allAttributeIds.Add(aid);
                        }

                        endRow++;
                        continue;
                    }

                    if (categoryCellNum > 0)
                    {
                        var categoryIds = worksheet.Cells[endRow, categoryCellNum].Value.Return(p => p.ToString(), string.Empty);

                        if (!categoryIds.IsEmpty())
                            allCategoriesNames.AddRange(categoryIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
                    }

                    if (skuCellNum > 0)
                    {
                        var sku = worksheet.Cells[endRow, skuCellNum].Value.Return(p => p.ToString(), string.Empty);

                        if (!sku.IsEmpty())
                            allSku.Add(sku);
                    }

                    if (destinationCellNum > 0)
                    {
                        var destinationIds = worksheet.Cells[endRow, destinationCellNum].Value.Return(p => p.ToString(), string.Empty);
                        if (!destinationIds.IsEmpty())
                            allDestinationsNames.AddRange(destinationIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
                    }

                    //counting the number of products
                    countProductsInFile += 1;

                    endRow++;
                }

                //performance optimization, the check for the existence of the categories in one SQL request
                var notExistingCategories = _categoryService.GetNotExistingCategories(allCategoriesNames.ToArray());
                if (notExistingCategories.Any())
                {
                    throw new ArgumentException(string.Format("The following category name(s) don't exist - {0}", string.Join(", ", notExistingCategories)));
                }

                //performance optimization, the check for the existence of the destinations in one SQL request
                var notExistingDestinations = _destinationService.GetNotExistingDestinations(allDestinationsNames.ToArray());
                if (notExistingDestinations.Any())
                {
                    throw new ArgumentException(string.Format("The following destination name(s) don't exist - {0}", string.Join(", ", notExistingDestinations)));
                }

                //performance optimization, the check for the existence of the product attributes in one SQL request
                var notExistingProductAttributes = _productAttributeService.GetNotExistingAttributes(allAttributeIds.ToArray());
                if (notExistingProductAttributes.Any())
                {
                    throw new ArgumentException(string.Format("The following product attribute ID(s) don't exist - {0}", string.Join(", ", notExistingProductAttributes)));
                }

                //performance optimization, load all products by SKU in one SQL request
                var allProductsByProductCode = _productService.GetProductsByProductCode(allSku.ToArray());



                //performance optimization, load all categories IDs for products in one SQL request
                var allProductsCategoryIds = _categoryService.GetProductCategoryIds(allProductsByProductCode.Select(p => p.Id).ToArray());

                //performance optimization, load all categories in one SQL request
                var allCategories = _categoryService.GetAllCategories(showHidden: true);

                //performance optimization, load all destinations IDs for products in one SQL request
                var allProductsDestinationIds = _destinationService.GetProductDestinationIds(allProductsByProductCode.Select(p => p.Id).ToArray());

                //performance optimization, load all destinations in one SQL request
                var allDestinations = _destinationService.GetAllDestinations(showHidden: true);

                //product to import images
                var productPictureMetadata = new List<ProductPictureMetadata>();

                Product lastLoadedProduct = null;

                for (var iRow = 2; iRow < endRow; iRow++)
                {
                    //imports product attributes
                    if (worksheet.Row(iRow).OutlineLevel != 0)
                    {
                        if (_catalogSettings.ExportImportProductAttributes)
                        {
                            managerProductAttribute.ReadFromXlsx(worksheet, iRow,
                                ExportProductAttribute.ProducAttributeCellOffset);
                            if (lastLoadedProduct == null || managerProductAttribute.IsCaption)
                                continue;

                            var productAttributeId = managerProductAttribute.GetProperty("AttributeId").IntValue;
                            var attributeControlTypeId = managerProductAttribute.GetProperty("AttributeControlType").IntValue;

                            var productAttributeValueId = managerProductAttribute.GetProperty("ProductAttributeValueId").IntValue;
                            var associatedProductId = managerProductAttribute.GetProperty("AssociatedProductId").IntValue;
                            var valueName = managerProductAttribute.GetProperty("ValueName").StringValue;
                            var attributeValueTypeId = managerProductAttribute.GetProperty("AttributeValueType").IntValue;
                            var colorSquaresRgb = managerProductAttribute.GetProperty("ColorSquaresRgb").StringValue;
                            var imageSquaresPictureId = managerProductAttribute.GetProperty("ImageSquaresPictureId").IntValue;
                            var priceAdjustment = managerProductAttribute.GetProperty("PriceAdjustment").DecimalValue;
                            var cost = managerProductAttribute.GetProperty("Cost").DecimalValue;
                            var customerEntersQty = managerProductAttribute.GetProperty("CustomerEntersQty").BooleanValue;
                            var quantity = managerProductAttribute.GetProperty("Quantity").IntValue;
                            var isPreSelected = managerProductAttribute.GetProperty("IsPreSelected").BooleanValue;
                            var displayOrder = managerProductAttribute.GetProperty("DisplayOrder").IntValue;
                            var pictureId = managerProductAttribute.GetProperty("PictureId").IntValue;
                            var textPrompt = managerProductAttribute.GetProperty("AttributeTextPrompt").StringValue;
                            var isRequired = managerProductAttribute.GetProperty("AttributeIsRequired").BooleanValue;
                            var attributeDisplayOrder = managerProductAttribute.GetProperty("AttributeDisplayOrder").IntValue;

                            var productAttributeMapping = lastLoadedProduct.ProductAttributeMappings.FirstOrDefault(pam => pam.ProductAttributeId == productAttributeId);

                            if (productAttributeMapping == null)
                            {
                                //insert mapping
                                productAttributeMapping = new ProductAttributeMapping
                                {
                                    ProductId = lastLoadedProduct.Id,
                                    ProductAttributeId = productAttributeId,
                                    TextPrompt = textPrompt,
                                    IsRequired = isRequired,
                                    AttributeControlTypeId = attributeControlTypeId,
                                    DisplayOrder = attributeDisplayOrder
                                };
                                _productAttributeService.InsertProductAttributeMapping(productAttributeMapping);
                            }
                            else
                            {
                                productAttributeMapping.AttributeControlTypeId = attributeControlTypeId;
                                productAttributeMapping.TextPrompt = textPrompt;
                                productAttributeMapping.IsRequired = isRequired;
                                productAttributeMapping.DisplayOrder = attributeDisplayOrder;
                                _productAttributeService.UpdateProductAttributeMapping(productAttributeMapping);
                            }

                            var pav = _productAttributeService.GetProductAttributeValueById(productAttributeValueId);

                            var attributeControlType = (AttributeControlType)attributeControlTypeId;

                            if (pav == null)
                            {
                                switch (attributeControlType)
                                {
                                    case AttributeControlType.Datepicker:
                                    case AttributeControlType.FileUpload:
                                    case AttributeControlType.MultilineTextbox:
                                    case AttributeControlType.TextBox:
                                        continue;
                                }

                                pav = new ProductAttributeValue
                                {
                                    ProductAttributeMappingId = productAttributeMapping.Id,
                                    AttributeValueType = (AttributeValueType)attributeValueTypeId,
                                    AssociatedProductId = associatedProductId,
                                    Name = valueName,
                                    PriceAdjustment = priceAdjustment,
                                    Cost = cost,
                                    IsPreSelected = isPreSelected,
                                    DisplayOrder = displayOrder,
                                    ColorSquaresRgb = colorSquaresRgb,
                                    ImageSquaresPictureId = imageSquaresPictureId,
                                    CustomerEntersQty = customerEntersQty,
                                    Quantity = quantity,
                                    PictureId = pictureId
                                };

                                _productAttributeService.InsertProductAttributeValue(pav);
                            }
                            else
                            {
                                pav.AttributeValueTypeId = attributeValueTypeId;
                                pav.AssociatedProductId = associatedProductId;
                                pav.Name = valueName;
                                pav.ColorSquaresRgb = colorSquaresRgb;
                                pav.ImageSquaresPictureId = imageSquaresPictureId;
                                pav.PriceAdjustment = priceAdjustment;
                                pav.Cost = cost;
                                pav.CustomerEntersQty = customerEntersQty;
                                pav.Quantity = quantity;
                                pav.IsPreSelected = isPreSelected;
                                pav.DisplayOrder = displayOrder;
                                pav.PictureId = pictureId;

                                _productAttributeService.UpdateProductAttributeValue(pav);
                            }
                        }
                        continue;
                    }

                    manager.ReadFromXlsx(worksheet, iRow);

                    var product = skuCellNum > 0 ? allProductsByProductCode.FirstOrDefault(p => p.ProductCode == manager.GetProperty("ProductCode").StringValue) : null;

                    var isNew = product == null;

                    product = product ?? new Product();

                    //some of previous values
                    var previousStockQuantity = product.StockQuantity;

                    if (isNew)
                        product.CreatedOnUtc = DateTime.UtcNow;

                    foreach (var property in manager.GetProperties)
                    {
                        switch (property.PropertyName)
                        {
                            case "ProductType":
                                product.ProductTypeId = property.IntValue;
                                break;
                            case "ParentGroupedProductId":
                                product.ParentGroupedProductId = property.IntValue;
                                break;
                            case "VisibleIndividually":
                                product.VisibleIndividually = property.BooleanValue;
                                break;
                            case "Name":
                                product.Name = property.StringValue;
                                break;
                            case "ShortDescription":
                                product.ShortDescription = property.StringValue;
                                break;
                            case "FullDescription":
                                product.FullDescription = property.StringValue;
                                break;

                            case "ProductTemplate":
                                product.ProductTemplateId = property.IntValue;
                                break;
                            case "ShowOnHomePage":
                                product.ShowOnHomePage = property.BooleanValue;
                                break;
                            case "MetaKeywords":
                                product.MetaKeywords = property.StringValue;
                                break;
                            case "MetaDescription":
                                product.MetaDescription = property.StringValue;
                                break;
                            case "MetaTitle":
                                product.MetaTitle = property.StringValue;
                                break;
                            case "AllowCustomerReviews":
                                product.AllowCustomerReviews = property.BooleanValue;
                                break;
                            case "Published":
                                product.Published = property.BooleanValue;
                                break;
                            case "ProductCode":
                                product.ProductCode = property.StringValue;
                                break;
                             
                            case "IsDownload":
                                product.IsDownload = property.BooleanValue;
                                break;
                            case "DownloadId":
                                product.DownloadId = property.IntValue;
                                break; 
                            case "HasSampleDownload":
                                product.HasSampleDownload = property.BooleanValue;
                                break;
                            case "SampleDownloadId":
                                product.SampleDownloadId = property.IntValue;
                                break;
                            case "HasUserAgreement":
                                product.HasUserAgreement = property.BooleanValue;
                                break;
                            case "UserAgreementText":
                                product.UserAgreementText = property.StringValue;
                                break; 
                            case "IsTaxExempt":
                                product.IsTaxExempt = property.BooleanValue;
                                break;
                            case "TaxCategory":
                                product.TaxCategoryId = property.IntValue;
                                break;
                            case "IsTelecommunicationsOrBroadcastingOrElectronicServices":
                                product.IsTelecommunicationsOrBroadcastingOrElectronicServices = property.BooleanValue;
                                break;
                            case "ManageInventoryMethod":
                                product.ManageInventoryMethodId = property.IntValue;
                                break;
                            case "ProductAvailabilityRange":
                                product.ProductAvailabilityRangeId = property.IntValue;
                                break; 
                            case "StockQuantity":
                                product.StockQuantity = property.IntValue;
                                break;
                            case "DisplayStockAvailability":
                                product.DisplayStockAvailability = property.BooleanValue;
                                break;
                            case "DisplayStockQuantity":
                                product.DisplayStockQuantity = property.BooleanValue;
                                break;
                            case "MinStockQuantity":
                                product.MinStockQuantity = property.IntValue;
                                break;
                            case "LowStockActivity":
                                product.LowStockActivityId = property.IntValue;
                                break;
                            case "NotifyAdminForQuantityBelow":
                                product.NotifyAdminForQuantityBelow = property.IntValue;
                                break;
                            case "BackorderMode":
                                product.BackorderModeId = property.IntValue;
                                break;
                            case "AllowBackInStockSubscriptions":
                                product.AllowBackInStockSubscriptions = property.BooleanValue;
                                break;
                            case "OrderMinimumQuantity":
                                product.OrderMinimumQuantity = property.IntValue;
                                break;
                            case "OrderMaximumQuantity":
                                product.OrderMaximumQuantity = property.IntValue;
                                break;
                            case "AllowedQuantities":
                                product.AllowedQuantities = property.StringValue;
                                break;
                            case "AllowAddingOnlyExistingAttributeCombinations":
                                product.AllowAddingOnlyExistingAttributeCombinations = property.BooleanValue;
                                break; 
                            case "DisableBuyButton":
                                product.DisableBuyButton = property.BooleanValue;
                                break;
                            case "DisableWishlistButton":
                                product.DisableWishlistButton = property.BooleanValue;
                                break; 
                            case "CallForPrice":
                                product.CallForPrice = property.BooleanValue;
                                break;
                            case "Price":
                                product.Price = property.DecimalValue;
                                break;
                            case "OldPrice":
                                product.OldPrice = property.DecimalValue;
                                break;
                            case "ProductCost":
                                product.ProductCost = property.DecimalValue;
                                break;
                            case "CustomerEntersPrice":
                                product.CustomerEntersPrice = property.BooleanValue;
                                break;
                            case "MinimumCustomerEnteredPrice":
                                product.MinimumCustomerEnteredPrice = property.DecimalValue;
                                break;
                            case "MaximumCustomerEnteredPrice":
                                product.MaximumCustomerEnteredPrice = property.DecimalValue;
                                break;
                            case "BasepriceEnabled":
                                product.BasepriceEnabled = property.BooleanValue;
                                break;
                            case "BasepriceAmount":
                                product.BasepriceAmount = property.DecimalValue;
                                break;
                            case "BasepriceUnit":
                                product.BasepriceUnitId = property.IntValue;
                                break;
                            case "BasepriceBaseAmount":
                                product.BasepriceBaseAmount = property.DecimalValue;
                                break;
                            case "BasepriceBaseUnit":
                                product.BasepriceBaseUnitId = property.IntValue;
                                break;
                            case "MarkAsNew":
                                product.MarkAsNew = property.BooleanValue;
                                break;
                            case "MarkAsNewStartDateTimeUtc":
                                product.MarkAsNewStartDateTimeUtc = property.DateTimeNullable;
                                break;
                            case "MarkAsNewEndDateTimeUtc":
                                product.MarkAsNewEndDateTimeUtc = property.DateTimeNullable;
                                break; 
                        }
                    }

                    //set some default default values if not specified
                    if (isNew && properties.All(p => p.PropertyName != "ProductType"))
                        product.ProductType = ProductType.SimpleProduct;
                    if (isNew && properties.All(p => p.PropertyName != "VisibleIndividually"))
                        product.VisibleIndividually = true;
                    if (isNew && properties.All(p => p.PropertyName != "Published"))
                        product.Published = true;
 

                    product.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                    {
                        _productService.InsertProduct(product);
                    }
                    else
                    {
                        _productService.UpdateProduct(product);
                    }

                    //quantity change history
                    if (isNew)
                    {
                        _productService.AddStockQuantityHistoryEntry(product, product.StockQuantity - previousStockQuantity, product.StockQuantity, _localizationService.GetResource("Admin.StockQuantityHistory.Messages.ImportProduct.Edit"));
                    }
                    //warehouse is changed 
                    else
                    {
                        //compose a message
                        var oldWarehouseMessage = string.Empty;
                       
                        var message = string.Format(_localizationService.GetResource("Admin.StockQuantityHistory.Messages.ImportProduct.EditWarehouse"), oldWarehouseMessage);

                        //record history
                        _productService.AddStockQuantityHistoryEntry(product, -previousStockQuantity, 0, message);
                        _productService.AddStockQuantityHistoryEntry(product, product.StockQuantity, product.StockQuantity, message);
                    }

                    tempProperty = manager.GetProperty("SeName");
                    if (tempProperty != null)
                    {
                        var seName = tempProperty.StringValue;
                        //search engine name
                        _urlRecordService.SaveSlug(product, product.ValidateSeName(seName, product.Name, true), 0);
                    }

                    tempProperty = manager.GetProperty("Categories");

                    if (tempProperty != null)
                    {
                        var categoryNames = tempProperty.StringValue;

                        //category mappings
                        var categories = isNew || !allProductsCategoryIds.ContainsKey(product.Id) ? new int[0] : allProductsCategoryIds[product.Id];
                        var importedCategories = categoryNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => allCategories.First(c => c.Name == x.Trim()).Id).ToList();
                        foreach (var categoryId in importedCategories)
                        {
                            if (categories.Any(c => c == categoryId))
                                continue;

                            var productCategory = new ProductCategory
                            {
                                ProductId = product.Id,
                                CategoryId = categoryId,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1
                            };
                            _categoryService.InsertProductCategory(productCategory);
                        }

                        //delete product categories
                        var deletedProductCategories = categories.Where(categoryId => !importedCategories.Contains(categoryId))
                                .Select(categoryId => product.ProductCategories.First(pc => pc.CategoryId == categoryId));
                        foreach (var deletedProductCategory in deletedProductCategories)
                        {
                            _categoryService.DeleteProductCategory(deletedProductCategory);
                        }
                    }

                    tempProperty = manager.GetProperty("Destinations");
                    if (tempProperty != null)
                    {
                        var destinationNames = tempProperty.StringValue;

                        //destination mappings
                        var destinations = isNew || !allProductsDestinationIds.ContainsKey(product.Id) ? new int[0] : allProductsDestinationIds[product.Id];
                        var importedDestinations = destinationNames.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => allDestinations.First(m => m.Name == x.Trim()).Id).ToList();
                        foreach (var destinationId in importedDestinations)
                        {
                            if (destinations.Any(c => c == destinationId))
                                continue;

                            var productDestination = new ProductDestination
                            {
                                ProductId = product.Id,
                                DestinationId = destinationId,
                                IsFeaturedProduct = false,
                                DisplayOrder = 1
                            };
                            _destinationService.InsertProductDestination(productDestination);
                        }

                        //delete product destinations
                        var deletedProductsDestinations = destinations.Where(destinationId => !importedDestinations.Contains(destinationId))
                                .Select(destinationId => product.ProductDestinations.First(pc => pc.DestinationId == destinationId));
                        foreach (var deletedProductDestination in deletedProductsDestinations)
                        {
                            _destinationService.DeleteProductDestination(deletedProductDestination);
                        }
                    }

                    tempProperty = manager.GetProperty("ProductTags");
                    if (tempProperty != null)
                    {
                        var productTags = tempProperty.StringValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

                        //product tag mappings
                        _productTagService.UpdateProductTags(product, productTags);
                    }

                    var picture1 = manager.GetProperty("Picture1").Return(p => p.StringValue, String.Empty);
                    var picture2 = manager.GetProperty("Picture2").Return(p => p.StringValue, String.Empty);
                    var picture3 = manager.GetProperty("Picture3").Return(p => p.StringValue, String.Empty);

                    productPictureMetadata.Add(new ProductPictureMetadata
                    {
                        ProductItem = product,
                        Picture1Path = picture1,
                        Picture2Path = picture2,
                        Picture3Path = picture3,
                        IsNew = isNew
                    });

                    lastLoadedProduct = product;

                    //update "HasTierPrices" and "HasDiscountsApplied" properties
                    //_productService.UpdateHasTierPricesProperty(product);
                    //_productService.UpdateHasDiscountsApplied(product);
                }

                if (_mediaSettings.ImportProductImagesUsingHash && _pictureService.StoreInDb && _dataProvider.SupportedLengthOfBinaryHash() > 0)
                    ImportProductImagesUsingHash(productPictureMetadata, allProductsByProductCode);
                else
                    ImportProductImagesUsingServices(productPictureMetadata);
            }
            //Trace.WriteLine(DateTime.Now-start);
        }

        /// <summary>
        /// Import newsletter subscribers from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported subscribers</returns>
        public virtual int ImportNewsletterSubscribersFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    string email;
                    bool isActive = true;
                    int storeId = _storeContext.CurrentStore.Id;
                    //parse
                    if (tmp.Length == 1)
                    {
                        //"email" only
                        email = tmp[0].Trim();
                    }
                    else if (tmp.Length == 2)
                    {
                        //"email" and "active" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                    }
                    else if (tmp.Length == 3)
                    {
                        //"email" and "active" and "storeId" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                        storeId = Int32.Parse(tmp[2].Trim());
                    }
                    else
                        throw new NopException("Wrong file format");

                    //import
                    var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email, storeId);
                    if (subscription != null)
                    {
                        subscription.Email = email;
                        subscription.Active = isActive;
                        _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);
                    }
                    else
                    {
                        subscription = new NewsLetterSubscription
                        {
                            Active = isActive,
                            CreatedOnUtc = DateTime.UtcNow,
                            Email = email,
                            StoreId = storeId,
                            NewsLetterSubscriptionGuid = Guid.NewGuid()
                        };
                        _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);
                    }
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Import states from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported states</returns>
        public virtual int ImportStatesFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    if (tmp.Length != 5)
                        throw new NopException("Wrong file format");

                    //parse
                    var countryTwoLetterIsoCode = tmp[0].Trim();
                    var name = tmp[1].Trim();
                    var abbreviation = tmp[2].Trim();
                    bool published = Boolean.Parse(tmp[3].Trim());
                    int displayOrder = Int32.Parse(tmp[4].Trim());

                    var country = _countryService.GetCountryByTwoLetterIsoCode(countryTwoLetterIsoCode);
                    if (country == null)
                    {
                        //country cannot be loaded. skip
                        continue;
                    }

                    //import
                    var states = _stateProvinceService.GetStateProvincesByCountryId(country.Id, showHidden: true);
                    var state = states.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                    if (state != null)
                    {
                        state.Abbreviation = abbreviation;
                        state.Published = published;
                        state.DisplayOrder = displayOrder;
                        _stateProvinceService.UpdateStateProvince(state);
                    }
                    else
                    {
                        state = new StateProvince
                        {
                            CountryId = country.Id,
                            Name = name,
                            Abbreviation = abbreviation,
                            Published = published,
                            DisplayOrder = displayOrder,
                        };
                        _stateProvinceService.InsertStateProvince(state);
                    }
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Import destinations from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportDestinationsFromXlsx(Stream stream)
        {
            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                //the columns
                var properties = GetPropertiesByExcelCells<Destination>(worksheet);

                var manager = new PropertyManager<Destination>(properties);

                var iRow = 2;
                var setSeName = properties.Any(p => p.PropertyName == "SeName");

                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[iRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    manager.ReadFromXlsx(worksheet, iRow);

                    var destination = _destinationService.GetDestinationById(manager.GetProperty("Id").IntValue);

                    var isNew = destination == null;

                    destination = destination ?? new Destination();

                    if (isNew)
                    {
                        destination.CreatedOnUtc = DateTime.UtcNow;

                        //default values
                        destination.PageSize = _catalogSettings.DefaultDestinationPageSize;
                        destination.PageSizeOptions = _catalogSettings.DefaultDestinationPageSizeOptions;
                        destination.Published = true;
                        destination.AllowCustomersToSelectPageSize = true;
                    }

                    var seName = string.Empty;

                    foreach (var property in manager.GetProperties)
                    {
                        switch (property.PropertyName)
                        {
                            case "Name":
                                destination.Name = property.StringValue;
                                break;
                            case "Description":
                                destination.Description = property.StringValue;
                                break;
                            case "DestinationTemplateId":
                                destination.DestinationTemplateId = property.IntValue;
                                break;
                            case "MetaKeywords":
                                destination.MetaKeywords = property.StringValue;
                                break;
                            case "MetaDescription":
                                destination.MetaDescription = property.StringValue;
                                break;
                            case "MetaTitle":
                                destination.MetaTitle = property.StringValue;
                                break;
                            case "Picture":
                                var picture = LoadPicture(manager.GetProperty("Picture").StringValue, destination.Name,
                                    isNew ? null : (int?)destination.PictureId);

                                if (picture != null)
                                    destination.PictureId = picture.Id;

                                break;
                            case "PageSize":
                                destination.PageSize = property.IntValue;
                                break;
                            case "AllowCustomersToSelectPageSize":
                                destination.AllowCustomersToSelectPageSize = property.BooleanValue;
                                break;
                            case "PageSizeOptions":
                                destination.PageSizeOptions = property.StringValue;
                                break;
                            case "PriceRanges":
                                destination.PriceRanges = property.StringValue;
                                break;
                            case "Published":
                                destination.Published = property.BooleanValue;
                                break;
                            case "DisplayOrder":
                                destination.DisplayOrder = property.IntValue;
                                break;
                            case "SeName":
                                seName = property.StringValue;
                                break;
                        }
                    }

                    destination.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                        _destinationService.InsertDestination(destination);
                    else
                        _destinationService.UpdateDestination(destination);

                    //search engine name
                    if (setSeName)
                        _urlRecordService.SaveSlug(destination, destination.ValidateSeName(seName, destination.Name, true), 0);

                    iRow++;
                }
            }
        }

        /// <summary>
        /// Import categories from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportCategoriesFromXlsx(Stream stream)
        {
            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                //the columns
                var properties = GetPropertiesByExcelCells<Category>(worksheet);

                var manager = new PropertyManager<Category>(properties);

                var iRow = 2;
                var setSeName = properties.Any(p => p.PropertyName == "SeName");

                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[iRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    manager.ReadFromXlsx(worksheet, iRow);

                    var category = _categoryService.GetCategoryById(manager.GetProperty("Id").IntValue);

                    var isNew = category == null;

                    category = category ?? new Category();

                    if (isNew)
                    {
                        category.CreatedOnUtc = DateTime.UtcNow;
                        //default values
                        category.PageSize = _catalogSettings.DefaultCategoryPageSize;
                        category.PageSizeOptions = _catalogSettings.DefaultCategoryPageSizeOptions;
                        category.Published = true;
                        category.IncludeInTopMenu = true;
                        category.AllowCustomersToSelectPageSize = true;
                    }

                    var seName = string.Empty;

                    foreach (var property in manager.GetProperties)
                    {
                        switch (property.PropertyName)
                        {
                            case "Name":
                                category.Name = property.StringValue;
                                break;
                            case "Description":
                                category.Description = property.StringValue;
                                break;
                            case "CategoryTemplateId":
                                category.CategoryTemplateId = property.IntValue;
                                break;
                            case "MetaKeywords":
                                category.MetaKeywords = property.StringValue;
                                break;
                            case "MetaDescription":
                                category.MetaDescription = property.StringValue;
                                break;
                            case "MetaTitle":
                                category.MetaTitle = property.StringValue;
                                break;
                            case "ParentCategoryId":
                                category.ParentCategoryId = property.IntValue;
                                break;
                            case "Picture":
                                var picture = LoadPicture(manager.GetProperty("Picture").StringValue, category.Name, isNew ? null : (int?)category.PictureId);
                                if (picture != null)
                                    category.PictureId = picture.Id;
                                break;
                            case "PageSize":
                                category.PageSize = property.IntValue;
                                break;
                            case "AllowCustomersToSelectPageSize":
                                category.AllowCustomersToSelectPageSize = property.BooleanValue;
                                break;
                            case "PageSizeOptions":
                                category.PageSizeOptions = property.StringValue;
                                break;
                            case "PriceRanges":
                                category.PriceRanges = property.StringValue;
                                break;
                            case "ShowOnHomePage":
                                category.ShowOnHomePage = property.BooleanValue;
                                break;
                            case "IncludeInTopMenu":
                                category.IncludeInTopMenu = property.BooleanValue;
                                break;
                            case "Published":
                                category.Published = property.BooleanValue;
                                break;
                            case "DisplayOrder":
                                category.DisplayOrder = property.IntValue;
                                break;
                            case "SeName":
                                seName = property.StringValue;
                                break;
                        }
                    }

                    category.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                        _categoryService.InsertCategory(category);
                    else
                        _categoryService.UpdateCategory(category);

                    //search engine name
                    if (setSeName)
                        _urlRecordService.SaveSlug(category, category.ValidateSeName(seName, category.Name, true), 0);

                    iRow++;
                }
            }
        }

        #endregion

        #region Nested classes

        protected class ProductPictureMetadata
        {
            public Product ProductItem { get; set; }
            public string Picture1Path { get; set; }
            public string Picture2Path { get; set; }
            public string Picture3Path { get; set; }
            public bool IsNew { get; set; }
        }

        #endregion
    }
}
