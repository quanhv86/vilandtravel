using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Destination template service
    /// </summary>
    public partial class DestinationTemplateService : IDestinationTemplateService
    {
        #region Fields

        private readonly IRepository<DestinationTemplate> _destinationTemplateRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion
        
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="destinationTemplateRepository">Destination template repository</param>
        /// <param name="eventPublisher">Event published</param>
        public DestinationTemplateService(IRepository<DestinationTemplate> destinationTemplateRepository,
            IEventPublisher eventPublisher)
        {
            this._destinationTemplateRepository = destinationTemplateRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        public virtual void DeleteDestinationTemplate(DestinationTemplate destinationTemplate)
        {
            if (destinationTemplate == null)
                throw new ArgumentNullException("destinationTemplate");

            _destinationTemplateRepository.Delete(destinationTemplate);

            //event notification
            _eventPublisher.EntityDeleted(destinationTemplate);
        }

        /// <summary>
        /// Gets all destination templates
        /// </summary>
        /// <returns>Destination templates</returns>
        public virtual IList<DestinationTemplate> GetAllDestinationTemplates()
        {
            var query = from pt in _destinationTemplateRepository.Table
                        orderby pt.DisplayOrder, pt.Id
                        select pt;

            var templates = query.ToList();
            return templates;
        }

        /// <summary>
        /// Gets a destination template
        /// </summary>
        /// <param name="destinationTemplateId">Destination template identifier</param>
        /// <returns>Destination template</returns>
        public virtual DestinationTemplate GetDestinationTemplateById(int destinationTemplateId)
        {
            if (destinationTemplateId == 0)
                return null;

            return _destinationTemplateRepository.GetById(destinationTemplateId);
        }

        /// <summary>
        /// Inserts destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        public virtual void InsertDestinationTemplate(DestinationTemplate destinationTemplate)
        {
            if (destinationTemplate == null)
                throw new ArgumentNullException("destinationTemplate");

            _destinationTemplateRepository.Insert(destinationTemplate);

            //event notification
            _eventPublisher.EntityInserted(destinationTemplate);
        }

        /// <summary>
        /// Updates the destination template
        /// </summary>
        /// <param name="destinationTemplate">Destination template</param>
        public virtual void UpdateDestinationTemplate(DestinationTemplate destinationTemplate)
        {
            if (destinationTemplate == null)
                throw new ArgumentNullException("destinationTemplate");

            _destinationTemplateRepository.Update(destinationTemplate);

            //event notification
            _eventPublisher.EntityUpdated(destinationTemplate);
        }
        
        #endregion
    }
}
