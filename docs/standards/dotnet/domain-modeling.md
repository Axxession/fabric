# Domain Modeling: Entities, Value Objects, and Aggregates

## Guiding Principles

1. **Simplicity first.** Only add complexity when the problem actually requires it.
2. **Consistency over local optimality.** One pattern applied everywhere is more valuable than several patterns each slightly optimal in their own context.
3. **Hard rules over guidelines.** A rule that can be broken without sign-off is not a rule.

## Entity

An object with a persistent identity. Two entities with identical data are still distinct — their identity is what distinguishes them, not their contents. Entities have an `Id`.

```csharp
public class ChipDesign
{
    public Guid Id { get; set; }       // set, not init — framework must materialise the key
    public string Name { get; set; } = default!;
    public TemplateSpecification Specification { get; set; } = new();
}
```

> **Rule**: Use `{ get; set; }` (not `{ get; init; }`) on all properties that EF Core reads or writes — including `Id`. Frameworks cannot write back to `init`-only properties during materialisation, which causes silent state mismatches. `init` is safe only on types the persistence layer never touches (e.g. pure DTOs, request/response types, value objects mapped as JSON columns).

## Value Object

An object defined entirely by its contents, not by an identity. Two value objects with the same data are interchangeable. Value objects are **immutable** — they are replaced, never mutated in place.

```csharp
public record TemplateSpecification
{
    public PiccSpecification Picc { get; init; } = default!;
    public Dictionary<string, ApplicationSpecification> Applications { get; init; } = [];
}
```

Use `record` with `init`-only properties. No public setters. Replace, never mutate.

## Aggregate

**Rule: any entity with a lifecycle — any state or flag that can transition — must be an aggregate. No exceptions.**

An aggregate wraps an entity and is the sole enforcer of all business rules on state. External code calls named methods on the aggregate and receives a `Result` back. There is no way to bypass the rules.

```csharp
public sealed class KeyGroup
{
    private KeyGroup() { }

    public static KeyGroup Create(string name, KeyType keyType, int numberOfKeys, int numberOfKeySets)
    {
        return new KeyGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyType = keyType,
            NumberOfKeys = numberOfKeys,
            NumberOfKeySets = numberOfKeySets,
            KeySets = [],
            Locked = false,
        };
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public KeyType KeyType { get; private set; }
    public int NumberOfKeys { get; private set; }
    public int NumberOfKeySets { get; private set; }
    public bool Locked { get; private set; }
    public KeySet[] KeySets { get; private set; } = [];

    public Result Lock()
    {
        if (Locked)
            return Result.Failure(KeyGroupErrors.AlreadyLocked);
        Locked = true;
        return Result.Success();
    }

    public Result UpdateKeys(KeySet[] keySets)
    {
        if (Locked)
            return Result.Failure(KeyGroupErrors.CannotEditLockedKeyGroup);
        KeySets = keySets;
        return Result.Success();
    }
}
```

**What triggers the aggregate pattern:**
- A boolean flag that transitions in one direction (`Locked`, `Canceled`, `Published`, `Archived`)
- An enum state with enforced transitions (`Scheduled → Approved → Started → Finished`)
- Any property that can only be changed under specific conditions

**What does not trigger it:**
- A configuration record with no rules on mutation
- A DTO or read model
- A value object

## Child entities owned by an aggregate

Use `internal set` instead of `private set` on aggregate-owned child entity properties. This allows the aggregate and EF Core's change tracker (both in the same assembly) to write properties, while blocking mutation from outside the assembly.

```csharp
public class Door
{
    public Guid Id { get; internal set; }
    public string Name { get; internal set; } = default!;
    public Guid ControllerId { get; internal set; }
    public List<DoorReaderLink> ReaderLinks { get; private set; } = [];

    internal void Rename(string name) => Name = name;
}
```

**Rule**: Use `internal set` (not `public set`) on aggregate-owned child entity properties. Mutation methods are `internal`. The aggregate root is the only entry point for creating child entities.

## Decision Flowchart

```
Is this concept referenced by other entities using an Id?
│
├─ YES → It is an entity (or aggregate — see below).
│
└─ NO  → Is it defined entirely by its contents (no independent identity)?
          └─ YES → Value object. Immutable. record with init-only properties.

Does this entity have any state that can transition?
│
├─ YES → AGGREGATE. Hard rule. Private setters, private constructor,
│        static factory method, named methods for every mutation,
│        Result<T> returns. No exceptions.
│
└─ NO  → Plain entity. Public or init setters. No ceremony.
```
