import { useEffect, useState, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { api } from "../api";
import { OrderStatus, orderStatusLabel } from "../orderStatus";
import type { HubConnection } from "@microsoft/signalr";
import { startOrdersHub } from "../realtime/ordersHub";
import type { StatusChangedPayload } from "../realtime/ordersHub";
import { normalizeOrderId } from "../realtime/normalizeOrderStatus";

type OrderItem = {
  menuItemId: number;
  quantity: number;
  unitPrice: number;
  note: string | null;
};

type OrderDetailsDto = {
  id: number;
  status: string | number;
  orderType: string | number;
  tableNumber: string | null;
  subtotal: number;
  deliveryFee: number;
  total: number;
  createdAt: string;
  items: OrderItem[];
};

export default function OrderDetails() {
  const { id } = useParams();
  const nav = useNavigate();
  const [data, setData] = useState<OrderDetailsDto | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const orderId = Number(id);
  const connRef = useRef<HubConnection | null>(null);
  const [rtState, setRtState] = useState("connecting…");
  

  async function load() {
    setErr(null);
    const dto = await api<OrderDetailsDto>(`/api/admin/orders/${orderId}`);
    setData(dto);
  }

  useEffect(() => {
    let mounted = true;
  
    async function init() {
      await load();
  
      connRef.current = await startOrdersHub({
        onNewOrder: () => {}, // not needed here
        onOrderStatusChanged: (p: StatusChangedPayload) => {
          const oid = normalizeOrderId((p as any).orderId);
          if (oid === orderId) load().catch(() => {});
                  },
        onConnected: () => setRtState("connected"),
        onDisconnected: () => setRtState("disconnected"),
        onError: (e) => console.error("SignalR error", e),
      });
    }
  
    init().catch((e: any) => setErr(e.message || "Realtime failed"));
  
    return () => {
      mounted = false;
      connRef.current?.stop();
      connRef.current = null;
    };
  }, [orderId]);

  async function changeStatus(newStatus: OrderStatus) {
    setErr(null);
    try {
      await api<void>(`/api/admin/orders/${orderId}/status?newStatus=${newStatus}`, {
        method: "PATCH",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || "Failed to change status");
    }
  }

  if (!Number.isFinite(orderId)) return <div style={{ maxWidth: 1100, margin: "0 auto" }}>Invalid id.</div>;

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto" }}>
      <button onClick={() => nav("/orders")}>← Back</button>
      <h2>Order #{orderId}</h2>
      {err && <div style={{ color: "crimson" }}>{err}</div>}
      {!data ? (
        <div>Loading…</div>
      ) : (
        <>
          <div style={{ marginBottom: 12 }}>
            <div><b>Status:</b> {orderStatusLabel(data.status)}</div>
            <div><b>Type:</b> {String(data.orderType)}</div>
            <div><b>Table:</b> {data.tableNumber ?? "-"}</div>
            <div><b>Created:</b> {new Date(data.createdAt).toLocaleString()}</div>
            <div style={{ color: "#666" }}>Realtime: {rtState}</div>
          </div>

          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 16 }}>
            <button onClick={() => changeStatus(OrderStatus.Accepted)}>Accept</button>
            <button onClick={() => changeStatus(OrderStatus.Preparing)}>Preparing</button>
            <button onClick={() => changeStatus(OrderStatus.Ready)}>Ready</button>
            <button onClick={() => changeStatus(OrderStatus.Completed)}>Completed</button>
            <button onClick={() => changeStatus(OrderStatus.Cancelled)}>Cancel</button>
          </div>

          <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ borderBottom: "1px solid #ccc" }}>
                <th align="left">MenuItemId</th>
                <th align="right">Qty</th>
                <th align="right">Unit</th>
                <th align="left">Note</th>
                <th align="right">Line</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((i, idx) => (
                <tr key={idx} style={{ borderBottom: "1px solid #eee" }}>
                  <td>{i.menuItemId}</td>
                  <td align="right">{i.quantity}</td>
                  <td align="right">{Number(i.unitPrice).toFixed(2)}</td>
                  <td>{i.note ?? "-"}</td>
                  <td align="right">{(i.quantity * i.unitPrice).toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div style={{ marginTop: 16 }}>
            <div><b>Subtotal:</b> {Number(data.subtotal).toFixed(2)}</div>
            <div><b>Delivery fee:</b> {Number(data.deliveryFee).toFixed(2)}</div>
            <div><b>Total:</b> {Number(data.total).toFixed(2)}</div>
          </div>
        </>
      )}
    </div>
  );
}