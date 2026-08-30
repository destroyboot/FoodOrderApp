import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { getUserRoles } from "../auth";
import { useI18n } from "../i18n";
import { PageShell } from "../Components/PageShell";
import { CuisineModal } from "./restaurants/CuisineModal";
import { defaultSettings, emptyRestaurantForm, emptyTableForm, restaurantRoles } from "./restaurants/defaults";
import { RestaurantDetailsSection } from "./restaurants/RestaurantDetailsSection";
import { RestaurantSelector } from "./restaurants/RestaurantSelector";
import { TableModal } from "./restaurants/TableModal";
import { minutesToTimeInput, timeInputToMinutes } from "./restaurants/timeUtils";
import type {
  AppLanguageDto,
  AppLanguagePayload,
  CuisineDto,
  RestaurantDto,
  RestaurantFormState,
  RestaurantSettingsDto,
  RestaurantTableDto,
  RestaurantUserRoleDto,
  SettingsTab,
  TableFormState,
} from "./restaurants/types";

function SettingsTabButton({
  active,
  label,
  onClick,
}: {
  active: boolean;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        borderRadius: 6,
        border: active ? "1px solid #111827" : "1px solid #d1d5db",
        background: active ? "#111827" : "#fff",
        color: active ? "#fff" : "#111827",
        padding: "8px 12px",
      }}
    >
      {label}
    </button>
  );
}

