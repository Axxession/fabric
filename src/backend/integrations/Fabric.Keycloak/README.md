## Fabric.Keycloak

Generated Keycloak Admin API client for backend integrations.

### Inputs

- OpenAPI spec snapshot: `openapi/keycloak-admin-api.json`
- NSwag config: `nswag.json`

Current spec source: `https://raw.githubusercontent.com/ccouzens/keycloak-openapi/main/keycloak/21.0.2.json`

The committed snapshot normalizes wildcard success response codes from `2XX` to `200` because NSwag generates invalid C# for the wildcard form.

### Regenerate client

Run from `src/backend`:

```bash
dotnet msbuild integrations/Fabric.Keycloak/Fabric.Keycloak.csproj /t:GenerateKeycloakClient
```

Generated code is committed under `Generated/`.
