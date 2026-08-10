using Fabric.Server.Printing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Printing.Persistence.Configuration;

public sealed class PrintDesignConfiguration : IEntityTypeConfiguration<PrintDesign>
{
    public void Configure(EntityTypeBuilder<PrintDesign> builder)
    {
        builder.ToTable("print_designs");
        builder.HasKey(design => design.Id);
        builder.Property(design => design.Id).ValueGeneratedNever();
        builder.Property(design => design.Name).IsRequired().HasMaxLength(200);
        builder.Property(design => design.Version).IsRequired();
        builder.Property(design => design.Description).HasMaxLength(1000);
        builder.Property(design => design.SurfaceKind).IsRequired();
        builder.Property(design => design.DesignJson).IsRequired().HasColumnType("jsonb");
        builder.Property(design => design.MediaLabel).IsRequired().HasMaxLength(200);
        builder.Property(design => design.MediaWidth).IsRequired();
        builder.Property(design => design.MediaHeight).IsRequired();
        builder.Property(design => design.MediaOrientation).IsRequired();
        builder.Property(design => design.Dpi).IsRequired();
        builder.Property(design => design.CreatedAt).IsRequired();
        builder.Property(design => design.UpdatedAt).IsRequired();
        builder.HasIndex(design => new { design.Name, design.Version }).IsUnique();
        builder.HasIndex(design => design.SurfaceKind);
        builder.HasIndex(design => design.MediaLabel);
    }
}
