using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Fabric.Server.Sagas.VisitorPreOnboarding;

public static class VisitorPreOnboardingSagaEndpoints
{
    public static IEndpointRouteBuilder MapVisitorPreOnboardingSagaEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/sagas/visitor-pre-onboarding");

        group.MapPost("/{id:guid}/retry", RetrySaga)
            .WithDescription("Retry an expired visitor pre-onboarding saga")
            .WithSummary("Retry saga")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        group.MapGet("/configuration", GetConfiguration)
            .Produces<VisitorPreOnboardingSagaConfig>();
        group.MapPut("/configuration", UpdateConfiguration)
            .Produces<VisitorPreOnboardingSagaConfig>();
        group.MapGet("/{visitId:guid}", GetOnboardingSagas)
            .Produces<List<VisitorPreOnboardingSaga>>();
        group.MapGet("/{visitId:guid}/{invitationId:guid}", GetOnboardingSaga)
            .Produces<VisitorPreOnboardingSaga>()
            .Produces(StatusCodes.Status404NotFound);
        group.MapGet("/qr", GetQrCode)
            .AllowAnonymous()
            .WithDescription("Generate a visitor QR image")
            .WithSummary("Generate visitor QR")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static IResult GetQrCode(
        [FromQuery] string code,
        [FromQuery] int size = 150)
    {
        if (string.IsNullOrWhiteSpace(code))
            return QrValidationProblem("QR code data is required.");

        if (size is < 32 or > 1024)
            return QrValidationProblem("QR size must be between 32 and 1024 pixels.");

        using QRCodeData qrCodeData = QRCodeGenerator.GenerateQrCode(code, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        int pixelsPerModule = Math.Max(1, (int)Math.Round((double)size / qrCodeData.ModuleMatrix.Count));
        byte[] image = qrCode.GetGraphic(pixelsPerModule);

        return Results.File(image, "image/png");
    }

    private static IResult QrValidationProblem(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid QR image request.",
            detail: detail);

    private static async Task<IResult> RetrySaga(
        Guid id,
        VisitorPreOnboardingSagaService service,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await service.RetryAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(
                new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = ex.Message }
            );
        }
    }

    private static async Task<IResult> GetConfiguration(
        VisitorPreOnboardingSagaService service,
        CancellationToken cancellationToken = default
    )
    {
        VisitorPreOnboardingSagaConfig config = await service.GetConfigurationAsync(cancellationToken);
        return Results.Ok(config);
    }

    private static async Task<IResult> UpdateConfiguration(
        [FromBody] VisitorPreOnboardingSagaConfigRequest request,
        VisitorPreOnboardingSagaService service,
        CancellationToken cancellationToken = default
    )
    {
        IResult? validationResult = ValidateRequest(request);
        if (validationResult is not null)
            return validationResult;

        var config = new VisitorPreOnboardingSagaConfig
        {
            UseCustomInviteNotification = request.UseCustomInviteNotification,
            CustomInviteNotification = request.UseCustomInviteNotification ? request.CustomInviteNotification : null,
            QrGenerationMode = request.QrGenerationMode,
            SystemId = request.QrGenerationMode == CredentialGenerationMode.AccessControlQr ? request.SystemId : null,
            BadgeTypeId = request.QrGenerationMode == CredentialGenerationMode.AccessControlQr ? request.BadgeTypeId : null,
            SendConfirmNotificationToHost = request.SendConfirmNotificationToHost,
            UseCustomConfirmNotification = request.SendConfirmNotificationToHost && request.UseCustomConfirmNotification,
            CustomConfirmNotification = request.SendConfirmNotificationToHost && request.UseCustomConfirmNotification ? request.CustomConfirmNotification : null,
            SendCancellationNotification = request.SendCancellationNotification,
            UseCustomCancellationNotification = request.SendCancellationNotification && request.UseCustomCancellationNotification,
            CustomCancellationNotification = request.SendCancellationNotification && request.UseCustomCancellationNotification ? request.CustomCancellationNotification : null,
            SendRescheduleNotification = request.SendRescheduleNotification,
            UseCustomRescheduleNotification = request.SendRescheduleNotification && request.UseCustomRescheduleNotification,
            CustomRescheduleNotification = request.SendRescheduleNotification && request.UseCustomRescheduleNotification ? request.CustomRescheduleNotification : null,
            SendRelocationNotification = request.SendRelocationNotification,
            UseCustomRelocationNotification = request.SendRelocationNotification && request.UseCustomRelocationNotification,
            CustomRelocationNotification = request.SendRelocationNotification && request.UseCustomRelocationNotification ? request.CustomRelocationNotification : null,
            SendArrivalNotificationToHost = request.SendArrivalNotificationToHost,
            UseCustomArrivalNotification = request.SendArrivalNotificationToHost && request.UseCustomArrivalNotification,
            CustomArrivalNotification = request.SendArrivalNotificationToHost && request.UseCustomArrivalNotification ? request.CustomArrivalNotification : null,
        };

        VisitorPreOnboardingSagaConfig updated = await service.UpdateConfigurationAsync(config, cancellationToken);
        return Results.Ok(updated);
    }

    private static IResult? ValidateRequest(VisitorPreOnboardingSagaConfigRequest request)
    {
        if (!IsValidCustomNotification(request.UseCustomInviteNotification, request.CustomInviteNotification))
            return ValidationProblem("Custom invitation notification requires subject and body.");

        if (request.QrGenerationMode == CredentialGenerationMode.AccessControlQr && (!request.SystemId.HasValue || !request.BadgeTypeId.HasValue))
            return ValidationProblem("Access control QR requires system and badge type.");

        if (!IsValidCustomNotification(request.SendConfirmNotificationToHost && request.UseCustomConfirmNotification, request.CustomConfirmNotification))
            return ValidationProblem("Custom confirmation notification requires subject and body.");

        if (!IsValidCustomNotification(request.SendCancellationNotification && request.UseCustomCancellationNotification, request.CustomCancellationNotification))
            return ValidationProblem("Custom cancellation notification requires subject and body.");

        if (!IsValidCustomNotification(request.SendRescheduleNotification && request.UseCustomRescheduleNotification, request.CustomRescheduleNotification))
            return ValidationProblem("Custom reschedule notification requires subject and body.");

        if (!IsValidCustomNotification(request.SendRelocationNotification && request.UseCustomRelocationNotification, request.CustomRelocationNotification))
            return ValidationProblem("Custom relocation notification requires subject and body.");

        if (!IsValidCustomNotification(request.SendArrivalNotificationToHost && request.UseCustomArrivalNotification, request.CustomArrivalNotification))
            return ValidationProblem("Custom arrival notification requires subject and body.");

        return null;
    }

    private static bool IsValidCustomNotification(bool enabled, CustomNotification? notification)
    {
        if (!enabled)
            return true;

        return notification is not null
            && !string.IsNullOrWhiteSpace(notification.Subject)
            && !string.IsNullOrWhiteSpace(notification.Body);
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid visitor pre-onboarding configuration.",
            detail: detail);

    private static async Task<IResult> GetOnboardingSagas(
        Guid visitId,
        SagasDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        List<VisitorPreOnboardingSaga> sagas = await dbContext
            .VisitorPreOnboardingSagas.AsNoTracking()
            .Where(x => x.VisitId == visitId)
            .ToListAsync(cancellationToken);

        return Results.Ok(sagas);
    }

    private static async Task<IResult> GetOnboardingSaga(
        Guid visitId,
        Guid invitationId,
        SagasDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        VisitorPreOnboardingSaga? saga = await dbContext
        .VisitorPreOnboardingSagas.AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.InvitationId == invitationId && x.VisitId == visitId,
            cancellationToken: cancellationToken
        );

        if (saga is null)
            return Results.NotFound();

        return Results.Ok(saga);
    }
}

public sealed record VisitorPreOnboardingSagaConfigRequest(
    bool UseCustomInviteNotification,
    CustomNotification? CustomInviteNotification,
    CredentialGenerationMode QrGenerationMode,
    Guid? SystemId,
    Guid? BadgeTypeId,
    bool SendConfirmNotificationToHost,
    bool UseCustomConfirmNotification,
    CustomNotification? CustomConfirmNotification,
    bool SendCancellationNotification,
    bool UseCustomCancellationNotification,
    CustomNotification? CustomCancellationNotification,
    bool SendRescheduleNotification,
    bool UseCustomRescheduleNotification,
    CustomNotification? CustomRescheduleNotification,
    bool SendRelocationNotification,
    bool UseCustomRelocationNotification,
    CustomNotification? CustomRelocationNotification,
    bool SendArrivalNotificationToHost,
    bool UseCustomArrivalNotification,
    CustomNotification? CustomArrivalNotification);
