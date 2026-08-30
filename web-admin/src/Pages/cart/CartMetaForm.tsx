import type { RestaurantDto, RestaurantTableDto, SelectOption, TFunction } from "./types";

type Props = {
  orderType: number;
  restaurantId: string;
  restaurantTableId: string;
  paymentMethod: number;
  receiptEmail: string;
  restaurants: RestaurantDto[];
  restaurantTables: RestaurantTableDto[];
  orderTypeOptions: SelectOption[];
  paymentMethodOptions: SelectOption[];
  onOrderTypeChange: (value: number) => void;
  onRestaurantChange: (value: string) => void;
  onRestaurantTableChange: (value: string) => void;
  onPaymentMethodChange: (value: number) => void;
  onReceiptEmailChange: (value: string) => void;
  onSaveMeta: () => void;
  t: TFunction;
};

export function CartMetaForm({
  orderType,
  restaurantId,
  restaurantTableId,
  paymentMethod,
  receiptEmail,
  restaurants,
  restaurantTables,
  orderTypeOptions,
  paymentMethodOptions,
  onOrderTypeChange,
  onRestaurantChange,
  onRestaurantTableChange,
  onPaymentMethodChange,
  onReceiptEmailChange,
  onSaveMeta,
  t,
}: Props) {
  return (
    <>
      <div style={{ marginBottom: 12 }}>
        <label>{t("orders.type", "Order type")}</label>
        <br />
        <select
          value={orderType}
          onChange={(e) => onOrderTypeChange(Number(e.target.value))}
          style={{ width: "100%" }}
        >
          {orderTypeOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("nav.restaurants", "Restaurants")}</label>
        <br />
        <select
          value={restaurantId}
          onChange={(e) => onRestaurantChange(e.target.value)}
          style={{ width: "100%" }}
        >
          <option value="">{t("common.selectRestaurantOption", "-- select restaurant --")}</option>
          {restaurants.map((restaurant) => (
            <option key={restaurant.id} value={restaurant.id}>
              {restaurant.name}
            </option>
          ))}
        </select>
      </div>

      {orderType === 0 ? (
        <div style={{ marginBottom: 12 }}>
          <label>{t("nav.tables", "Tables")}</label>
          <br />
          <select
            value={restaurantTableId}
            onChange={(e) => onRestaurantTableChange(e.target.value)}
            style={{ width: "100%" }}
            disabled={!restaurantId}
          >
            <option value="">{t("common.selectTableOption", "-- select table --")}</option>
            {restaurantTables.map((table) => (
              <option key={table.id} value={table.id}>
                {table.label}
                {table.seats ? ` (${table.seats} ${t("common.seats", "seats")})` : ""}
              </option>
            ))}
          </select>
        </div>
      ) : null}

      <div style={{ marginBottom: 12 }}>
        <label>{t("orders.paymentMethod", "Payment method")}</label>
        <br />
        <select
          value={paymentMethod}
          onChange={(e) => onPaymentMethodChange(Number(e.target.value))}
          style={{ width: "100%" }}
        >
          {paymentMethodOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("orders.receiptEmail", "Receipt email")}</label>
        <br />
        <input
          value={receiptEmail}
          onChange={(e) => onReceiptEmailChange(e.target.value)}
          style={{ width: "100%" }}
        />
      </div>

      <button onClick={onSaveMeta}>{t("cart.saveMeta", "Save meta data")}</button>
    </>
  );
}
