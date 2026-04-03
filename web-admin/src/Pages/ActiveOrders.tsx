import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import type { HubConnection } from "@microsoft/signalr";
import { api } from "../api";
import { orderStatusLabel } from "../orderStatus";
import { startOrdersHub} from "../realtime/ordersHub";
import type { NewOrderPayload, StatusChangedPayload } from "../realtime/ordersHub";
import { normalizeOrderId, normalizeOrderStatus } from "../realtime/normalizeOrderStatus";

type OrderRow = {
  id: number;
  status: number;
  orderType: number;
  tableNumber: string | null;
  total: number;
  createdAt: string;
};

export default function ActiveOrders() {
  const [orders, setOrders] = useState<OrderRow[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [rt, setRt] = useState<string>("connecting…");

  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    let mounted = true;

    async function init() {
      setErr(null);

      // 1) load current active orders
      const data = await api<OrderRow[]>("/api/admin/orders/active");
      if (!mounted) return;
      setOrders(data);

      // 2) connect realtime
      connRef.current = await startOrdersHub({
        onConnected: () => mounted && setRt("connected"),
        onDisconnected: () => mounted && setRt("disconnected"),
        onError: (e) => mounted && setRt("error"),
        onNewOrder: (p) => {
          setOrders((prev) => {
            const id = normalizeOrderId((p as any).orderId);
            if (prev.some((x) => x.id === id)) return prev;
        
            const row: OrderRow = {
              id,
              status: normalizeOrderStatus((p as any).status),
              orderType: Number((p as any).orderType),
              tableNumber: (p as any).tableNumber ?? null,
              total: Number((p as any).total),
              createdAt: (p as any).createdAt,
            };
        
            return [row, ...prev];
          });
        },
        
        onOrderStatusChanged: (p) => {
          const id = normalizeOrderId((p as any).orderId);
          const ns = normalizeOrderStatus((p as any).newStatus);
        
          setOrders((prev) => prev.map((o) => (o.id === id ? { ...o, status: ns } : o)));
        },
      });
    }

    init().catch((e: any) => {
      if (!mounted) return;
      setErr(e.message || "Failed to load/connect");
      setRt("error");
    });

    return () => {
      mounted = false;
      connRef.current?.stop();
      connRef.current = null;
    };
  }, []);

  return (
    <div style={{ maxWidth: 1100, margin: "0 auto" }}>
      <div style={{ display: "flex", alignItems: "baseline", gap: 12 }}>
        <h2>Active Orders</h2>
        <span style={{ color: "#666" }}>Realtime: {rt}</span>
      </div>

      {err && <div style={{ color: "crimson", marginBottom: 12 }}>{err}</div>}

      <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
        <thead>
          <tr style={{ borderBottom: "1px solid #ccc" }}>
            <th align="left">ID</th>
            <th align="left">Status</th>
            <th align="left">Type</th>
            <th align="left">Table</th>
            <th align="right">Total</th>
            <th align="left">Created</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((o) => (
            <tr key={o.id} style={{ borderBottom: "1px solid #eee" }}>
              <td>
                <Link to={`/orders/${o.id}`}>{o.id}</Link>
              </td>
              <td>{orderStatusLabel(o.status)}</td>
              <td>{String(o.orderType)}</td>
              <td>{o.tableNumber ?? "-"}</td>
              <td align="right">{o.total.toFixed(2)}</td>
              <td>{new Date(o.createdAt).toLocaleString()}</td>
            </tr>
          ))}
          {orders.length === 0 && (
            <tr>
              <td colSpan={6} style={{ padding: 16, color: "#666" }}>
                No active orders.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}