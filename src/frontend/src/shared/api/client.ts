import createClient from 'openapi-fetch';
import type { Middleware } from 'openapi-fetch';
import type { paths } from './generated/schema';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

let accessToken: string | undefined;

const authMiddleware: Middleware = {
  onRequest({ request }) {
    if (!accessToken) {
      return undefined;
    }

    request.headers.set('Authorization', `Bearer ${accessToken}`);
    return request;
  },
};

export const api = createClient<paths>({
  baseUrl: apiBaseUrl,
});

api.use(authMiddleware);

export function setAccessToken(token: string | undefined) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}
