import type { CSSProperties } from "react";
import type { RestaurantFormState, TFunction } from "./types";

const readOnlyFieldStyle: CSSProperties = {
  background: "#f3f4f6",
  color: "#6b7280",
  borderColor: "#d1d5db",
};

type Props = {
  form: RestaurantFormState;
  cuisineNameMap: Record<string, string>;
  isMainAdmin: boolean;
  editingDetails: boolean;
  canEditName: boolean;
  onFormChange: (updater: (current: RestaurantFormState) => RestaurantFormState) => void;
  onOpenCuisineModal: () => void;
  onRemoveCuisine: (cuisine: string) => void;
  onStartEdit: () => void;
  onSave: () => void;
  onCancel: () => void;
  t: TFunction;
};

export function RestaurantDetailsSection({
  form,
  cuisineNameMap,
  isMainAdmin,
  editingDetails,
  canEditName,
  onFormChange,
  onOpenCuisineModal,
  onRemoveCuisine,
  onStartEdit,
  onSave,
  onCancel,
  t,
}: Props) {
  return (
    <section style={{ marginBottom: 28, border: "1px solid #e5e7eb", borderRadius: 8, padding: 16 }}>
      <h3 style={{ marginTop: 0 }}>{t("restaurant.details", "Restaurant details")}</h3>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 8 }}>
        <input placeholder={t("common.name", "Name")} value={form.name} onChange={(e) => onFormChange((prev) => ({ ...prev, name: e.target.value }))} disabled={!editingDetails || !canEditName} style={!editingDetails || !canEditName ? readOnlyFieldStyle : undefined} />
        <input placeholder={t("common.city", "City")} value={form.city} onChange={(e) => onFormChange((prev) => ({ ...prev, city: e.target.value }))} disabled={!editingDetails} style={!editingDetails ? readOnlyFieldStyle : undefined} />
        <input placeholder={t("common.street", "Street")} value={form.street} onChange={(e) => onFormChange((prev) => ({ ...prev, street: e.target.value }))} disabled={!editingDetails} style={!editingDetails ? readOnlyFieldStyle : undefined} />
        <input placeholder={t("common.number", "Number")} value={form.houseNumber} onChange={(e) => onFormChange((prev) => ({ ...prev, houseNumber: e.target.value }))} disabled={!editingDetails} style={!editingDetails ? readOnlyFieldStyle : undefined} />
        <input placeholder={t("common.postalCode", "Postal code")} value={form.postalCode} onChange={(e) => onFormChange((prev) => ({ ...prev, postalCode: e.target.value }))} disabled={!editingDetails} style={!editingDetails ? readOnlyFieldStyle : undefined} />
        <label style={{ gridColumn: "span 2" }}>
          <div style={{ marginBottom: 4 }}>{t("restaurant.cuisineTypes", "Cuisine types (up to 5)")}</div>
          <div style={{ display: "grid", gap: 8 }}>
            <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
              {form.cuisineTypes.map((cuisine) => (
                <span key={cuisine} style={{ display: "inline-flex", alignItems: "center", gap: 6, border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px", ...(editingDetails ? {} : readOnlyFieldStyle) }}>
                  {cuisineNameMap[cuisine] ?? cuisine}
                  {editingDetails ? <button type="button" onClick={() => onRemoveCuisine(cuisine)} style={{ padding: "0 4px" }}>x</button> : null}
                </span>
              ))}
              {form.cuisineTypes.length === 0 ? <span style={{ color: "#666" }}>{t("restaurant.noCuisines", "No cuisines selected yet.")}</span> : null}
            </div>
            <div>
              <button type="button" onClick={onOpenCuisineModal} disabled={!editingDetails}>+</button>
            </div>
          </div>
        </label>
        {isMainAdmin ? (
          <label style={{ whiteSpace: "nowrap", alignSelf: "center" }}>
            <input type="checkbox" checked={form.isActive} onChange={(e) => onFormChange((prev) => ({ ...prev, isActive: e.target.checked }))} disabled={!editingDetails} />
            &nbsp;{t("common.active", "Active")}
          </label>
        ) : null}
      </div>
      <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
        {!editingDetails ? (
          <button onClick={onStartEdit}>
            {t("restaurant.editDetails", "Edit Restaurant Details")}
          </button>
        ) : (
          <>
            <button onClick={onSave}>{form.id ? t("restaurant.save", "Save restaurant") : t("restaurant.create", "Create restaurant")}</button>
            <button onClick={onCancel}>{t("common.cancel", "Cancel")}</button>
          </>
        )}
      </div>
    </section>
  );
}
