import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import type { HubConnection } from "@microsoft/signalr";
import { api } from "../api";
import { getUserRoles } from "../auth";
import { orderStatusDisplayLabel } from "../orderStatus";
import { orderTypeAccent, orderTypeDescription, orderTypeLabel, orderTypeNumber } from "../orderPresentation";
import { startOrdersHub } from "../realtime/ordersHub";
import { normalizeOrderId, normalizeOrderStatus } from "../realtime/normalizeOrderStatus";
import { matchesTokenizedSearch } from "../tokenSearch";
import { useI18n } from "../i18n";

type OrderRow = {
  id: number;
  status: number;
  orderType: number;
  customerUserId: string | null;
  customerEmail: string | null;
  isAnonymousCustomer: boolean;
  tableNumber: string | null;
  pickupContactName: string | null;
  pickupPhone: string | null;
  deliveryContactName: string | null;
  deliveryPhone: string | null;
  deliveryAddressLine1: string | null;
  deliveryCity: string | null;
  assignedDeliveryDriverName: string | null;
  scheduledFor: string | null;
  total: number;
  createdAt: string;
};

export default function ActiveOrders() {
  const { t } = useI18n();
  const nav = useNavigate();
  const roles = getUserRoles();
  const isChefOnly = roles.includes("Chef")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Waiter")
    && !roles.includes("DeliveryDriver");
  const isDriverOnly = roles.includes("DeliveryDriver")
    && !roles.includes("Admin")
    && !roles.includes("RestaurantAdmin")
    && !roles.includes("Waiter")
    && !roles.includes("Chef");
  const [orders, setOrders] = useState<OrderRow[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [searchText, setSearchText] = useState("");
  const [typeFilter, setTypeFilter] = useState<"all" | number>("all");

  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    let mounted = true;
    let refreshHandle: number | undefined;

    async function init() {
      setErr(null);

      const loadOrders = async () => {
        const data = await api<OrderRow[]>("/api/admin/orders/active");
        if (mounted) {
          setOrders(data);
        }
      };

      await loadOrders();

      const conn = await startOrdersHub({
        onConnected: () => {},
        onDisconnected: () => {},
        onError: () => {},
        onNewOrder: (p) => {
          setOrders((prev) => {
            const id = normalizeOrderId((p as any).orderId);
            if (prev.some((x) => x.id === id)) return prev;

            const row: OrderRow = {
              id,
              status: normalizeOrderStatus((p as any).status),
              orderType: Number((p as any).orderType),
              customerUserId: null,
              customerEmail: null,
              isAnonymousCustomer: true,
              tableNumber: (p as any).tableNumber ?? null,
              pickupContactName: null,
              pickupPhone: null,
              deliveryContactName: null,
              deliveryPhone: null,
              deliveryAddressLine1: null,
              deliveryCity: null,
              assignedDeliveryDriverName: null,
              scheduledFor: null,
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

      if (!mounted) {
        void conn.stop();
        return;
      }

      connRef.current = conn;

      refreshHandle = window.setInterval(() => {
        void loadOrders().catch(() => {
        });
      }, 15000);
    }

    init().catch((e: any) => {
      if (!mounted) return;
      setErr(e.message || t("orders.active.loadFailed", "Failed to load/connect"));
    });

    return () => {
      mounted = false;
      if (refreshHandle !== undefined) {
        window.clearInterval(refreshHandle);
      }
      connRef.current?.stop();
      connRef.current = null;
    };
  }, []);

  const filteredOrders = orders.filter((order) =>
    (typeFilter === "all" || order.orderType === typeFilter) &&
    matchesTokenizedSearch(
      [
        order.id,
        orderStatusDisplayLabel(order.status, order.orderType, t),
        orderTypeLabel(order.orderType, t),
        order.isAnonymousCustomer ? t("orders.customer.anonymous", "anonymous") : t("orders.customer.user", "user"),
        order.customerEmail ?? "",
        order.tableNumber ?? "",
        order.pickupContactName ?? "",
        order.pickupPhone ?? "",
        order.deliveryContactName ?? "",
        order.deliveryPhone ?? "",
        order.deliveryAddressLine1 ?? "",
        order.deliveryCity ?? "",
        order.assignedDeliveryDriverName ?? "",
        order.scheduledFor ? new Date(order.scheduledFor).toLocaleString() : "",
        order.total.toFixed(2),
        new Date(order.createdAt).toLocaleString(),
      ].join(" "),
      searchText
    )
  );

  const summaryOrders = orders.filter((order) =>
    matchesTokenizedSearch(
      [order.id, order.customerEmail ?? "", order.tableNumber ?? "", order.pickupContactName ?? "", order.deliveryContactName ?? "", order.deliveryCity ?? ""].join(" "),
      searchText
    )
  );

  const groups = [
    { orderType: 0, label: orderTypeLabel(0, t), description: orderTypeDescription(0, t), orders: filteredOrders.filter((order) => orderTypeNumber(order.orderType) === 0) },
    { orderType: 1, label: orderTypeLabel(1, t), description: orderTypeDescription(1, t), orders: filteredOrders.filter((order) => orderTypeNumber(order.orderType) === 1) },
    { orderType: 2, label: orderTypeLabel(2, t), description: orderTypeDescription(2, t), orders: filteredOrders.filter((order) => orderTypeNumber(order.orderType) === 2) },
  ];

  const groupedOrders = groups.filter((group) => {
    if (isDriverOnly && group.orderType !== 2) {
      return false;
    }

    if (typeFilter !== "all" && group.orderType !== typeFilter) {
      return false;
    }

    return true;
  });

  const filterButtons: Array<{ value: "all" | number; label: string }> = [
    { value: "all", label: t("common.all", "All") },
    { value: 0, label: t("mobile.restaurants.toTable", "To table") },
    { value: 1, label: t("mobile.restaurants.pickup", "Pick up") },
    { value: 2, label: t("mobile.restaurants.delivery", "Delivery") },
  ];
  const summaryCards = [
    {
      key: "total",
      label: t("orders.summary.totalActive", "Total active"),
      value: summaryOrders.length,
      tone: "#111111",
      background: "#ffffff",
      border: "#eceef3",
    },
    {
      key: "table",
      label: t("mobile.restaurants.toTable", "To table"),
      value: summaryOrders.filter((order) => orderTypeNumber(order.orderType) === 0).length,
      tone: "#9a3412",
      background: "#fff7ed",
      border: "#fed7aa",
    },
    {
      key: "pickup",
      label: t("mobile.restaurants.pickup", "Pick up"),
      value: summaryOrders.filter((order) => orderTypeNumber(order.orderType) === 1).length,
      tone: "#92400e",
      background: "#fffbeb",
      border: "#fde68a",
    },
    {
      key: "delivery",
      label: t("mobile.restaurants.delivery", "Delivery"),
      value: summaryOrders.filter((order) => orderTypeNumber(order.orderType) === 2).length,
      tone: "#1d4ed8",
      background: "#eff6ff",
      border: "#bfdbfe",
    },
  ];

  function renderDetails(order: OrderRow) {
    if (orderTypeNumber(order.orderType) === 0) {
      return order.tableNumber ?? "-";
    }

    if (orderTypeNumber(order.orderType) === 1) {
      return (
        <div className="stack-xs">
          <span>{[order.pickupContactName, order.pickupPhone].filter(Boolean).join(" / ") || "-"}</span>
          {order.scheduledFor ? (
            <span className="muted-small">
              {t("orders.requested", "Requested")}: {new Date(order.scheduledFor).toLocaleString()}
            </span>
          ) : null}
        </div>
      );
    }

    return (
      <div className="stack-xs">
        <span>{[order.deliveryContactName, order.deliveryPhone].filter(Boolean).join(" / ") || "-"}</span>
        <span className="muted-small">
          {[order.deliveryAddressLine1, order.deliveryCity].filter(Boolean).join(", ") || t("orders.addressPending", "Address pending")}
        </span>
        {order.assignedDeliveryDriverName ? (
          <span className="muted-small">{t("orders.driver", "Driver")}: {order.assignedDeliveryDriverName}</span>
        ) : null}
      </div>
    );
  }

  function kitchenIndicator(order: OrderRow) {
    if (order.status === 7 || order.status === 3) {
      return <span style={{ color: "#92400e", fontSize: 12, fontWeight: 700 }}>{t("orders.inKitchen", "In kitchen")}</span>;
    }

    if (order.status === 8) {
      return <span style={{ color: "#065f46", fontSize: 12, fontWeight: 700 }}>{t("orders.readyForWaiter", "Ready for waiter")}</span>;
    }

    return null;
  }

  return (
    <div className="page-stack page-stack-wide">
      <section className="surface-panel">
        <div className="cluster cluster-between">
          <div className="stack-sm">
            <h2 className="page-title">
              {isDriverOnly ? t("nav.myDeliveries", "My Deliveries") : t("nav.activeOrders", "Active Orders")}
            </h2>
            <span className="muted">
              {t("orders.active.subtitle", "Keep the floor moving with one live view for table service, pickup, and delivery.")}
            </span>
          </div>
        </div>

        <div className="summary-grid">
          {summaryCards
            .filter((card) => !isDriverOnly || card.key === "total" || card.key === "delivery")
            .map((card) => (
              <button
                key={card.key}
                type="button"
                onClick={() => setTypeFilter(card.key === "total" ? "all" : card.key === "table" ? 0 : card.key === "pickup" ? 1 : 2)}
                className="summary-card"
                style={{
                  border: `1px solid ${card.border}`,
                  background: card.background,
                }}
              >
                <span className="summary-card-label">{card.label}</span>
                <strong className="summary-card-value" style={{ color: card.tone }}>{card.value}</strong>
              </button>
            ))}
        </div>
      </section>

      {err && (
        <div className="alert-error">
          {err}
        </div>
      )}

      <section className="surface-panel surface-panel-tight">
        <div className="cluster">
          <div className="filter-search">
            <input
              placeholder={t("orders.search", "Search orders")}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          </div>
          <div className="cluster-sm">
            {filterButtons
              .filter((button) => !isDriverOnly || button.value === "all" || button.value === 2)
              .map((button) => {
                const active = typeFilter === button.value;
                return (
                  <button
                    key={String(button.value)}
                    onClick={() => setTypeFilter(button.value)}
                    className={`filter-button${active ? " filter-button-active" : ""}`}
                  >
                    {button.label}
                  </button>
                );
              })}
          </div>
        </div>
      </section>

      <div className="stack-md">
        {groupedOrders.map((group) => {
          const accent = orderTypeAccent(group.orderType);
          return (
            <section
              key={group.orderType}
              className="surface-panel surface-panel-framed"
            >
              <div className="panel-header">
                <div className="stack-xs">
                  <div className="cluster-sm">
                    <span
                      className="status-chip"
                      style={{
                        background: accent.bg,
                        color: accent.fg,
                        border: `1px solid ${accent.border}`,
                      }}
                    >
                      {group.label}
                    </span>
                    <strong>{group.orders.length} {t("orders.activeCount", "active")}</strong>
                  </div>
                  <span className="muted">{group.description}</span>
                </div>
              </div>

              {group.orders.length === 0 ? (
                <div className="empty-state">{t("orders.active.noneForGroup", "No active orders in this section.")}</div>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th align="left">ID</th>
                      <th align="left">{t("orders.customer", "Customer")}</th>
                      <th align="left">{t("orders.status", "Status")}</th>
                      <th align="left">{t("orders.details", "Details")}</th>
                      <th align="right">{t("orders.total", "Total")}</th>
                      <th align="left">{t("orders.created", "Created")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {group.orders.map((order) => (
                      <tr
                        key={order.id}
                        className="clickable-row"
                        onClick={() => nav(`/orders/${order.id}`)}
                      >
                        <td>
                          <Link to={`/orders/${order.id}`} onClick={(e) => e.stopPropagation()}>{order.id}</Link>
                        </td>
                        <td>
                          {order.isAnonymousCustomer || !order.customerUserId ? (
                            <span>{t("orders.customer.anonymousDisplay", "Anonymous")}</span>
                          ) : (
                            <Link to={`/users?edit=${encodeURIComponent(order.customerUserId)}`} onClick={(e) => e.stopPropagation()}>
                              {order.customerEmail ?? t("orders.customer.userDisplay", "User")}
                            </Link>
                          )}
                        </td>
                        <td>
                          <div className="stack-xs">
                            <span>{orderStatusDisplayLabel(order.status, order.orderType, t)}</span>
                            {!isChefOnly ? kitchenIndicator(order) : null}
                          </div>
                        </td>
                        <td>{renderDetails(order)}</td>
                        <td align="right">{order.total.toFixed(2)}</td>
                        <td>{new Date(order.createdAt).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          );
        })}
      </div>
    </div>
  );
}