export default function Restaurants() {
  const { t, culture } = useI18n();
  const roles = useMemo(() => getUserRoles(), []);
  const isMainAdmin = roles.includes("Admin");
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [tables, setTables] = useState<RestaurantTableDto[]>([]);
  const [assignments, setAssignments] = useState<RestaurantUserRoleDto[]>([]);
  const [cuisineOptions, setCuisineOptions] = useState<CuisineDto[]>([]);
  const [appLanguages, setAppLanguages] = useState<AppLanguageDto[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState(0);
  const [restaurantForm, setRestaurantForm] = useState<RestaurantFormState>(emptyRestaurantForm());
  const [settingsForm, setSettingsForm] = useState<RestaurantSettingsDto>(defaultSettings());
  const [tableForm, setTableForm] = useState<TableFormState>(emptyTableForm());
  const [inviteEmail, setInviteEmail] = useState("");
  const [selectedAwaitingUserId, setSelectedAwaitingUserId] = useState("");
  const [selectedRole, setSelectedRole] = useState("Waiter");

  function roleLabel(role: string) {
    return t(`roles.${role}`, role);
  }
  const [activeTab, setActiveTab] = useState<SettingsTab>("payment");
  const [showCuisineModal, setShowCuisineModal] = useState(false);
  const [showTableModal, setShowTableModal] = useState(false);
  const [editingDetails, setEditingDetails] = useState(false);
  const [cuisineSearch, setCuisineSearch] = useState("");
  const [selectedCuisineToAdd, setSelectedCuisineToAdd] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const canEditName = isMainAdmin;

  const awaitingAssignmentOptions = useMemo(
    () => assignments.filter((item) => item.isAwaitingAssignment && !item.userId.startsWith("invite:")),
    [assignments]
  );

  const filteredCuisineOptions = useMemo(() => {
    const normalized = cuisineSearch.trim().toLowerCase();
    return cuisineOptions.filter((option) => {
      if (restaurantForm.cuisineTypes.includes(option.code)) {
        return false;
      }

      if (!normalized) {
        return true;
      }

      return normalized.split(/\s+/).every((token) =>
        option.code.toLowerCase().includes(token) || option.name.toLowerCase().includes(token)
      );
    });
  }, [cuisineOptions, cuisineSearch, restaurantForm.cuisineTypes]);

  const cuisineNameMap = useMemo(
    () => Object.fromEntries(cuisineOptions.map((option) => [option.code, option.name])),
    [cuisineOptions]
  );

  async function load(nextRestaurantId = selectedRestaurantId) {
    setLoading(true);
    setErr(null);

    try {
      const [restaurantList, cuisines, languagePayload] = await Promise.all([
        api<RestaurantDto[]>(`/api/admin/restaurants?culture=${encodeURIComponent(culture)}`),
        api<CuisineDto[]>(`/api/admin/restaurants/cuisines?culture=${encodeURIComponent(culture)}`),
        api<AppLanguagePayload>("/api/platform/languages"),
      ]);
      setRestaurants(restaurantList ?? []);
      setCuisineOptions(cuisines ?? []);
      setAppLanguages(languagePayload?.languages ?? []);

      let restaurantId = nextRestaurantId;
      if (!isMainAdmin && restaurantList.length > 0) {
        restaurantId = restaurantList[0].id;
        setSelectedRestaurantId(restaurantId);
      } else if (!restaurantId && restaurantList.length > 0) {
        restaurantId = restaurantList[0].id;
        setSelectedRestaurantId(restaurantId);
      }

      if (restaurantId) {
        const selected = restaurantList.find((r) => r.id === restaurantId);
        setRestaurantForm(
          selected
            ? {
                id: selected.id,
                name: selected.name,
                city: selected.city ?? "",
                street: selected.street ?? "",
                postalCode: selected.postalCode ?? "",
                houseNumber: selected.houseNumber ?? "",
                cuisineTypes: selected.cuisineTypes ?? [],
                isActive: selected.isActive,
              }
            : emptyRestaurantForm()
        );

        const [settings, tableList, assignmentList] = await Promise.all([
          api<RestaurantSettingsDto>(`/api/admin/restaurants/${restaurantId}/settings`),
          api<RestaurantTableDto[]>(`/api/admin/restaurants/${restaurantId}/tables`),
          api<RestaurantUserRoleDto[]>(`/api/admin/restaurants/${restaurantId}/users`),
        ]);

        setSettingsForm(settings ?? defaultSettings(restaurantId));
        setTables(tableList ?? []);
        setAssignments(assignmentList ?? []);
        setEditingDetails(false);
      } else {
        setRestaurantForm(emptyRestaurantForm());
        setSettingsForm(defaultSettings());
        setTables([]);
        setAssignments([]);
        setEditingDetails(false);
      }
    } catch (e: any) {
      setErr(e.message || t("restaurant.loadFailed", "Failed to load restaurants"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [culture]);

  function setSuccess(message: string) {
    setInfo(message);
    setErr(null);
  }

  async function saveRestaurant() {
    if (!isMainAdmin && !restaurantForm.id) {
      setErr(t("restaurant.onlyMainAdminCanCreate", "Only application admins can create restaurants."));
      return;
    }

    const name = restaurantForm.name.trim();
    if (!name) {
      setErr(t("restaurant.nameRequired", "Restaurant name is required."));
      return;
    }

    if (restaurantForm.cuisineTypes.length > 5) {
      setErr(t("restaurant.cuisineLimit", "Choose up to 5 cuisine types."));
      return;
    }

    const body = JSON.stringify({
      name,
      city: restaurantForm.city.trim() || null,
      street: restaurantForm.street.trim() || null,
      postalCode: restaurantForm.postalCode.trim() || null,
      houseNumber: restaurantForm.houseNumber.trim() || null,
      cuisineTypes: restaurantForm.cuisineTypes,
      isActive: restaurantForm.isActive,
    });

    if (restaurantForm.id) {
      await api(`/api/admin/restaurants/${restaurantForm.id}`, { method: "PUT", body });
      await load(restaurantForm.id);
      setEditingDetails(false);
      setSuccess(t("restaurant.saved", "Restaurant details saved."));
      return;
    }

    const created = await api<RestaurantDto>("/api/admin/restaurants", { method: "POST", body });
    setSelectedRestaurantId(created.id);
    await load(created.id);
    setEditingDetails(false);
    setSuccess(t("restaurant.created", "Restaurant created."));
  }

  function resetNewRestaurant() {
    if (!isMainAdmin) return;
    setSelectedRestaurantId(0);
    setRestaurantForm(emptyRestaurantForm());
    setSettingsForm(defaultSettings());
    setTables([]);
    setAssignments([]);
    setInfo(null);
    setErr(null);
    setEditingDetails(false);
  }

  async function saveSettings() {
    if (!selectedRestaurantId) {
      setErr(t("restaurant.selectFirst", "Select a restaurant first."));
      return;
    }

    await api(`/api/admin/restaurants/${selectedRestaurantId}/settings`, {
      method: "PUT",
      body: JSON.stringify(settingsForm),
    });

    await load(selectedRestaurantId);
    setSuccess(t("restaurant.settingsSaved", "Restaurant settings saved."));
  }

  async function saveTable() {
    if (!selectedRestaurantId) {
      setErr(t("restaurant.selectFirst", "Select a restaurant first."));
      return;
    }

    const label = tableForm.label.trim();
    if (!label) {
      setErr(t("restaurant.tableRequired", "Table label is required."));
      return;
    }

    const body = JSON.stringify({
      label,
      seats: tableForm.seats ? Number(tableForm.seats) : null,
      sortOrder: Number(tableForm.sortOrder),
      isActive: tableForm.isActive,
      isReservable: tableForm.isReservable,
    });

    if (tableForm.id) {
      await api(`/api/admin/restaurants/${selectedRestaurantId}/tables/${tableForm.id}`, { method: "PUT", body });
      setSuccess(t("restaurant.tableSaved", "Table saved."));
    } else {
      await api(`/api/admin/restaurants/${selectedRestaurantId}/tables`, { method: "POST", body });
      setSuccess(t("restaurant.tableAdded", "Table added."));
    }

    setTableForm(emptyTableForm());
    setShowTableModal(false);
    await load(selectedRestaurantId);
  }

  async function removeTable(tableId: number) {
    if (!selectedRestaurantId || !confirm(t("restaurant.deleteTableConfirm", "Delete table?"))) return;
    await api(`/api/admin/restaurants/${selectedRestaurantId}/tables/${tableId}`, { method: "DELETE" });
    await load(selectedRestaurantId);
    setSuccess(t("restaurant.tableDeleted", "Table deleted."));
  }

  async function inviteUser() {
    if (!selectedRestaurantId) {
      setErr(t("restaurant.selectFirst", "Select a restaurant first."));
      return;
    }

    if (!inviteEmail.trim()) {
      setErr(t("restaurant.emailRequired", "Email is required."));
      return;
    }

    await api(`/api/admin/restaurants/${selectedRestaurantId}/invites`, {
      method: "POST",
      body: JSON.stringify({
        email: inviteEmail.trim(),
      }),
    });

    setInviteEmail("");
    await load(selectedRestaurantId);
    setSuccess(t("restaurant.inviteSent", "Invitation sent."));
  }

  async function assignAwaitingUser() {
    if (!selectedRestaurantId || !selectedAwaitingUserId) {
      setErr(t("restaurant.chooseAwaitingUser", "Choose a user waiting for assignment."));
      return;
    }

    await api(`/api/admin/restaurants/${selectedRestaurantId}/users`, {
      method: "POST",
      body: JSON.stringify({
        userId: selectedAwaitingUserId,
        role: selectedRole,
      }),
    });

    setSelectedAwaitingUserId("");
    await load(selectedRestaurantId);
    setSuccess(t("restaurant.roleAssigned", "Role assigned."));
  }

  async function removeAssignment(assignment: RestaurantUserRoleDto) {
    if (!selectedRestaurantId) return;
    if (!confirm(assignment.isPendingInvite || assignment.isAwaitingAssignment
      ? t("restaurant.removeInviteConfirm", "Remove this invite?")
      : t("restaurant.removeRoleConfirm", "Remove role assignment?"))) return;

    if (assignment.inviteId) {
      await api(`/api/admin/restaurants/${selectedRestaurantId}/invites/${assignment.inviteId}`, { method: "DELETE" });
    } else {
      await api(`/api/admin/restaurants/${selectedRestaurantId}/users/${assignment.id}`, { method: "DELETE" });
    }

    await load(selectedRestaurantId);
    setSuccess(assignment.inviteId
      ? t("restaurant.inviteRemoved", "Invite removed.")
      : t("restaurant.roleRemoved", "Role removed."));
  }

  function addCuisineSelection() {
    if (!selectedCuisineToAdd) {
      setErr(t("restaurant.chooseCuisineFirst", "Choose a cuisine first."));
      return;
    }

    if (restaurantForm.cuisineTypes.length >= 5) {
      setErr(t("restaurant.cuisineLimit", "Choose up to 5 cuisine types."));
      return;
    }

    setRestaurantForm((prev) => ({
      ...prev,
      cuisineTypes: [...prev.cuisineTypes, selectedCuisineToAdd],
    }));
    setSelectedCuisineToAdd("");
    setCuisineSearch("");
    setShowCuisineModal(false);
    setErr(null);
  }

  function removeCuisineSelection(cuisine: string) {
    setRestaurantForm((prev) => ({
      ...prev,
      cuisineTypes: prev.cuisineTypes.filter((item) => item !== cuisine),
    }));
  }

  function renderSettingsTab() {
    if (activeTab === "payment") {
      return (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 1fr)", gap: 10 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enablePayInApp} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enablePayInApp: e.target.checked }))} />{t("restaurant.payInApp", "Pay in app")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enablePayAtCounter} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enablePayAtCounter: e.target.checked }))} />{t("restaurant.payAtCounter", "Pay at counter")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enablePayOnDelivery} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enablePayOnDelivery: e.target.checked }))} />{t("restaurant.payOnDelivery", "Pay on delivery")}</label>
          <label>{t("restaurant.extraIngredientPrice", "Extra ingredient price")}<input type="number" step="0.01" min={0} value={settingsForm.extraIngredientPrice.toFixed(2)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, extraIngredientPrice: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
        </div>
      );
    }

    if (activeTab === "delivery") {
      return (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 10 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enableDeliveryOrders} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enableDeliveryOrders: e.target.checked }))} />{t("restaurant.deliveryEnabled", "Delivery enabled")}</label>
          <label>{t("restaurant.deliveryFee", "Delivery fee")}<input type="number" step="0.01" value={settingsForm.deliveryFee.toFixed(2)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryFee: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.deliveryRadius", "Radius (km)")}<input type="number" step="0.1" value={settingsForm.deliveryRadiusKm} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryRadiusKm: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.minimumOrder", "Minimum order")}<input type="number" step="0.01" value={settingsForm.minimumDeliveryOrder.toFixed(2)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, minimumDeliveryOrder: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.startTime", "Start time")}<input type="time" step={900} value={minutesToTimeInput(settingsForm.deliveryStartMinuteOfDay)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryStartMinuteOfDay: timeInputToMinutes(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.endTime", "End time")}<input type="time" step={900} value={minutesToTimeInput(settingsForm.deliveryEndMinuteOfDay)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryEndMinuteOfDay: timeInputToMinutes(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.leadTimeMinutes", "Lead time (minutes)")}<input type="number" min={0} step={5} value={settingsForm.deliveryLeadTimeMinutes} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryLeadTimeMinutes: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.driverAssignment", "Driver assignment")}<select value={settingsForm.deliveryAssignmentMode} onChange={(e) => setSettingsForm((prev) => ({ ...prev, deliveryAssignmentMode: Number(e.target.value) }))} style={{ width: "100%" }}><option value={0}>{t("restaurant.assignmentManual", "Manual")}</option><option value={1}>{t("restaurant.assignmentAutomatic", "Automatic")}</option></select></label>
        </div>
      );
    }

    if (activeTab === "reservations") {
      return (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 10 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enableReservations} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enableReservations: e.target.checked }))} />{t("restaurant.reservationsEnabled", "Reservations enabled")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.allowUserTableSelectionForReservations} onChange={(e) => setSettingsForm((prev) => ({ ...prev, allowUserTableSelectionForReservations: e.target.checked }))} />{t("restaurant.allowTableSelection", "Allow user table selection")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enableTableOrders} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enableTableOrders: e.target.checked }))} />{t("restaurant.tableOrdersEnabled", "Table orders enabled")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.enableTakeawayOrders} onChange={(e) => setSettingsForm((prev) => ({ ...prev, enableTakeawayOrders: e.target.checked }))} />{t("restaurant.pickupEnabled", "Pickup enabled")}</label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.reservationRequiresInAppPayment} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationRequiresInAppPayment: e.target.checked }))} />{t("restaurant.reservationRequiresPayment", "Reservation requires in-app payment")}</label>
          <label>{t("restaurant.startTime", "Start time")}<input type="time" step={900} value={minutesToTimeInput(settingsForm.reservationStartMinuteOfDay)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationStartMinuteOfDay: timeInputToMinutes(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.lastStartTime", "Last start time")}<input type="time" step={900} value={minutesToTimeInput(settingsForm.reservationLastStartMinuteOfDay)} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationLastStartMinuteOfDay: timeInputToMinutes(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.defaultDuration", "Default duration")}<input type="number" min={15} step={15} value={settingsForm.defaultReservationDurationMinutes} onChange={(e) => setSettingsForm((prev) => ({ ...prev, defaultReservationDurationMinutes: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.gracePeriod", "Grace period")}<input type="number" min={0} step={5} value={settingsForm.reservationGracePeriodMinutes} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationGracePeriodMinutes: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.preorderMinOffset", "Preorder min offset")}<input type="number" value={settingsForm.reservationPreorderMinOffsetMinutes} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationPreorderMinOffsetMinutes: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label>{t("restaurant.preorderMaxAfterStart", "Preorder max after start")}<input type="number" value={settingsForm.reservationPreorderMaxAfterStartMinutes} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationPreorderMaxAfterStartMinutes: Number(e.target.value) }))} style={{ width: "100%" }} /></label>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={settingsForm.reservationHoldsTableUntilClose} onChange={(e) => setSettingsForm((prev) => ({ ...prev, reservationHoldsTableUntilClose: e.target.checked }))} />{t("restaurant.holdUntilClose", "Hold table until close")}</label>
        </div>
      );
    }

    if (activeTab === "tables") {
      return (
        <>
          <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 12 }}>
            <button onClick={() => { setTableForm(emptyTableForm()); setShowTableModal(true); }}>{t("restaurant.addTable", "Add table")}</button>
          </div>
          <table width="100%" cellPadding={8} style={{ marginTop: 12 }}>
            <thead>
              <tr><th align="left">{t("restaurant.tableLabel", "Label")}</th><th align="left">{t("restaurant.tableSeats", "Seats")}</th><th align="left">{t("restaurant.tableSort", "Sort")}</th><th align="left">{t("common.active", "Active")}</th><th align="left">{t("restaurant.reservable", "Reservable")}</th><th></th></tr>
            </thead>
            <tbody>
              {tables.map((table) => (
                <tr key={table.id}>
                  <td>{table.label}</td>
                  <td>{table.seats ?? "-"}</td>
                  <td>{table.sortOrder}</td>
                  <td>{table.isActive ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                  <td>{table.isReservable ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                  <td>
                    <button onClick={() => { setTableForm({ id: table.id, label: table.label, seats: table.seats == null ? "" : String(table.seats), sortOrder: table.sortOrder, isActive: table.isActive, isReservable: table.isReservable }); setShowTableModal(true); }}>{t("common.edit", "Edit")}</button>
                    <button onClick={() => void removeTable(table.id)} className="button-danger" style={{ marginLeft: 6 }}>{t("common.delete", "Delete")}</button>
                  </td>
                </tr>
              ))}
              {tables.length === 0 ? (
                <tr><td colSpan={6} style={{ color: "#666", padding: 16 }}>{t("restaurant.tableNone", "No tables added yet.")}</td></tr>
              ) : null}
            </tbody>
          </table>
        </>
      );
    }

    if (activeTab === "roles") {
      return (
        <>
          <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 8, marginBottom: 12 }}>
            <input placeholder={t("restaurant.inviteByEmail", "Invite by email")} value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} />
            <button onClick={() => void inviteUser()}>{t("restaurant.sendInvite", "Send invite")}</button>
          </div>

          <div style={{ display: "grid", gridTemplateColumns: "1fr 220px auto", gap: 8, marginBottom: 16 }}>
            <select value={selectedAwaitingUserId} onChange={(e) => setSelectedAwaitingUserId(e.target.value)}>
              <option value="">{t("restaurant.awaitingAssignment", "-- awaiting assignment --")}</option>
              {awaitingAssignmentOptions.map((item) => (
                <option key={`${item.userId}-${item.id}`} value={item.userId}>
                  {item.email ?? item.userId}
                </option>
              ))}
            </select>
            <select value={selectedRole} onChange={(e) => setSelectedRole(e.target.value)}>
              {restaurantRoles.filter((role) => isMainAdmin || role !== "RestaurantAdmin").map((role) => <option key={`assign-${role}`} value={role}>{roleLabel(role)}</option>)}
            </select>
            <button onClick={() => void assignAwaitingUser()}>{t("restaurant.assignRole", "Assign role")}</button>
          </div>

          <table width="100%" cellPadding={8}>
            <thead>
              <tr><th align="left">{t("restaurant.user", "User")}</th><th align="left">{t("restaurant.stateOrRole", "State / Role")}</th><th></th></tr>
            </thead>
            <tbody>
              {assignments.map((assignment) => (
                <tr key={`${assignment.inviteId ?? assignment.id}-${assignment.userId}`}>
                  <td>{assignment.email ?? assignment.userId}</td>
                  <td>{assignment.role}</td>
                  <td>
                    <button onClick={() => void removeAssignment(assignment)}>
                      {assignment.inviteId ? t("restaurant.removeInvite", "Remove invite") : t("restaurant.removeRole", "Remove")}
                    </button>
                  </td>
                </tr>
              ))}
              {assignments.length === 0 ? (
                <tr><td colSpan={3} style={{ color: "#666" }}>{t("restaurant.noStaffLinked", "No staff linked to this restaurant yet.")}</td></tr>
              ) : null}
            </tbody>
          </table>
        </>
      );
    }

    const selectedCultures = settingsForm.supportedCultures
      .split(",")
      .map((value) => value.trim())
      .filter(Boolean);

    return (
      <div style={{ display: "grid", gap: 12 }}>
        <div>
          <strong>{t("restaurant.supportedLanguages", "Supported languages")}</strong>
          <div style={{ display: "grid", gap: 8, marginTop: 8 }}>
            {appLanguages.filter((language) => language.isActive).map((language) => (
              <label key={language.culture}>
                <input
                  type="checkbox"
                  checked={selectedCultures.includes(language.culture)}
                  onChange={(e) => {
                    const next = new Set(selectedCultures);
                    if (e.target.checked) next.add(language.culture);
                    else next.delete(language.culture);
                    const nextList = Array.from(next);
                    setSettingsForm((prev) => ({
                      ...prev,
                      supportedCultures: nextList.join(","),
                      defaultCulture: nextList.includes(prev.defaultCulture) ? prev.defaultCulture : (nextList[0] ?? ""),
                    }));
                  }}
                />
                {" "}
                {language.nativeName} ({language.culture})
              </label>
            ))}
          </div>
        </div>
        <label>
          {t("restaurant.defaultLanguage", "Default language")}
          <select
            value={settingsForm.defaultCulture}
            onChange={(e) => setSettingsForm((prev) => ({ ...prev, defaultCulture: e.target.value }))}
            style={{ width: "100%" }}
          >
            {selectedCultures.map((culture) => (
              <option key={culture} value={culture}>{culture}</option>
            ))}
          </select>
        </label>
      </div>
    );
  }

  return (
    <PageShell title={isMainAdmin ? t("nav.restaurants", "Restaurants") : t("page.restaurantSettings", "Restaurant Settings")} error={err} maxWidth={1120}>
      {info && <div className="alert-success">{info}</div>}

      {isMainAdmin ? (
        <RestaurantSelector
          restaurants={restaurants}
          selectedRestaurantId={selectedRestaurantId}
          loading={loading}
          onSelectRestaurant={(id) => {
            setSelectedRestaurantId(id);
            void load(id);
          }}
          onNewRestaurant={resetNewRestaurant}
          onReload={() => void load()}
          t={t}
        />
      ) : null}

      <RestaurantDetailsSection
        form={restaurantForm}
        cuisineNameMap={cuisineNameMap}
        isMainAdmin={isMainAdmin}
        editingDetails={editingDetails}
        canEditName={canEditName}
        onFormChange={setRestaurantForm}
        onOpenCuisineModal={() => setShowCuisineModal(true)}
        onRemoveCuisine={removeCuisineSelection}
        onStartEdit={() => setEditingDetails(true)}
        onSave={() => void saveRestaurant()}
        onCancel={() => {
          setEditingDetails(false);
          void load(selectedRestaurantId);
        }}
        t={t}
      />

      {selectedRestaurantId > 0 && (
        <>
          <section style={{ marginBottom: 28, border: "1px solid #e5e7eb", borderRadius: 8, padding: 16 }}>
            <h3 style={{ marginTop: 0 }}>{t("restaurant.settings", "Restaurant settings")}</h3>
            <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 14 }}>
              <SettingsTabButton active={activeTab === "payment"} label={t("restaurant.paymentOptions", "Payment Options")} onClick={() => setActiveTab("payment")} />
              <SettingsTabButton active={activeTab === "delivery"} label={t("restaurant.delivery", "Delivery")} onClick={() => setActiveTab("delivery")} />
              <SettingsTabButton active={activeTab === "reservations"} label={t("restaurant.reservations", "Reservations")} onClick={() => setActiveTab("reservations")} />
              <SettingsTabButton active={activeTab === "tables"} label={t("restaurant.tables", "Tables")} onClick={() => setActiveTab("tables")} />
              <SettingsTabButton active={activeTab === "roles"} label={t("restaurant.roles", "Restaurant Roles")} onClick={() => setActiveTab("roles")} />
              <SettingsTabButton active={activeTab === "languages"} label={t("restaurant.languages", "Cultures / Language")} onClick={() => setActiveTab("languages")} />
            </div>
            {renderSettingsTab()}
            {activeTab === "payment" || activeTab === "delivery" || activeTab === "reservations" || activeTab === "languages" ? (
              <button onClick={() => void saveSettings()} style={{ marginTop: 12 }}>{t("common.save", "Save")}</button>
            ) : null}
          </section>
        </>
      )}

      {showCuisineModal ? (
        <CuisineModal
          cuisineSearch={cuisineSearch}
          selectedCuisineToAdd={selectedCuisineToAdd}
          filteredCuisineOptions={filteredCuisineOptions}
          onClose={() => {
            setShowCuisineModal(false);
            setCuisineSearch("");
            setSelectedCuisineToAdd("");
          }}
          onCuisineSearchChange={setCuisineSearch}
          onSelectedCuisineToAddChange={setSelectedCuisineToAdd}
          onAddCuisine={addCuisineSelection}
          t={t}
        />
      ) : null}

      {showTableModal ? (
        <TableModal
          tableForm={tableForm}
          onFormChange={setTableForm}
          onClose={() => {
            setShowTableModal(false);
            setTableForm(emptyTableForm());
          }}
          onSave={() => void saveTable()}
          t={t}
        />
      ) : null}
    </PageShell>
  );
}
