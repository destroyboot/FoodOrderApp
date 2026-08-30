import type { DeliveryDriverOptionDto, OrderDetailsDto, TFunction } from "./types";
import { OrderStatus } from "../../orderStatus";

type StatusAction = {
  status: OrderStatus;
  label: string;
};

type Props = {
  data: OrderDetailsDto;
  statusActions: StatusAction[];
  isHistoryView: boolean;
  isDriverOnly: boolean;
  drivers: DeliveryDriverOptionDto[];
  assigningDriver: boolean;
  onChangeStatus: (status: OrderStatus) => void;
  onCollectPaymentAndComplete: () => void;
  onAssignDriver: (userId: string) => void;
  t: TFunction;
};

export function OrderActionsPanel({
  data,
  statusActions,
  isHistoryView,
  isDriverOnly,
  drivers,
  assigningDriver,
  onChangeStatus,
  onCollectPaymentAndComplete,
  onAssignDriver,
  t,
}: Props) {
  return (
    <>
      {!isHistoryView ? (
        <div className="action-block">
          <div className="action-row">
            {statusActions
              .filter((action) => !isDriverOnly || action.status === OrderStatus.OutForDelivery || action.status === OrderStatus.Delivered)
              .filter((action) => action.status !== OrderStatus.Cancelled)
              .map((action) => (
                <button
                  key={action.status}
                  onClick={() => onChangeStatus(action.status)}
                  className="button-fixed-action"
                >
                  {action.label}
                </button>
              ))}
          </div>
          <button
            onClick={() => onChangeStatus(OrderStatus.Cancelled)}
            className="button-fixed-action button-danger spaced-top-md"
          >
            {t("common.cancel", "Cancel")}
          </button>
        </div>
      ) : null}

      {!isHistoryView && Number(data.orderType) === 2 && data.paymentMethod === 1 ? (
        <div className="action-block">
          <button onClick={onCollectPaymentAndComplete}>
            {t("orders.markPaymentCollectedAndComplete", "Mark payment collected and complete")}
          </button>
        </div>
      ) : null}

      {!isHistoryView && Number(data.orderType) === 2 && !isDriverOnly ? (
        <div className="action-block cluster-sm">
          <strong>{t("orders.assignDriver", "Assign driver")}</strong>
          <select
            value={data.assignedDeliveryDriverUserId ?? ""}
            onChange={(e) => onAssignDriver(e.target.value)}
            disabled={assigningDriver}
          >
            <option value="">{t("orders.unassigned", "-- unassigned --")}</option>
            {drivers.map((driver) => (
              <option key={driver.userId} value={driver.userId}>
                {driver.displayName}
              </option>
            ))}
          </select>
          {assigningDriver ? <span className="muted">{t("common.saving", "Saving...")}</span> : null}
        </div>
      ) : null}
    </>
  );
}
