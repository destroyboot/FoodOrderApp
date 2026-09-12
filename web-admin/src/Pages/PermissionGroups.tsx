import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { matchesTokenizedSearch } from "../tokenSearch";
import { PageShell } from "../Components/PageShell";

type AdminDataTableGrantDto = {
  tableName: string;
  canRead: boolean;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
};

type AdminDataRolePermissionsDto = {
  roleName: string;
  grants: AdminDataTableGrantDto[];
};

type PermissionName = keyof Omit<AdminDataTableGrantDto, "tableName">;
type PermissionFilter = "any" | "yes" | "no";

const permissions: PermissionName[] = ["canRead", "canCreate", "canUpdate", "canDelete"];

export default function PermissionGroups() {
  const { t } = useI18n();
  const [permissionGroups, setPermissionGroups] = useState<AdminDataRolePermissionsDto[]>([]);
  const [selectedPermissionRole, setSelectedPermissionRole] = useState("");
  const [tableSearchText, setTableSearchText] = useState("");
  const [filters, setFilters] = useState<Record<PermissionName, PermissionFilter>>({
    canRead: "any",
    canCreate: "any",
    canUpdate: "any",
    canDelete: "any",
  });
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function loadPermissionGroups() {
    setLoading(true);
    setErr(null);
    try {
      const result = await api<AdminDataRolePermissionsDto[]>("/api/admin/data/permission-groups");
      const groups = result ?? [];
      setPermissionGroups(groups);
      setSelectedPermissionRole((current) => current || groups[0]?.roleName || "");
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.loadFailed", "Failed to load data admin."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadPermissionGroups();
  }, []);

  function updatePermissionGrant(tableName: string, permission: PermissionName, checked: boolean) {
    setPermissionGroups((current) =>
      current.map((group) => {
        if (group.roleName !== selectedPermissionRole) return group;

        return {
          ...group,
          grants: group.grants.map((grant) =>
            grant.tableName === tableName
              ? { ...grant, [permission]: checked }
              : grant
          ),
        };
      })
    );
  }

  async function savePermissionGroup() {
    const group = permissionGroups.find((item) => item.roleName === selectedPermissionRole);
    if (!group) return;

    setBusy(true);
    setErr(null);
    try {
      await api(`/api/admin/data/permission-groups/${encodeURIComponent(group.roleName)}`, {
        method: "PUT",
        body: JSON.stringify(group),
      });
      await loadPermissionGroups();
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.permissionsSaveFailed", "Failed to save permissions."));
    } finally {
      setBusy(false);
    }
  }

  function setPermissionFilter(permission: PermissionName, value: PermissionFilter) {
    setFilters((current) => ({ ...current, [permission]: value }));
  }

  function resetFilters() {
    setTableSearchText("");
    setFilters({
      canRead: "any",
      canCreate: "any",
      canUpdate: "any",
      canDelete: "any",
    });
  }

  const selectedPermissionGroup = useMemo(
    () => permissionGroups.find((group) => group.roleName === selectedPermissionRole) ?? null,
    [permissionGroups, selectedPermissionRole]
  );

  const filteredGrants = useMemo(() => {
    if (!selectedPermissionGroup) return [];

    return selectedPermissionGroup.grants.filter((grant) => {
      if (!matchesTokenizedSearch(grant.tableName, tableSearchText)) {
        return false;
      }

      return permissions.every((permission) => {
        const filter = filters[permission];
        if (filter === "any") return true;
        return filter === "yes" ? grant[permission] : !grant[permission];
      });
    });
  }, [selectedPermissionGroup, tableSearchText, filters]);

  return (
    <PageShell title={t("dataAdmin.permissionGroups", "Permission Groups")} error={err} maxWidth={1200}>
      {loading && <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div>}

      <section style={{ display: "grid", gap: 12, marginBottom: 16 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <select value={selectedPermissionRole} onChange={(event) => setSelectedPermissionRole(event.target.value)}>
            {permissionGroups.map((group) => (
              <option key={group.roleName} value={group.roleName}>
                {t(`roles.${group.roleName}`, group.roleName)}
              </option>
            ))}
          </select>
          <button onClick={() => void savePermissionGroup()} disabled={busy || !selectedPermissionGroup}>
            {busy ? t("common.saving", "Saving...") : t("dataAdmin.savePermissions", "Save permissions")}
          </button>
          <button onClick={() => void loadPermissionGroups()} disabled={loading || busy}>
            {t("common.reload", "Reload")}
          </button>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <input
            placeholder={t("dataAdmin.searchTables", "Search tables")}
            value={tableSearchText}
            onChange={(event) => setTableSearchText(event.target.value)}
            style={{ minWidth: 260 }}
          />
          {permissions.map((permission) => (
            <label key={permission} style={{ display: "flex", alignItems: "center", gap: 6 }}>
              <span>{permissionLabel(permission, t)}</span>
              <select value={filters[permission]} onChange={(event) => setPermissionFilter(permission, event.target.value as PermissionFilter)}>
                <option value="any">{t("dataAdmin.any", "Any")}</option>
                <option value="yes">{t("common.yes", "Yes")}</option>
                <option value="no">{t("common.no", "No")}</option>
              </select>
            </label>
          ))}
          <button onClick={resetFilters}>{t("common.resetFilters", "Reset Filters")}</button>
        </div>
      </section>

      {!selectedPermissionGroup ? (
        <div>{t("dataAdmin.noneAccessible", "No accessible tables.")}</div>
      ) : (
        <div style={{ overflowX: "auto" }}>
          <table width="100%" cellPadding={8}>
            <thead>
              <tr>
                <th align="left">{t("dataAdmin.table", "Table")}</th>
                {permissions.map((permission) => (
                  <th key={permission} align="left">{permissionLabel(permission, t)}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filteredGrants.map((grant) => (
                <tr key={`${selectedPermissionGroup.roleName}-${grant.tableName}`}>
                  <td>{grant.tableName}</td>
                  {permissions.map((permission) => (
                    <td key={permission}>
                      <input
                        type="checkbox"
                        checked={grant[permission]}
                        onChange={(event) => updatePermissionGrant(grant.tableName, permission, event.target.checked)}
                      />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </PageShell>
  );
}

function permissionLabel(permission: PermissionName, t: (key: string, fallback: string) => string) {
  switch (permission) {
    case "canRead":
      return t("dataAdmin.canRead", "Read");
    case "canCreate":
      return t("dataAdmin.canCreate", "Create");
    case "canUpdate":
      return t("dataAdmin.canUpdate", "Update");
    case "canDelete":
      return t("dataAdmin.canDelete", "Delete");
  }
}
