import { ModalShell } from "../../Components/ModalShell";
import type { Reservation, TFunction } from "./types";

type Props = {
  reservation: Reservation;
  statuses: string[];
  onClose: () => void;
  onChangeStatus: (reservationId: number, status: number) => void;
  t: TFunction;
};

export function ReservationDetailsModal({ reservation, statuses, onClose, onChangeStatus, t }: Props) {
  return (
    <ModalShell title={`${t("nav.reservations", "Reservation")} #${reservation.id}`} onClose={onClose} width={640}>
      <div style={{ display: "grid", gap: 10 }}>
        <div><strong>{t("reservations.guest", "Guest")}:</strong> {reservation.guestName}</div>
        <div><strong>{t("common.email", "Email")}:</strong> {reservation.guestEmail ?? "-"}</div>
        <div><strong>{t("common.phone", "Phone")}:</strong> {reservation.guestPhone ?? "-"}</div>
        <div><strong>{t("reservations.table", "Table")}:</strong> {reservation.tableLabel ?? "-"}</div>
        <div><strong>{t("reservations.time", "Time")}:</strong> {new Date(reservation.startAt).toLocaleString()} - {new Date(reservation.endAt).toLocaleTimeString()}</div>
        <div><strong>{t("common.status", "Status")}:</strong> {statuses[reservation.status] ?? reservation.status}</div>
        <div><strong>{t("common.note", "Note")}:</strong> {reservation.note ?? "-"}</div>
        <label>
          <div style={{ marginBottom: 4 }}>{t("reservations.changeStatus", "Change status")}</div>
          <select
            value={reservation.status}
            onChange={(e) => onChangeStatus(reservation.id, Number(e.target.value))}
          >
            {statuses.map((status, index) => (
              <option key={status} value={index}>{status}</option>
            ))}
          </select>
        </label>
      </div>
    </ModalShell>
  );
}
