using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Helpers;
using Nop.Admin.Models.Catalog;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.ExportImport;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;

using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class DestinationController : BaseAdminController
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly IDestinationService _destinationService;
        private readonly IDestinationTemplateService _destinationTemplateService;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IExportManager _exportManager;
        private readonly IDiscountService _discountService;
        private readonly ICustomerActivityService _customerActivityService;
        
        private readonly IAclService _aclService; 
        private readonly IPermissionService _permissionService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IWorkContext _workContext;
        private readonly IImportManager _importManager;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Constructors

        public DestinationController(ICategoryService categoryService, 
            IDestinationService destinationService,
            IDestinationTemplateService destinationTemplateService,
            IProductService productService,
            ICustomerService customerService, 
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService, 
            IPictureService pictureService,
            ILanguageService languageService, 
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService, 
            IExportManager exportManager,
            IDiscountService discountService,
            ICustomerActivityService customerActivityService, 
            
            IAclService aclService,
            IPermissionService permissionService,
            CatalogSettings catalogSettings,
            IWorkContext workContext,
            IImportManager importManager, 
            ICacheManager cacheManager)
        {
            this._categoryService = categoryService;
            this._destinationTemplateService = destinationTemplateService;
            this._destinationService = destinationService;
            this._productService = productService;
            this._customerService = customerService;
            this._storeService = storeService;
            this._storeMappingService = storeMappingService;
            this._urlRecordService = urlRecordService;
            this._pictureService = pictureService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._exportManager = exportManager;
            this._discountService = discountService;
            this._customerActivityService = customerActivityService;
            
            this._aclService = aclService;
            this._permissionService = permissionService;
            this._catalogSettings = catalogSettings;
            this._workContext = workContext;
            this._importManager = importManager;
            this._cacheManager = cacheManager;
        }

        #endregion
        
        #region Utilities

        [NonAction]
        protected virtual void UpdateLocales(Destination destination, DestinationModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(destination,
                                                               x => x.Name,
                                                               localized.Name,
                                                               localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(destination,
                                                           x => x.Description,
                                                           localized.Description,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(destination,
                                                           x => x.MetaKeywords,
                                                           localized.MetaKeywords,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(destination,
                                                           x => x.MetaDescription,
                                                           localized.MetaDescription,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(destination,
                                                           x => x.MetaTitle,
                                                           localized.MetaTitle,
                                                           localized.LanguageId);

                //search engine name
                var seName = destination.ValidateSeName(localized.SeName, localized.Name, false);
                _urlRecordService.SaveSlug(destination, seName, localized.LanguageId);
            }
        }

        [NonAction]
        protected virtual void UpdatePictureSeoNames(Destination destination)
        {
            var picture = _pictureService.GetPictureById(destination.PictureId);
            if (picture != null)
                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(destination.Name));
        }

        [NonAction]
        protected virtual void PrepareTemplatesModel(DestinationModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            var templates = _destinationTemplateService.GetAllDestinationTemplates();
            foreach (var template in templates)
            {
                model.AvailableDestinationTemplates.Add(new SelectListItem
                {
                    Text = template.Name,
                    Value = template.Id.ToString()
                });
            }
        }

        [NonAction]
        protected virtual void PrepareDiscountModel(DestinationModel model, Destination destination, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (!excludeProperties && destination != null)
                model.SelectedDiscountIds = destination.AppliedDiscounts.Select(d => d.Id).ToList();

            foreach (var discount in _discountService.GetAllDiscounts(DiscountType.AssignedToDestinations, showHidden: true))
            {
                model.AvailableDiscounts.Add(new SelectListItem
                {
                    Text = discount.Name,
                    Value = discount.Id.ToString(),
                    Selected = model.SelectedDiscountIds.Contains(discount.Id)
                });
            }
        }

        [NonAction]
        protected virtual void PrepareAclModel(DestinationModel model, Destination destination, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (!excludeProperties && destination != null)
                model.SelectedCustomerRoleIds = _aclService.GetCustomerRoleIdsWithAccess(destination).ToList();

            var allRoles = _customerService.GetAllCustomerRoles(true);
            foreach (var role in allRoles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem
                {
                    Text = role.Name,
                    Value = role.Id.ToString(),
                    Selected = model.SelectedCustomerRoleIds.Contains(role.Id)
                });
            }
        }

        [NonAction]
        protected virtual void SaveDestinationAcl(Destination destination, DestinationModel model)
        {
            destination.SubjectToAcl = model.SelectedCustomerRoleIds.Any();

            var existingAclRecords = _aclService.GetAclRecords(destination);
            var allCustomerRoles = _customerService.GetAllCustomerRoles(true);
            foreach (var customerRole in allCustomerRoles)
            {
                if (model.SelectedCustomerRoleIds.Contains(customerRole.Id))
                {
                    //new role
                    if (existingAclRecords.Count(acl => acl.CustomerRoleId == customerRole.Id) == 0)
                        _aclService.InsertAclRecord(destination, customerRole.Id);
                }
                else
                {
                    //remove role
                    var aclRecordToDelete = existingAclRecords.FirstOrDefault(acl => acl.CustomerRoleId == customerRole.Id);
                    if (aclRecordToDelete != null)
                        _aclService.DeleteAclRecord(aclRecordToDelete);
                }
            }
        }

        [NonAction]
        protected virtual void PrepareStoresMappingModel(DestinationModel model, Destination destination, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (!excludeProperties && destination != null)
                model.SelectedStoreIds = _storeMappingService.GetStoresIdsWithAccess(destination).ToList();

            var allStores = _storeService.GetAllStores();
            foreach (var store in allStores)
            {
                model.AvailableStores.Add(new SelectListItem
                {
                    Text = store.Name,
                    Value = store.Id.ToString(),
                    Selected = model.SelectedStoreIds.Contains(store.Id)
                });
            }
        }

        [NonAction]
        protected virtual void SaveStoreMappings(Destination destination, DestinationModel model)
        {
            destination.LimitedToStores = model.SelectedStoreIds.Any();

            var existingStoreMappings = _storeMappingService.GetStoreMappings(destination);
            var allStores = _storeService.GetAllStores();
            foreach (var store in allStores)
            {
                if (model.SelectedStoreIds.Contains(store.Id))
                {
                    //new store
                    if (existingStoreMappings.Count(sm => sm.StoreId == store.Id) == 0)
                        _storeMappingService.InsertStoreMapping(destination, store.Id);
                }
                else
                {
                    //remove store
                    var storeMappingToDelete = existingStoreMappings.FirstOrDefault(sm => sm.StoreId == store.Id);
                    if (storeMappingToDelete != null)
                        _storeMappingService.DeleteStoreMapping(storeMappingToDelete);
                }
            }
        }

        #endregion
        
        #region List

        public virtual ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var model = new DestinationListModel();
            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            return View(model);
        }

        [HttpPost]
        public virtual ActionResult List(DataSourceRequest command, DestinationListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedKendoGridJson();

            var destinations = _destinationService.GetAllDestinations(model.SearchDestinationName,
                model.SearchStoreId, command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = destinations.Select(x => x.ToModel()),
                Total = destinations.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        #region Create / Edit / Delete

        public virtual ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var model = new DestinationModel();
            //locales
            AddLocales(_languageService, model.Locales);
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, null, true);
            //ACL
            PrepareAclModel(model, null, false);
            //Stores
            PrepareStoresMappingModel(model, null, false);
            //default values
            model.PageSize = _catalogSettings.DefaultDestinationPageSize;
            model.PageSizeOptions = _catalogSettings.DefaultDestinationPageSizeOptions;
            model.Published = true;
            model.AllowCustomersToSelectPageSize = true;
            
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual ActionResult Create(DestinationModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var destination = model.ToEntity();
                destination.CreatedOnUtc = DateTime.UtcNow;
                destination.UpdatedOnUtc = DateTime.UtcNow;
                _destinationService.InsertDestination(destination);
                //search engine name
                model.SeName = destination.ValidateSeName(model.SeName, destination.Name, true);
                _urlRecordService.SaveSlug(destination, model.SeName, 0);
                //locales
                UpdateLocales(destination, model);
                //discounts
                var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToDestinations, showHidden: true);
                foreach (var discount in allDiscounts)
                {
                    if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                        destination.AppliedDiscounts.Add(discount);
                }
                _destinationService.UpdateDestination(destination);
                //update picture seo file name
                UpdatePictureSeoNames(destination);
                //ACL (customer roles)
                SaveDestinationAcl(destination, model);
                //Stores
                SaveStoreMappings(destination, model);

                //activity log
                _customerActivityService.InsertActivity("AddNewDestination", _localizationService.GetResource("ActivityLog.AddNewDestination"), destination.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Destinations.Added"));

                if (continueEditing)
                {
                    //selected tab
                    SaveSelectedTabName();

                    return RedirectToAction("Edit", new { id = destination.Id });
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, null, true);
            //ACL
            PrepareAclModel(model, null, true);
            //Stores
            PrepareStoresMappingModel(model, null, true);

            return View(model);
        }

        public virtual ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var destination = _destinationService.GetDestinationById(id);
            if (destination == null || destination.Deleted)
                //No destination found with the specified id
                return RedirectToAction("List");

            var model = destination.ToModel();
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = destination.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = destination.GetLocalized(x => x.Description, languageId, false, false);
                locale.MetaKeywords = destination.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = destination.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = destination.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = destination.GetSeName(languageId, false, false);
            });
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, destination, false);
            //ACL
            PrepareAclModel(model, destination, false);
            //Stores
            PrepareStoresMappingModel(model, destination, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public virtual ActionResult Edit(DestinationModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var destination = _destinationService.GetDestinationById(model.Id);
            if (destination == null || destination.Deleted)
                //No destination found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                int prevPictureId = destination.PictureId;
                destination = model.ToEntity(destination);
                destination.UpdatedOnUtc = DateTime.UtcNow;
                _destinationService.UpdateDestination(destination);
                //search engine name
                model.SeName = destination.ValidateSeName(model.SeName, destination.Name, true);
                _urlRecordService.SaveSlug(destination, model.SeName, 0);
                //locales
                UpdateLocales(destination, model);
                //discounts
                var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToDestinations, showHidden: true);
                foreach (var discount in allDiscounts)
                {
                    if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                    {
                        //new discount
                        if (destination.AppliedDiscounts.Count(d => d.Id == discount.Id) == 0)
                            destination.AppliedDiscounts.Add(discount);
                    }
                    else
                    {
                        //remove discount
                        if (destination.AppliedDiscounts.Count(d => d.Id == discount.Id) > 0)
                            destination.AppliedDiscounts.Remove(discount);
                    }
                }
                _destinationService.UpdateDestination(destination);
                //delete an old picture (if deleted or updated)
                if (prevPictureId > 0 && prevPictureId != destination.PictureId)
                {
                    var prevPicture = _pictureService.GetPictureById(prevPictureId);
                    if (prevPicture != null)
                        _pictureService.DeletePicture(prevPicture);
                }
                //update picture seo file name
                UpdatePictureSeoNames(destination);
                //ACL
                SaveDestinationAcl(destination, model);
                //Stores
                SaveStoreMappings(destination, model);

                //activity log
                _customerActivityService.InsertActivity("EditDestination", _localizationService.GetResource("ActivityLog.EditDestination"), destination.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Destinations.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    SaveSelectedTabName();

                    return RedirectToAction("Edit",  new {id = destination.Id});
                }
                return RedirectToAction("List");
            }


            //If we got this far, something failed, redisplay form
            //templates
            PrepareTemplatesModel(model);
            //discounts
            PrepareDiscountModel(model, destination, true);
            //ACL
            PrepareAclModel(model, destination, true);
            //Stores
            PrepareStoresMappingModel(model, destination, true);

            return View(model);
        }

        [HttpPost]
        public virtual ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var destination = _destinationService.GetDestinationById(id);
            if (destination == null)
                //No destination found with the specified id
                return RedirectToAction("List");

            _destinationService.DeleteDestination(destination);

            //activity log
            _customerActivityService.InsertActivity("DeleteDestination", _localizationService.GetResource("ActivityLog.DeleteDestination"), destination.Name);

            SuccessNotification(_localizationService.GetResource("Admin.Catalog.Destinations.Deleted"));
            return RedirectToAction("List");
        }
        
        #endregion

        #region Export / Import

        public virtual ActionResult ExportXml()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            try
            {
                var destinations = _destinationService.GetAllDestinations(showHidden: true);
                var xml = _exportManager.ExportDestinationsToXml(destinations);
                return new XmlDownloadResult(xml, "destinations.xml");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        public virtual ActionResult ExportXlsx()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            try
            {
                var bytes = _exportManager.ExportDestinationsToXlsx(_destinationService.GetAllDestinations(showHidden: true).Where(p=>!p.Deleted));
                 
                return File(bytes, MimeTypes.TextXlsx, "destinations.xlsx");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public virtual ActionResult ImportFromXlsx()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            //a vendor cannot import destinations
            if (_workContext.CurrentVendor != null)
                return AccessDeniedView();

            try
            {
                var file = Request.Files["importexcelfile"];
                if (file != null && file.ContentLength > 0)
                {
                    _importManager.ImportDestinationsFromXlsx(file.InputStream);
                }
                else
                {
                    ErrorNotification(_localizationService.GetResource("Admin.Common.UploadFile"));
                    return RedirectToAction("List");
                }
                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Destinations.Imported"));
                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        #endregion

        #region Products

        [HttpPost]
        public virtual ActionResult ProductList(DataSourceRequest command, int destinationId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedKendoGridJson();

            var productDestinations = _destinationService.GetProductDestinationsByDestinationId(destinationId,
                command.Page - 1, command.PageSize, true);

            var gridModel = new DataSourceResult
            {
                Data = productDestinations
                .Select(x => new DestinationModel.DestinationProductModel
                {
                    Id = x.Id,
                    DestinationId = x.DestinationId,
                    ProductId = x.ProductId,
                    ProductName = _productService.GetProductById(x.ProductId).Name,
                    IsFeaturedProduct = x.IsFeaturedProduct,
                    DisplayOrder = x.DisplayOrder
                }),
                Total = productDestinations.TotalCount
            };


            return Json(gridModel);
        }

        [HttpPost]
        public virtual ActionResult ProductUpdate(DestinationModel.DestinationProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var productDestination = _destinationService.GetProductDestinationById(model.Id);
            if (productDestination == null)
                throw new ArgumentException("No product destination mapping found with the specified id");

            productDestination.IsFeaturedProduct = model.IsFeaturedProduct;
            productDestination.DisplayOrder = model.DisplayOrder;
            _destinationService.UpdateProductDestination(productDestination);

            return new NullJsonResult();
        }

        [HttpPost]
        public virtual ActionResult ProductDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var productDestination = _destinationService.GetProductDestinationById(id);
            if (productDestination == null)
                throw new ArgumentException("No product destination mapping found with the specified id");

            //var destinationId = productDestination.DestinationId;
            _destinationService.DeleteProductDestination(productDestination);

            return new NullJsonResult();
        }

        public virtual ActionResult ProductAddPopup(int destinationId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            var model = new DestinationModel.AddDestinationProductModel();
            //categories
            model.AvailableCategories.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var categories = SelectListHelper.GetCategoryList(_categoryService, _cacheManager, true);
            foreach (var c in categories)
                model.AvailableCategories.Add(c);

            //destinations
            model.AvailableDestinations.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var destinations = SelectListHelper.GetDestinationList(_destinationService, _cacheManager, true);
            foreach (var m in destinations)
                model.AvailableDestinations.Add(m);

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var vendors = SelectListHelper.GetVendorList(_vendorService, _cacheManager, true);
            foreach (var v in vendors)
                model.AvailableVendors.Add(v);

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

            return View(model);
        }

        [HttpPost]
        public virtual ActionResult ProductAddPopupList(DataSourceRequest command, DestinationModel.AddDestinationProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedKendoGridJson();

            var gridModel = new DataSourceResult();
            var products = _productService.SearchProducts(
                categoryIds: new List<int> { model.SearchCategoryId },
                destinationId: model.SearchDestinationId,
                storeId: model.SearchStoreId,
                vendorId: model.SearchVendorId,
                productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
                keywords: model.SearchProductName,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize,
                showHidden: true
                );
            gridModel.Data = products.Select(x => x.ToModel());
            gridModel.Total = products.TotalCount;

            return Json(gridModel);
        }
        
        [HttpPost]
        [FormValueRequired("save")]
        public virtual ActionResult ProductAddPopup(string btnId, string formId, DestinationModel.AddDestinationProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDestinations))
                return AccessDeniedView();

            if (model.SelectedProductIds != null)
            {
                foreach (int id in model.SelectedProductIds)
                {
                    var product = _productService.GetProductById(id);
                    if (product != null)
                    {
                        var existingProductdestinations = _destinationService.GetProductDestinationsByDestinationId(model.DestinationId, showHidden: true);
                        if (existingProductdestinations.FindProductDestination(id, model.DestinationId) == null)
                        {
                            _destinationService.InsertProductDestination(
                                new ProductDestination
                                {
                                    DestinationId = model.DestinationId,
                                    ProductId = id,
                                    IsFeaturedProduct = false,
                                    DisplayOrder = 1
                                });
                        }
                    }
                }
            }

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            return View(model);
        }

        #endregion

        
    }
}
