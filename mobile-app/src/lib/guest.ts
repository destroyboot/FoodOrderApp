export function createGuestToken() {
  const random = Math.random().toString(36).slice(2, 12);
  return `guest-${Date.now()}-${random}`;
}
