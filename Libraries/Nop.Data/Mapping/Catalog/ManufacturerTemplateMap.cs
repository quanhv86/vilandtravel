using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class DestinationTemplateMap : NopEntityTypeConfiguration<DestinationTemplate>
    {
        public DestinationTemplateMap()
        {
            this.ToTable("DestinationTemplate");
            this.HasKey(p => p.Id);
            this.Property(p => p.Name).IsRequired().HasMaxLength(400);
            this.Property(p => p.ViewPath).IsRequired().HasMaxLength(400);
        }
    }
}