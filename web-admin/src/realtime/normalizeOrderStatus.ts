import { OrderStatus } from "../orderStatus";

export function normalizeOrderId(v: unknown): number {
  const n = typeof v === "number" ? v : Number(v);
  return Number.isFinite(n) ? n : -1;
}

export function normalizeOrderStatus(v: unknown): number {

  if (typeof v === "number") return v;

  if (typeof v === "string") {
    const s = v.trim();

    const n = Number(s);
    if (Number.isFinite(n)) return n;

    const mapped = (OrderStatus as any)[s];
    if (typeof mapped === "number") return mapped;
  }

  return -1;
}