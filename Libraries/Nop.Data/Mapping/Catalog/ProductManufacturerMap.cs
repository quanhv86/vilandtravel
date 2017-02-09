using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductDestinationMap : NopEntityTypeConfiguration<ProductDestination>
    {
        public ProductDestinationMap()
        {
            this.ToTable("Product_Destination_Mapping");
            this.HasKey(pm => pm.Id);
            
            this.HasRequired(pm => pm.Destination)
                .WithMany()
                .HasForeignKey(pm => pm.DestinationId);


            this.HasRequired(pm => pm.Product)
                .WithMany(p => p.ProductDestinations)
                .HasForeignKey(pm => pm.ProductId);
        }
    }
}