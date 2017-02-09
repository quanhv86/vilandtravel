using FluentValidation;
using Nop.Admin.Models.Templates;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Admin.Validators.Templates
{
    public partial class DestinationTemplateValidator : BaseNopValidator<DestinationTemplateModel>
    {
        public DestinationTemplateValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResource("Admin.System.Templates.Destination.Name.Required"));
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(localizationService.GetResource("Admin.System.Templates.Destination.ViewPath.Required"));
        }
    }
}