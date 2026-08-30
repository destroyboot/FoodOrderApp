function decodeBase64(input: string) {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
  let str = input.replace(/=+$/, "");
  let output = "";

  if (str.length % 4 === 1) {
    throw new Error("Invalid base64 input");
  }

  for (
    let bc = 0, bs = 0, buffer, index = 0;
    (buffer = str.charAt(index++));
    ~buffer && ((bs = bc % 4 ? bs * 64 + buffer : buffer), bc++ % 4)
      ? (output += String.fromCharCode(255 & (bs >> ((-2 * bc) & 6))))
      : 0
  ) {
    buffer = chars.indexOf(buffer);
  }

  try {
    return decodeURIComponent(
      output
        .split("")
        .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
        .join("")
    );
  } catch {
    return output;
  }
}

function getJwtPayload(token: string | null | undefined) {
  if (!token) {
    return null;
  }

  try {
    const encoded = token.split(".")[1] ?? "";
    const base64 = encoded.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    return JSON.parse(decodeBase64(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function tryGetEmailFromToken(token: string | null | undefined) {
  const payload = getJwtPayload(token);
  return typeof payload?.email === "string" ? payload.email : "";
}

export function getUsernameFromEmail(email: string | null | undefined) {
  const localPart = (email ?? "").split("@")[0]?.trim() ?? "";
  return localPart || "Account";
}

export function getDisplayUsername(token: string | null | undefined) {
  return getUsernameFromEmail(tryGetEmailFromToken(token));
}
