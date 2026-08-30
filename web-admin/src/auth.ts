const KEY = "foodapp_token";
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export function getToken(): string | null {
  const token = localStorage.getItem(KEY);
  if (!token) return null;

  try {
    const [, payload] = token.split(".");
    if (!payload) {
      clearToken();
      return null;
    }

    const json = JSON.parse(decodeBase64Url(payload)) as Record<string, unknown>;
    const exp = typeof json.exp === "number" ? json.exp : null;
    if (exp && Date.now() >= exp * 1000) {
      clearToken();
      return null;
    }

    return token;
  } catch {
    clearToken();
    return null;
  }
}

export function setToken(token: string) {
  localStorage.setItem(KEY, token);
}

export function clearToken() {
  localStorage.removeItem(KEY);
}

export function getUserRoles(): string[] {
  const token = getToken();
  if (!token) return [];

  try {
    const [, payload] = token.split(".");
    if (!payload) return [];

    const json = JSON.parse(decodeBase64Url(payload)) as Record<string, unknown>;
    const rawRoles = json[ROLE_CLAIM] ?? json.role ?? json.roles;

    if (Array.isArray(rawRoles)) {
      return rawRoles.filter((value): value is string => typeof value === "string");
    }

    return typeof rawRoles === "string" ? [rawRoles] : [];
  } catch {
    return [];
  }
}

export function hasAnyRole(allowedRoles: string[]): boolean {
  if (allowedRoles.length === 0) return true;

  const roles = getUserRoles();
  return allowedRoles.some((role) => roles.includes(role));
}

export function getDefaultAuthorizedRoute(): string {
  const roles = getUserRoles();
  if (roles.length === 0 && getToken()) {
    return "/awaiting-role";
  }

  if (roles.includes("Admin")) {
    return "/restaurants";
  }

  if (roles.some((role) => ["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"].includes(role))) {
    return "/orders";
  }

  return "/cart";
}

export function getUserEmail(): string | null {
  const token = getToken();
  if (!token) return null;

  try {
    const [, payload] = token.split(".");
    if (!payload) return null;
    const json = JSON.parse(decodeBase64Url(payload)) as Record<string, unknown>;
    const email = json.email;
    return typeof email === "string" ? email : null;
  } catch {
    return null;
  }
}

export function getDisplayName(): string | null {
  const email = getUserEmail();
  if (!email) return null;
  const [name] = email.split("@");
  return name || email;
}

function decodeBase64Url(value: string): string {
  const padded = value.replace(/-/g, "+").replace(/_/g, "/");
  const normalized = padded + "=".repeat((4 - (padded.length % 4)) % 4);
  return atob(normalized);
}
