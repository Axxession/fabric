# Middleware Pipeline

## Order

Middleware is evaluated in the order it is registered. The correct order for the standard pipeline:

```csharp
var app = builder.Build();

app.UseExceptionHandler();           // 1 — catch all unhandled exceptions
app.UseCors();                       // 2 — CORS headers before auth
app.UseAuthentication();             // 3 — authenticate the request
app.UseAuthorization();              // 4 — authorize the request
app.UseOpenApi();                    // 5 — Swagger/OpenAPI UI
app.MapControllers();                // 6 — endpoint handlers
```

**Rule:** Exception handler must be first. CORS must be before Authentication. Authentication must be before Authorization.

## Exception Handling

Use a custom exception handler middleware that maps common exception types to ProblemDetails responses:

```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (statusCode, detail) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest, ex.Message),
            NotFoundException      => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Detail = detail,
        });
    });
});
```

**Rule:** Do not use `app.UseDeveloperExceptionPage()` in production. Use the structured exception handler above in all environments and add developer page conditionally via feature flag.

## CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCors", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("ApiCors");
```

CORS origins come from configuration — never hardcode. Use `AllowCredentials()` only when cookies or token headers are needed.
