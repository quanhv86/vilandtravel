namespace Nop.Core.Domain.Discounts
{
    /// <summary>
    /// Represents a discount type
    /// </summary>
    public enum DiscountType
    {
        /// <summary>
        /// Assigned to order total 
        /// </summary>
        AssignedToOrderTotal = 1,
        /// <summary>
        /// Assigned to products (SKUs)
        /// </summary>
        AssignedToProductCodes = 2,
        /// <summary>
        /// Assigned to categories (all products in a category)
        /// </summary>
        AssignedToCategories = 5,
        /// <summary>
        /// Assigned to destinations (all products of a destination)
        /// </summary>
        AssignedToDestinations = 6,
        /// <summary>
        /// Assigned to shipping
        /// </summary>
        AssignedToShipping = 10,
        /// <summary>
        /// Assigned to order subtotal
        /// </summary>
        AssignedToOrderSubTotal = 20,
    }
}
