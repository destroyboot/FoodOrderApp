import * as signalR from "@microsoft/signalr";
import { getToken } from "./auth";

export type NewOrderPayload = {
  orderId: number;
  status: string;
  orderType: string;
  tableNumber: string | null;
  total: number;
  createdAt: string;
};

export type StatusChangedPayload = {
  orderId: number;
  oldStatus: string;
  newStatus: string;
};

export async function connectOrdersHub(
  onNewOrder: (p: NewOrderPayload) => void,
  onStatusChanged: (p: StatusChangedPayload) => void
) {
  const token = getToken();
  if (!token) throw new Error("No token");

  const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders", {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build();

  conn.on("NewOrder", onNewOrder);
  conn.on("OrderStatusChanged", onStatusChanged);

  await conn.start();
  return conn;
}