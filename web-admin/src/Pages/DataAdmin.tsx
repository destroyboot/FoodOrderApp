import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { matchesTokenizedSearch } from "../tokenSearch";
import { useI18n } from "../i18n";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";

type AdminDataColumnDto = {
  name: string;
  dataType: string;
  isNullable: boolean;
  isPrimaryKey: boolean;
  isEditable: boolean;
  enumValues: string[];
  foreignKeyTableName?: string | null;
  foreignKeyPrimaryKeyName?: string | null;
  foreignKeyLabelPropertyName?: string | null;
};

type AdminDataTablePermissionsDto = {
  canRead: boolean;
  canCreate: boolean;
  canUpdate: boolean;
  canDelete: boolean;
};

type AdminDataTableDto = {
  tableName: string;
  displayName: string;
  primaryKeyName: string;
  isRestaurantScoped: boolean;
  permissions: AdminDataTablePermissionsDto;
  columns: AdminDataColumnDto[];
};

type AdminDataOptionDto = {
  value: string;
  label: string;
};

type AdminDataColumnOptionsDto = {
  columnName: string;
  options: AdminDataOptionDto[];
};

type RowData = Record<string, unknown>;
type ColumnFilterValue = string | boolean;

function valueToInput(value: unknown, dataType: string) {
  if (value === null || value === undefined) return "";

  if (dataType === "enum") return String(value);
  if (dataType === "datetime") {
    const date = new Date(String(value));
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, "0");
    const day = `${date.getDate()}`.padStart(2, "0");
    const hours = `${date.getHours()}`.padStart(2, "0");
    const minutes = `${date.getMinutes()}`.padStart(2, "0");
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  if (dataType === "boolean") return Boolean(value);
  return String(value);
}

function normalizeEnumInput(value: unknown, enumValues: string[]) {
  if (value === null || value === undefined || value === "") return "";

  const text = String(value);
  if (/^\d+$/.test(text)) {
    const index = Number(text);
    return enumValues[index] ?? text;
  }

  return text;
}

function inputToValue(value: string | boolean, dataType: string) {
  if (dataType === "boolean") return Boolean(value);
  if (value === "") return null;

  if (dataType === "number") return Number(value);
  if (dataType === "datetime") return new Date(String(value)).toISOString();
  return value;
}

function formatDateTime(value: unknown) {
  if (value === null || value === undefined || value === "") return "";

  const date = new Date(String(value));
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return date.toLocaleString();
}

