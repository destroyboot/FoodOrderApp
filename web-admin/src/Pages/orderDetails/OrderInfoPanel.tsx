import { Link } from "react-router-dom";
import { orderTypeLabel } from "../../orderPresentation";
import { orderStatusDisplayLabel } from "../../orderStatus";
import type { OrderDetailsDto, TFunction } from "./types";

type Props = {
  data: OrderDetailsDto;
  realtimeState: string;
  t: TFunction;
};

export function OrderInfoPanel({ data, realtimeState, t }: Props) {
  return (
    <div className="detail-block">
      <div><b>{t("orders.status", "Status")}:</b> {orderStatusDisplayLabel(data.status, data.orderType, t)}</div>
      <div><b>{t("orders.type", "Type")}:</b> {orderTypeLabel(data.orderType, t)}</div>
      {Number(data.orderType) === 0 ? <div><b>{t("orders.table", "Table")}:</b> {data.tableNumber ?? "-"}</div> : null}
      {Number(data.orderType) === 1 ? <PickupDetails data={data} t={t} /> : null}
      {Number(data.orderType) === 2 ? <DeliveryDetails data={data} t={t} /> : null}
      <div>
        <b>{t("orders.customer", "Customer")}:</b>{" "}
        {data.isAnonymousCustomer || !data.customerUserId ? (
          t("orders.customer.anonymousDisplay", "Anonymous")
        ) : (
          <Link to={`/users?edit=${encodeURIComponent(data.customerUserId)}`}>
            {data.customerEmail ?? t("orders.customer.userDisplay", "User")}
          </Link>
        )}
      </div>
      <div><b>{t("orders.customerEmail", "Customer email")}:</b> {data.customerEmail ?? "-"}</div>
      <div><b>{t("orders.created", "Created")}:</b> {new Date(data.createdAt).toLocaleString()}</div>
      <div className="muted">{t("orders.realtime", "Realtime")}: {realtimeState}</div>
    </div>
  );
}

function PickupDetails({ data, t }: { data: OrderDetailsDto; t: TFunction }) {
  return (
    <>
      <div><b>{t("orders.pickupName", "Pickup name")}:</b> {data.pickupContactName ?? "-"}</div>
      <div><b>{t("orders.pickupPhone", "Pickup phone")}:</b> {data.pickupPhone ?? "-"}</div>
      {data.scheduledFor ? <div><b>{t("orders.requestedPickup", "Requested pickup")}:</b> {new Date(data.scheduledFor).toLocaleString()}</div> : null}
      {data.pickupNote ? <div><b>{t("orders.pickupNote", "Pickup note")}:</b> {data.pickupNote}</div> : null}
    </>
  );
}

function DeliveryDetails({ data, t }: { data: OrderDetailsDto; t: TFunction }) {
  return (
    <>
      <div><b>{t("orders.deliveryContact", "Delivery contact")}:</b> {data.deliveryContactName ?? "-"}</div>
      <div><b>{t("orders.deliveryPhone", "Delivery phone")}:</b> {data.deliveryPhone ?? "-"}</div>
      <div>
        <b>{t("orders.address", "Address")}:</b>{" "}
        {[
          data.deliveryAddressLine1,
          data.deliveryAddressLine2,
          data.deliveryCity,
          data.deliveryPostalCode,
          data.deliveryCountry,
        ].filter(Boolean).join(", ") || "-"}
      </div>
      {data.deliveryNote ? <div><b>{t("orders.deliveryNote", "Delivery note")}:</b> {data.deliveryNote}</div> : null}
      <div><b>{t("orders.assignedDriver", "Assigned driver")}:</b> {data.assignedDeliveryDriverName ?? "-"}</div>
    </>
  );
}
