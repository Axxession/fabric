namespace Fabric.Server.AccessControl.Domain;

public static class CredentialPacsFailureReasons
{
    public const string CredentialTechnologyNotSupported = "credential_technology_not_supported";
    public const string IdentifierNotNumericForUnipass = "identifier_not_numeric_for_unipass";
    public const string ProviderConfigurationMissing = "provider_configuration_missing";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string ProviderRejected = "provider_rejected";
    public const string PacsSubjectCreationFailed = "pacs_subject_creation_failed";
}
