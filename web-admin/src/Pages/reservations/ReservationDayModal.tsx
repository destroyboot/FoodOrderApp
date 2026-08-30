import { ModalShell } from "../../Components/ModalShell";
import type { Reservation, TFunction } from "./types";

type Props = {
  selectedDay: string;
  reservations: Reservation[];
  statuses: string[];
  onClose: () => void;
  onOpenReservation: (reservationId: number) => void;
  t: TFunction;
};

export function ReservationDayModal({ selectedDay, reservations, statuses, onClose, onOpenReservation, t }: Props) {
  return (
    <ModalShell title={`${t("reservations.forDate", "Reservations for")} ${selectedDay}`} onClose={onClose}>
      <div style={{ display: "grid", gap: 8 }}>
        {reservations.map((reservation) => (
          <div key={reservation.id} style={{ borderTop: "1px solid #eee", paddingTop: 8, display: "flex", justifyContent: "space-between", gap: 8 }}>
            <div>
              <strong>{reservation.guestName}</strong>
              <div>{new Date(reservation.startAt).toLocaleString()}</div>
              <div>{reservation.tableLabel ?? "-"} | {statuses[reservation.status] ?? reservation.status}</div>
            </div>
            <button onClick={() => onOpenReservation(reservation.id)}>{t("common.open", "Open")}</button>
          </div>
        ))}
        {reservations.length === 0 && <div style={{ color: "#666" }}>{t("reservations.noneForDay", "No reservations for this day.")}</div>}
      </div>
    </ModalShell>
  );
}