export default function DataAdmin() {
  const { t } = useI18n();

  const [tables, setTables] = useState<AdminDataTableDto[]>([]);
  const [selectedTableName, setSelectedTableName] = useState("");
  const [rows, setRows] = useState<RowData[]>([]);
  const [columnOptions, setColumnOptions] = useState<Record<string, AdminDataOptionDto[]>>({});
  const [form, setForm] = useState<Record<string, unknown>>({});
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [tableSearchText, setTableSearchText] = useState("");
  const [debouncedSearchText, setDebouncedSearchText] = useState("");
  const [columnFilters, setColumnFilters] = useState<Record<string, ColumnFilterValue>>({});
  const [selectedKeys, setSelectedKeys] = useState<string[]>([]);
  const [bulkBusy, setBulkBusy] = useState(false);
  const [showRowModal, setShowRowModal] = useState(false);

  const selectedTable = useMemo(
    () => tables.find((table) => table.tableName === selectedTableName) ?? null,
    [tables, selectedTableName]
  );

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setDebouncedSearchText(searchText.trim());
    }, 250);

    return () => window.clearTimeout(handle);
  }, [searchText]);

  async function loadTables(nextTableName?: string) {
    const result = await api<AdminDataTableDto[]>("/api/admin/data/tables");
    const nextTables = result ?? [];
    setTables(nextTables);

    const preferredTableName = nextTableName && nextTables.some((table) => table.tableName === nextTableName)
      ? nextTableName
      : nextTables.find((table) => table.permissions.canRead)?.tableName ?? nextTables[0]?.tableName ?? "";

    setSelectedTableName(preferredTableName);
    return { nextTables, targetTableName: preferredTableName };
  }

  async function loadRows(tableName = selectedTableName, nextSearch = debouncedSearchText) {
    if (!tableName) {
      setRows([]);
      return;
    }

    const suffix = nextSearch ? `?search=${encodeURIComponent(nextSearch)}` : "";
    const result = await api<RowData[]>(`/api/admin/data/tables/${encodeURIComponent(tableName)}/rows${suffix}`);
    setRows(result ?? []);
  }

  async function loadEditorOptions(tableName: string) {
    if (!tableName) {
      setColumnOptions({});
      return;
    }

    const result = await api<AdminDataColumnOptionsDto[]>(
      `/api/admin/data/tables/${encodeURIComponent(tableName)}/editor-options`
    );

    const nextOptions: Record<string, AdminDataOptionDto[]> = {};
    for (const column of result ?? []) {
      nextOptions[column.columnName] = column.options;
    }

    setColumnOptions(nextOptions);
  }

  async function init() {
    setLoading(true);
    setErr(null);

    try {
      const { nextTables, targetTableName } = await loadTables();
      await Promise.all([
        targetTableName ? loadEditorOptions(targetTableName) : Promise.resolve(),
        targetTableName && (nextTables.find((table) => table.tableName === targetTableName)?.permissions.canRead ?? true)
          ? loadRows(targetTableName, "")
          : Promise.resolve(),
      ]);
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.loadFailed", "Failed to load data admin."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    init();
  }, []);

  useEffect(() => {
    if (!selectedTableName || !selectedTable?.permissions.canRead) return;

    loadRows(selectedTableName, debouncedSearchText).catch((e: any) => {
      setErr(e.message || t("dataAdmin.searchFailed", "Failed to search table rows."));
    });
  }, [debouncedSearchText, selectedTableName]);

  useEffect(() => {
    setSelectedKeys([]);
  }, [rows, columnFilters, selectedTableName]);

  function startCreate() {
    setEditingKey(null);
    if (!selectedTable) {
      setForm({});
      return;
    }

    const nextForm: Record<string, unknown> = {};
    for (const column of selectedTable.columns.filter((column) => column.isEditable)) {
      nextForm[column.name] = column.dataType === "boolean" ? false : "";
    }
    setForm(nextForm);
    setShowRowModal(true);
  }

  function startEdit(row: RowData) {
    if (!selectedTable) return;

    const nextForm: Record<string, unknown> = {};
    for (const column of selectedTable.columns.filter((column) => column.isEditable)) {
      nextForm[column.name] = column.dataType === "enum"
        ? normalizeEnumInput(row[column.name], column.enumValues)
        : valueToInput(row[column.name], column.dataType);
    }

    setEditingKey(String(row[selectedTable.primaryKeyName]));
    setForm(nextForm);
    setShowRowModal(true);
  }

  async function changeTable(tableName: string) {
    setSelectedTableName(tableName);
    setEditingKey(null);
    setForm({});
    setErr(null);
    setColumnFilters({});
    setSelectedKeys([]);

    try {
      await Promise.all([
        loadEditorOptions(tableName),
        loadRows(tableName, debouncedSearchText),
      ]);
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.rowsLoadFailed", "Failed to load table rows."));
    }
  }

  async function saveRow() {
    if (!selectedTable) return;

    setErr(null);
    const payload: Record<string, unknown> = {};
    for (const column of selectedTable.columns.filter((column) => column.isEditable)) {
      payload[column.name] = inputToValue(form[column.name] as string | boolean, column.dataType);
    }

    try {
      if (editingKey) {
        await api(`/api/admin/data/tables/${encodeURIComponent(selectedTable.tableName)}/rows/${encodeURIComponent(editingKey)}`, {
          method: "PUT",
          body: JSON.stringify({ values: payload }),
        });
      } else {
        await api(`/api/admin/data/tables/${encodeURIComponent(selectedTable.tableName)}/rows`, {
          method: "POST",
          body: JSON.stringify({ values: payload }),
        });
      }

      startCreate();
      setShowRowModal(false);
      await loadRows(selectedTable.tableName, debouncedSearchText);
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.rowSaveFailed", "Failed to save row."));
    }
  }

  async function deleteRow(row: RowData) {
    if (!selectedTable) return;
    const key = String(row[selectedTable.primaryKeyName]);
    if (!confirm(`${t("dataAdmin.deleteRowConfirm", "Delete row")} ${key} ${t("dataAdmin.fromTable", "from")} ${selectedTable.tableName}?`)) return;

    setErr(null);
    try {
      await api(`/api/admin/data/tables/${encodeURIComponent(selectedTable.tableName)}/rows/${encodeURIComponent(key)}`, {
        method: "DELETE",
      });
      await loadRows(selectedTable.tableName, debouncedSearchText);
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.rowDeleteFailed", "Failed to delete row."));
    }
  }

  async function bulkDeleteSelected() {
    if (!selectedTable || selectedKeys.length === 0) return;
    if (!confirm(`${t("dataAdmin.deleteSelectedConfirm", "Delete")} ${selectedKeys.length} ${t("dataAdmin.selectedRows", "selected row(s)")} ${t("dataAdmin.fromTable", "from")} ${selectedTable.tableName}?`)) return;

    setErr(null);
    setBulkBusy(true);
    try {
      await api(`/api/admin/data/tables/${encodeURIComponent(selectedTable.tableName)}/rows/bulk-delete`, {
        method: "POST",
        body: JSON.stringify({ keys: selectedKeys }),
      });

      setSelectedKeys([]);
      await loadRows(selectedTable.tableName, debouncedSearchText);
    } catch (e: any) {
      setErr(e.message || t("dataAdmin.bulkDeleteFailed", "Failed to delete selected rows."));
    } finally {
      setBulkBusy(false);
    }
  }

  function renderCellValue(column: AdminDataColumnDto, value: unknown) {
    if (value === null || value === undefined) return "";

    if (column.dataType === "datetime") {
      return formatDateTime(value);
    }

    if (column.dataType === "boolean") {
      return Boolean(value) ? t("common.yes", "Yes") : t("common.no", "No");
    }

    if (column.dataType === "enum") {
      return normalizeEnumInput(value, column.enumValues);
    }

    const options = columnOptions[column.name];
    if (options && options.length > 0) {
      const match = options.find((option) => option.value === String(value));
      if (match) return match.label;
    }

    return String(value);
  }

  const filteredRows = useMemo(() => {
    if (!selectedTable) return rows;

    return rows.filter((row) =>
      selectedTable.columns.every((column) => {
        const filterValue = columnFilters[column.name];
        if (filterValue === undefined || filterValue === "" || filterValue === "__any__") {
          return true;
        }

        const renderedValue = renderCellValue(column, row[column.name]);
        if (column.dataType === "boolean") {
          const expected = filterValue === true || filterValue === "true";
          return renderedValue === (expected ? "Yes" : "No");
        }

        if (column.dataType === "enum" || column.foreignKeyTableName) {
          return String(filterValue) === String(row[column.name]) || String(filterValue) === String(renderedValue);
        }

        return matchesTokenizedSearch(String(renderedValue), String(filterValue));
      })
    );
  }, [rows, selectedTable, columnFilters, columnOptions]);

  const allVisibleKeys = useMemo(() => {
    if (!selectedTable) return [];
    return filteredRows.map((row) => String(row[selectedTable.primaryKeyName]));
  }, [filteredRows, selectedTable]);

  function toggleRowSelection(key: string, checked: boolean) {
    setSelectedKeys((prev) =>
      checked ? Array.from(new Set([...prev, key])) : prev.filter((item) => item !== key)
    );
  }

  function selectAllVisible() {
    setSelectedKeys(allVisibleKeys);
  }

  function clearSelection() {
    setSelectedKeys([]);
  }

  const filteredTables = useMemo(
    () => tables.filter((table) => matchesTokenizedSearch(`${table.tableName} ${table.displayName}`, tableSearchText)),
    [tables, tableSearchText]
  );

  return (
    <PageShell title={t("nav.dataTables", "Data Tables")} error={err} maxWidth={1400}>
      {loading && <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div>}

      <div style={{ display: "grid", gridTemplateColumns: "260px 1fr", gap: 20 }}>
        <aside>
          <h3>{t("dataAdmin.accessibleTables", "Accessible tables")}</h3>
          <div style={{ display: "grid", gap: 8, marginBottom: 10 }}>
            <input
              placeholder={t("dataAdmin.searchTables", "Search tables")}
              value={tableSearchText}
              onChange={(e) => setTableSearchText(e.target.value)}
            />
            <button onClick={() => setTableSearchText("")}>{t("common.resetFilters", "Reset Filters")}</button>
          </div>
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {filteredTables.map((table) => (
              <button
                key={table.tableName}
                onClick={() => changeTable(table.tableName)}
                style={{
                  textAlign: "left",
                  padding: "8px 10px",
                  background: table.tableName === selectedTableName ? "#efefef" : "#fff",
                  border: "1px solid #ccc",
                  borderRadius: 6,
                }}
              >
                {table.tableName}
              </button>
            ))}
          </div>
        </aside>

        <main>
          {!selectedTable ? (
            <div>{t("dataAdmin.noneAccessible", "No accessible tables.")}</div>
          ) : (
            <>
              <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 12, flexWrap: "wrap" }}>
                <h3 style={{ margin: 0 }}>{selectedTable.tableName}</h3>
                <button onClick={startCreate} disabled={!selectedTable.permissions.canCreate}>
                  {t("dataAdmin.newRow", "New row")}
                </button>
                <button onClick={() => loadRows(selectedTable.tableName, debouncedSearchText)} disabled={!selectedTable.permissions.canRead}>
                  {t("common.reload", "Reload")}
                </button>
                {selectedTable.permissions.canDelete && (
                  <>
                    <button onClick={selectAllVisible} disabled={allVisibleKeys.length === 0}>
                      {t("dataAdmin.selectAll", "Select all")}
                    </button>
                    <button onClick={clearSelection} disabled={selectedKeys.length === 0}>
                      {t("dataAdmin.deselectAll", "Deselect all")}
                    </button>
                    <button onClick={bulkDeleteSelected} disabled={selectedKeys.length === 0 || bulkBusy}>
                      {bulkBusy ? t("dataAdmin.deleting", "Deleting...") : `${t("dataAdmin.deleteSelected", "Delete selected")} (${selectedKeys.length})`}
                    </button>
                  </>
                )}
                <input
                  placeholder={t("dataAdmin.searchColumns", "Search all listed columns")}
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                  style={{ marginLeft: "auto", minWidth: 280 }}
                />
                <button
                  onClick={() => {
                    setSearchText("");
                    setColumnFilters({});
                  }}
                >
                  {t("common.resetFilters", "Reset Filters")}
                </button>
              </div>

              <div style={{ marginBottom: 16 }}>
                <strong>{t("dataAdmin.columns", "Columns")}:</strong>{" "}
                {selectedTable.columns.map((column) => `${column.name}${column.isPrimaryKey ? " (PK)" : ""}`).join(", ")}
              </div>

              {!selectedTable.permissions.canRead && (
                <div style={{ marginBottom: 16 }}>
                  {t("dataAdmin.writeWithoutRead", "You have write access to this table, but not read access.")}
                </div>
              )}

              {selectedTable.permissions.canRead && (
                <div style={{ overflowX: "auto" }}>
                  <table width="100%" cellPadding={8}>
                    <thead>
                      <tr>
                        {selectedTable.permissions.canDelete && <th align="left">{t("users.select", "Select")}</th>}
                        {selectedTable.columns.map((column) => (
                          <th key={column.name} align="left">{column.name}</th>
                        ))}
                        {(selectedTable.permissions.canUpdate || selectedTable.permissions.canDelete) && <th></th>}
                      </tr>
                      <tr>
                        {selectedTable.permissions.canDelete && <th></th>}
                        {selectedTable.columns.map((column) => (
                          <th key={`${column.name}-filter`} align="left">
                            {column.dataType === "boolean" ? (
                              <select
                                value={String(columnFilters[column.name] ?? "__any__")}
                                onChange={(e) => setColumnFilters((prev) => ({ ...prev, [column.name]: e.target.value }))}
                              >
                                <option value="__any__">{t("dataAdmin.any", "Any")}</option>
                                <option value="true">{t("common.yes", "Yes")}</option>
                                <option value="false">{t("common.no", "No")}</option>
                              </select>
                            ) : column.dataType === "enum" ? (
                              <select
                                value={String(columnFilters[column.name] ?? "")}
                                onChange={(e) => setColumnFilters((prev) => ({ ...prev, [column.name]: e.target.value }))}
                              >
                                <option value="">{t("common.all", "All")}</option>
                                {column.enumValues.map((enumValue) => (
                                  <option key={enumValue} value={enumValue}>
                                    {enumValue}
                                  </option>
                                ))}
                              </select>
                            ) : column.foreignKeyTableName ? (
                              <select
                                value={String(columnFilters[column.name] ?? "")}
                                onChange={(e) => setColumnFilters((prev) => ({ ...prev, [column.name]: e.target.value }))}
                              >
                                <option value="">{t("common.all", "All")}</option>
                                {(columnOptions[column.name] ?? []).map((option) => (
                                  <option key={option.value} value={option.value}>
                                    {option.label}
                                  </option>
                                ))}
                              </select>
                            ) : (
                              <input
                                placeholder={t("dataAdmin.filter", "Filter")}
                                value={String(columnFilters[column.name] ?? "")}
                                onChange={(e) => setColumnFilters((prev) => ({ ...prev, [column.name]: e.target.value }))}
                                style={{ width: "100%" }}
                              />
                            )}
                          </th>
                        ))}
                        {(selectedTable.permissions.canUpdate || selectedTable.permissions.canDelete) && <th></th>}
                      </tr>
                    </thead>
                    <tbody>
                      {filteredRows.map((row, index) => {
                        const rowKey = String(row[selectedTable.primaryKeyName]);
                        const checked = selectedKeys.includes(rowKey);

                        return (
                          <tr key={`${selectedTable.tableName}-${row[selectedTable.primaryKeyName] ?? index}`}>
                            {selectedTable.permissions.canDelete && (
                              <td>
                                <input
                                  type="checkbox"
                                  checked={checked}
                                  onChange={(e) => toggleRowSelection(rowKey, e.target.checked)}
                                />
                              </td>
                            )}
                            {selectedTable.columns.map((column) => (
                              <td key={column.name}>{renderCellValue(column, row[column.name])}</td>
                            ))}
                            {(selectedTable.permissions.canUpdate || selectedTable.permissions.canDelete) && (
                              <td>
                                <div style={{ display: "flex", gap: 8 }}>
                                  {selectedTable.permissions.canUpdate && (
                                    <button onClick={() => startEdit(row)}>{t("common.edit", "Edit")}</button>
                                  )}
                                  {selectedTable.permissions.canDelete && (
                                    <button onClick={() => deleteRow(row)} className="button-danger">
                                      {t("common.delete", "Delete")}
                                    </button>
                                  )}
                                </div>
                              </td>
                            )}
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </main>
      </div>

      {showRowModal && selectedTable ? (
        <ModalShell
          title={editingKey ? `${t("dataAdmin.editRow", "Edit row")} ${editingKey}` : t("dataAdmin.createRow", "Create row")}
          onClose={() => {
            setShowRowModal(false);
            setEditingKey(null);
            setForm({});
          }}
          className="modal-card-wide"
        >
          <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8 }}>
            {selectedTable.columns.filter((column) => column.isEditable).map((column) => (
              <label key={`modal-${column.name}`} style={{ display: "grid", gap: 6 }}>
                <span>{column.name}</span>
                {column.dataType === "boolean" ? (
                  <div>
                    <input
                      type="checkbox"
                      checked={Boolean(form[column.name])}
                      onChange={(e) => setForm((prev) => ({ ...prev, [column.name]: e.target.checked }))}
                    />
                  </div>
                ) : column.dataType === "enum" ? (
                  <select value={String(form[column.name] ?? "")} onChange={(e) => setForm((prev) => ({ ...prev, [column.name]: e.target.value }))}>
                    {column.isNullable && <option value="">-- none --</option>}
                    {column.enumValues.map((enumValue) => <option key={enumValue} value={enumValue}>{enumValue}</option>)}
                  </select>
                ) : column.foreignKeyTableName ? (
                  <select value={String(form[column.name] ?? "")} onChange={(e) => setForm((prev) => ({ ...prev, [column.name]: e.target.value }))}>
                    <option value="">{column.isNullable ? "-- none --" : "-- select --"}</option>
                    {(columnOptions[column.name] ?? []).map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                  </select>
                ) : (
                  <input
                    type={column.dataType === "number" ? "number" : column.dataType === "datetime" ? "datetime-local" : "text"}
                    value={String(form[column.name] ?? "")}
                    onChange={(e) => setForm((prev) => ({ ...prev, [column.name]: e.target.value }))}
                  />
                )}
              </label>
            ))}
          </div>
          <div style={{ display: "flex", gap: 8, justifyContent: "flex-end", marginTop: 12 }}>
            <button onClick={saveRow}>{editingKey ? t("users.saveChanges", "Save changes") : t("dataAdmin.createRow", "Create row")}</button>
            <button onClick={() => { setShowRowModal(false); setEditingKey(null); setForm({}); }}>{t("common.cancel", "Cancel")}</button>
          </div>
        </ModalShell>
      ) : null}
    </PageShell>
  );
}
