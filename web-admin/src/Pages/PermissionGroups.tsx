import { type CSSProperties, useEffect, useMemo, useState } from "react";
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

type AdminFeatureGrantDto = {
  featureKey: string;
  name: string;
  description: string;
  groupName: string;
  isAllowed: boolean;
};

type AdminFeatureRolePermissionsDto = {
  roleName: string;
  grants: AdminFeatureGrantDto[];
};

type PermissionName = keyof Omit<AdminDataTableGrantDto, "tableName">;
type PermissionFilter = "any" | "yes" | "no";
type TabKey = "crud" | "features";

const permissions: PermissionName[] = ["canRead", "canCreate", "canUpdate", "canDelete"];

export default function PermissionGroups() {
  const { t } = useI18n();
  const [activeTab, setActiveTab] = useState<TabKey>("crud");
  const [permissionGroups, setPermissionGroups] = useState<AdminDataRolePermissionsDto[]>([]);
  const [originalPermissionGroups, setOriginalPermissionGroups] = useState<AdminDataRolePermissionsDto[]>([]);
  const [featurePermissionGroups, setFeaturePermissionGroups] = useState<AdminFeatureRolePermissionsDto[]>([]);
  const [originalFeaturePermissionGroups, setOriginalFeaturePermissionGroups] = useState<AdminFeatureRolePermissionsDto[]>([]);
  const [selectedPermissionRole, setSelectedPermissionRole] = useState("");
  const [selectedFeatureRole, setSelectedFeatureRole] = useState("");
  const [tableSearchText, setTableSearchText] = useState("");
  const [featureSearchText, setFeatureSearchText] = useState("");
  const [featureGroupFilter, setFeatureGroupFilter] = useState("all");
  const [featureAllowedFilter, setFeatureAllowedFilter] = useState<PermissionFilter>("any");
  const [filters, setFilters] = useState<Record<PermissionName, PermissionFilter>>({
    canRead: "any",
    canCreate: "any",
    canUpdate: "any",
    canDelete: "any",
  });
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  async function loadAllPermissionGroups() {
    setLoading(true);
    setErr(null);
    setInfo(null);
    try {
      const [crudResult, featureResult] = await Promise.all([
        api<AdminDataRolePermissionsDto[]>("/api/admin/data/permission-groups"),
        api<AdminFeatureRolePermissionsDto[]>("/api/admin/data/feature-permission-groups"),
      ]);
      const crudGroups = crudResult ?? [];
      const featureGroups = featureResult ?? [];
      setPermissionGroups(crudGroups);
      setOriginalPermissionGroups(crudGroups);
      setFeaturePermissionGroups(featureGroups);
      setOriginalFeaturePermissionGroups(featureGroups);
      setSelectedPermissionRole((current) => current || crudGroups[0]?.roleName || "");
      setSelectedFeatureRole((current) => current || featureGroups[0]?.roleName || "");
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.loadFailed", "Failed to load data admin."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadAllPermissionGroups();
  }, []);

  function updatePermissionGrant(tableName: string, permission: PermissionName, checked: boolean) {
    setInfo(null);
    setPermissionGroups((current) =>
      current.map((group) => {
        if (group.roleName !== selectedPermissionRole) return group;

        return {
          ...group,
          grants: group.grants.map((grant) =>
            grant.tableName === tableName ? { ...grant, [permission]: checked } : grant
          ),
        };
      })
    );
  }

  function updateFeatureGrant(featureKey: string, checked: boolean) {
    setInfo(null);
    setFeaturePermissionGroups((current) =>
      current.map((group) => {
        if (group.roleName !== selectedFeatureRole) return group;

        return {
          ...group,
          grants: group.grants.map((grant) =>
            grant.featureKey === featureKey ? { ...grant, isAllowed: checked } : grant
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
    setInfo(null);
    try {
      await api(`/api/admin/data/permission-groups/${encodeURIComponent(group.roleName)}`, {
        method: "PUT",
        body: JSON.stringify(group),
      });
      await loadAllPermissionGroups();
      setInfo(t("dataAdmin.permissionsSaved", "Permissions saved."));
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.permissionsSaveFailed", "Failed to save permissions."));
    } finally {
      setBusy(false);
    }
  }

  async function saveFeaturePermissionGroup() {
    const group = featurePermissionGroups.find((item) => item.roleName === selectedFeatureRole);
    if (!group) return;

    setBusy(true);
    setErr(null);
    setInfo(null);
    try {
      await api(`/api/admin/data/feature-permission-groups/${encodeURIComponent(group.roleName)}`, {
        method: "PUT",
        body: JSON.stringify(group),
      });
      await loadAllPermissionGroups();
      setInfo(t("dataAdmin.permissionsSaved", "Permissions saved."));
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.permissionsSaveFailed", "Failed to save permissions."));
    } finally {
      setBusy(false);
    }
  }

  function setPermissionFilter(permission: PermissionName, value: PermissionFilter) {
    setFilters((current) => ({ ...current, [permission]: value }));
  }

  function resetTableFilters() {
    setTableSearchText("");
    setFilters({
      canRead: "any",
      canCreate: "any",
      canUpdate: "any",
      canDelete: "any",
    });
  }

  function resetFeatureFilters() {
    setFeatureSearchText("");
    setFeatureGroupFilter("all");
    setFeatureAllowedFilter("any");
  }

  function isTableGrantDirty(grant: AdminDataTableGrantDto) {
    const original = originalPermissionGroups
      .find((group) => group.roleName === selectedPermissionRole)
      ?.grants.find((item) => item.tableName === grant.tableName);

    return !!original && permissions.some((permission) => original[permission] !== grant[permission]);
  }

  function isFeatureGrantDirty(grant: AdminFeatureGrantDto) {
    const original = originalFeaturePermissionGroups
      .find((group) => group.roleName === selectedFeatureRole)
      ?.grants.find((item) => item.featureKey === grant.featureKey);

    return !!original && original.isAllowed !== grant.isAllowed;
  }

  const selectedPermissionGroup = useMemo(
    () => permissionGroups.find((group) => group.roleName === selectedPermissionRole) ?? null,
    [permissionGroups, selectedPermissionRole]
  );

  const selectedFeatureGroup = useMemo(
    () => featurePermissionGroups.find((group) => group.roleName === selectedFeatureRole) ?? null,
    [featurePermissionGroups, selectedFeatureRole]
  );

  const featureGroups = useMemo(() => {
    const names = featurePermissionGroups.flatMap((group) => group.grants.map((grant) => grant.groupName));
    return Array.from(new Set(names)).sort((a, b) => a.localeCompare(b));
  }, [featurePermissionGroups]);

  const filteredGrants = useMemo(() => {
    if (!selectedPermissionGroup) return [];

    return selectedPermissionGroup.grants.filter((grant) => {
      if (!matchesTokenizedSearch(grant.tableName, tableSearchText)) return false;

      return permissions.every((permission) => {
        const filter = filters[permission];
        if (filter === "any") return true;
        return filter === "yes" ? grant[permission] : !grant[permission];
      });
    });
  }, [selectedPermissionGroup, tableSearchText, filters]);

  const filteredFeatureGrants = useMemo(() => {
    if (!selectedFeatureGroup) return [];

    return selectedFeatureGroup.grants.filter((grant) => {
      const text = `${grant.featureKey} ${grant.name} ${grant.description} ${grant.groupName}`;
      if (!matchesTokenizedSearch(text, featureSearchText)) return false;
      if (featureGroupFilter !== "all" && grant.groupName !== featureGroupFilter) return false;
      if (featureAllowedFilter === "yes" && !grant.isAllowed) return false;
      if (featureAllowedFilter === "no" && grant.isAllowed) return false;
      return true;
    });
  }, [selectedFeatureGroup, featureSearchText, featureGroupFilter, featureAllowedFilter]);

  return (
    <PageShell title={t("dataAdmin.permissionGroups", "Permission Groups")} error={err} maxWidth={1200}>
      {loading && <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div>}
      {info ? <div className="alert-success" style={{ marginBottom: 12 }}>{info}</div> : null}

      <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 16 }}>
        <button
          style={tabButtonStyle(activeTab === "crud")}
          onClick={() => setActiveTab("crud")}
        >
          {t("dataAdmin.crudPermissions", "CRUD permissions")}
        </button>
        <button
          style={tabButtonStyle(activeTab === "features")}
          onClick={() => setActiveTab("features")}
        >
          {t("dataAdmin.appAccess", "Application access")}
        </button>
      </div>

      {activeTab === "crud" ? (
        <>
          <section style={{ display: "grid", gap: 12, marginBottom: 16 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
              <select value={selectedPermissionRole} onChange={(event) => setSelectedPermissionRole(event.target.value)}>
                {permissionGroups.map((group) => (
                  <option key={group.roleName} value={group.roleName}>
                    {t(`roles.${group.roleName}`, group.roleName)}
                  </option>
                ))}
              </select>
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
              <button onClick={resetTableFilters}>{t("common.resetFilters", "Reset Filters")}</button>
            </div>
          </section>

          {!selectedPermissionGroup ? (
            <div>{t("dataAdmin.noneAccessible", "No accessible tables.")}</div>
          ) : (
            <>
              <div style={{ display: "flex", alignItems: "center", gap: 8, justifyContent: "flex-end", marginBottom: 10 }}>
                <button onClick={() => void savePermissionGroup()} disabled={busy || !selectedPermissionGroup}>
                  {busy ? t("common.saving", "Saving...") : t("dataAdmin.savePermissions", "Save permissions")}
                </button>
                <button onClick={() => void loadAllPermissionGroups()} disabled={loading || busy}>
                  {t("common.reload", "Reload")}
                </button>
              </div>
              <div style={{ overflowX: "auto" }}>
                <table width="100%" cellPadding={8}>
                  <thead>
                    <tr>
                      <th align="left">{t("dataAdmin.table", "Table")}</th>
                      {permissions.map((permission) => (
                        <th key={permission} align="left">
                          {permissionLabel(permission, t)}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {filteredGrants.map((grant) => (
                      <tr key={`${selectedPermissionGroup.roleName}-${grant.tableName}`} style={dirtyRowStyle(isTableGrantDirty(grant))}>
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
            </>
          )}
        </>
      ) : (
        <>
          <section style={{ display: "grid", gap: 12, marginBottom: 16 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
              <select value={selectedFeatureRole} onChange={(event) => setSelectedFeatureRole(event.target.value)}>
                {featurePermissionGroups.map((group) => (
                  <option key={group.roleName} value={group.roleName}>
                    {t(`roles.${group.roleName}`, group.roleName)}
                  </option>
                ))}
              </select>
            </div>

            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
              <input
                placeholder={t("dataAdmin.searchFeatures", "Search application access")}
                value={featureSearchText}
                onChange={(event) => setFeatureSearchText(event.target.value)}
                style={{ minWidth: 260 }}
              />
              <select value={featureGroupFilter} onChange={(event) => setFeatureGroupFilter(event.target.value)}>
                <option value="all">{t("dataAdmin.allGroups", "All groups")}</option>
                {featureGroups.map((group) => (
                  <option key={group} value={group}>{group}</option>
                ))}
              </select>
              <select value={featureAllowedFilter} onChange={(event) => setFeatureAllowedFilter(event.target.value as PermissionFilter)}>
                <option value="any">{t("dataAdmin.any", "Any")}</option>
                <option value="yes">{t("common.yes", "Yes")}</option>
                <option value="no">{t("common.no", "No")}</option>
              </select>
              <button onClick={resetFeatureFilters}>{t("common.resetFilters", "Reset Filters")}</button>
            </div>
          </section>

          {!selectedFeatureGroup ? (
            <div>{t("dataAdmin.noneAccessible", "No application permissions.")}</div>
          ) : (
            <>
              <div style={{ display: "flex", alignItems: "center", gap: 8, justifyContent: "flex-end", marginBottom: 10 }}>
                <button onClick={() => void saveFeaturePermissionGroup()} disabled={busy || !selectedFeatureGroup}>
                  {busy ? t("common.saving", "Saving...") : t("dataAdmin.savePermissions", "Save permissions")}
                </button>
                <button onClick={() => void loadAllPermissionGroups()} disabled={loading || busy}>
                  {t("common.reload", "Reload")}
                </button>
              </div>
              <div style={{ overflowX: "auto" }}>
                <table width="100%" cellPadding={8}>
                  <thead>
                    <tr>
                      <th align="left">{t("dataAdmin.feature", "Feature")}</th>
                      <th align="left">{t("dataAdmin.group", "Group")}</th>
                      <th align="left">{t("dataAdmin.description", "Description")}</th>
                      <th align="left">{t("dataAdmin.allowed", "Allowed")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredFeatureGrants.map((grant) => (
                      <tr key={`${selectedFeatureGroup.roleName}-${grant.featureKey}`} style={dirtyRowStyle(isFeatureGrantDirty(grant))}>
                        <td>
                          <strong>{grant.name}</strong>
                          <div style={{ color: "#667085", fontSize: 12 }}>{grant.featureKey}</div>
                        </td>
                        <td>{grant.groupName}</td>
                        <td>{grant.description}</td>
                        <td>
                          <input
                            type="checkbox"
                            checked={grant.isAllowed}
                            onChange={(event) => updateFeatureGrant(grant.featureKey, event.target.checked)}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </>
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

function tabButtonStyle(active: boolean): CSSProperties {
  return {
    borderColor: active ? "#2563eb" : "#d0d5dd",
    background: active ? "#eff6ff" : "#fff",
    color: active ? "#1d4ed8" : "#344054",
  };
}

function dirtyRowStyle(dirty: boolean): CSSProperties | undefined {
  return dirty
    ? {
        background: "#fff7ed",
        boxShadow: "inset 3px 0 0 #f97316",
      }
    : undefined;
}
