# Testing

## Unit Testing Aggregates

Test business logic directly — no mocking needed for pure domain rules. The aggregate encapsulates all state transitions and validation.

```csharp
[Fact]
public void Lock_WhenAlreadyLocked_ReturnsFailure()
{
    var keyGroup = KeyGroup.Create("Test", KeyType.A, 10, 2);
    keyGroup.Lock();

    var result = keyGroup.Lock();

    Assert.True(result.IsFailure(out var error));
    Assert.Equal(KeyGroupError.AlreadyLocked, error);
}

[Fact]
public void Lock_WhenUnlocked_ReturnsSuccess()
{
    var keyGroup = KeyGroup.Create("Test", KeyType.A, 10, 2);

    var result = keyGroup.Lock();

    Assert.True(result.IsSuccess());
}
```

**Naming convention:** `{Method}_{Scenario}_Returns{Outcome}`.

## Integration Testing Endpoints

Use `WebApplicationFactory` to test the full HTTP layer. Focus on status codes, response shapes, and error contracts — not framework behavior.

```csharp
public class ListKeyGroupsTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public ListKeyGroupsTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_WhenKeyGroupsExist_Returns200WithPagedResult()
    {
        var response = await _client.GetAsync("/key-groups?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IPaged<KeyGroupResponse>>();
        Assert.NotNull(body);
    }
}
```

## Mocking Strategy

- Mock at the **boundary** — only external dependencies (HTTP clients, file I/O, message buses).
- Do **not** mock DbContext or EF Core — use the real database (test container or SQLite in-memory).
- Prefer hand-written test doubles over mocking frameworks when the interface is simple.

## Test Project Structure

```
tests/
├── MyService.Domain.Tests/    — aggregate unit tests
├── MyService.Api.Tests/       — endpoint integration tests
└── MyService.Integration.Tests/ — cross-service scenario tests
```
