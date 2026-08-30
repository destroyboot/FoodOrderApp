import type { CartPreviewResponseDto, TFunction } from "./types";

type Props = {
  preview: CartPreviewResponseDto | null;
  onSaveItems: () => void;
  onPreview: () => void;
  onFinalize: () => void;
  t: TFunction;
};

export function CartPreviewPanel({ preview, onSaveItems, onPreview, onFinalize, t }: Props) {
  return (
    <>
      <div style={{ marginTop: 12, display: "flex", gap: 8 }}>
        <button onClick={onSaveItems}>{t("cart.saveItems", "Save Items")}</button>
        <button onClick={onPreview}>{t("cart.preview", "Preview")}</button>
        <button onClick={onFinalize}>{t("cart.finalize", "Finalize")}</button>
      </div>

      {preview ? (
        <div style={{ marginTop: 20, border: "1px solid #ddd", padding: 12 }}>
          <h4>{t("cart.preview", "Preview")}</h4>
          <div>{t("cart.subtotal", "Subtotal")}: {preview.subtotal}</div>
          <div>{t("cart.deliveryFee", "Delivery fee")}: {preview.deliveryFee}</div>
          <div>{t("orders.total", "Total")}: {preview.total}</div>
          <div>{t("cart.estimatedPreparation", "Estimated preparation time")}: {preview.estimatedPreparationMinutes} {t("common.minutes", "min")}</div>
          <div>{t("cart.estimatedReadyAt", "Estimated ready at")}: {preview.estimatedReadyAt ?? "-"}</div>
        </div>
      ) : null}
    </>
  );
}
