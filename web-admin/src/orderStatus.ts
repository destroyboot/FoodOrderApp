export const OrderStatus = {
  Draft: 0,
  Pending: 1,
  Accepted: 2,
  Preparing: 3,
  Ready: 4,
  Completed: 5,
  Cancelled: 6,
  SentToKitchen: 7,
  ReadyForWaiter: 8,
  Delivered: 9,
  OutForDelivery: 10,
} as const;

export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

type Translate = (key: string, fallback: string) => string;

const statusKeys: Record<number, [string, string]> = {
  [OrderStatus.Draft]: ["orders.status.draft", "Draft"],
  [OrderStatus.Pending]: ["orders.status.pending", "Pending"],
  [OrderStatus.Accepted]: ["orders.status.accepted", "Accepted"],
  [OrderStatus.Preparing]: ["orders.status.preparing", "Preparing"],
  [OrderStatus.Ready]: ["orders.status.ready", "Ready"],
  [OrderStatus.Completed]: ["orders.status.completed", "Completed"],
  [OrderStatus.Cancelled]: ["orders.status.cancelled", "Cancelled"],
  [OrderStatus.SentToKitchen]: ["orders.status.sentToKitchen", "Sent to kitchen"],
  [OrderStatus.ReadyForWaiter]: ["orders.status.readyForWaiter", "Ready for waiter"],
  [OrderStatus.Delivered]: ["orders.status.delivered", "Delivered"],
  [OrderStatus.OutForDelivery]: ["orders.status.outForDelivery", "Out for delivery"],
};

export function orderStatusLabel(s: number | string, t?: Translate) {
  if (typeof s === "string") return s;
  const value = statusKeys[s];
  return value ? (t ? t(value[0], value[1]) : value[1]) : String(s);
}

export function orderStatusDisplayLabel(status: number | string, orderType?: number | string, t?: Translate) {
  const numericStatus = Number(status);
  const numericOrderType = orderType === undefined ? null : Number(orderType);

  if (numericOrderType === 1) {
    if (numericStatus === OrderStatus.Ready) return t ? t("orders.status.readyForPickup", "Ready for pickup") : "Ready for pickup";
    if (numericStatus === OrderStatus.Delivered) return t ? t("orders.status.pickedUp", "Picked up") : "Picked up";
  }

  if (numericOrderType === 2) {
    if (numericStatus === OrderStatus.Ready) return t ? t("orders.status.readyForDispatch", "Ready for dispatch") : "Ready for dispatch";
  }

  return orderStatusLabel(status, t);
}
