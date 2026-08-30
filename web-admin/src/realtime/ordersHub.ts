import * as signalR from "@microsoft/signalr";
import { getToken } from "../auth";

export type NewOrderPayload = {
  orderId: number;
  status: number | string;
  orderType: number | string;
  tableNumber: string | null;
  total: number;
  createdAt: string;
};

export type StatusChangedPayload = {
  orderId: number;
  oldStatus: number | string;
  newStatus: number | string;
};

export async function startOrdersHub(
  handlers: {
    onNewOrder: (p: NewOrderPayload) => void;
    onOrderStatusChanged: (p: StatusChangedPayload) => void;
    onConnected?: () => void;
    onDisconnected?: () => void;
    onError?: (err: unknown) => void;
  }
) {
  const token = getToken();
  if (!token) throw new Error("Missing token.");

  const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders", {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build();

  conn.on("NewOrder", handlers.onNewOrder);
  conn.on("OrderStatusChanged", handlers.onOrderStatusChanged);

  conn.onreconnected(() => handlers.onConnected?.());
  conn.onclose(() => handlers.onDisconnected?.());

  try {
    await conn.start();
    handlers.onConnected?.();
  } catch (err) {
    handlers.onError?.(err);
    throw err;
  }

  return conn;
}
