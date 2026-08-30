import { useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { api } from "../api";
import { getToken, getUserRoles } from "../auth";
import { useI18n } from "../i18n";
import { OrderStatus } from "../orderStatus";
import { normalizeOrderId } from "../realtime/normalizeOrderStatus";
import { startOrdersHub } from "../realtime/ordersHub";
import type { StatusChangedPayload } from "../realtime/ordersHub";
import { BillingPanel } from "./orderDetails/BillingPanel";
import { OrderActionsPanel } from "./orderDetails/OrderActionsPanel";
import { OrderInfoPanel } from "./orderDetails/OrderInfoPanel";
import { OrderItemsTable } from "./orderDetails/OrderItemsTable";
import type { DeliveryDriverOptionDto, OrderDetailsDto } from "./orderDetails/types";

function getStatusActions(orderType: number, roles: string[], t: (key: string, fallback: string) => string) {
  const isChefOnly = roles.includes("Chef")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Waiter")
    && !roles.includes("DeliveryDriver");
  const isWaiterOnly = roles.includes("Waiter")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Chef")
    && !roles.includes("DeliveryDriver");

  if (isChefOnly) {
    return [
      { status: OrderStatus.Preparing, label: t("orders.status.preparing", "Preparing") },
      { status: OrderStatus.ReadyForWaiter, label: t("orders.status.readyForWaiter", "Ready for waiter") },
    ];
  }

  if (isWaiterOnly) {
    if (orderType === 1) {
      return [
        { status: OrderStatus.Accepted, label: t("orders.action.accept", "Accept") },
        { status: OrderStatus.SentToKitchen, label: t("orders.action.sendToKitchen", "Send to kitchen") },
        { status: OrderStatus.Ready, label: t("orders.status.readyForPickup", "Ready for pickup") },
        { status: OrderStatus.Completed, label: t("orders.status.completed", "Completed") },
        { status: OrderStatus.Cancelled, label: t("common.cancel", "Cancel") },
      ];
    }

    return [
      { status: OrderStatus.Accepted, label: t("orders.action.accept", "Accept") },
      { status: OrderStatus.SentToKitchen, label: t("orders.action.sendToKitchen", "Send to kitchen") },
      { status: OrderStatus.Ready, label: orderType === 2 ? t("orders.status.readyForDispatch", "Ready for dispatch") : t("orders.status.ready", "Ready") },
      { status: OrderStatus.Completed, label: t("orders.status.completed", "Completed") },
      { status: OrderStatus.Cancelled, label: t("common.cancel", "Cancel") },
    ];
  }

  if (orderType === 1) {
    return [
      { status: OrderStatus.Accepted, label: t("orders.action.accept", "Accept") },
      { status: OrderStatus.SentToKitchen, label: t("orders.action.sendToKitchen", "Send to kitchen") },
      { status: OrderStatus.Preparing, label: t("orders.status.preparing", "Preparing") },
      { status: OrderStatus.Ready, label: t("orders.status.readyForPickup", "Ready for pickup") },
      { status: OrderStatus.Completed, label: t("orders.status.completed", "Completed") },
      { status: OrderStatus.Cancelled, label: t("common.cancel", "Cancel") },
    ];
  }

  if (orderType === 2) {
    return [
      { status: OrderStatus.Accepted, label: t("orders.action.accept", "Accept") },
      { status: OrderStatus.SentToKitchen, label: t("orders.action.sendToKitchen", "Send to kitchen") },
      { status: OrderStatus.Preparing, label: t("orders.status.preparing", "Preparing") },
      { status: OrderStatus.Ready, label: t("orders.status.readyForDispatch", "Ready for dispatch") },
      { status: OrderStatus.OutForDelivery, label: t("orders.status.outForDelivery", "Out for delivery") },
      { status: OrderStatus.Delivered, label: t("orders.status.delivered", "Delivered") },
      { status: OrderStatus.Completed, label: t("orders.status.completed", "Completed") },
      { status: OrderStatus.Cancelled, label: t("common.cancel", "Cancel") },
    ];
  }

  return [
    { status: OrderStatus.Accepted, label: t("orders.action.accept", "Accept") },
    { status: OrderStatus.SentToKitchen, label: t("orders.action.sendToKitchen", "Send to kitchen") },
    { status: OrderStatus.Preparing, label: t("orders.status.preparing", "Preparing") },
    { status: OrderStatus.Ready, label: t("orders.status.ready", "Ready") },
    { status: OrderStatus.ReadyForWaiter, label: t("orders.status.readyForWaiter", "Ready for waiter") },
    { status: OrderStatus.Delivered, label: t("orders.status.delivered", "Delivered") },
    { status: OrderStatus.Completed, label: t("orders.status.completed", "Completed") },
    { status: OrderStatus.Cancelled, label: t("common.cancel", "Cancel") },
  ];
}

export default function OrderDetails() {
  const { t } = useI18n();
  const roles = getUserRoles();
  const isDriverOnly = roles.includes("DeliveryDriver")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Waiter")
    && !roles.includes("Chef");
  const isChefOnly = roles.includes("Chef")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Waiter")
    && !roles.includes("DeliveryDriver");
  const location = useLocation();
  const isHistoryView = Boolean((location.state as { historyView?: boolean } | null)?.historyView);
  const { id } = useParams();
  const nav = useNavigate();
  const [data, setData] = useState<OrderDetailsDto | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [drivers, setDrivers] = useState<DeliveryDriverOptionDto[]>([]);
  const [assigningDriver, setAssigningDriver] = useState(false);
  const orderId = Number(id);
  const connRef = useRef<HubConnection | null>(null);
  const [rtState, setRtState] = useState(t("orders.realtime.connecting", "connecting..."));

  async function load() {
    setErr(null);
    const dto = await api<OrderDetailsDto>(`/api/admin/orders/${orderId}`);
    setData(dto);
    if (Number(dto.orderType) === 2) {
      const driverOptions = await api<DeliveryDriverOptionDto[]>(`/api/admin/orders/${orderId}/delivery-drivers`);
      setDrivers(driverOptions);
    } else {
      setDrivers([]);
    }
  }

  useEffect(() => {
    let mounted = true;

    async function init() {
      await load();

      const conn = await startOrdersHub({
        onNewOrder: () => {},
        onOrderStatusChanged: (p: StatusChangedPayload) => {
          const oid = normalizeOrderId((p as any).orderId);
          if (oid === orderId) {
            void load();
          }
        },
        onConnected: () => setRtState(t("orders.realtime.connected", "connected")),
        onDisconnected: () => setRtState(t("orders.realtime.disconnected", "disconnected")),
        onError: () => setRtState(t("orders.realtime.error", "error")),
      });

      if (!mounted) {
        void conn.stop();
        return;
      }

      connRef.current = conn;
    }

    init().catch((e: any) => {
      if (!mounted) return;
      setErr(e.message || t("orders.realtime.failed", "Realtime failed"));
    });

    return () => {
      mounted = false;
      connRef.current?.stop();
      connRef.current = null;
    };
  }, [orderId]);

  async function changeStatus(newStatus: OrderStatus) {
    const nextLabel = getStatusActions(Number(data?.orderType ?? 0), roles, t).find((action) => action.status === newStatus)?.label ?? t("orders.action.changeStatus", "change status");
    if (!window.confirm(t("orders.confirmStatusChange", `Are you sure you want to ${nextLabel.toLowerCase()} for order #${orderId}?`))) {
      return;
    }

    setErr(null);
    try {
      await api<void>(`/api/admin/orders/${orderId}/status?newStatus=${newStatus}`, {
        method: "PATCH",
      });
      if (newStatus === OrderStatus.Cancelled) {
        nav("/orders");
        return;
      }

      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.statusChangeFailed", "Failed to change status"));
    }
  }

  async function assignDriver(userId: string) {
    setErr(null);
    setAssigningDriver(true);
    try {
      const query = userId ? `?userId=${encodeURIComponent(userId)}` : "";
      await api<void>(`/api/admin/orders/${orderId}/assign-delivery-driver${query}`, {
        method: "PATCH",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.assignDriverFailed", "Failed to assign delivery driver"));
    } finally {
      setAssigningDriver(false);
    }
  }

  async function collectPaymentAndComplete() {
    if (!window.confirm(t("orders.confirmMarkPaidAndComplete", `Are you sure you want to mark order #${orderId} as paid and completed?`))) {
      return;
    }

    setErr(null);
    try {
      await api<void>(`/api/admin/orders/${orderId}/collect-payment-and-complete`, {
        method: "PATCH",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.collectPaymentFailed", "Failed to collect payment and complete order"));
    }
  }

  async function markPaid() {
    if (!window.confirm(t("orders.confirmMarkPaid", `Are you sure you want to mark order #${orderId} as paid?`))) {
      return;
    }

    setErr(null);
    try {
      await api<void>(`/api/admin/orders/${orderId}/mark-paid`, {
        method: "PATCH",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.markPaidFailed", "Failed to mark order as paid"));
    }
  }

  async function updateInvoiceStatus(status: number) {
    setErr(null);
    try {
      await api<void>(`/api/admin/orders/${orderId}/invoice-status?status=${status}`, {
        method: "PATCH",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.invoiceStatusUpdateFailed", "Failed to update invoice status"));
    }
  }

  async function sendSummary() {
    setErr(null);
    try {
      await api<{ sentTo: string }>(`/api/admin/orders/${orderId}/send-receipt`, {
        method: "POST",
      });
      await load();
    } catch (e: any) {
      setErr(e.message || t("orders.sendReceiptFailed", "Failed to send receipt"));
    }
  }

  async function downloadInvoice() {
    try {
      const token = getToken();
      const res = await fetch(`/api/admin/orders/${orderId}/invoice-pdf`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${data?.invoiceNumber ?? `invoice-${orderId}`}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (e: any) {
      setErr(e.message || t("orders.downloadInvoiceFailed", "Failed to download invoice"));
    }
  }

  async function downloadSummaryPdf() {
    try {
      const token = getToken();
      const res = await fetch(`/api/admin/orders/${orderId}/summary-pdf`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }

      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `order-summary-${data?.displayOrderNumber ?? orderId}.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (e: any) {
      setErr(e.message || t("orders.downloadSummaryFailed", "Failed to download order summary"));
    }
  }

  const invoiceRequested = (data?.billingDetails?.invoiceStatus ?? 0) !== 0 || !!data?.hasInvoiceDocument;

  if (!Number.isFinite(orderId)) {
    return <div className="page-stack">{t("common.invalidId", "Invalid id.")}</div>;
  }

  return (
    <div className="page-stack">
      <button onClick={() => nav(isHistoryView ? "/orders/history" : "/orders")}>{t("common.back", "Back")}</button>
      <h2>{t("orders.order", "Order")} #{data?.displayOrderNumber ?? orderId}</h2>
      {err && <div className="alert-error">{err}</div>}
      {!data ? (
        <div>{t("common.loading", "Loading...")}</div>
      ) : (
        <>
          <OrderInfoPanel data={data} realtimeState={rtState} t={t} />

          <OrderActionsPanel
            data={data}
            statusActions={getStatusActions(Number(data.orderType), roles, t)}
            isHistoryView={isHistoryView}
            isDriverOnly={isDriverOnly}
            drivers={drivers}
            assigningDriver={assigningDriver}
            onChangeStatus={(status) => void changeStatus(status)}
            onCollectPaymentAndComplete={() => void collectPaymentAndComplete()}
            onAssignDriver={(userId) => void assignDriver(userId)}
            t={t}
          />

          <OrderItemsTable data={data} t={t} />

          {!isDriverOnly && !isChefOnly ? (
            <BillingPanel
              data={data}
              invoiceRequested={invoiceRequested}
              isHistoryView={isHistoryView}
              onMarkPaid={() => void markPaid()}
              onSendSummary={() => void sendSummary()}
              onDownloadSummaryPdf={() => void downloadSummaryPdf()}
              onUpdateInvoiceStatus={(status) => void updateInvoiceStatus(status)}
              onDownloadInvoice={() => void downloadInvoice()}
              t={t}
            />
          ) : null}
        </>
      )}
    </div>
  );
}
