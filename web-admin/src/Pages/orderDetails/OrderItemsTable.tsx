import type { OrderDetailsDto, TFunction } from "./types";

function normalizeOrderNote(note: string | null | undefined) {
  if (!note?.trim()) {
    return "-";
  }

  const parts = note
    .split("|")
    .map((part) => part.trim())
    .filter(Boolean);

  return Array.from(new Set(parts)).join(" | ") || "-";
}

type Props = {
  data: OrderDetailsDto;
  t: TFunction;
};

export function OrderItemsTable({ data, t }: Props) {
  return (
    <>
      <table>
        <thead>
          <tr>
            <th align="left">{t("orders.itemName", "Product")}</th>
            <th align="left">{t("common.id", "ID")}</th>
            <th align="right">{t("orders.quantity", "Quantity")}</th>
            <th align="right">{t("orders.price", "Price")}</th>
            <th align="left">{t("common.note", "Note")}</th>
            <th align="right">{t("orders.lineTotal", "Total")}</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((item, idx) => (
            <tr key={idx}>
              <td>{item.menuItemName ?? "-"}</td>
              <td>{item.menuItemId}</td>
              <td align="right">{item.quantity}</td>
              <td align="right">{Number(item.unitPrice).toFixed(2)}</td>
              <td>{normalizeOrderNote(item.note)}</td>
              <td align="right">{(item.quantity * item.unitPrice).toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="stack-xs spaced-top-lg">
        <div><b>{t("orders.subtotal", "Subtotal")}:</b> {Number(data.subtotal).toFixed(2)}</div>
        <div><b>{t("orders.deliveryFee", "Delivery fee")}:</b> {Number(data.deliveryFee).toFixed(2)}</div>
        <div><b>{t("orders.total", "Total")}:</b> {Number(data.total).toFixed(2)}</div>
      </div>
    </>
  );
}
