import { Platform, Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { API_BASE_URL } from "../../lib/config";
import { showMessage } from "../../lib/dialogs";
import { sharedStyles } from "../../lib/theme";
import { OrderDetails, OrderStatus, OrderSummary, OrderType } from "../../types/api";

export type TFunction = (key: string, fallback: string) => string;

function formatOrderStatus(status: OrderStatus, t: TFunction) {
  switch (status) {
    case OrderStatus.Pending:
      return t("orders.status.pending", "Pending");
    case OrderStatus.Accepted:
      return t("orders.status.accepted", "Accepted");
    case OrderStatus.SentToKitchen:
      return t("orders.status.sentToKitchen", "Sent to kitchen");
    case OrderStatus.Preparing:
      return t("orders.status.preparing", "Preparing");
    case OrderStatus.Ready:
      return t("orders.status.ready", "Ready");
    case OrderStatus.ReadyForWaiter:
      return t("orders.status.readyForWaiter", "Ready for waiter");
    case OrderStatus.Delivered:
      return t("orders.status.delivered", "Delivered");
    case OrderStatus.OutForDelivery:
      return t("orders.status.outForDelivery", "Out for delivery");
    case OrderStatus.Completed:
      return t("orders.status.completed", "Completed");
    case OrderStatus.Cancelled:
      return t("orders.status.cancelled", "Cancelled");
    default:
      return t("orders.status.draft", "Draft");
  }
}

export function formatOrderStatusName(status: string, t: TFunction) {
  switch (status) {
    case "Pending":
      return t("orders.status.pending", "Pending");
    case "Accepted":
      return t("orders.status.accepted", "Accepted");
    case "SentToKitchen":
      return t("orders.status.sentToKitchen", "Sent to kitchen");
    case "Preparing":
      return t("orders.status.preparing", "Preparing");
    case "Ready":
      return t("orders.status.ready", "Ready");
    case "ReadyForWaiter":
      return t("orders.status.readyForWaiter", "Ready for waiter");
    case "Delivered":
      return t("orders.status.delivered", "Delivered");
    case "OutForDelivery":
      return t("orders.status.outForDelivery", "Out for delivery");
    case "Completed":
      return t("orders.status.completed", "Completed");
    case "Cancelled":
      return t("orders.status.cancelled", "Cancelled");
    default:
      return status;
  }
}

export function formatOrderStatusForDisplay(order: { orderType: OrderType; status: OrderStatus }, t: TFunction) {
  if (order.orderType === OrderType.Takeaway && order.status === OrderStatus.Ready) {
    return t("orders.status.readyForPickup", "Ready for pickup");
  }

  if (order.orderType === OrderType.Delivery && order.status === OrderStatus.Ready) {
    return t("orders.status.readyForDispatch", "Ready for dispatch");
  }

  return formatOrderStatus(order.status, t);
}

export function formatOrderType(orderType: OrderType, t: TFunction) {
  switch (orderType) {
    case OrderType.Table:
      return t("mobile.restaurants.toTable", "To table");
    case OrderType.Takeaway:
      return t("mobile.restaurants.pickup", "Pick up");
    case OrderType.Delivery:
      return t("mobile.restaurants.delivery", "Delivery");
    default:
      return t("orders.type.order", "Order");
  }
}

type Props = {
  order: OrderSummary;
  details?: OrderDetails;
  expanded: boolean;
  token: string | null;
  currentCulture: string;
  onToggle: () => void;
  t: TFunction;
};

export function OrderSummaryCard({ order, details, expanded, token, currentCulture, onToggle, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <View style={sharedStyles.rowBetween}>
          <View style={[sharedStyles.flexOne, sharedStyles.stackSm]}>
            <Text style={sharedStyles.sectionTitle}>{t("orders.orderNumber", "Order")} #{order.id}</Text>
            <Text style={sharedStyles.mutedText}>
              {formatOrderType(order.orderType, t)}
              {order.tableNumber ? ` - ${order.tableNumber}` : ""}
            </Text>
            {order.orderType === OrderType.Takeaway ? (
              <Text style={sharedStyles.mutedText}>
                {[order.pickupContactName, order.pickupPhone].filter(Boolean).join(" - ") || t("orders.pickupOrder", "Pickup order")}
              </Text>
            ) : null}
            {order.orderType === OrderType.Delivery ? (
              <Text style={sharedStyles.mutedText}>
                {[order.deliveryContactName, order.deliveryPhone].filter(Boolean).join(" - ") || t("orders.deliveryOrder", "Delivery order")}
              </Text>
            ) : null}
          </View>
          <View style={{ alignItems: "flex-end", gap: 4 }}>
            <Text style={sharedStyles.title}>{formatOrderStatusForDisplay(order, t)}</Text>
            <Text>{order.total.toFixed(2)}</Text>
          </View>
        </View>

        <Text style={sharedStyles.mutedText}>
          {new Date(order.createdAt).toLocaleString(currentCulture)} - {order.itemCount} {order.itemCount === 1 ? t("orders.item", "item") : t("orders.items", "items")}
        </Text>
        {order.orderType === OrderType.Takeaway && order.scheduledFor ? (
          <Text style={sharedStyles.mutedText}>{t("orders.requestedPickup", "Requested pickup")}: {new Date(order.scheduledFor).toLocaleString(currentCulture)}</Text>
        ) : null}
        {order.orderType === OrderType.Delivery ? (
          <Text style={sharedStyles.mutedText}>
            {[order.deliveryAddressLine1, order.deliveryCity].filter(Boolean).join(", ") || t("orders.deliveryAddressPending", "Delivery address pending")}
          </Text>
        ) : null}

        <PrimaryButton label={expanded ? t("orders.hideDetails", "Hide details") : t("orders.showDetails", "Show details")} onPress={onToggle} />

        {expanded && details ? (
          <OrderDetailsPanel details={details} token={token} currentCulture={currentCulture} t={t} />
        ) : null}
      </View>
    </SectionCard>
  );
}

function OrderDetailsPanel({
  details,
  token,
  currentCulture,
  t,
}: {
  details: OrderDetails;
  token: string | null;
  currentCulture: string;
  t: TFunction;
}) {
  return (
    <View style={sharedStyles.stackMd}>
      <Text>{t("orders.subtotal", "Subtotal")}: {details.subtotal.toFixed(2)}</Text>
      <Text>{t("orders.deliveryFee", "Delivery fee")}: {details.deliveryFee.toFixed(2)}</Text>
      <Text style={sharedStyles.title}>{t("orders.total", "Total")}: {details.total.toFixed(2)}</Text>
      {details.estimatedReadyAt ? <Text>{t("orders.estimatedReady", "Estimated ready")}: {new Date(details.estimatedReadyAt).toLocaleString(currentCulture)}</Text> : null}
      {details.orderType === OrderType.Takeaway ? (
        <View style={sharedStyles.stackXs}>
          <Text>{t("orders.pickupName", "Pickup name")}: {details.pickupContactName ?? "-"}</Text>
          <Text>{t("orders.pickupPhone", "Pickup phone")}: {details.pickupPhone ?? "-"}</Text>
          {details.scheduledFor ? <Text>{t("orders.requestedPickup", "Requested pickup")}: {new Date(details.scheduledFor).toLocaleString(currentCulture)}</Text> : null}
          {details.pickupNote ? <Text>{t("orders.pickupNote", "Pickup note")}: {details.pickupNote}</Text> : null}
        </View>
      ) : null}
      {details.orderType === OrderType.Delivery ? (
        <View style={sharedStyles.stackXs}>
          <Text>{t("orders.deliveryContact", "Delivery contact")}: {details.deliveryContactName ?? "-"}</Text>
          <Text>{t("orders.deliveryPhone", "Delivery phone")}: {details.deliveryPhone ?? "-"}</Text>
          <Text>
            {t("orders.address", "Address")}: {[
              details.deliveryAddressLine1,
              details.deliveryAddressLine2,
              details.deliveryCity,
              details.deliveryPostalCode,
              details.deliveryCountry,
            ].filter(Boolean).join(", ") || "-"}
          </Text>
          {details.deliveryNote ? <Text>{t("orders.deliveryNote", "Delivery note")}: {details.deliveryNote}</Text> : null}
        </View>
      ) : null}
      {details.items.map((item) => (
        <View key={`${details.id}-${item.menuItemId}`} style={[sharedStyles.dividerTop, sharedStyles.stackXs]}>
          <Text style={sharedStyles.semibold}>{item.menuItemName ?? `${t("orders.item", "Item")} #${item.menuItemId}`}</Text>
          <Text>{t("orders.qty", "Qty")}: {item.quantity}</Text>
          <Text>{t("orders.unitPrice", "Unit price")}: {item.unitPrice.toFixed(2)}</Text>
          {item.note ? <Text>{t("mobile.reservations.note", "Note")}: {item.note}</Text> : null}
        </View>
      ))}
      {details.hasInvoiceDocument ? (
        <PrimaryButton
          label={t("orders.downloadInvoice", "Download invoice")}
          onPress={async () => {
            if (Platform.OS === "web") {
              const res = await fetch(`${API_BASE_URL}/api/orders/${details.id}/invoice-pdf`, {
                headers: token ? { Authorization: `Bearer ${token}` } : {},
              });
              if (!res.ok) {
                return;
              }
              const blob = await res.blob();
              const url = URL.createObjectURL(blob);
              const a = document.createElement("a");
              a.href = url;
              a.download = `${details.invoiceNumber ?? `invoice-${details.id}`}.pdf`;
              document.body.appendChild(a);
              a.click();
              a.remove();
              URL.revokeObjectURL(url);
              return;
            }

            showMessage(t("orders.invoiceDownload", "Invoice download"), t("orders.invoiceDownloadWebOnly", "Invoice download is currently available in the web build."));
          }}
        />
      ) : null}
      {!details.hasInvoiceDocument ? <Text style={sharedStyles.mutedText}>{t("orders.receiptHandledByRestaurant", "Receipt is handled by the restaurant.")}</Text> : null}
    </View>
  );
}
