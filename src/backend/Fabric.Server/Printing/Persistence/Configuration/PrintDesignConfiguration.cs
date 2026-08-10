using System.Text.Json;
using System.Text.Json.Serialization;
using Fabric.Server.Printing.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fabric.Server.Printing.Persistence.Configuration;

public sealed class PrintDesignConfiguration : IEntityTypeConfiguration<PrintDesign>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
        builder.Property(design => design.DefaultRenderProfile)
            .HasColumnType("jsonb")
            .HasConversion(DefaultRenderProfileConverter, DefaultRenderProfileComparer);
        builder.Property(design => design.CreatedAt).IsRequired();
        builder.Property(design => design.UpdatedAt).IsRequired();
        builder.HasIndex(design => new { design.Name, design.Version }).IsUnique();
        builder.HasIndex(design => design.SurfaceKind);
        builder.HasIndex(design => design.MediaLabel);
    }

    private static readonly ValueConverter<RenderProfile?, string?> DefaultRenderProfileConverter = new(
        profile => profile == null ? null : JsonSerializer.Serialize(profile, JsonOptions),
        json => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<RenderProfile>(json, JsonOptions));

    private static readonly ValueComparer<RenderProfile?> DefaultRenderProfileComparer = new(
        (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
        profile => profile == null ? 0 : JsonSerializer.Serialize(profile, JsonOptions).GetHashCode(StringComparison.Ordinal),
        profile => profile == null ? null : JsonSerializer.Deserialize<RenderProfile>(JsonSerializer.Serialize(profile, JsonOptions), JsonOptions));
}
