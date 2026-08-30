import { useEffect, useMemo, useState } from "react";
import { ScrollView, Text, TextInput, View } from "react-native";
import { PrimaryButton } from "../components/PrimaryButton";
import { SectionCard } from "../components/SectionCard";
import { useAppSession } from "../context/AppSessionContext";
import { inputStyle, sharedStyles } from "../lib/theme";
import { NotificationType } from "../types/api";
import { formatOrderStatusForDisplay, formatOrderStatusName, formatOrderType, OrderSummaryCard } from "./orders/OrderSummaryCard";

function matchesTokenizedSearch(haystack: string, search: string) {
  const normalized = search.trim().toLowerCase();
  if (!normalized) return true;
  const text = haystack.toLowerCase();
  return normalized.split(/\s+/).every((term) => text.includes(term));
}

function parseNotificationPayload(payloadJson?: string | null) {
  if (!payloadJson) {
    return null;
  }

  try {
    return JSON.parse(payloadJson) as { orderId?: number; newStatus?: string };
  } catch {
    return null;
  }
}

export function MyOrdersScreen() {
  const {
    token,
    activeOrders,
    orderHistory,
    orderDetailsById,
    notifications,
    pushStatus,
    loadMyActiveOrders,
    loadMyOrderHistory,
    loadOrderDetails,
    loadNotifications,
    t,
    currentCulture,
  } = useAppSession();
  const [expandedOrderId, setExpandedOrderId] = useState<number | null>(null);
  const [mode, setMode] = useState<"active" | "history">("active");
  const [historySearch, setHistorySearch] = useState("");

  useEffect(() => {
    if (!token) {
      return;
    }

    void Promise.all([
      loadMyActiveOrders(),
      loadMyOrderHistory(),
      loadNotifications(false),
    ]);
  }, [token]);

  const statusNotifications = useMemo(
    () => notifications.filter((item) => item.type === NotificationType.OrderStatusChanged).slice(0, 3),
    [notifications]
  );

  const filteredHistory = useMemo(
    () =>
      orderHistory.filter((order) =>
        matchesTokenizedSearch(
          [
            order.id,
            formatOrderStatusForDisplay(order, t),
            formatOrderType(order.orderType, t),
            order.tableNumber ?? "",
            order.pickupContactName ?? "",
            order.pickupPhone ?? "",
            order.deliveryContactName ?? "",
            order.deliveryPhone ?? "",
            order.deliveryAddressLine1 ?? "",
            order.deliveryCity ?? "",
            order.total,
            new Date(order.createdAt).toLocaleString(),
          ].join(" "),
          historySearch
        )
      ),
    [historySearch, orderHistory]
  );

  async function toggleOrder(orderId: number) {
    const nextExpanded = expandedOrderId === orderId ? null : orderId;
    setExpandedOrderId(nextExpanded);
    if (nextExpanded && !orderDetailsById[orderId]) {
      await loadOrderDetails(orderId);
    }
  }

  function renderNotificationBody(notification: (typeof notifications)[number]) {
    const payload = parseNotificationPayload(notification.payloadJson);
    if (notification.type !== NotificationType.OrderStatusChanged || !payload?.newStatus) {
      return notification.body;
    }

    const orderLabel = payload.orderId ? `${t("orders.order", "Order")} #${payload.orderId}` : t("orders.order", "Order");
    return `${orderLabel}: ${formatOrderStatusName(payload.newStatus, t)}`;
  }

  if (!token) {
    return (
      <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
        <Text style={sharedStyles.pageTitleCompact}>{t("nav.orders", "My orders")}</Text>
        <SectionCard>
          <View style={sharedStyles.stackMd}>
            <Text style={sharedStyles.title}>{t("mobile.reservations.signInRequired", "Sign in required")}</Text>
            <Text style={sharedStyles.mutedText}>
              {t("orders.signInHint", "Order history is available only to logged in users. Create an account or log in from the Account tab.")}
            </Text>
          </View>
        </SectionCard>
      </ScrollView>
    );
  }

  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <Text style={sharedStyles.pageTitle}>{mode === "active" ? t("nav.orders", "My orders") : t("nav.orderHistory", "Order history")}</Text>
      <Text style={sharedStyles.themeMutedText}>
        {t("orders.trackHint", "Track active orders across all restaurants and revisit your order history when you need it.")}
      </Text>

      <SectionCard>
        <View style={{ gap: 6 }}>
          <Text style={sharedStyles.sectionTitle}>{t("orders.pushStatus", "Push status")}</Text>
          <Text style={sharedStyles.bodyMuted}>{pushStatus}</Text>
        </View>
      </SectionCard>

      <View style={sharedStyles.rowTop}>
        <PrimaryButton label={t("orders.activeTab", "Active orders")} onPress={() => setMode("active")} disabled={mode === "active"} />
        <PrimaryButton label={t("nav.orderHistory", "Order History")} onPress={() => setMode("history")} disabled={mode === "history"} />
      </View>

      <PrimaryButton
        label={t("orders.refresh", "Refresh orders")}
        onPress={() =>
          Promise.all([
            loadMyActiveOrders(),
            loadMyOrderHistory(),
            loadNotifications(false),
          ]).then(() => undefined)
        }
      />

      {mode === "active" ? (
        <>
          <SectionCard>
            <View style={sharedStyles.stackMd}>
              <Text style={sharedStyles.sectionTitle}>{t("orders.activeSection", "Active")}</Text>
              {activeOrders.length === 0 ? (
                <Text style={sharedStyles.mutedText}>{t("orders.noneActive", "No active orders right now.")}</Text>
              ) : (
                activeOrders.map((order) => (
                  <OrderSummaryCard
                    key={order.id}
                    order={order}
                    details={orderDetailsById[order.id]}
                    expanded={expandedOrderId === order.id}
                    token={token}
                    currentCulture={currentCulture}
                    t={t}
                    onToggle={() => void toggleOrder(order.id)}
                  />
                ))
              )}
            </View>
          </SectionCard>

          <SectionCard>
            <View style={sharedStyles.stackMd}>
              <Text style={sharedStyles.sectionTitle}>{t("orders.recentUpdates", "Recent updates")}</Text>
              {statusNotifications.length === 0 ? (
                <Text style={sharedStyles.mutedText}>{t("orders.noUpdates", "No order updates yet.")}</Text>
              ) : (
                statusNotifications.map((notification) => (
                  <View key={notification.id} style={[sharedStyles.dividerTop, sharedStyles.stackXs]}>
                    <Text style={sharedStyles.semibold}>{notification.type === NotificationType.OrderStatusChanged ? t("orders.statusUpdated", "Order status updated") : notification.title}</Text>
                    <Text>{renderNotificationBody(notification)}</Text>
                    <Text style={sharedStyles.mutedText}>{new Date(notification.createdAt).toLocaleString(currentCulture)}</Text>
                  </View>
                ))
              )}
            </View>
          </SectionCard>
        </>
      ) : null}

      {mode === "history" ? (
        <SectionCard>
          <View style={sharedStyles.stackMd}>
            <Text style={sharedStyles.sectionTitle}>{t("orders.historySection", "History")}</Text>
            <TextInput
              placeholder={t("orders.history.search", "Search order history")}
              value={historySearch}
              onChangeText={setHistorySearch}
              style={inputStyle}
            />
            {filteredHistory.length === 0 ? (
              <Text style={sharedStyles.mutedText}>{t("orders.history.noneMatch", "No orders match this search.")}</Text>
            ) : (
              filteredHistory.map((order) => (
                <OrderSummaryCard
                  key={order.id}
                  order={order}
                  details={orderDetailsById[order.id]}
                  expanded={expandedOrderId === order.id}
                  token={token}
                  currentCulture={currentCulture}
                  t={t}
                  onToggle={() => void toggleOrder(order.id)}
                />
              ))
            )}
          </View>
        </SectionCard>
      ) : null}
    </ScrollView>
  );
}
