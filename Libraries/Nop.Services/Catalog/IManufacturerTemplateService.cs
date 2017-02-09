using System.Collections.Generic;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Destination template service interface
    /// </summary>
    public partial interface IDestinationTemplateService
    {
        /// <summary>
        /// Delete destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        void DeleteDestinationTemplate(DestinationTemplate destinationTemplate);

        /// <summary>
        /// Gets all destination templates
        /// </summary>
        /// <returns>Destination templates</returns>
        IList<DestinationTemplate> GetAllDestinationTemplates();

        /// <summary>
        /// Gets a destination template
        /// </summary>
        /// <param name="destinationTemplateId">Destination template identifier</param>
        /// <returns>Destination template</returns>
        DestinationTemplate GetDestinationTemplateById(int destinationTemplateId);

        /// <summary>
        /// Inserts destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        void InsertDestinationTemplate(DestinationTemplate destinationTemplate);

        /// <summary>
        /// Updates the destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        void UpdateDestinationTemplate(DestinationTemplate destinationTemplate);
    }
}
