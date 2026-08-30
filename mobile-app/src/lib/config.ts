import Constants from "expo-constants";
import { Platform } from "react-native";

const configuredApiBaseUrl = process.env.EXPO_PUBLIC_API_BASE_URL?.trim() || "";

function tryParseUrl(value: string) {
  try {
    return new URL(value);
  } catch {
    return null;
  }
}

function extractExpoHost() {
  const candidates = [
    (Constants.expoConfig as any)?.hostUri,
    (Constants.platform as any)?.hostUri,
    (Constants.expoGoConfig as any)?.debuggerHost,
    Constants.linkingUri,
    Constants.experienceUrl,
  ];

  for (const candidate of candidates) {
    if (typeof candidate !== "string" || candidate.trim().length === 0) {
      continue;
    }

    const trimmed = candidate.trim();

    if (trimmed.includes("://")) {
      const parsed = tryParseUrl(trimmed);
      if (parsed?.hostname) {
        return parsed.hostname;
      }
    }

    const withoutPath = trimmed.split("/")[0];
    const host = withoutPath.split(":")[0];
    if (host) {
      return host;
    }
  }

  return null;
}

function buildAndroidApiBaseUrl() {
  const parsedConfigured = tryParseUrl(configuredApiBaseUrl);

  if (parsedConfigured) {
    if (
      parsedConfigured.hostname === "localhost" ||
      parsedConfigured.hostname === "127.0.0.1"
    ) {
      parsedConfigured.hostname = "10.0.2.2";
    }

    if (parsedConfigured.protocol === "https:") {
      parsedConfigured.protocol = "http:";
      parsedConfigured.port = "5271";
    }

    return parsedConfigured.toString().replace(/\/$/, "");
  }

  return "http://10.0.2.2:5271";
}

function getDefaultApiBaseUrl() {
  if (Platform.OS === "web") {
    return configuredApiBaseUrl || "https://localhost:7234";
  }

  if (Platform.OS === "android") {
    return buildAndroidApiBaseUrl();
  }

  if (configuredApiBaseUrl) {
    return configuredApiBaseUrl;
  }

  const expoHost = extractExpoHost();
  if (expoHost && expoHost !== "localhost" && expoHost !== "127.0.0.1") {
    return `http://${expoHost}:5271`;
  }

  return "https://localhost:7234";
}

export const API_BASE_URL = getDefaultApiBaseUrl();

export function resolveApiUrl(path?: string | null) {
  if (!path) {
    return "";
  }

  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }

  return `${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`;
}
