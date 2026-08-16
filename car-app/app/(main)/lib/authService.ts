import { useAuthStore } from '../stores/auth-store';

interface RefreshResponse {
  accessToken: string;
  refreshToken?: string;
}

export async function refreshAccessToken(): Promise<string> {
  const refreshToken = useAuthStore.getState().refreshToken;

  if (!refreshToken) {
    useAuthStore.getState().logout();
    throw new Error('No refresh token available');
  }

  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  });

  if (!response.ok) {
    useAuthStore.getState().logout();
    window.location.href = '/login';
    throw new Error('Session expired');
  }

  const data: RefreshResponse = await response.json();

  useAuthStore.getState().setTokens(data.accessToken, data.refreshToken);

  return data.accessToken;
}
