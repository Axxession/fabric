using System.Security.Claims;
using System.Text.Encodings.Web;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fabric.Server.Infrastructure.Authentication;

public sealed class ReceptionDeskWorkstationAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ReceptionDbContext db,
    ReceptionKioskKeyHasher keyHasher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string WorkstationIdHeader = "reception-desk-workstation-id";
    private const string WorkstationKeyHeader = "reception-desk-workstation-key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string workstationIdValue = Request.Headers[WorkstationIdHeader].ToString();
        string workstationKey = Request.Headers[WorkstationKeyHeader].ToString();

        if (string.IsNullOrWhiteSpace(workstationIdValue) && string.IsNullOrWhiteSpace(workstationKey))
            return AuthenticateResult.NoResult();

        if (!Guid.TryParse(workstationIdValue, out Guid workstationId))
            return AuthenticateResult.Fail("Reception desk workstation id is invalid.");

        if (string.IsNullOrWhiteSpace(workstationKey))
            return AuthenticateResult.Fail("Reception desk workstation key is required.");

        ReceptionDeskWorkstation? workstation = await db.ReceptionDeskWorkstations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == workstationId, Context.RequestAborted);

        if (workstation is null || !workstation.Enabled)
            return AuthenticateResult.Fail("Reception desk workstation is not enabled.");

        if (!keyHasher.Verify(workstationKey, workstation.ApiKeyHash, workstation.ApiKeySalt))
            return AuthenticateResult.Fail("Reception desk workstation credentials are invalid.");

        Claim[] claims =
        [
            new(ClaimTypes.Role, ReceptionDeskWorkstationAuthenticationDefaults.Role),
            new(ReceptionDeskWorkstationAuthenticationDefaults.WorkstationIdClaim, workstation.Id.ToString()),
            new(ReceptionDeskWorkstationAuthenticationDefaults.WorkstationNameClaim, workstation.Name),
            new(ReceptionDeskWorkstationAuthenticationDefaults.WorkstationLocationIdClaim, workstation.LocationId.ToString())
        ];

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
