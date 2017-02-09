using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Price formatter
    /// </summary>
    public partial interface IPriceFormatter
    {
        /// <summary>
        /// Formats the price
        /// </summary>
        /// <param name="price">Price</param>
        /// <returns>Price</returns>
        string FormatPrice(decimal price);
 
        string FormatPrice(decimal price, bool showCurrency, Currency targetCurrency);
 
        string FormatPrice(decimal price, bool showCurrency, bool showTax);

 
        string FormatPrice(decimal price, bool showCurrency, 
            string currencyCode, bool showTax, Language language);

 
        string FormatPrice(decimal price, bool showCurrency,
            string currencyCode, Language language, bool priceIncludesTax);
 
        string FormatPrice(decimal price, bool showCurrency, 
            Currency targetCurrency, Language language, bool priceIncludesTax);
 
        string FormatPrice(decimal price, bool showCurrency, 
            Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);

     
        string FormatPaymentMethodAdditionalFee(decimal price, bool showCurrency);

    
        string FormatPaymentMethodAdditionalFee(decimal price, bool showCurrency,
            Currency targetCurrency, Language language, bool priceIncludesTax);

    
        string FormatPaymentMethodAdditionalFee(decimal price, bool showCurrency, 
            Currency targetCurrency, Language language, bool priceIncludesTax, bool showTax);

    
        string FormatPaymentMethodAdditionalFee(decimal price, bool showCurrency, 
            string currencyCode, Language language, bool priceIncludesTax);


 
        string FormatTaxRate(decimal taxRate);
    }
}
