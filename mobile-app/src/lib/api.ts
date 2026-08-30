import { API_BASE_URL } from "./config";

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  token?: string | null;
  guestToken?: string | null;
};

export async function apiRequest<T>(path: string, options: RequestOptions = {}) {
  if (!API_BASE_URL) {
    throw new Error("API base URL is not configured for this device. Start Expo with EXPO_PUBLIC_API_BASE_URL set to your backend address.");
  }

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  if (options.guestToken) {
    headers["X-Guest-Token"] = options.guestToken;
  }

  const url = `${API_BASE_URL}${path}`;
  const controller = new AbortController();
  const timeoutHandle = setTimeout(() => controller.abort(), 12000);

  let response: Response;
  try {
    response = await fetch(url, {
      method: options.method ?? "GET",
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: controller.signal,
    });
  } catch (error: any) {
    clearTimeout(timeoutHandle);
    if (error?.name === "AbortError") {
      throw new Error(`Request timed out for ${url}. Check that the API is running and reachable from this device.`);
    }
    throw new Error(`Network request failed for ${url}: ${error?.message || "unknown error"}`);
  }
  clearTimeout(timeoutHandle);

  if (!response.ok) {
    const text = await response.text();
    try {
      const payload = JSON.parse(text) as { error?: string; message?: string; title?: string };
      throw new Error(payload.error || payload.message || payload.title || `Request failed for ${url}: ${response.status}`);
    } catch (error: any) {
      if (error instanceof SyntaxError) {
        throw new Error(text || `Request failed for ${url}: ${response.status}`);
      }
      throw error;
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
