import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { matchesTokenizedSearch } from "../tokenSearch";
import { PageShell } from "../Components/PageShell";
import { CallerReservationModal } from "./reservations/CallerReservationModal";
import { buildMonthCells, minutesToTimeInput, monthStart, todayString } from "./reservations/dateUtils";
import { PreviousReservationsSection } from "./reservations/PreviousReservationsSection";
import { ReservationCalendar } from "./reservations/ReservationCalendar";
import { ReservationDayModal } from "./reservations/ReservationDayModal";
import { ReservationDetailsModal } from "./reservations/ReservationDetailsModal";
import { ReservationSearchModal } from "./reservations/ReservationSearchModal";
import { ReservationSettingsModal } from "./reservations/ReservationSettingsModal";
import { ReservationsToolbar } from "./reservations/ReservationsToolbar";
import { TodaysReservationsSection } from "./reservations/TodaysReservationsSection";
import type {
  Reservation,
  ReservationAvailability,
  ReservationCalendarDay,
  ReservationSchedule,
  RestaurantOption,
  RestaurantSettingsDto,
  RestaurantTableDto,
} from "./reservations/types";

export default function Reservations() {
  const { t } = useI18n();
  const [restaurant, setRestaurant] = useState<RestaurantOption | null>(null);
  const [restaurantSettings, setRestaurantSettings] = useState<RestaurantSettingsDto | null>(null);
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [schedules, setSchedules] = useState<ReservationSchedule[]>([]);
  const [todayReservations, setTodayReservations] = useState<Reservation[]>([]);
  const [previousReservations, setPreviousReservations] = useState<Reservation[]>([]);
  const [calendarDays, setCalendarDays] = useState<ReservationCalendarDay[]>([]);
  const [selectedMonth, setSelectedMonth] = useState(monthStart());
  const [selectedDay, setSelectedDay] = useState(todayString());
  const [dayReservations, setDayReservations] = useState<Reservation[]>([]);
  const [availability, setAvailability] = useState<ReservationAvailability | null>(null);
  const [reservationDetails, setReservationDetails] = useState<Reservation | null>(null);
  const [showDayModal, setShowDayModal] = useState(false);
  const [showCallerModal, setShowCallerModal] = useState(false);
  const [showSearchModal, setShowSearchModal] = useState(false);
  const [showSettingsModal, setShowSettingsModal] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [historySearchText, setHistorySearchText] = useState("");
  const [historyFromDate, setHistoryFromDate] = useState("");
  const [historyToDate, setHistoryToDate] = useState("");
  const [appliedHistoryFromDate, setAppliedHistoryFromDate] = useState("");
  const [appliedHistoryToDate, setAppliedHistoryToDate] = useState("");
  const [reservationSearchText, setReservationSearchText] = useState("");
  const [reservationSearchResults, setReservationSearchResults] = useState<Reservation[]>([]);
  const [scheduleIntervalMinutes, setScheduleIntervalMinutes] = useState("15");
  const [selectedTableIds, setSelectedTableIds] = useState<number[]>([]);
  const [guestName, setGuestName] = useState("");
  const [guestEmail, setGuestEmail] = useState("");
  const [guestPhone, setGuestPhone] = useState("");
  const [partySize, setPartySize] = useState("1");
  const [note, setNote] = useState("");
  const [callerDate, setCallerDate] = useState(todayString());
  const [selectedCallerTableId, setSelectedCallerTableId] = useState("");
  const [selectedCallerStartTime, setSelectedCallerStartTime] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const monthCells = useMemo(() => buildMonthCells(selectedMonth), [selectedMonth]);
  const statuses = [
    t("reservations.status.requested", "Requested"),
    t("reservations.status.confirmed", "Confirmed"),
    t("reservations.status.seated", "Seated"),
    t("reservations.status.completed", "Completed"),
    t("reservations.status.cancelled", "Cancelled"),
    t("reservations.status.noShow", "No show"),
  ];
  const calendarCountByDate = useMemo(
    () => Object.fromEntries(calendarDays.map((item) => [item.date.slice(0, 10), item.reservationCount])),
    [calendarDays]
  );

  const filteredTodayReservations = useMemo(
    () =>
      todayReservations.filter((reservation) =>
        matchesTokenizedSearch(
          [
            reservation.id,
            reservation.guestName,
            reservation.guestEmail ?? "",
            reservation.guestPhone ?? "",
            reservation.tableLabel ?? "",
            reservation.note ?? "",
            statuses[reservation.status] ?? reservation.status,
          ].join(" "),
          searchText
        )
      ),
    [searchText, todayReservations]
  );

  const filteredPreviousReservations = useMemo(
    () =>
      previousReservations.filter((reservation) => {
        const reservationDate = new Date(reservation.startAt);
        const fromMatch = !appliedHistoryFromDate || reservationDate >= new Date(`${appliedHistoryFromDate}T00:00:00`);
        const toMatch = !appliedHistoryToDate || reservationDate <= new Date(`${appliedHistoryToDate}T23:59:59`);
        const searchMatch = matchesTokenizedSearch(
          [
            reservation.id,
            reservation.guestName,
            reservation.guestEmail ?? "",
            reservation.guestPhone ?? "",
            reservation.tableLabel ?? "",
            reservation.note ?? "",
            statuses[reservation.status] ?? reservation.status,
            reservation.partySize,
            reservation.startAt,
          ].join(" "),
          historySearchText
        );
        return fromMatch && toMatch && searchMatch;
      }),
    [appliedHistoryFromDate, historySearchText, appliedHistoryToDate, previousReservations]
  );

  const selectedCallerTable = useMemo(
    () => availability?.tables.find((table) => String(table.tableId) === selectedCallerTableId) ?? null,
    [availability, selectedCallerTableId]
  );

  useEffect(() => {
    if (!restaurant) return;
    setSelectedCallerTableId("");
    setSelectedCallerStartTime("");
    loadAvailability(restaurant.id, callerDate).catch((e: any) => {
      setErr(e.message || t("reservations.loadAvailabilityFailed", "Failed to load availability."));
    });
  }, [callerDate]);

  async function loadContext() {
    const result = await api<RestaurantOption>("/api/admin/reservations/context");
    setRestaurant(result);
    return result;
  }

  async function loadTables(restaurantId: number) {
    const result = await api<RestaurantTableDto[]>(`/api/admin/tables?restaurantId=${restaurantId}`);
    setTables(result ?? []);
  }

  async function loadRestaurantSettings(restaurantId: number) {
    const result = await api<RestaurantSettingsDto>(`/api/restaurants/${restaurantId}/settings`);
    setRestaurantSettings(result);
  }

  async function loadSchedules(restaurantId: number) {
    const result = await api<ReservationSchedule[]>(`/api/admin/reservations/schedules?restaurantId=${restaurantId}`);
    const nextSchedules = result ?? [];
    setSchedules(nextSchedules);
    setSelectedTableIds(Array.from(new Set(nextSchedules.map((schedule) => schedule.restaurantTableId))));
    if (nextSchedules[0]) {
      setScheduleIntervalMinutes(String(nextSchedules[0].intervalMinutes));
    }
  }

  async function loadTodayReservations(restaurantId: number) {
    const result = await api<Reservation[]>(`/api/admin/reservations?restaurantId=${restaurantId}&date=${encodeURIComponent(todayString())}`);
    setTodayReservations(result ?? []);
  }

  async function loadPreviousReservations(restaurantId: number) {
    const result = await api<Reservation[]>(`/api/admin/reservations?restaurantId=${restaurantId}&historyOnly=true&take=100`);
    setPreviousReservations(result ?? []);
  }

  async function loadDayReservations(restaurantId: number, date: string) {
    const result = await api<Reservation[]>(`/api/admin/reservations?restaurantId=${restaurantId}&date=${encodeURIComponent(date)}`);
    setDayReservations(result ?? []);
  }

  async function loadCalendar(restaurantId: number, monthDate: Date) {
    const result = await api<ReservationCalendarDay[]>(
      `/api/admin/reservations/calendar?restaurantId=${restaurantId}&year=${monthDate.getFullYear()}&month=${monthDate.getMonth() + 1}`
    );
    setCalendarDays(result ?? []);
  }

  async function loadAvailability(restaurantId: number, date: string) {
    const result = await api<ReservationAvailability>(
      `/api/admin/reservations/availability?restaurantId=${restaurantId}&date=${encodeURIComponent(date)}`
    );
    setAvailability(result);
  }

  async function loadAll() {
    setErr(null);
    try {
      const context = await loadContext();
      await Promise.all([
        loadRestaurantSettings(context.id),
        loadTables(context.id),
        loadSchedules(context.id),
        loadTodayReservations(context.id),
        loadPreviousReservations(context.id),
        loadCalendar(context.id, selectedMonth),
        loadAvailability(context.id, selectedDay),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.loadFailed", "Failed to load reservations"));
    }
  }

  async function saveReservationSettings() {
    if (!restaurant || !restaurantSettings) return;

    const shouldUpdateSchedules =
      schedules.length > 0 &&
      window.confirm(t("reservations.confirmUpdateSchedulesForHours", "Do you want to update the current schedules to match the new reservation hours?"));

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/restaurants/${restaurant.id}/settings`, {
        method: "PUT",
        body: JSON.stringify(restaurantSettings),
      });

      if (shouldUpdateSchedules) {
        const existingTableIds = Array.from(new Set(schedules.map((schedule) => schedule.restaurantTableId)));
        await api(`/api/admin/reservations/schedules?restaurantId=${restaurant.id}`, {
          method: "DELETE",
        });

        if (existingTableIds.length > 0) {
          await api("/api/admin/reservations/schedules", {
            method: "POST",
            body: JSON.stringify({
              restaurantId: restaurant.id,
              tableIds: existingTableIds,
              startTime: minutesToTimeInput(restaurantSettings.reservationStartMinuteOfDay),
              endTime: minutesToTimeInput(restaurantSettings.reservationLastStartMinuteOfDay),
              intervalMinutes: Number(scheduleIntervalMinutes) || 15,
            }),
          });
        }
      }

      await Promise.all([
        loadRestaurantSettings(restaurant.id),
        loadSchedules(restaurant.id),
        loadAvailability(restaurant.id, selectedDay),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.saveSettingsFailed", "Failed to save reservation settings"));
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    void loadAll();
  }, []);

  useEffect(() => {
    if (!restaurant) return;
    void loadCalendar(restaurant.id, selectedMonth);
  }, [restaurant?.id, selectedMonth]);

  useEffect(() => {
    if (!restaurant) return;
    void loadAvailability(restaurant.id, selectedDay);
  }, [restaurant?.id, selectedDay]);

  function toggleTableId(tableId: number) {
    setSelectedTableIds((current) =>
      current.includes(tableId)
        ? current.filter((id) => id !== tableId)
        : [...current, tableId]
    );
  }

  async function createSchedule() {
    if (!restaurant || !restaurantSettings) return;
    if (selectedTableIds.length === 0) {
      setErr(t("reservations.chooseAtLeastOneTable", "Choose at least one table."));
      return;
    }

    if (schedules.length > 0) {
      const confirmed = window.confirm(
        t("reservations.confirmOverwriteSchedule", "Do you want to update the current schedule? Tables you deselected will be removed from the schedule.")
      );
      if (!confirmed) {
        return;
      }
    }

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/reservations/schedules?restaurantId=${restaurant.id}`, {
        method: "DELETE",
      });
      await api("/api/admin/reservations/schedules", {
        method: "POST",
        body: JSON.stringify({
          restaurantId: restaurant.id,
          tableIds: selectedTableIds,
          startTime: minutesToTimeInput(restaurantSettings.reservationStartMinuteOfDay),
          endTime: minutesToTimeInput(restaurantSettings.reservationLastStartMinuteOfDay),
          intervalMinutes: Number(scheduleIntervalMinutes) || 15,
        }),
      });

      await Promise.all([
        loadSchedules(restaurant.id),
        loadAvailability(restaurant.id, selectedDay),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.saveScheduleFailed", "Failed to save reservation schedule"));
    } finally {
      setBusy(false);
    }
  }

  async function clearSchedule() {
    if (!restaurant) return;
    if (!window.confirm(t("reservations.confirmClearSchedule", "Do you want to clear the whole schedule?"))) {
      return;
    }

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/reservations/schedules?restaurantId=${restaurant.id}`, {
        method: "DELETE",
      });
      setSelectedTableIds([]);
      await Promise.all([
        loadSchedules(restaurant.id),
        loadAvailability(restaurant.id, selectedDay),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.clearScheduleFailed", "Failed to clear reservation schedule"));
    } finally {
      setBusy(false);
    }
  }

  async function deleteSchedule(scheduleId: number) {
    if (!window.confirm(t("reservations.confirmDeleteSchedule", "Delete this reservation schedule?"))) return;

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/reservations/schedules/${scheduleId}`, { method: "DELETE" });
      if (restaurant) {
        await Promise.all([
          loadSchedules(restaurant.id),
          loadAvailability(restaurant.id, selectedDay),
        ]);
      }
    } catch (e: any) {
      setErr(e.message || t("reservations.deleteScheduleFailed", "Failed to delete schedule"));
    } finally {
      setBusy(false);
    }
  }

  async function openDayModal(date: string) {
    if (!restaurant) return;
    setSelectedDay(date);
    await loadDayReservations(restaurant.id, date);
    setShowDayModal(true);
  }

  async function openReservationDetails(reservationId: number) {
    try {
      const result = await api<Reservation>(`/api/admin/reservations/${reservationId}`);
      setReservationDetails(result);
    } catch (e: any) {
      setErr(e.message || t("reservations.loadDetailsFailed", "Failed to load reservation details"));
    }
  }

  async function changeReservationStatus(reservationId: number, status: number) {
    if (status === 5 && !window.confirm(t("reservations.confirmNoShow", "Are you sure you want to mark this reservation as no-show?"))) {
      return;
    }

    await api(`/api/admin/reservations/${reservationId}/status?status=${status}`, { method: "PATCH" });
    if (restaurant) {
      await Promise.all([
        loadTodayReservations(restaurant.id),
        loadPreviousReservations(restaurant.id),
        loadDayReservations(restaurant.id, selectedDay),
        loadCalendar(restaurant.id, selectedMonth),
        loadAvailability(restaurant.id, selectedDay),
      ]);
    }
    if (reservationDetails?.id === reservationId) {
      await openReservationDetails(reservationId);
    }
  }

  async function runSearch() {
    if (!restaurant) return;
    const result = await api<Reservation[]>(
      `/api/admin/reservations?restaurantId=${restaurant.id}&search=${encodeURIComponent(reservationSearchText)}&take=200`
    );
    setReservationSearchResults(result ?? []);
  }

  async function createCallerReservation() {
    if (!restaurant) return;
    if (!guestName.trim()) {
      setErr(t("reservations.guestNameRequired", "Guest name is required."));
      return;
    }
    if (!selectedCallerTableId || !selectedCallerStartTime) {
      setErr(t("reservations.chooseTableAndSlot", "Choose table and slot."));
      return;
    }

    setBusy(true);
    setErr(null);
    try {
      const startAt = new Date(`${callerDate}T${selectedCallerStartTime}:00`).toISOString();
      await api("/api/admin/reservations", {
        method: "POST",
        body: JSON.stringify({
          restaurantId: restaurant.id,
          restaurantTableId: Number(selectedCallerTableId),
          guestName: guestName.trim(),
          guestEmail: guestEmail.trim() || null,
          guestPhone: guestPhone.trim() || null,
          partySize: Math.max(1, Number(partySize) || 1),
          startAt,
          note: note.trim() || null,
        }),
      });

      setGuestName("");
      setGuestEmail("");
      setGuestPhone("");
      setPartySize("1");
      setNote("");
      setCallerDate(selectedDay);
      setSelectedCallerTableId("");
      setSelectedCallerStartTime("");
      setShowCallerModal(false);

      await Promise.all([
        loadTodayReservations(restaurant.id),
        loadPreviousReservations(restaurant.id),
        loadDayReservations(restaurant.id, selectedDay),
        loadCalendar(restaurant.id, selectedMonth),
        loadAvailability(restaurant.id, selectedDay),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.createFailed", "Failed to create reservation"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <PageShell title={t("nav.reservations", "Reservations")} error={err} maxWidth={1200}>
      <div style={{ display: "grid", gap: 16, marginBottom: 20 }}>
        <ReservationsToolbar
          selectedMonth={selectedMonth}
          busy={busy}
          onOpenSettings={() => setShowSettingsModal(true)}
          onOpenCaller={() => setShowCallerModal(true)}
          onOpenSearch={() => setShowSearchModal(true)}
          onPreviousMonth={() => setSelectedMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}
          onNextMonth={() => setSelectedMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}
          t={t}
        />
        <ReservationCalendar
          selectedMonth={selectedMonth}
          monthCells={monthCells}
          calendarCountByDate={calendarCountByDate}
          onOpenDay={(date) => void openDayModal(date)}
          t={t}
        />
      </div>

      <TodaysReservationsSection
        reservations={filteredTodayReservations}
        statuses={statuses}
        searchText={searchText}
        onSearchTextChange={setSearchText}
        onResetSearch={() => setSearchText("")}
        onOpenReservation={(reservationId) => void openReservationDetails(reservationId)}
        t={t}
      />

      <PreviousReservationsSection
        reservations={filteredPreviousReservations}
        statuses={statuses}
        searchText={historySearchText}
        fromDate={historyFromDate}
        toDate={historyToDate}
        onSearchTextChange={setHistorySearchText}
        onFromDateChange={setHistoryFromDate}
        onToDateChange={setHistoryToDate}
        onApplyFilters={() => {
          setAppliedHistoryFromDate(historyFromDate);
          setAppliedHistoryToDate(historyToDate);
        }}
        onResetFilters={() => {
          setHistorySearchText("");
          setHistoryFromDate("");
          setHistoryToDate("");
          setAppliedHistoryFromDate("");
          setAppliedHistoryToDate("");
        }}
        onOpenReservation={(reservationId) => void openReservationDetails(reservationId)}
        t={t}
      />

      {showSettingsModal && (
        <ReservationSettingsModal
          restaurantSettings={restaurantSettings}
          tables={tables}
          schedules={schedules}
          selectedTableIds={selectedTableIds}
          scheduleIntervalMinutes={scheduleIntervalMinutes}
          busy={busy}
          onClose={() => setShowSettingsModal(false)}
          onSettingsChange={setRestaurantSettings}
          onScheduleIntervalMinutesChange={setScheduleIntervalMinutes}
          onToggleTableId={toggleTableId}
          onCreateSchedule={() => void createSchedule()}
          onClearSchedule={() => void clearSchedule()}
          onSaveReservationSettings={() => void saveReservationSettings()}
          onDeleteSchedule={(scheduleId) => void deleteSchedule(scheduleId)}
          t={t}
        />
      )}

      {showDayModal && (
        <ReservationDayModal
          selectedDay={selectedDay}
          reservations={dayReservations}
          statuses={statuses}
          onClose={() => setShowDayModal(false)}
          onOpenReservation={(reservationId) => void openReservationDetails(reservationId)}
          t={t}
        />
      )}

      {reservationDetails && (
        <ReservationDetailsModal
          reservation={reservationDetails}
          statuses={statuses}
          onClose={() => setReservationDetails(null)}
          onChangeStatus={(reservationId, status) => void changeReservationStatus(reservationId, status)}
          t={t}
        />
      )}

      {showSearchModal && (
        <ReservationSearchModal
          searchText={reservationSearchText}
          results={reservationSearchResults}
          statuses={statuses}
          onClose={() => setShowSearchModal(false)}
          onSearchTextChange={setReservationSearchText}
          onRunSearch={() => void runSearch()}
          onOpenReservation={(reservationId) => {
            setShowSearchModal(false);
            void openReservationDetails(reservationId);
          }}
          t={t}
        />
      )}

      {showCallerModal && (
        <CallerReservationModal
          callerDate={callerDate}
          guestName={guestName}
          guestEmail={guestEmail}
          guestPhone={guestPhone}
          partySize={partySize}
          note={note}
          selectedTableId={selectedCallerTableId}
          selectedStartTime={selectedCallerStartTime}
          tables={availability?.tables ?? []}
          selectedTable={selectedCallerTable}
          busy={busy}
          onClose={() => setShowCallerModal(false)}
          onCallerDateChange={(value) => {
            setCallerDate(value);
            setSelectedCallerTableId("");
            setSelectedCallerStartTime("");
          }}
          onGuestNameChange={setGuestName}
          onGuestEmailChange={setGuestEmail}
          onGuestPhoneChange={setGuestPhone}
          onPartySizeChange={setPartySize}
          onNoteChange={setNote}
          onTableIdChange={(value) => {
            setSelectedCallerTableId(value);
            setSelectedCallerStartTime("");
          }}
          onStartTimeChange={setSelectedCallerStartTime}
          onCreateReservation={() => void createCallerReservation()}
          t={t}
        />
      )}
    </PageShell>
  );
}
