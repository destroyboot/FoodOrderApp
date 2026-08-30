import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";

type RestaurantDto = {
  id: number;
  name: string;
};

type RestaurantSettingsDto = {
  restaurantId: number;
  enableReservations: boolean;
};

type ReservationBlockedRangeDto = {
  startTime: string;
  endTime: string;
  label: string;
};

type ReservationTableAvailabilityDto = {
  tableId: number;
  label: string;
  seats?: number | null;
  availableStartTimes: string[];
  blockedRanges: ReservationBlockedRangeDto[];
};

type ReservationAvailabilityDto = {
  restaurantId: number;
  date: string;
  slotMinutes: number;
  tables: ReservationTableAvailabilityDto[];
};

type ReservationDto = {
  id: number;
  restaurantId: number;
  restaurantTableId?: number | null;
  tableLabel?: string | null;
  guestName: string;
  guestEmail?: string | null;
  startAt: string;
  endAt: string;
  status: number;
  cancelledAt?: string | null;
  releasedAt?: string | null;
  note?: string | null;
};

function toDateInputValue(date: Date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function buildUtcIso(dateText: string, timeText: string) {
  return new Date(`${dateText}T${timeText}:00`).toISOString();
}

export default function MyReservations() {
  const { t } = useI18n();
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState("");
  const [date, setDate] = useState(toDateInputValue(new Date()));
  const [availability, setAvailability] = useState<ReservationAvailabilityDto | null>(null);
  const [myReservations, setMyReservations] = useState<ReservationDto[]>([]);
  const [selectedTableId, setSelectedTableId] = useState("");
  const [selectedTime, setSelectedTime] = useState("");
  const [note, setNote] = useState("");
  const [canReserve, setCanReserve] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const selectedTable = useMemo(
    () => availability?.tables.find((table) => String(table.tableId) === selectedTableId) ?? null,
    [availability, selectedTableId]
  );
  const statuses = [
    t("reservations.status.requested", "Requested"),
    t("reservations.status.confirmed", "Confirmed"),
    t("reservations.status.seated", "Seated"),
    t("reservations.status.completed", "Completed"),
    t("reservations.status.cancelled", "Cancelled"),
    t("reservations.status.noShow", "No show"),
  ];

  async function loadRestaurants() {
    const result = await api<RestaurantDto[]>("/api/restaurants");
    setRestaurants(result ?? []);
    return result ?? [];
  }

  async function loadAvailability(restaurantId: string, nextDate = date) {
    if (!restaurantId) {
      setAvailability(null);
      setCanReserve(false);
      return;
    }

    const settings = await api<RestaurantSettingsDto>(`/api/restaurants/${restaurantId}/settings`);
    setCanReserve(settings.enableReservations);

    if (!settings.enableReservations) {
      setAvailability({ restaurantId: Number(restaurantId), date: nextDate, slotMinutes: 15, tables: [] });
      return;
    }

    const result = await api<ReservationAvailabilityDto>(
      `/api/reservations/availability?restaurantId=${restaurantId}&date=${encodeURIComponent(nextDate)}`
    );

    setAvailability(result);
  }

  async function loadMine() {
    const result = await api<ReservationDto[]>("/api/reservations/mine");
    setMyReservations(result ?? []);
  }

  async function init() {
    setLoading(true);
    setErr(null);

    try {
      const restaurantList = await loadRestaurants();
      const nextRestaurantId = selectedRestaurantId || String(restaurantList[0]?.id ?? "");
      setSelectedRestaurantId(nextRestaurantId);

      await Promise.all([
        loadMine(),
        nextRestaurantId ? loadAvailability(nextRestaurantId, date) : Promise.resolve(),
      ]);
    } catch (e: any) {
      setErr(e.message || t("reservations.loadFailed", "Failed to load reservations."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    init();
  }, []);

  async function refreshAvailability(nextRestaurantId = selectedRestaurantId, nextDate = date) {
    setErr(null);
    setSelectedTableId("");
    setSelectedTime("");

    try {
      await loadAvailability(nextRestaurantId, nextDate);
    } catch (e: any) {
      setErr(e.message || t("reservations.availabilityLoadFailed", "Failed to load availability."));
    }
  }

  async function createReservation() {
    if (!selectedRestaurantId || !selectedTableId || !selectedTime) {
      setErr(t("myReservations.selectRestaurantTableTime", "Select a restaurant, table and time first."));
      return;
    }

    if (!confirm(t("myReservations.confirmCreate", "Are you sure you want to make this reservation?"))) {
      return;
    }

    setErr(null);
    try {
      await api("/api/reservations", {
        method: "POST",
        body: JSON.stringify({
          restaurantId: Number(selectedRestaurantId),
          restaurantTableId: Number(selectedTableId),
          startAt: buildUtcIso(date, selectedTime),
          note: note.trim() || null,
        }),
      });

      setNote("");
      await Promise.all([
        refreshAvailability(selectedRestaurantId, date),
        loadMine(),
      ]);
    } catch (e: any) {
      setErr(e.message || t("myReservations.createFailed", "Failed to create reservation."));
    }
  }

  async function cancelReservation(id: number) {
    if (!confirm(t("myReservations.confirmCancel", "Cancel this reservation?"))) {
      return;
    }

    setErr(null);
    try {
      await api(`/api/reservations/${id}/cancel`, { method: "PATCH" });
      await Promise.all([
        loadMine(),
        selectedRestaurantId ? refreshAvailability(selectedRestaurantId, date) : Promise.resolve(),
      ]);
    } catch (e: any) {
      setErr(e.message || t("myReservations.cancelFailed", "Failed to cancel reservation."));
    }
  }

  return (
    <div style={{ maxWidth: 1100, margin: "20px auto", fontFamily: "Arial" }}>
      <h2>{t("nav.myReservations", "My Reservations")}</h2>

      {err && <div className="alert-error">{err}</div>}
      {loading && <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div>}

      <section style={{ marginBottom: 32 }}>
        <h3>{t("myReservations.makeReservation", "Make a reservation")}</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 180px auto", gap: 8, alignItems: "end" }}>
          <label>
            {t("nav.restaurants", "Restaurants")}
            <select
              value={selectedRestaurantId}
              onChange={(e) => {
                const value = e.target.value;
                setSelectedRestaurantId(value);
                refreshAvailability(value, date);
              }}
              style={{ width: "100%" }}
            >
              <option value="">{t("common.selectRestaurantOption", "-- select restaurant --")}</option>
              {restaurants.map((restaurant) => (
                <option key={restaurant.id} value={restaurant.id}>
                  {restaurant.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            {t("common.date", "Date")}
            <input
              type="date"
              value={date}
              onChange={(e) => {
                const value = e.target.value;
                setDate(value);
                refreshAvailability(selectedRestaurantId, value);
              }}
              style={{ width: "100%" }}
            />
          </label>

          <button onClick={() => refreshAvailability(selectedRestaurantId, date)}>
            {t("reservations.refreshAvailability", "Refresh availability")}
          </button>
        </div>

        {!canReserve && selectedRestaurantId && (
          <div style={{ marginTop: 12 }}>{t("myReservations.disabledForRestaurant", "Reservations are disabled for this restaurant.")}</div>
        )}

        {availability && canReserve && (
          <div style={{ marginTop: 16 }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
              <div>
                <label>
                  {t("nav.tables", "Tables")}
                  <select
                    value={selectedTableId}
                    onChange={(e) => {
                      setSelectedTableId(e.target.value);
                      setSelectedTime("");
                    }}
                    style={{ width: "100%" }}
                  >
                    <option value="">{t("common.selectTableOption", "-- select table --")}</option>
                    {availability.tables.map((table) => (
                      <option key={table.tableId} value={table.tableId}>
                        {table.label}
                        {table.seats ? ` (${table.seats} ${t("common.seats", "seats")})` : ""}
                      </option>
                    ))}
                  </select>
                </label>

                <label style={{ display: "block", marginTop: 12 }}>
                  {t("reservations.startTime", "Start time")}
                  <select
                    value={selectedTime}
                    onChange={(e) => setSelectedTime(e.target.value)}
                    style={{ width: "100%" }}
                    disabled={!selectedTable}
                  >
                    <option value="">{t("common.selectTimeOption", "-- select time --")}</option>
                    {(selectedTable?.availableStartTimes ?? []).map((time) => (
                      <option key={time} value={time}>
                        {time}
                      </option>
                    ))}
                  </select>
                </label>

                <label style={{ display: "block", marginTop: 12 }}>
                  {t("myReservations.noteForRestaurant", "Note for restaurant")}
                  <textarea
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                    rows={3}
                    style={{ width: "100%" }}
                  />
                </label>

                <button onClick={createReservation} style={{ marginTop: 12 }}>
                  {t("myReservations.makeReservation", "Make a reservation")}
                </button>
              </div>

              <div>
                <h4>{t("reservations.tableAvailability", "Table availability")}</h4>
                {availability.tables.map((table) => (
                  <div key={table.tableId} style={{ marginBottom: 14, border: "1px solid #ddd", padding: 10 }}>
                    <div>
                      <strong>{table.label}</strong>
                      {table.seats ? ` (${table.seats} ${t("common.seats", "seats")})` : ""}
                    </div>
                    <div>
                      {t("reservations.availableStarts", "Available starts")}: {table.availableStartTimes.length > 0 ? table.availableStartTimes.join(", ") : t("common.none", "none")}
                    </div>
                    <div>
                      {t("reservations.reservedBlocks", "Reserved blocks")}: {table.blockedRanges.length > 0
                        ? table.blockedRanges.map((range) => `${range.startTime}-${range.endTime}`).join(", ")
                        : t("common.none", "none")}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </section>

      <section>
        <h3>{t("myReservations.history", "My reservation history")}</h3>
        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("nav.tables", "Tables")}</th>
              <th align="left">{t("common.time", "Time")}</th>
              <th align="left">{t("orders.status", "Status")}</th>
              <th align="left">{t("common.note", "Note")}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {myReservations.map((reservation) => {
              const cancellable = reservation.status === 0 || reservation.status === 1;
              return (
                <tr key={reservation.id}>
                  <td>{reservation.tableLabel ?? "-"}</td>
                  <td>
                    {new Date(reservation.startAt).toLocaleString()} - {new Date(reservation.endAt).toLocaleTimeString()}
                  </td>
                  <td>{statuses[reservation.status] ?? reservation.status}</td>
                  <td>{reservation.note ?? "-"}</td>
                  <td>
                    {cancellable && (
                      <button onClick={() => cancelReservation(reservation.id)}>
                        {t("common.cancel", "Cancel")}
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </section>
    </div>
  );
}
