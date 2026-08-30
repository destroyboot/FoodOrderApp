import { ModalShell } from "../../Components/ModalShell";
import type { TableFormState, TFunction } from "./types";

type Props = {
  tableForm: TableFormState;
  onFormChange: (updater: (current: TableFormState) => TableFormState) => void;
  onClose: () => void;
  onSave: () => void;
  t: TFunction;
};

export function TableModal({ tableForm, onFormChange, onClose, onSave, t }: Props) {
  return (
    <ModalShell title={tableForm.id ? t("tables.edit", "Edit Table") : t("tables.create", "Create Table")} onClose={onClose} minWidth="min(640px, calc(100vw - 32px))" maxWidth={840}>
      <div style={{ display: "grid", gap: 10 }}>
        <input placeholder={t("restaurant.tableLabel", "Label")} value={tableForm.label} onChange={(e) => onFormChange((prev) => ({ ...prev, label: e.target.value }))} />
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
          <input placeholder={t("restaurant.tableSeats", "Seats")} type="number" value={tableForm.seats} onChange={(e) => onFormChange((prev) => ({ ...prev, seats: e.target.value }))} />
          <input placeholder={t("restaurant.tableSort", "Sort")} type="number" value={tableForm.sortOrder} onChange={(e) => onFormChange((prev) => ({ ...prev, sortOrder: Number(e.target.value) }))} />
        </div>
        <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={tableForm.isActive} onChange={(e) => onFormChange((prev) => ({ ...prev, isActive: e.target.checked }))} />{t("common.active", "Active")}</label>
        <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={tableForm.isReservable} onChange={(e) => onFormChange((prev) => ({ ...prev, isReservable: e.target.checked }))} />{t("restaurant.reservable", "Reservable")}</label>
        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button onClick={onSave}>{tableForm.id ? t("restaurant.saveTable", "Save table") : t("restaurant.addTable", "Add table")}</button>
          <button onClick={onClose}>{t("common.cancel", "Cancel")}</button>
        </div>
      </div>
    </ModalShell>
  );
}
