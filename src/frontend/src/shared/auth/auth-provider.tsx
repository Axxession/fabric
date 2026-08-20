import { AuthProvider } from 'react-oidc-context';
import { WebStorageStateStore } from 'oidc-client-ts';
import type { ReactNode } from 'react';

export type OidcClientSettings = {
  metadataUrl: string;
  clientId: string;
};

export function FabricAuthProvider({
  settings,
  callbackPath,
  postLogoutPath,
  storageKeyPrefix,
  children,
}: {
  settings: OidcClientSettings;
  callbackPath: string;
  postLogoutPath: string;
  storageKeyPrefix: string;
  children: ReactNode;
}) {
  const origin = window.location.origin;

  return (
    <AuthProvider
      authority={getAuthority(settings.metadataUrl)}
      metadataUrl={settings.metadataUrl}
      client_id={settings.clientId}
      redirect_uri={`${origin}${callbackPath}`}
      post_logout_redirect_uri={`${origin}${postLogoutPath}`}
      response_type="code"
      scope="openid profile email"
      automaticSilentRenew
      userStore={new WebStorageStateStore({ store: window.localStorage, prefix: storageKeyPrefix })}
      onSigninCallback={(user) => {
        window.history.replaceState({}, document.title, getReturnTo(user?.state));
      }}
    >
      {children}
    </AuthProvider>
  );
}

function getAuthority(metadataUrl: string): string {
  const url = new URL(metadataUrl);
  const wellKnownPath = '/.well-known/openid-configuration';

  if (url.pathname.endsWith(wellKnownPath)) {
    url.pathname = url.pathname.slice(0, -wellKnownPath.length) || '/';
    url.search = '';
    url.hash = '';
  }

  return url.toString().replace(/\/$/, '');
}

function getReturnTo(state: unknown): string {
  if (isRecord(state) && typeof state.returnTo === 'string' && state.returnTo.startsWith('/')) {
    return state.returnTo;
  }

  return '/';
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
