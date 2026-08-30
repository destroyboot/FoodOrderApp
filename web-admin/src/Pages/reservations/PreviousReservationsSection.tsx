import { ReservationsTable } from "./ReservationsTable";
import type { Reservation, TFunction } from "./types";

type Props = {
  reservations: Reservation[];
  statuses: string[];
  searchText: string;
  fromDate: string;
  toDate: string;
  onSearchTextChange: (value: string) => void;
  onFromDateChange: (value: string) => void;
  onToDateChange: (value: string) => void;
  onApplyFilters: () => void;
  onResetFilters: () => void;
  onOpenReservation: (reservationId: number) => void;
  t: TFunction;
};

export function PreviousReservationsSection({
  reservations,
  statuses,
  searchText,
  fromDate,
  toDate,
  onSearchTextChange,
  onFromDateChange,
  onToDateChange,
  onApplyFilters,
  onResetFilters,
  onOpenReservation,
  t,
}: Props) {
  return (
    <details style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16, marginTop: 20 }}>
      <summary style={{ cursor: "pointer", fontWeight: 700, fontSize: "1.1rem" }}>{t("reservations.previous", "Previous reservations")}</summary>
      <div style={{ marginTop: 16 }}>
        <div style={{ display: "grid", gap: 12, marginBottom: 12 }}>
          <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8 }}>
            <input
              placeholder={t("reservations.searchPrevious", "Search previous reservations")}
              value={searchText}
              onChange={(e) => onSearchTextChange(e.target.value)}
              style={{ width: "100%" }}
            />
            <button onClick={onResetFilters}>
              {t("common.resetFilters", "Reset Filters")}
            </button>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 8 }}>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => onFromDateChange(e.target.value)}
            />
            <input type="date" value={toDate} onChange={(e) => onToDateChange(e.target.value)} />
            <button type="button" onClick={onApplyFilters}>
              {t("orders.history.applyFilters", "Apply filters")}
            </button>
          </div>
        </div>
        <ReservationsTable
          reservations={reservations}
          statuses={statuses}
          emptyMessage={t("reservations.noPreviousMatches", "No previous reservations match the current filters.")}
          onOpen={onOpenReservation}
          t={t}
        />
      </div>
    </details>
  );
}
