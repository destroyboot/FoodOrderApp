import { ModalShell } from "../../Components/ModalShell";
import { formatMinuteOfDay, minutesToTimeInput, timeInputToMinutes } from "./dateUtils";
import type { ReservationSchedule, RestaurantSettingsDto, RestaurantTableDto, TFunction } from "./types";

type Props = {
  restaurantSettings: RestaurantSettingsDto | null;
  tables: RestaurantTableDto[];
  schedules: ReservationSchedule[];
  selectedTableIds: number[];
  scheduleIntervalMinutes: string;
  busy: boolean;
  onClose: () => void;
  onSettingsChange: (updater: (current: RestaurantSettingsDto | null) => RestaurantSettingsDto | null) => void;
  onScheduleIntervalMinutesChange: (value: string) => void;
  onToggleTableId: (tableId: number) => void;
  onCreateSchedule: () => void;
  onClearSchedule: () => void;
  onSaveReservationSettings: () => void;
  onDeleteSchedule: (scheduleId: number) => void;
  t: TFunction;
};

export function ReservationSettingsModal({
  restaurantSettings,
  tables,
  schedules,
  selectedTableIds,
  scheduleIntervalMinutes,
  busy,
  onClose,
  onSettingsChange,
  onScheduleIntervalMinutesChange,
  onToggleTableId,
  onCreateSchedule,
  onClearSchedule,
  onSaveReservationSettings,
  onDeleteSchedule,
  t,
}: Props) {
  return (
    <ModalShell title={t("reservations.generalSchedule", "General Reservation Schedule")} onClose={onClose} width={820}>
      <div style={{ display: "grid", gap: 14 }}>
        {restaurantSettings ? (
          <div style={{ border: "1px solid #e5e7eb", borderRadius: 8, padding: 12, display: "grid", gap: 12 }}>
            <div style={{ fontWeight: 700 }}>{t("reservations.reservationHours", "Reservation Hours")}</div>
            <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr 1fr" }}>
              <label>
                <div style={{ marginBottom: 4 }}>{t("reservations.startTime", "Start Time")}</div>
                <input
                  type="time"
                  step={900}
                  value={minutesToTimeInput(restaurantSettings.reservationStartMinuteOfDay)}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, reservationStartMinuteOfDay: timeInputToMinutes(e.target.value) } : current)}
                />
              </label>
              <label>
                <div style={{ marginBottom: 4 }}>{t("reservations.endTime", "End Time")}</div>
                <input
                  type="time"
                  step={900}
                  value={minutesToTimeInput(restaurantSettings.reservationLastStartMinuteOfDay)}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, reservationLastStartMinuteOfDay: timeInputToMinutes(e.target.value) } : current)}
                />
              </label>
              <label>
                <div style={{ marginBottom: 4 }}>{t("reservations.defaultLength", "Default Length")}</div>
                <input
                  type="number"
                  min={15}
                  step={15}
                  value={restaurantSettings.defaultReservationDurationMinutes}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, defaultReservationDurationMinutes: Number(e.target.value) || 15 } : current)}
                />
              </label>
            </div>
            <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "center" }}>
              <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <input
                  type="checkbox"
                  checked={restaurantSettings.enableReservations}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, enableReservations: e.target.checked } : current)}
                />
                {t("reservations.enabled", "Reservations enabled")}
              </label>
              <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <input
                  type="checkbox"
                  checked={restaurantSettings.reservationHoldsTableUntilClose}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, reservationHoldsTableUntilClose: e.target.checked } : current)}
                />
                {t("reservations.holdUntilClose", "Hold table until close")}
              </label>
              <label>
                <span style={{ marginRight: 8 }}>{t("reservations.gracePeriod", "Grace period")}</span>
                <input
                  type="number"
                  min={0}
                  step={5}
                  value={restaurantSettings.reservationGracePeriodMinutes}
                  onChange={(e) => onSettingsChange((current) => current ? { ...current, reservationGracePeriodMinutes: Number(e.target.value) || 0 } : current)}
                  style={{ width: 90 }}
                />
              </label>
            </div>
            <div style={{ color: "#666", fontSize: 13 }}>
              {t("reservations.hoursHint", "This is daily reservation window used for schedules. For after-midnight service, set an overnight window here, for example 17:00 to 01:00. Schedule rows will be split automatically across midnight.")}
            </div>
          </div>
        ) : null}

        <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr" }}>
          <label>
            <div style={{ marginBottom: 4 }}>{t("reservations.intervalMinutes", "Interval Minutes")}</div>
            <input type="number" min={15} step={15} value={scheduleIntervalMinutes} onChange={(e) => onScheduleIntervalMinutesChange(e.target.value)} />
          </label>
          <div style={{ color: "#666", alignSelf: "end", fontSize: 13 }}>
            {t("reservations.scheduleOverwriteHint", "Saving a schedule now overwrites the existing schedule for the selected tables using the reservation hours above.")}
          </div>
        </div>

        <div>
          <div style={{ marginBottom: 6, fontWeight: 700 }}>{t("nav.tables", "Tables")}</div>
          <div style={{ display: "grid", gap: 6, maxHeight: 180, overflow: "auto" }}>
            {tables.filter((table) => table.isActive && table.isReservable).map((table) => (
              <label key={table.id} style={{ display: "flex", gap: 8, alignItems: "center" }}>
                <input type="checkbox" checked={selectedTableIds.includes(table.id)} onChange={() => onToggleTableId(table.id)} />
                {table.label}{table.seats ? ` (${table.seats} ${t("common.seats", "seats")})` : ""}
              </label>
            ))}
          </div>
        </div>

        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={onCreateSchedule} disabled={busy}>
            {busy ? t("common.working", "Working...") : t("reservations.saveSchedule", "Save Schedule")}
          </button>
          <button onClick={onClearSchedule} disabled={busy || schedules.length === 0}>
            {t("reservations.clearWholeSchedule", "Clear Whole Schedule")}
          </button>
          <button onClick={onSaveReservationSettings} disabled={busy}>
            {busy ? t("common.working", "Working...") : t("reservations.saveHours", "Save Reservation Hours")}
          </button>
        </div>

        <div style={{ marginTop: 8 }}>
          <h4 style={{ margin: "0 0 8px" }}>{t("reservations.currentSchedules", "Current schedules")}</h4>
          <div style={{ display: "grid", gap: 8, maxHeight: 280, overflow: "auto" }}>
            {schedules.map((schedule) => (
              <div key={schedule.id} style={{ borderTop: "1px solid #eee", paddingTop: 8, display: "flex", justifyContent: "space-between", gap: 8, alignItems: "center" }}>
                <div>
                  <strong>{schedule.tableLabel ?? schedule.restaurantTableId}</strong>
                  {schedule.seats ? ` (${schedule.seats} ${t("common.seats", "seats")})` : ""}
                  <div style={{ color: "#666" }}>
                    {formatMinuteOfDay(schedule.startMinuteOfDay)} - {formatMinuteOfDay(schedule.endMinuteOfDay)} {t("reservations.every", "every")} {schedule.intervalMinutes} {t("common.minutes", "min")}
                  </div>
                </div>
                <button onClick={() => onDeleteSchedule(schedule.id)} disabled={busy}>{t("common.delete", "Delete")}</button>
              </div>
            ))}
            {schedules.length === 0 && <div style={{ color: "#666" }}>{t("reservations.noSchedules", "No reservation schedules configured yet.")}</div>}
          </div>
        </div>
      </div>
    </ModalShell>
  );
}
