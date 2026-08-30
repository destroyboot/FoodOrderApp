import { ModalShell } from "../../Components/ModalShell";
import type { Reservation, TFunction } from "./types";

type Props = {
  searchText: string;
  results: Reservation[];
  statuses: string[];
  onClose: () => void;
  onSearchTextChange: (value: string) => void;
  onRunSearch: () => void;
  onOpenReservation: (reservationId: number) => void;
  t: TFunction;
};

export function ReservationSearchModal({
  searchText,
  results,
  statuses,
  onClose,
  onSearchTextChange,
  onRunSearch,
  onOpenReservation,
  t,
}: Props) {
  return (
    <ModalShell title={t("reservations.search", "Search reservations")} onClose={onClose} width={760}>
      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <input
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
          placeholder={t("reservations.searchPlaceholder", "Search by guest, table, phone, note")}
          style={{ flex: 1 }}
        />
        <button onClick={onRunSearch}>{t("common.search", "Search")}</button>
      </div>
      <div style={{ display: "grid", gap: 8 }}>
        {results.map((reservation) => (
          <div key={reservation.id} style={{ borderTop: "1px solid #eee", paddingTop: 8, display: "flex", justifyContent: "space-between", gap: 8 }}>
            <div>
              <strong>{reservation.guestName}</strong>
              <div>{new Date(reservation.startAt).toLocaleString()}</div>
              <div>{reservation.tableLabel ?? "-"} | {statuses[reservation.status] ?? reservation.status}</div>
            </div>
            <button onClick={() => onOpenReservation(reservation.id)}>
              {t("common.open", "Open")}
            </button>
          </div>
        ))}
        {results.length === 0 && <div style={{ color: "#666" }}>{t("reservations.noSearchResults", "No search results yet.")}</div>}
      </div>
    </ModalShell>
  );
}
