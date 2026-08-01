# EF Core Configuration

All entity configuration uses `IEntityTypeConfiguration<T>` classes. **No attribute-based configuration** (`[Column]`, `[Required]`, `[MaxLength]`, etc.) on domain entities.

```
Infrastructure/Persistence/
├── Migrations/
├── Configuration/          — IEntityTypeConfiguration<T> classes, one per entity
└── MyDbContext.cs
```

The `DbContext` scans and applies all configurations automatically:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
}
```

## Aggregates with private setters

```csharp
public class KeyGroupConfiguration : IEntityTypeConfiguration<KeyGroup>
{
    public void Configure(EntityTypeBuilder<KeyGroup> builder)
    {
        builder.ToTable("key_groups");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).IsRequired().HasMaxLength(200);
        builder.Property(k => k.KeyType).IsRequired();
        builder.Property(k => k.Locked).IsRequired();
        builder.OwnsMany(k => k.KeySets, ks =>
        {
            ks.ToTable("key_sets");
            ks.WithOwner().HasForeignKey("KeyGroupId");
        });
    }
}
```

## `OwnsMany` — two required rules

1. **`ValueGeneratedNever()` on the owned entity's `Id`** — EF Core defaults to `ValueGeneratedOnAdd` for `Guid` keys. Since we assign `Id = Guid.NewGuid()` ourselves, omitting this causes EF to read back a null/empty key after INSERT and issue a spurious UPDATE → `DbUpdateConcurrencyException`.

2. **Collection navigation must use `{ get; private set; }`, not `{ get; init; }`** — EF Core replaces the collection with its change-tracking proxy during entity materialisation. An `init`-only property blocks this substitution.

## `record` types in `OwnsMany` collections

Do not use `record` with `init`-only properties for entities in `OwnsMany` collections. Use a `class` with `private set` properties instead:

```csharp
// ✗ — record with init blocks EF change-tracking proxy
public record ScheduleBlock { public TimeSpan Start { get; init; } public TimeSpan End { get; init; } }

// ✓ — class with private set works with OwnsMany
public class ScheduleBlock { public TimeSpan Start { get; private set; } public TimeSpan End { get; private set; } }
```

```csharp
builder.OwnsMany(c => c.ContactPersons, cp =>
{
    cp.ToTable("company_contact_persons");
    cp.WithOwner().HasForeignKey("company_id");
    cp.HasKey(p => p.Id);
    cp.Property(p => p.Id).ValueGeneratedNever();
});
```

## Value objects mapping

Value objects are mapped as owned entities or JSON columns — never as separate top-level tables with their own `Id`:

```csharp
// Owned entity — flat value object, fields become columns on the parent table
builder.OwnsOne(c => c.Specification, spec =>
{
    spec.Property(s => s.SomeField).HasColumnName("specification_some_field");
});

// JSON column — nested or variable-structure value object
builder.Property(c => c.Specification).HasColumnType("jsonb");
```

Use owned entity mapping when the value object is flat and individual fields may be queried. Use JSON column when the structure is nested or always read/written as a whole.

## Link table configurations

Small join-table entities with no business logic of their own may be co-located in the same file as their owning entity's configuration. Only when the link entity has no independent lifecycle and its configuration is <20 lines.
