# Locations

`Locations` is its own bounded context. It is a foundational physical hierarchy that other contexts reference by id, but it does not know about PACS, packages, approvals, visitors, employees, or credentials.

Current implementation has `Site`, `Building`, and `Room`. Conceptually, access scoping may also use `Zone` later.

```mermaid
classDiagram
    class Site {
        Guid Id
        string Name
        string Address
    }

    class Building {
        Guid Id
        string Name
        string Address
    }

    class Room {
        Guid Id
        string Name
        int Capacity
        bool WheelchairAccessible
    }

    class LocationLookup {
        Guid Id
        LocationType Type
        Guid SiteId
        Guid BuildingId
        Guid RoomId
    }

    class LocationType {
        Site
        Building
        Room
    }

    Site "1" --> "*" Building
    Building "1" --> "*" Room
    Site "1" --> "*" LocationLookup
    Building "1" --> "*" LocationLookup
    Room "1" --> "*" LocationLookup
```

Boundary rules:

- Locations owns the hierarchy and location metadata.
- Other contexts store `LocationId` references.
- Linking PACS to locations does not belong in Locations; it belongs in Access Control.
