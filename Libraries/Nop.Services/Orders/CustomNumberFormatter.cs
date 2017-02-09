//using System;
//using System.Text.RegularExpressions;

using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders
{
    public partial class CustomNumberFormatter : ICustomNumberFormatter
    {
        #region Fields

        private OrderSettings _orderSettings;

        #endregion

        #region Ctor

        public CustomNumberFormatter(OrderSettings orderSettings)
        {
            _orderSettings = orderSettings;
        }

        #endregion

        #region Methods
 
        public virtual string GenerateOrderCustomNumber(Order order)
        {
            if (string.IsNullOrEmpty(_orderSettings.CustomOrderNumberMask))
                return order.Id.ToString();

            var customNumber = _orderSettings.CustomOrderNumberMask
                .Replace("{ID}", order.Id.ToString())
                .Replace("{YYYY}", order.CreatedOnUtc.ToString("yyyy"))
                .Replace("{YY}", order.CreatedOnUtc.ToString("yy"))
                .Replace("{MM}", order.CreatedOnUtc.ToString("MM"))
                .Replace("{DD}", order.CreatedOnUtc.ToString("dd")).Trim();

    
            return customNumber;
        }

        #endregion
    }
}