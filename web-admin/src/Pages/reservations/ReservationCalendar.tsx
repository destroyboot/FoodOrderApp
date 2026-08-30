import { formatLocalDate, todayString } from "./dateUtils";
import type { TFunction } from "./types";

type Props = {
  selectedMonth: Date;
  monthCells: Date[];
  calendarCountByDate: Record<string, number>;
  onOpenDay: (date: string) => void;
  t: TFunction;
};

export function ReservationCalendar({ selectedMonth, monthCells, calendarCountByDate, onOpenDay, t }: Props) {
  return (
    <section style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16 }}>
      <div style={{ marginBottom: 12 }}>
        <h3 style={{ margin: 0 }}>{t("reservations.calendar", "Reservation calendar")}</h3>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(7, 1fr)", gap: 8 }}>
        {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((label) => (
          <div key={label} style={{ fontWeight: 700, textAlign: "center" }}>{label}</div>
        ))}
        {monthCells.map((date) => {
          const dateKey = formatLocalDate(date);
          const isCurrentMonth = date.getMonth() === selectedMonth.getMonth();
          const isToday = dateKey === todayString();
          const count = calendarCountByDate[dateKey] ?? 0;

          return (
            <button
              key={dateKey}
              onClick={() => onOpenDay(dateKey)}
              style={{
                minHeight: 72,
                borderRadius: 8,
                border: "1px solid #ddd",
                background: isToday ? "#dcfce7" : count > 0 ? "#dbeafe" : "#fff",
                color: isCurrentMonth ? "#111827" : "#9ca3af",
                padding: 8,
                textAlign: "left",
              }}
            >
              <div style={{ fontWeight: 700 }}>{date.getDate()}</div>
              {count > 0 && <div style={{ marginTop: 8, fontSize: 12 }}>{count} reservation{count === 1 ? "" : "s"}</div>}
            </button>
          );
        })}
      </div>
    </section>
  );
}
