﻿using System.Web.Mvc;
using Nop.Services.Blogs;
using Nop.Services.Catalog;
using Nop.Services.News;
using Nop.Services.Seo;
using Nop.Services.Topics;


namespace Nop.Web.Controllers
{
    public partial class BackwardCompatibility2XController : BasePublicController
    {
		#region Fields

        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IDestinationService _destinationService;
        private readonly INewsService _newsService;
        private readonly IBlogService _blogService;
        private readonly ITopicService _topicService;
        

        #endregion

		#region Constructors

        public BackwardCompatibility2XController(IProductService productService,
            ICategoryService categoryService, 
            IDestinationService destinationService,
            INewsService newsService, 
            IBlogService blogService,
            ITopicService topicService,
            IVendorService vendorService)
        {
            this._productService = productService;
            this._categoryService = categoryService;
            this._destinationService = destinationService;
            this._newsService = newsService;
            this._blogService = blogService;
            this._topicService = topicService;
            
        }

		#endregion
        
        #region Methods
        
        //in versions 2.00-2.65 we had ID in product URLs
        public virtual ActionResult RedirectProductById(int productId)
        {
            var product = _productService.GetProductById(productId);
            if (product == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("Product", new { SeName = product.GetSeName() });
        }
        //in versions 2.00-2.65 we had ID in category URLs
        public virtual ActionResult RedirectCategoryById(int categoryId)
        {
            var category = _categoryService.GetCategoryById(categoryId);
            if (category == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("Category", new { SeName = category.GetSeName() });
        }
        //in versions 2.00-2.65 we had ID in destination URLs
        public virtual ActionResult RedirectDestinationById(int destinationId)
        {
            var destination = _destinationService.GetDestinationById(destinationId);
            if (destination == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("Destination", new { SeName = destination.GetSeName() });
        }
        //in versions 2.00-2.70 we had ID in news URLs
        public virtual ActionResult RedirectNewsItemById(int newsItemId)
        {
            var newsItem = _newsService.GetNewsById(newsItemId);
            if (newsItem == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("NewsItem", new { SeName = newsItem.GetSeName(newsItem.LanguageId, ensureTwoPublishedLanguages: false) });
        }
        //in versions 2.00-2.70 we had ID in blog URLs
        public virtual ActionResult RedirectBlogPostById(int blogPostId)
        {
            var blogPost = _blogService.GetBlogPostById(blogPostId);
            if (blogPost == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("BlogPost", new { SeName = blogPost.GetSeName(blogPost.LanguageId, ensureTwoPublishedLanguages: false) });
        }
        //in versions 2.00-3.20 we had SystemName in topic URLs
        public virtual ActionResult RedirectTopicBySystemName(string systemName)
        {
            var topic = _topicService.GetTopicBySystemName(systemName);
            if (topic == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("Topic", new { SeName = topic.GetSeName() });
        }
        //in versions 3.00-3.20 we had ID in vendor URLs
        public virtual ActionResult RedirectVendorById(int vendorId)
        {
            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                return RedirectToRoutePermanent("HomePage");

            return RedirectToRoutePermanent("Vendor", new { SeName = vendor.GetSeName() });
        }
        #endregion
    }
}
