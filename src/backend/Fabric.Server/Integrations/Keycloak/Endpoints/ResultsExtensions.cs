using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Integrations.Keycloak.Endpoints;

internal static class ResultsExtensions
{
    public static IResult Problem(KeycloakAdminError error)
    {
        int statusCode = error.Code switch
        {
            KeycloakAdminErrorCode.IntegrationDisabled => StatusCodes.Status409Conflict,
            KeycloakAdminErrorCode.IntegrationNotConfigured => StatusCodes.Status409Conflict,
            KeycloakAdminErrorCode.InvalidRequest => StatusCodes.Status400BadRequest,
            KeycloakAdminErrorCode.NotFound => StatusCodes.Status404NotFound,
            KeycloakAdminErrorCode.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status502BadGateway,
        };

        return Results.Problem(statusCode: statusCode, title: "Keycloak request failed", detail: error.Detail);
    }
}
