import { ReservationsTable } from "./ReservationsTable";
import type { Reservation, TFunction } from "./types";

type Props = {
  reservations: Reservation[];
  statuses: string[];
  searchText: string;
  onSearchTextChange: (value: string) => void;
  onResetSearch: () => void;
  onOpenReservation: (reservationId: number) => void;
  t: TFunction;
};

export function TodaysReservationsSection({
  reservations,
  statuses,
  searchText,
  onSearchTextChange,
  onResetSearch,
  onOpenReservation,
  t,
}: Props) {
  return (
    <section style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16 }}>
      <div style={{ display: "flex", justifyContent: "space-between", gap: 8, alignItems: "center", marginBottom: 12 }}>
        <h3 style={{ margin: 0 }}>{t("reservations.todays", "Today's reservations")}</h3>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8, marginBottom: 12 }}>
        <input
          placeholder={t("reservations.searchToday", "Search today's reservations")}
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
          style={{ width: "100%" }}
        />
        <button onClick={onResetSearch}>{t("common.resetFilters", "Reset Filters")}</button>
      </div>

      <ReservationsTable
        reservations={reservations}
        statuses={statuses}
        emptyMessage={t("reservations.noMatches", "No reservations match this search.")}
        onOpen={onOpenReservation}
        t={t}
      />
    </section>
  );
}
