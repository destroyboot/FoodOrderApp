import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { matchesTokenizedSearch } from "../tokenSearch";
import { useI18n } from "../i18n";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";

type RestaurantOption = {
  id: number;
  name: string;
};

type RestaurantTableDto = {
  id: number;
  restaurantId: number;
  label: string;
  seats?: number | null;
  isActive: boolean;
  isReservable: boolean;
  sortOrder: number;
};

type TableFormState = {
  id?: number;
  label: string;
  seats: string;
  sortOrder: string;
  isActive: boolean;
  isReservable: boolean;
};

const emptyForm = (): TableFormState => ({
  label: "",
  seats: "",
  sortOrder: "0",
  isActive: true,
  isReservable: true,
});

export default function Tables() {
  const { t } = useI18n();
  const [restaurant, setRestaurant] = useState<RestaurantOption | null>(null);
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [form, setForm] = useState<TableFormState>(emptyForm());
  const [searchText, setSearchText] = useState("");
  const [seatMin, setSeatMin] = useState("");
  const [seatMax, setSeatMax] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [reservableFilter, setReservableFilter] = useState("all");
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showModal, setShowModal] = useState(false);

  async function loadAll() {
    setErr(null);
    try {
      const context = await api<RestaurantOption>("/api/admin/tables/restaurants");
      const resolved = Array.isArray(context) ? context[0] : context;
      if (!resolved) {
        setRestaurant(null);
        setTables([]);
        return;
      }

      setRestaurant(resolved);
      const result = await api<RestaurantTableDto[]>(
        `/api/admin/tables?restaurantId=${encodeURIComponent(String(resolved.id))}`
      );
      setTables(result ?? []);
    } catch (e: any) {
      setErr(e.message || t("tables.loadFailed", "Failed to load tables"));
    }
  }

  useEffect(() => {
    void loadAll();
  }, []);

  const filteredTables = useMemo(
    () =>
      tables.filter((table) => {
        const matchesSearch = matchesTokenizedSearch(
          [
            table.label,
            table.seats ?? "",
            table.sortOrder,
            table.isActive ? "active" : "inactive",
            table.isReservable ? "reservable" : "not reservable",
          ].join(" "),
          searchText
        );
        const matchesSeatMin = !seatMin || (table.seats ?? 0) >= Number(seatMin);
        const matchesSeatMax = !seatMax || (table.seats ?? 0) <= Number(seatMax);
        const matchesActive = activeFilter === "all" || String(table.isActive) === activeFilter;
        const matchesReservable = reservableFilter === "all" || String(table.isReservable) === reservableFilter;
        return matchesSearch && matchesSeatMin && matchesSeatMax && matchesActive && matchesReservable;
      }),
    [searchText, tables, seatMin, seatMax, activeFilter, reservableFilter]
  );

  function editTable(table: RestaurantTableDto) {
    setForm({
      id: table.id,
      label: table.label,
      seats: table.seats?.toString() ?? "",
      sortOrder: table.sortOrder.toString(),
      isActive: table.isActive,
      isReservable: table.isReservable,
    });
    setShowModal(true);
  }

  function resetForm() {
    setForm(emptyForm());
  }

  async function saveTable() {
    if (!restaurant) {
      setErr(t("tables.noContext", "No restaurant context available."));
      return;
    }

    const label = form.label.trim();
    if (!label) {
      setErr(t("restaurant.tableRequired", "Table label is required."));
      return;
    }

    setBusy(true);
    setErr(null);
    try {
      const body = JSON.stringify({
        id: form.id ?? 0,
        restaurantId: restaurant.id,
        label,
        seats: form.seats.trim() ? Number(form.seats) : null,
        sortOrder: Number(form.sortOrder) || 0,
        isActive: form.isActive,
        isReservable: form.isReservable,
      });

      if (form.id) {
        await api(`/api/admin/tables/${form.id}`, { method: "PUT", body });
      } else {
        await api("/api/admin/tables", { method: "POST", body });
      }

      resetForm();
      setShowModal(false);
      await loadAll();
    } catch (e: any) {
      setErr(e.message || t("tables.saveFailed", "Failed to save table"));
    } finally {
      setBusy(false);
    }
  }

  async function deleteTable(tableId: number) {
    if (!restaurant) return;
    if (!window.confirm(t("restaurant.deleteTableConfirm", "Delete this table?"))) return;

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/tables/${tableId}?restaurantId=${encodeURIComponent(String(restaurant.id))}`, {
        method: "DELETE",
      });
      if (form.id === tableId) {
        resetForm();
      }
      await loadAll();
    } catch (e: any) {
      setErr(e.message || t("tables.deleteFailed", "Failed to delete table"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <PageShell title={t("nav.tables", "Tables")} error={err} maxWidth={1100}>
      <div style={{ display: "grid", gap: 16 }}>
        <div style={{ display: "flex", justifyContent: "space-between", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
          <button type="button" onClick={() => { resetForm(); setShowModal(true); }}>{t("tables.create", "Create Table")}</button>
          <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8, minWidth: 420 }}>
            <input
              placeholder={t("tables.search", "Search tables")}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              style={{ width: "100%" }}
            />
            <button onClick={() => { setSearchText(""); setSeatMin(""); setSeatMax(""); setActiveFilter("all"); setReservableFilter("all"); }}>
              {t("common.resetFilters", "Reset Filters")}
            </button>
          </div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 8 }}>
            <input placeholder={t("ui.seatsFrom", "Seats from")} value={seatMin} onChange={(e) => setSeatMin(e.target.value)} />
            <input placeholder={t("ui.seatsTo", "Seats to")} value={seatMax} onChange={(e) => setSeatMax(e.target.value)} />
            <select value={activeFilter} onChange={(e) => setActiveFilter(e.target.value)}>
              <option value="all">{t("ui.allActivity", "All activity")}</option>
              <option value="true">{t("common.active", "Active")}</option>
              <option value="false">{t("common.inactive", "Inactive")}</option>
            </select>
            <select value={reservableFilter} onChange={(e) => setReservableFilter(e.target.value)}>
              <option value="all">{t("ui.allReservableStates", "All reservable states")}</option>
              <option value="true">{t("restaurant.reservable", "Reservable")}</option>
              <option value="false">{t("ui.notReservable", "Not reservable")}</option>
            </select>
        </div>

          <table width="100%" cellPadding={8}>
            <thead>
              <tr>
                <th align="left">{t("restaurant.tableLabel", "Label")}</th>
                <th align="left">{t("restaurant.tableSeats", "Seats")}</th>
                <th align="left">{t("restaurant.tableSort", "Sort")}</th>
                <th align="left">{t("common.active", "Active")}</th>
                <th align="left">{t("restaurant.reservable", "Reservable")}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {filteredTables.map((table) => (
                <tr key={table.id}>
                  <td>{table.label}</td>
                  <td>{table.seats ?? "-"}</td>
                  <td>{table.sortOrder}</td>
                  <td>{table.isActive ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                  <td>{table.isReservable ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                  <td style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
                    <button type="button" onClick={() => editTable(table)}>{t("common.edit", "Edit")}</button>
                    <button type="button" onClick={() => void deleteTable(table.id)} disabled={busy} className="button-danger">{t("common.delete", "Delete")}</button>
                  </td>
                </tr>
              ))}
              {filteredTables.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ color: "#666", padding: 16 }}>
                    {t("tables.none", "No tables match this search.")}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
      </div>

      {showModal ? (
        <ModalShell
          title={form.id ? t("tables.edit", "Edit Table") : t("tables.new", "New Table")}
          onClose={() => {
            setShowModal(false);
            resetForm();
          }}
          minWidth="min(520px, calc(100vw - 32px))"
          maxWidth={720}
        >
          <div style={{ display: "grid", gap: 10 }}>
            <label>
              <div style={{ marginBottom: 4 }}>{t("restaurant.tableLabel", "Label")}</div>
              <input value={form.label} onChange={(e) => setForm((current) => ({ ...current, label: e.target.value }))} />
            </label>
            <div style={{ display: "grid", gap: 10, gridTemplateColumns: "1fr 1fr" }}>
              <label>
                <div style={{ marginBottom: 4 }}>{t("restaurant.tableSeats", "Seats")}</div>
                <input type="number" min={0} value={form.seats} onChange={(e) => setForm((current) => ({ ...current, seats: e.target.value }))} />
              </label>
              <label>
                <div style={{ marginBottom: 4 }}>{t("tables.sortOrder", "Sort Order")}</div>
                <input type="number" value={form.sortOrder} onChange={(e) => setForm((current) => ({ ...current, sortOrder: e.target.value }))} />
              </label>
            </div>
            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm((current) => ({ ...current, isActive: e.target.checked }))} />
              {t("common.active", "Active")}
            </label>
            <label style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <input type="checkbox" checked={form.isReservable} onChange={(e) => setForm((current) => ({ ...current, isReservable: e.target.checked }))} />
              {t("restaurant.reservable", "Reservable")}
            </label>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button onClick={() => void saveTable()} disabled={busy || !restaurant}>
                {busy ? t("tables.working", "Working...") : form.id ? t("restaurant.saveTable", "Save Table") : t("tables.create", "Create Table")}
              </button>
              <button type="button" onClick={() => { setShowModal(false); resetForm(); }} disabled={busy}>
                {t("common.cancel", "Cancel")}
              </button>
            </div>
          </div>
        </ModalShell>
      ) : null}
    </PageShell>
  );
}
