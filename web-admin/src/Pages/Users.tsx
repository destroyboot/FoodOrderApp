import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { api } from "../api";
import { matchesTokenizedSearch } from "../tokenSearch";
import { useI18n } from "../i18n";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";

type RestaurantAssignmentDto = {
  restaurantId: number;
  restaurantName?: string | null;
  role: string;
};

type UserDto = {
  id: string;
  email?: string | null;
  userName?: string | null;
  emailConfirmed: boolean;
  roles: string[];
  restaurantAssignments: RestaurantAssignmentDto[];
};

type RestaurantDto = {
  id: number;
  name: string;
};

type UserFormState = {
  id?: string;
  email: string;
  userName: string;
  password: string;
  emailConfirmed: boolean;
  isAppAdmin: boolean;
  restaurantAssignments: RestaurantAssignmentDto[];
};

const restaurantRoles = ["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"];

function emptyForm(): UserFormState {
  return {
    email: "",
    userName: "",
    password: "",
    emailConfirmed: true,
    isAppAdmin: false,
    restaurantAssignments: [],
  };
}

export default function Users() {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const [data, setData] = useState<UserDto[]>([]);
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [resettingId, setResettingId] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<UserFormState>(emptyForm());
  const [searchText, setSearchText] = useState("");
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [bulkRestaurantId, setBulkRestaurantId] = useState(0);
  const [bulkRole, setBulkRole] = useState(restaurantRoles[0]);
  const [bulkAssigning, setBulkAssigning] = useState(false);
  const [roleFilter, setRoleFilter] = useState("all");
  const [restaurantFilter, setRestaurantFilter] = useState("all");
  const [confirmedFilter, setConfirmedFilter] = useState("all");
  const [showUserModal, setShowUserModal] = useState(false);

  function roleLabel(role: string) {
    return t(`roles.${role}`, role);
  }

  async function load() {
    setErr(null);
    try {
      const [users, restaurantList] = await Promise.all([
        api<UserDto[]>("/api/admin/users"),
        api<RestaurantDto[]>("/api/admin/restaurants"),
      ]);

      setData(users ?? []);
      setRestaurants(restaurantList ?? []);
      if (!bulkRestaurantId && (restaurantList?.length ?? 0) > 0) {
        setBulkRestaurantId(restaurantList![0].id);
      }
    } catch (e: any) {
      setErr(e.message || t("users.loadFailed", "Failed to load users"));
    }
  }

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    const editId = searchParams.get("edit");
    if (!editId || data.length === 0) {
      return;
    }

    const user = data.find((item) => item.id === editId);
    if (!user) {
      return;
    }

    startEdit(user);
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      next.delete("edit");
      return next;
    }, { replace: true });
  }, [data, searchParams, setSearchParams]);

  function startCreate() {
    setEditingId(null);
    setForm(emptyForm());
    setShowUserModal(true);
  }

  function startEdit(user: UserDto) {
    setEditingId(user.id);
    setForm({
      id: user.id,
      email: user.email ?? "",
      userName: user.userName ?? "",
      password: "",
      emailConfirmed: user.emailConfirmed,
      isAppAdmin: user.roles.includes("Admin"),
      restaurantAssignments: user.restaurantAssignments.map((assignment) => ({
        restaurantId: assignment.restaurantId,
        restaurantName: assignment.restaurantName,
        role: assignment.role,
      })),
    });
    setShowUserModal(true);
  }

  function updateAssignment(index: number, field: keyof RestaurantAssignmentDto, value: string | number) {
    setForm((prev) => {
      const next = [...prev.restaurantAssignments];
      next[index] = { ...next[index], [field]: value };
      return { ...prev, restaurantAssignments: next };
    });
  }

  function addAssignment() {
    setForm((prev) => ({
      ...prev,
      restaurantAssignments: [
        ...prev.restaurantAssignments,
        {
          restaurantId: restaurants[0]?.id ?? 0,
          role: restaurantRoles[0],
        },
      ],
    }));
  }

  function removeAssignment(index: number) {
    setForm((prev) => ({
      ...prev,
      restaurantAssignments: prev.restaurantAssignments.filter((_, i) => i !== index),
    }));
  }

  async function save() {
    setErr(null);
    setSaving(true);

    try {
      const payload = {
        email: form.email,
        userName: form.userName,
        emailConfirmed: form.emailConfirmed,
        isAppAdmin: form.isAppAdmin,
        restaurantAssignments: form.restaurantAssignments
          .filter((assignment) => assignment.restaurantId > 0 && assignment.role)
          .map((assignment) => ({
            restaurantId: Number(assignment.restaurantId),
            role: assignment.role,
          })),
      };

      if (editingId) {
        await api(`/api/admin/users/${editingId}`, {
          method: "PUT",
          body: JSON.stringify(payload),
        });
      } else {
        await api("/api/admin/users", {
          method: "POST",
          body: JSON.stringify({
            ...payload,
            password: form.password,
          }),
        });
      }

      setEditingId(null);
      setForm(emptyForm());
      setShowUserModal(false);
      await load();
    } catch (e: any) {
      setErr(e.message || t("users.saveFailed", "Failed to save user"));
    } finally {
      setSaving(false);
    }
  }

  async function removeUser(user: UserDto) {
    if (!confirm(`${t("users.deleteConfirm", "Delete user")} ${user.email ?? user.userName ?? user.id}?`)) return;

    try {
      setErr(null);
      await api(`/api/admin/users/${user.id}`, { method: "DELETE" });
      if (editingId === user.id) {
        setEditingId(null);
        setForm(emptyForm());
      }
      setSelectedUserIds((prev) => prev.filter((id) => id !== user.id));
      await load();
    } catch (e: any) {
      setErr(e.message || t("users.deleteFailed", "Failed to delete user"));
    }
  }

  async function resetPassword(user: UserDto) {
    try {
      setErr(null);
      setResettingId(user.id);
      await api(`/api/admin/users/${user.id}/reset-password`, {
        method: "POST",
      });
    } catch (e: any) {
      setErr(e.message || t("users.resetFailed", "Failed to send password reset"));
    } finally {
      setResettingId(null);
    }
  }

  const filteredUsers = useMemo(
    () =>
      data.filter((user) => {
        const matchesSearch = matchesTokenizedSearch(
          [
            user.email ?? "",
            user.userName ?? "",
            user.emailConfirmed ? "confirmed yes" : "confirmed no",
            user.roles.join(" "),
            user.restaurantAssignments
              .map((assignment) => `${assignment.restaurantName ?? assignment.restaurantId} ${assignment.role}`)
              .join(" "),
          ].join(" "),
          searchText
        );
        const matchesRole = roleFilter === "all" || user.roles.includes(roleFilter) || user.restaurantAssignments.some((assignment) => assignment.role === roleFilter);
        const matchesRestaurant = restaurantFilter === "all" || user.restaurantAssignments.some((assignment) => String(assignment.restaurantId) === restaurantFilter);
        const matchesConfirmed = confirmedFilter === "all" || String(user.emailConfirmed) === confirmedFilter;
        return matchesSearch && matchesRole && matchesRestaurant && matchesConfirmed;
      }),
    [data, searchText, roleFilter, restaurantFilter, confirmedFilter]
  );

  const allFilteredIds = filteredUsers.map((user) => user.id);

  function toggleSelectedUser(userId: string, checked: boolean) {
    setSelectedUserIds((prev) =>
      checked ? Array.from(new Set([...prev, userId])) : prev.filter((id) => id !== userId)
    );
  }

  function selectAllFiltered() {
    setSelectedUserIds(allFilteredIds);
  }

  function clearSelection() {
    setSelectedUserIds([]);
  }

  async function bulkAssignUsers() {
    if (selectedUserIds.length === 0) {
      setErr(t("users.selectAtLeastOne", "Select at least one user first."));
      return;
    }

    if (!bulkRestaurantId) {
      setErr(t("users.selectRestaurantForAssignment", "Select a restaurant for the assignment."));
      return;
    }

    try {
      setErr(null);
      setBulkAssigning(true);
      await api("/api/admin/users/assignments/bulk", {
        method: "POST",
        body: JSON.stringify({
          userIds: selectedUserIds,
          restaurantId: bulkRestaurantId,
          role: bulkRole,
        }),
      });
      setSelectedUserIds([]);
      await load();
    } catch (e: any) {
      setErr(e.message || t("users.bulkAssignFailed", "Failed to assign selected users"));
    } finally {
      setBulkAssigning(false);
    }
  }

  return (
    <PageShell
      title={
        <span style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <span>{t("nav.users", "Users")}</span>      
        </span>
      }
      error={err}
      maxWidth={1250}
    >
      <button style={{marginTop: 16, backgroundColor: "lightgreen"}} onClick={startCreate}>{t("users.create", "Create user")}</button>
      <div style={{ display: "grid", gap: 12, marginTop: 16, marginBottom: 16 }}>
        <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8 }}>
          <input
            placeholder={t("users.search", "Search users")}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ width: "100%" }}
          />
          <button
            onClick={() => {
              setSearchText("");
              setRoleFilter("all");
              setRestaurantFilter("all");
              setConfirmedFilter("all");
            }}
          >
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr)) 280px", gap: 8 }}>
        <select value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}>
          <option value="all">{t("ui.allRoles", "All roles")}</option>
          <option value="Admin">{roleLabel("Admin")}</option>
          <option value="Customer">{roleLabel("Customer")}</option>
          {restaurantRoles.map((role) => <option key={role} value={role}>{roleLabel(role)}</option>)}
        </select>
        <select value={restaurantFilter} onChange={(e) => setRestaurantFilter(e.target.value)}>
          <option value="all">{t("ui.allRestaurants", "All restaurants")}</option>
          {restaurants.map((restaurant) => <option key={restaurant.id} value={restaurant.id}>{restaurant.name}</option>)}
        </select>
        <select value={confirmedFilter} onChange={(e) => setConfirmedFilter(e.target.value)}>
          <option value="all">{t("ui.allConfirmationStates", "All confirmation states")}</option>
          <option value="true">{t("ui.confirmed", "Confirmed")}</option>
          <option value="false">{t("ui.notConfirmed", "Not confirmed")}</option>
        </select>
        <div style={{ display: "flex", alignItems: "center", color: "#555" }}>
          {filteredUsers.length} {t("users.count", "users")}
        </div>
      </div>
      </div>

      <section style={{ marginBottom: 20, padding: 12, border: "1px solid #ddd", borderRadius: 6 }}>
        <h3 style={{ marginTop: 0 }}>{t("users.assignSelected", "Assign selected users")}</h3>
        <div style={{ color: "#555", marginBottom: 10 }}>
          {t("users.assignSelectedHint", "Select users in the list below, then choose a restaurant and a role to assign them in one action.")}
        </div>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
          <button onClick={selectAllFiltered} disabled={allFilteredIds.length === 0}>{t("users.selectAllListed", "Select all listed")}</button>
          <button onClick={clearSelection} disabled={selectedUserIds.length === 0}>{t("users.clearSelection", "Clear selection")}</button>
          <span style={{ color: "#555" }}>{selectedUserIds.length} {t("users.selected", "selected")}</span>
          <select value={bulkRestaurantId} onChange={(e) => setBulkRestaurantId(Number(e.target.value))} disabled={selectedUserIds.length === 0}>
            <option value={0}>-- {t("users.selectRestaurant", "select restaurant")} --</option>
            {restaurants.map((restaurant) => (
              <option key={restaurant.id} value={restaurant.id}>
                {restaurant.name}
              </option>
            ))}
          </select>
          <select value={bulkRole} onChange={(e) => setBulkRole(e.target.value)} disabled={selectedUserIds.length === 0}>
            {restaurantRoles.map((role) => (
              <option key={role} value={role}>
                {roleLabel(role)}
              </option>
            ))}
          </select>
          <button onClick={bulkAssignUsers} disabled={bulkAssigning || selectedUserIds.length === 0}>
            {bulkAssigning ? t("users.assigning", "Assigning...") : t("users.assignSelected", "Assign selected users")}
          </button>
        </div>
      </section>

      <table width="100%" cellPadding={8}>
        <thead>
          <tr>
            <th align="left">{t("users.select", "Select")}</th>
            <th align="left">{t("common.email", "Email")}</th>
            <th align="left">{t("users.userName", "User Name")}</th>
            <th align="left">{t("users.confirmed", "Confirmed")}</th>
            <th align="left">{t("users.roles", "Roles")}</th>
            <th align="left">{t("users.restaurantAssignments", "Restaurant assignments")}</th>
            <th align="left">{t("users.passwordReset", "Password reset")}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {filteredUsers.map((user) => (
            <tr key={user.id}>
              <td>
                <input
                  type="checkbox"
                  checked={selectedUserIds.includes(user.id)}
                  onChange={(e) => toggleSelectedUser(user.id, e.target.checked)}
                />
              </td>
              <td>{user.email ?? "-"}</td>
              <td>{user.userName ?? "-"}</td>
              <td>{user.emailConfirmed ? t("common.yes", "Yes") : t("common.no", "No")}</td>
              <td>{user.roles.join(", ") || "-"}</td>
              <td>
                {user.restaurantAssignments.length === 0
                  ? "-"
                  : user.restaurantAssignments
                      .map((assignment) => `${assignment.restaurantName ?? assignment.restaurantId}: ${assignment.role}`)
                      .join("; ")}
              </td>
              <td>
                <button onClick={() => resetPassword(user)} disabled={resettingId === user.id}>
                  {resettingId === user.id ? t("users.sending", "Sending...") : t("users.resetPassword", "Reset password")}
                </button>
              </td>
              <td>
                <div style={{ display: "flex", gap: 6 }}>
                  <button onClick={() => startEdit(user)}>{t("common.edit", "Edit")}</button>
                  <button onClick={() => removeUser(user)} className="button-danger">{t("common.delete", "Delete")}</button>
                </div>
              </td>
            </tr>
          ))}
          {filteredUsers.length === 0 && (
            <tr>
              <td colSpan={8} style={{ color: "#666", padding: 16 }}>
                {t("users.none", "No users match this search.")}
              </td>
            </tr>
          )}
        </tbody>
      </table>

      {showUserModal ? (
        <ModalShell
          title={editingId ? t("users.edit", "Edit user") : t("users.create", "Create user")}
          onClose={() => {
            setShowUserModal(false);
            setEditingId(null);
            setForm(emptyForm());
          }}
          minWidth="min(760px, calc(100vw - 32px))"
          maxWidth={960}
        >
          <div style={{ display: "grid", gap: 12 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8 }}>
              <input placeholder={t("common.email", "Email")} value={form.email} onChange={(e) => setForm((prev) => ({ ...prev, email: e.target.value }))} />
              <input placeholder={t("users.userName", "User Name")} value={form.userName} onChange={(e) => setForm((prev) => ({ ...prev, userName: e.target.value }))} />
              {!editingId && (
                <input type="password" placeholder={t("common.password", "Password")} value={form.password} onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))} />
              )}
            </div>

            <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
              <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <input type="checkbox" checked={form.emailConfirmed} onChange={(e) => setForm((prev) => ({ ...prev, emailConfirmed: e.target.checked }))} />
                {t("users.emailConfirmed", "Email confirmed")}
              </label>
              <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <input type="checkbox" checked={form.isAppAdmin} onChange={(e) => setForm((prev) => ({ ...prev, isAppAdmin: e.target.checked }))} />
                {t("users.appAdmin", "App Admin")}
              </label>
            </div>

            <div>
              <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8 }}>
                <strong>{t("users.restaurantAssignments", "Restaurant assignments")}</strong>
                <button onClick={addAssignment}>{t("users.addAssignment", "Add assignment")}</button>
              </div>

              {form.restaurantAssignments.length === 0 ? (
                <div style={{ color: "#666" }}>{t("users.noAssignments", "No restaurant assignments.")}</div>
              ) : (
                form.restaurantAssignments.map((assignment, index) => (
                  <div key={`${assignment.restaurantId}-${assignment.role}-${index}`} style={{ display: "grid", gridTemplateColumns: "1fr 220px auto", gap: 8, marginBottom: 8 }}>
                    <select value={assignment.restaurantId} onChange={(e) => updateAssignment(index, "restaurantId", Number(e.target.value))}>
                      <option value={0}>-- {t("users.selectRestaurant", "select restaurant")} --</option>
                      {restaurants.map((restaurant) => <option key={restaurant.id} value={restaurant.id}>{restaurant.name}</option>)}
                    </select>
                    <select value={assignment.role} onChange={(e) => updateAssignment(index, "role", e.target.value)}>
                      {restaurantRoles.map((role) => <option key={role} value={role}>{roleLabel(role)}</option>)}
                    </select>
                    <button onClick={() => removeAssignment(index)}>{t("common.remove", "Remove")}</button>
                  </div>
                ))
              )}
            </div>

            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button onClick={save} disabled={saving}>
                {saving ? t("users.saving", "Saving...") : editingId ? t("users.saveChanges", "Save changes") : t("users.create", "Create user")}
              </button>
              <button onClick={() => { setShowUserModal(false); setEditingId(null); setForm(emptyForm()); }}>
                {t("common.cancel", "Cancel")}
              </button>
            </div>
          </div>
        </ModalShell>
      ) : null}
    </PageShell>
  );
}
