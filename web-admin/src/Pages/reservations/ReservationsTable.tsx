import type { Reservation, TFunction } from "./types";

type ReservationsTableProps = {
  reservations: Reservation[];
  statuses: string[];
  emptyMessage: string;
  onOpen: (reservationId: number) => void;
  t: TFunction;
};

export function ReservationsTable({ reservations, statuses, emptyMessage, onOpen, t }: ReservationsTableProps) {
  return (
    <table width="100%" cellPadding={8}>
      <thead>
        <tr>
          <th align="left">{t("reservations.guest", "Guest")}</th>
          <th align="left">{t("reservations.time", "Time")}</th>
          <th align="left">{t("reservations.table", "Table")}</th>
          <th align="left">{t("common.status", "Status")}</th>
          <th align="left">{t("common.note", "Note")}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {reservations.map((reservation) => (
          <tr key={reservation.id}>
            <td>
              {reservation.guestName}
              <br />
              {reservation.guestEmail ?? reservation.guestPhone ?? ""}
            </td>
            <td>{new Date(reservation.startAt).toLocaleString()}</td>
            <td>{reservation.tableLabel ?? "-"}</td>
            <td>{statuses[reservation.status] ?? reservation.status}</td>
            <td>{reservation.note ?? "-"}</td>
            <td><button onClick={() => onOpen(reservation.id)}>{t("common.open", "Open")}</button></td>
          </tr>
        ))}
        {reservations.length === 0 ? (
          <tr>
            <td colSpan={6} style={{ color: "#666", padding: 16 }}>{emptyMessage}</td>
          </tr>
        ) : null}
      </tbody>
    </table>
  );
}
