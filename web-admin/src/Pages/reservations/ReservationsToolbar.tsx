import type { TFunction } from "./types";

type Props = {
  selectedMonth: Date;
  busy: boolean;
  onOpenSettings: () => void;
  onOpenCaller: () => void;
  onOpenSearch: () => void;
  onPreviousMonth: () => void;
  onNextMonth: () => void;
  t: TFunction;
};

export function ReservationsToolbar({
  selectedMonth,
  busy,
  onOpenSettings,
  onOpenCaller,
  onOpenSearch,
  onPreviousMonth,
  onNextMonth,
  t,
}: Props) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
      <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
        <button onClick={onOpenSettings}>{t("reservations.settingsButton", "Reservation Settings")}</button>
        <button onClick={onOpenCaller} disabled={busy}>{t("reservations.bookCaller", "Book for a caller")}</button>
        <button onClick={onOpenSearch} disabled={busy}>{t("reservations.search", "Search reservations")}</button>
      </div>
      <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
        <button onClick={onPreviousMonth}>{t("common.previous", "Previous")}</button>
        <strong>{selectedMonth.toLocaleString(undefined, { month: "long", year: "numeric" })}</strong>
        <button onClick={onNextMonth}>{t("common.next", "Next")}</button>
      </div>
    </div>
  );
}
