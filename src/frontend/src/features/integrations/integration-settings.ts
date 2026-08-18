import type { AdminTenantSettings, UpdateTenantSettingsRequest } from '@/shared/tenant/tenant-settings';

export const adminTenantSettingsQueryKey = ['settings', 'tenant', 'admin'] as const;

export function buildUpdateTenantSettingsRequest(
  settings: AdminTenantSettings,
  overrides: {
    readonly email?: UpdateTenantSettingsRequest['email'];
    readonly keycloak?: UpdateTenantSettingsRequest['keycloak'];
  } = {},
): UpdateTenantSettingsRequest {
  return {
    oidc: {
      metadataUrl: settings.oidc.metadataUrl,
      clientId: settings.oidc.clientId,
      requireHttpsMetadata: settings.oidc.requireHttpsMetadata,
    },
    theme: {
      backgroundColor: settings.theme.backgroundColor,
      contentColor: settings.theme.contentColor,
      primaryColor: settings.theme.primaryColor,
      textColor: settings.theme.textColor,
      textMutedColor: settings.theme.textMutedColor,
      borderColor: settings.theme.borderColor,
      hoverBlueColor: settings.theme.hoverBlueColor,
      activeBlueColor: settings.theme.activeBlueColor,
      hoverGrayColor: settings.theme.hoverGrayColor,
      errorColor: settings.theme.errorColor,
      errorBackgroundColor: settings.theme.errorBackgroundColor,
      dangerColor: settings.theme.dangerColor,
      successColor: settings.theme.successColor,
      successBackgroundColor: settings.theme.successBackgroundColor,
    },
    email: overrides.email ?? toEmailRequest(settings),
    keycloak: overrides.keycloak ?? toKeycloakRequest(settings),
  };
}

function toEmailRequest(settings: AdminTenantSettings): UpdateTenantSettingsRequest['email'] {
  if (!settings.email) {
    return null;
  }

  return {
    fromEmail: settings.email.fromEmail,
    fromName: settings.email.fromName,
    azureTenantId: settings.email.azureTenantId,
    applicationId: settings.email.applicationId,
    secret: '',
    saveSentItems: settings.email.saveSentItems,
  };
}

function toKeycloakRequest(settings: AdminTenantSettings): UpdateTenantSettingsRequest['keycloak'] {
  if (!settings.keycloak) {
    return null;
  }

  return {
    url: settings.keycloak.url,
    realm: settings.keycloak.realm,
    clientId: settings.keycloak.clientId,
    clientSecret: '',
  };
}
