using System.Collections.Generic;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;



namespace Nop.Services.Messages
{
    public partial interface IWorkflowMessageService
    {
        #region Customer workflow

        /// <summary>
        /// Sends 'New customer' notification message to a store owner
        /// </summary>
        int SendCustomerRegisteredNotificationMessage(Customer customer, int languageId);

        /// <summary>
        /// Sends a welcome message to a customer
        /// </summary>
        int SendCustomerWelcomeMessage(Customer customer, int languageId);

        /// <summary>
        /// Sends an email validation message to a customer
        /// </summary>
        int SendCustomerEmailValidationMessage(Customer customer, int languageId);
        
        /// <summary>
        /// Sends an email re-validation message to a customer
        /// </summary>
        int SendCustomerEmailRevalidationMessage(Customer customer, int languageId);

        /// <summary>
        /// Sends password recovery message to a customer
        /// </summary> 
        int SendCustomerPasswordRecoveryMessage(Customer customer, int languageId);
        
        #endregion

        #region Order workflow
 
        /// <summary>
        /// Sends an order placed notification to a store owner
        /// </summary> 
        int SendOrderPlacedStoreOwnerNotification(Order order, int languageId);

        /// <summary>
        /// Sends an order paid notification to a store owner
        /// </summary> 
        int SendOrderPaidStoreOwnerNotification(Order order, int languageId);

        /// <summary>
        /// Sends an order paid notification to a customer
        /// </summary> 
        int SendOrderPaidCustomerNotification(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null);
 
        /// <summary>
        /// Sends an order placed notification to a customer
        /// </summary> 
        int SendOrderPlacedCustomerNotification(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null);

   
 
        /// <summary>
        /// Sends an order completed notification to a customer
        /// </summary> 
        int SendOrderCompletedCustomerNotification(Order order, int languageId, 
            string attachmentFilePath = null, string attachmentFileName = null);

        /// <summary>
        /// Sends an order cancelled notification to a customer
        /// </summary> 
        int SendOrderCancelledCustomerNotification(Order order, int languageId);

        /// <summary>
        /// Sends an order refunded notification to a store owner
        /// </summary> 
        int SendOrderRefundedStoreOwnerNotification(Order order, decimal refundedAmount, int languageId);

        /// <summary>
        /// Sends an order refunded notification to a customer
        /// </summary> 
        int SendOrderRefundedCustomerNotification(Order order, decimal refundedAmount, int languageId);

        /// <summary>
        /// Sends a new order note added notification to a customer
        /// </summary> 
        int SendNewOrderNoteAddedCustomerNotification(OrderNote orderNote, int languageId);
 
  
        #endregion

        #region Newsletter workflow

        /// <summary>
        /// Sends a newsletter subscription activation message
        /// </summary> 
        int SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
            int languageId);

        /// <summary>
        /// Sends a newsletter subscription deactivation message
        /// </summary> 
        int SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
            int languageId);

        #endregion

        #region Send a message to a friend

        /// <summary>
        /// Sends "email a friend" message
        /// </summary> 
        int SendProductEmailAFriendMessage(Customer customer, int languageId,
            Product product, string customerEmail, string friendsEmail, string personalMessage);

        /// <summary>
        /// Sends wishlist "email a friend" message
        /// </summary> 
        int SendWishlistEmailAFriendMessage(Customer customer, int languageId,
             string customerEmail, string friendsEmail, string personalMessage);

        #endregion 
        
        #region Misc

  
 
        /// <summary>
        /// Sends a product review notification message to a store owner
        /// </summary> 
        int SendProductReviewNotificationMessage(ProductReview productReview,
            int languageId);
 
        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary> 
        int SendQuantityBelowStoreOwnerNotification(Product product, int languageId);

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary> 
        int SendQuantityBelowStoreOwnerNotification(ProductAttributeCombination combination, int languageId);

        /// <summary>
        /// Sends a "new VAT submitted" notification to a store owner
        /// </summary> 
        int SendNewVatSubmittedStoreOwnerNotification(Customer customer,
            string vatName, string vatAddress, int languageId);

        /// <summary>
        /// Sends a blog comment notification message to a store owner
        /// </summary> 
        int SendBlogCommentNotificationMessage(BlogComment blogComment, int languageId);

        /// <summary>
        /// Sends a news comment notification message to a store owner
        /// </summary> 
        int SendNewsCommentNotificationMessage(NewsComment newsComment, int languageId);

  
        /// <summary>
        /// Sends a test email
        /// </summary> 
        int SendTestEmail(int messageTemplateId, string sendToEmail,
            List<Token> tokens, int languageId);

        #endregion
    }
}
