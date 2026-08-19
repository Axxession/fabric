using Azure.Identity;
using Fabric.Server.Core;
using Fabric.Server.Notifications;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Fabric.Server.Tenants.Application;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Visitors.Domain;
using Fluid;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Options;

namespace Fabric.Server.Notifications.Services;

public sealed class EmailNotificationSender(
    IOptions<EmailOptions> options,
    TenantIntegrationService tenantIntegrationService,
    ILogger<EmailNotificationSender> logger)
{
    private const string GraphEndpoint = "https://graph.microsoft.com/.default";
    private static readonly FluidParser _parser = new();
    private static readonly TemplateOptions _templateOptions = CreateTemplateOptions();

    public async Task<Result<EmailErrors>> SendEmail(string subject, string body, object model, IEnumerable<string> receivers, IEnumerable<string>? carbonCopy = null, CancellationToken ct = default)
    {
        string[] cc = carbonCopy?.ToArray() ?? [];
        string[] to = receivers.ToArray();

        if (to.Length == 0)
            return Result.Failure(EmailErrors.InvalidEmail);

        if (string.IsNullOrWhiteSpace(subject))
            return Result.Failure(EmailErrors.EmptySubject);

        string renderedSubject;
        string renderedBody;

        try
        {
            renderedSubject = await RenderTemplate(subject, model);
            renderedBody = await RenderTemplate(body, model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.EmailTemplateRenderFailed(ex);
            return Result.Failure(EmailErrors.GraphFailed);
        }

        MicrosoftGraphEmailIntegrationConfig? tenantEmailConfig = await tenantIntegrationService.GetMicrosoftGraphEmailConfigAsync(ct);
        GraphEmailSettings? emailSettings = tenantEmailConfig is { IsEnabled: true }
            ? ToGraphEmailSettings(tenantEmailConfig)
            : options.Value.Graph;
        if (emailSettings is null || !emailSettings.IsConfigured())
        {
            logger.EmailNotConfigured();
            return Result.Failure(EmailErrors.NotConfigured);
        }

        try
        {
            var credential = new ClientSecretCredential(emailSettings.AzureTenantId, emailSettings.ApplicationId, emailSettings.Secret);

            var graphClient = new GraphServiceClient(credential, [GraphEndpoint]);

            var message = new Message
            {
                Subject = renderedSubject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = renderedBody },
                Sender = new Recipient
                {
                    EmailAddress = new EmailAddress { Address = emailSettings.FromEmail, Name = emailSettings.FromName },
                },
                ToRecipients = ToRecipients(to),
            };

            if (cc.Length != 0)
            {
                message.CcRecipients = ToRecipients(cc);
            }

            await graphClient
                .Users[emailSettings.FromEmail]
                .SendMail.PostAsync(
                    new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = emailSettings.SaveSentItems,
                    },
                    cancellationToken: ct
                );

            return Result.Success<EmailErrors>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.EmailSendFailed(ex, string.Join(", ", to));
            return Result.Failure(EmailErrors.GraphFailed);
        }
    }

    private static List<Recipient> ToRecipients(IEnumerable<string> addresses)
    {
        return addresses.Select(addr => new Recipient { EmailAddress = new EmailAddress { Address = addr } }).ToList();
    }

    private static GraphEmailSettings ToGraphEmailSettings(MicrosoftGraphEmailIntegrationConfig config) =>
        new()
        {
            FromEmail = config.FromEmail,
            FromName = config.FromName,
            AzureTenantId = config.AzureTenantId,
            ApplicationId = config.ApplicationId,
            Secret = config.Secret,
            SaveSentItems = config.SaveSentItems
        };

    private static async Task<string> RenderTemplate(string templateText, object model)
    {
        IFluidTemplate template = _parser.Parse(templateText);
        var context = new TemplateContext(model, _templateOptions);
        return await template.RenderAsync(context);
    }

    private static TemplateOptions CreateTemplateOptions()
    {
        var options = new TemplateOptions();

        options.MemberAccessStrategy.Register<SagaNotificationModel>();
        options.MemberAccessStrategy.Register<VisitNotificationModel>();
        options.MemberAccessStrategy.Register<LocationNotificationModel>();
        options.MemberAccessStrategy.Register<LocationPartNotificationModel>();
        options.MemberAccessStrategy.Register<VisitInvitation>();

        return options;
    }
}


public enum EmailErrors
{
    NotConfigured,
    InvalidEmail,
    EmptySubject,
    GraphFailed
}

internal static partial class EmailNotificationSenderLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Email notification skipped because Graph email is not configured.")]
    public static partial void EmailNotConfigured(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send email to {Receivers} via Graph API.")]
    public static partial void EmailSendFailed(this ILogger logger, Exception exception, string receivers);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to render email template.")]
    public static partial void EmailTemplateRenderFailed(this ILogger logger, Exception exception);
}
