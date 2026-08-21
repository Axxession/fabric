using System.Globalization;
using Microsoft.Extensions.Logging;
using Fabric.Hardware.BelgianEid;
using Fabric.Hardware.BelgianEid.Middleware;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid;

public sealed class BelgianEidReader(BelgianEidSettings settings, ILogger<BelgianEidReader> logger) : IDisposable
{
    private readonly Module _module = Module.GetInstance(settings.Pkcs11ModulePath);
    private readonly TimeSpan _readDelay = TimeSpan.FromMilliseconds(settings.ReadTimeoutMilliseconds);

    public async Task<BelgianEidIdentity> ReadAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Slot? slot = GetTokenSlot();
            if (slot?.Token is null)
            {
                await Task.Delay(_readDelay, cancellationToken);
                continue;
            }

            try
            {
                logger.BelgianEidCardDetected(slot.SlotId);
                using Session session = slot.Token.OpenSession(true);
                using var reader = new CardReader(session);

                byte[] dataFile = reader.ReadFile("DATA_FILE") ?? throw new InvalidOperationException("Belgian eID identity data file is missing.");
                byte[] signatureFile = reader.ReadFile("SIGN_DATA_FILE") ?? [];
                byte[] certificateFile = reader.ReadFile("CERT_RN_FILE") ?? [];
                byte[] pictureFile = reader.ReadFile("PHOTO_FILE") ?? [];
                var identity = new Identity(dataFile, signatureFile, certificateFile, pictureFile);

                BelgianEidIdentity result = new(
                    identity.GetTag(Identity.BelgianIdentityTags.Firstname),
                    identity.GetTag(Identity.BelgianIdentityTags.Lastname),
                    identity.GetTag(Identity.BelgianIdentityTags.NationalNumber),
                    identity.GetTag(Identity.BelgianIdentityTags.CardNumber),
                    ParseDate(identity.GetTag(Identity.BelgianIdentityTags.CardValidityStop)),
                    TryGetTag(identity, Identity.BelgianIdentityTags.Nationality),
                    TryGetTag(identity, Identity.BelgianIdentityTags.BirthLocation),
                    TryGetTag(identity, Identity.BelgianIdentityTags.BirthData));

                logger.BelgianEidCardRead(result.FirstName, result.LastName, result.DocumentNumber);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.BelgianEidReadFailed(ex);
                throw new InvalidOperationException("Failed to read Belgian eID.", ex);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("Belgian eID read was cancelled before completion.");
    }

    public async Task WaitForRemovalAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Slot? slot = GetTokenSlot();
            if (slot?.Token is null)
                return;

            await Task.Delay(_readDelay, cancellationToken);
        }
    }

    public Task<bool> VerifyPinAsync(string? pin, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (settings.BypassPinCode)
        {
            logger.BelgianEidPinBypassed();
            return Task.FromResult(true);
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            logger.BelgianEidPinRejected();
            return Task.FromResult(false);
        }

        Slot? slot = GetTokenSlot();
        if (slot?.Token is null)
            throw new InvalidOperationException("Belgian eID card is not present.");

        using Session session = slot.Token.OpenSession(false);
        try
        {
            session.Login(UserType.USER, pin);
            session.Logout();
            logger.BelgianEidPinAccepted();
            return Task.FromResult(true);
        }
        catch (TokenException ex) when (ex.ErrorCode is CKR.PIN_INCORRECT or CKR.PIN_INVALID or CKR.PIN_LEN_RANGE or CKR.PIN_EXPIRED or CKR.PIN_LOCKED or CKR.USER_PIN_NOT_INITIALIZED)
        {
            logger.BelgianEidPinRejected();
            return Task.FromResult(false);
        }
    }

    public void Dispose() => _module.Dispose();

    private Slot? GetTokenSlot()
    {
        try
        {
            return _module.GetSlotList(true).FirstOrDefault();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Belgian eID reader is not available.", ex);
        }
    }

    private static string? TryGetTag(Identity identity, Identity.BelgianIdentityTags tag)
    {
        try
        {
            return identity.GetTag(tag);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static DateOnly? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
                ? parsed
                : null;
}

internal static partial class BelgianEidLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Belgian eID card detected on slot {SlotId}")]
    public static partial void BelgianEidCardDetected(this ILogger logger, uint slotId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Belgian eID read for {FirstName} {LastName} with document {DocumentNumber}")]
    public static partial void BelgianEidCardRead(this ILogger logger, string firstName, string lastName, string documentNumber);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Belgian eID read failed")]
    public static partial void BelgianEidReadFailed(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Belgian eID PIN check bypassed")]
    public static partial void BelgianEidPinBypassed(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Belgian eID PIN accepted")]
    public static partial void BelgianEidPinAccepted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Belgian eID PIN rejected")]
    public static partial void BelgianEidPinRejected(this ILogger logger);
}
