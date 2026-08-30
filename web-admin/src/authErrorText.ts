export function formatAuthApiError(error: unknown, t: (key: string, fallback: string) => string, fallback: string) {
  const message = error instanceof Error ? error.message : String(error ?? "");
  const normalized = message.toLowerCase();

  if (normalized === "unauthorized" || normalized.includes("invalid login")) {
    return t("auth.invalidCredentials", "Incorrect email or password.");
  }

  if (normalized.includes("password") && (
    normalized.includes("at least") || normalized.includes("uppercase") || normalized.includes("digit") || normalized.includes("non alphanumeric"))) {
    return t("auth.passwordRequirementsHint", "Password must have at least 8 characters, an uppercase letter, a digit, and a special character.");
  }

  if (normalized.includes("email") && (normalized.includes("invalid") || normalized.includes("already"))) {
    return t("validation.emailInvalid", "Enter a valid email address.");
  }
  return fallback;
}
