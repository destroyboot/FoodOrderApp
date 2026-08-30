import { ModalShell } from "../../Components/ModalShell";
import type { ReservationTableAvailability, TFunction } from "./types";

type Props = {
  callerDate: string;
  guestName: string;
  guestEmail: string;
  guestPhone: string;
  partySize: string;
  note: string;
  selectedTableId: string;
  selectedStartTime: string;
  tables: ReservationTableAvailability[];
  selectedTable: ReservationTableAvailability | null;
  busy: boolean;
  onClose: () => void;
  onCallerDateChange: (value: string) => void;
  onGuestNameChange: (value: string) => void;
  onGuestEmailChange: (value: string) => void;
  onGuestPhoneChange: (value: string) => void;
  onPartySizeChange: (value: string) => void;
  onNoteChange: (value: string) => void;
  onTableIdChange: (value: string) => void;
  onStartTimeChange: (value: string) => void;
  onCreateReservation: () => void;
  t: TFunction;
};

export function CallerReservationModal({
  callerDate,
  guestName,
  guestEmail,
  guestPhone,
  partySize,
  note,
  selectedTableId,
  selectedStartTime,
  tables,
  selectedTable,
  busy,
  onClose,
  onCallerDateChange,
  onGuestNameChange,
  onGuestEmailChange,
  onGuestPhoneChange,
  onPartySizeChange,
  onNoteChange,
  onTableIdChange,
  onStartTimeChange,
  onCreateReservation,
  t,
}: Props) {
  return (
    <ModalShell title={`${t("reservations.bookCaller", "Book for a caller")} - ${callerDate}`} onClose={onClose} width={720}>
      <div style={{ display: "grid", gap: 10 }}>
        <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr" }}>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.date", "Reservation date")}</div>
            <input
              type="date"
              value={callerDate}
              onChange={(e) => onCallerDateChange(e.target.value)}
              style={{ width: "100%" }}
            />
          </label>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.guestName", "Guest name")}</div>
            <input value={guestName} onChange={(e) => onGuestNameChange(e.target.value)} style={{ width: "100%" }} />
          </label>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.partySize", "Party size")}</div>
            <input type="number" min={1} value={partySize} onChange={(e) => onPartySizeChange(e.target.value)} style={{ width: "100%" }} />
          </label>
        </div>
        <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr" }}>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.guestEmail", "Guest email")}</div>
            <input value={guestEmail} onChange={(e) => onGuestEmailChange(e.target.value)} style={{ width: "100%" }} />
          </label>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.guestPhone", "Guest phone")}</div>
            <input value={guestPhone} onChange={(e) => onGuestPhoneChange(e.target.value)} style={{ width: "100%" }} />
          </label>
        </div>
        <label>
          <div style={{ marginBottom: 4 }}>{t("common.note", "Note")}</div>
          <textarea value={note} onChange={(e) => onNoteChange(e.target.value)} rows={3} style={{ width: "100%" }} />
        </label>
        <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr" }}>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.table", "Table")}</div>
            <select
              value={selectedTableId}
              onChange={(e) => onTableIdChange(e.target.value)}
              style={{ width: "100%" }}
            >
              <option value="">{t("reservations.chooseTable", "Choose table")}</option>
              {tables.map((table) => (
                <option key={table.tableId} value={table.tableId}>
                  {table.label}{table.seats ? ` (${table.seats} seats)` : ""}
                </option>
              ))}
            </select>
          </label>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.availableSlot", "Available slot")}</div>
            <select
              value={selectedStartTime}
              onChange={(e) => onStartTimeChange(e.target.value)}
              style={{ width: "100%" }}
            >
              <option value="">{t("reservations.chooseSlot", "Choose slot")}</option>
              {(selectedTable?.availableStartTimes ?? []).map((slot) => (
                <option key={slot} value={slot}>{slot}</option>
              ))}
            </select>
          </label>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={onCreateReservation} disabled={busy}>
            {busy ? t("common.working", "Working...") : t("reservations.create", "Create reservation")}
          </button>
          <button type="button" onClick={onClose}>
            {t("common.cancel", "Cancel")}
          </button>
        </div>
      </div>
    </ModalShell>
  );
}
