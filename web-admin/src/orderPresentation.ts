export type OrderTypeValue = number | string;
type Translate = (key: string, fallback: string) => string;

export function orderTypeNumber(orderType: OrderTypeValue) {
  return Number(orderType);
}

export function orderTypeLabel(orderType: OrderTypeValue, t?: Translate) {
  switch (orderTypeNumber(orderType)) {
    case 0:
      return t ? t("orders.type.table", "Table") : "Table";
    case 1:
      return t ? t("orders.type.pickup", "Pickup") : "Pickup";
    case 2:
      return t ? t("orders.type.delivery", "Delivery") : "Delivery";
    default:
      return String(orderType);
  }
}

export function orderTypeAccent(orderType: OrderTypeValue) {
  switch (orderTypeNumber(orderType)) {
    case 0:
      return {
        bg: "#dbeafe",
        fg: "#1d4ed8",
        border: "#93c5fd",
      };
    case 1:
      return {
        bg: "#dcfce7",
        fg: "#166534",
        border: "#86efac",
      };
    case 2:
      return {
        bg: "#fef3c7",
        fg: "#92400e",
        border: "#fcd34d",
      };
    default:
      return {
        bg: "#e5e7eb",
        fg: "#374151",
        border: "#d1d5db",
      };
  }
}

export function orderTypeDescription(orderType: OrderTypeValue, t?: Translate) {
  switch (orderTypeNumber(orderType)) {
    case 0:
      return t ? t("orders.typeDescription.table", "Dine-in orders waiting on table flow.") : "Dine-in orders waiting on table flow.";
    case 1:
      return t ? t("orders.typeDescription.pickup", "Pickup orders waiting on kitchen and handoff.") : "Pickup orders waiting on kitchen and handoff.";
    case 2:
      return t ? t("orders.typeDescription.delivery", "Delivery orders waiting on prep and dispatch.") : "Delivery orders waiting on prep and dispatch.";
    default:
      return t ? t("nav.orders", "Orders") : "Orders";
  }
}
