import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { getToken } from "../auth";
import { useI18n } from "../i18n";

type ReportRestaurantOption = {
  id: number;
  name: string;
};

type ReportMeta = {
  canChooseRestaurants: boolean;
  canSeeUserActivity: boolean;
  restaurants: ReportRestaurantOption[];
};

type ReportColumn = {
  key: string;
  labelKey: string;
  label: string;
};

type ReportSummaryMetric = {
  labelKey: string;
  label: string;
  value: string;
};

type ReportResult = {
  titleKey: string;
  title: string;
  columns: ReportColumn[];
  rows: Array<Record<string, unknown>>;
  summary: ReportSummaryMetric[];
};

type ReportOption = {
  key: string;
  labelKey: string;
  fallback: string;
  adminOnly?: boolean;
};

const REPORT_OPTIONS: ReportOption[] = [
  { key: "sales-summary", labelKey: "reports.salesSummaryTitle", fallback: "Sales summary" },
  { key: "customer-frequency", labelKey: "reports.customerFrequencyTitle", fallback: "Customer frequency" },
  { key: "customer-mix", labelKey: "reports.customerMixTitle", fallback: "Registered vs anonymous orders" },
  { key: "menu-popularity", labelKey: "reports.menuPopularityTitle", fallback: "Menu popularity" },
  { key: "user-activity", labelKey: "reports.userActivityTitle", fallback: "User activity", adminOnly: true },
];

function formatCell(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  if (typeof value === "number") {
    return Number.isInteger(value) ? String(value) : value.toFixed(2);
  }

  if (typeof value === "string") {
    const parsed = Date.parse(value);
    if (!Number.isNaN(parsed) && value.includes("T")) {
      return new Date(parsed).toLocaleString();
    }

    return value;
  }

  if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  }

  return String(value);
}

export default function Reports() {
  const { t } = useI18n();
  const [meta, setMeta] = useState<ReportMeta | null>(null);
  const [reportKey, setReportKey] = useState("sales-summary");
  const [from, setFrom] = useState(() => new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString().slice(0, 10));
  const [to, setTo] = useState(() => new Date().toISOString().slice(0, 10));
  const [selectedRestaurantIds, setSelectedRestaurantIds] = useState<number[]>([]);
  const [restaurantSearch, setRestaurantSearch] = useState("");
  const [report, setReport] = useState<ReportResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const availableReports = useMemo(
    () => REPORT_OPTIONS.filter((option) => meta?.canSeeUserActivity || !option.adminOnly),
    [meta?.canSeeUserActivity]
  );

  const filteredRestaurantOptions = useMemo(() => {
    const normalized = restaurantSearch.trim().toLowerCase();
    if (!normalized) {
      return meta?.restaurants ?? [];
    }

    return (meta?.restaurants ?? []).filter((restaurant) => restaurant.name.toLowerCase().includes(normalized));
  }, [meta?.restaurants, restaurantSearch]);

  useEffect(() => {
    void bootstrap();
  }, []);

  useEffect(() => {
    if (!meta) {
      return;
    }

    if (!availableReports.some((option) => option.key === reportKey)) {
      setReportKey(availableReports[0]?.key ?? "sales-summary");
    }
  }, [availableReports, meta, reportKey]);

  async function bootstrap() {
    setErr(null);
    const reportMeta = await api<ReportMeta>("/api/admin/reports/meta");
    setMeta(reportMeta);
  }

  async function loadReport() {
    setLoading(true);
    setErr(null);
    try {
      const params = new URLSearchParams();
      params.set("from", from);
      params.set("to", to);
      for (const restaurantId of selectedRestaurantIds) {
        params.append("restaurantIds", String(restaurantId));
      }

      const payload = await api<ReportResult>(`/api/admin/reports/${reportKey}?${params.toString()}`);
      setReport(payload);
    } catch (error: any) {
      setErr(error.message || t("reports.loadFailed", "Failed to load report."));
      setReport(null);
    } finally {
      setLoading(false);
    }
  }

  async function exportReport(format: "csv" | "xlsx") {
    try {
      const params = new URLSearchParams();
      params.set("format", format);
      params.set("from", from);
      params.set("to", to);
      for (const restaurantId of selectedRestaurantIds) {
        params.append("restaurantIds", String(restaurantId));
      }

      const token = getToken();
      const response = await fetch(`/api/admin/reports/${reportKey}/export?${params.toString()}`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `${reportKey}.${format === "xlsx" ? "xlsx" : "csv"}`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (error: any) {
      setErr(error.message || t("reports.exportFailed", "Failed to export report."));
    }
  }

  function toggleRestaurant(restaurantId: number) {
    setSelectedRestaurantIds((current) =>
      current.includes(restaurantId)
        ? current.filter((id) => id !== restaurantId)
        : [...current, restaurantId]
    );
  }

  function resetFilters() {
    setFrom(new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString().slice(0, 10));
    setTo(new Date().toISOString().slice(0, 10));
    setSelectedRestaurantIds([]);
    setRestaurantSearch("");
  }

  function selectAllRestaurants() {
    setSelectedRestaurantIds(filteredRestaurantOptions.map((restaurant) => restaurant.id));
  }

  function clearRestaurantSelection() {
    setSelectedRestaurantIds([]);
  }

  return (
    <div style={{ maxWidth: 1320, margin: "0 auto", display: "grid", gap: 18 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 16, flexWrap: "wrap" }}>
        <div>
          <h2 style={{ marginBottom: 6 }}>{t("page.reports", "Reports")}</h2>
          <div style={{ color: "#667085" }}>
            {t("reports.pageHint", "Generate business reports for restaurant operations and export them to CSV or Excel.")}
          </div>
        </div>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={() => void exportReport("csv")} disabled={!report}>
            {t("reports.exportCsv", "Export CSV")}
          </button>
          <button onClick={() => void exportReport("xlsx")} disabled={!report}>
            {t("reports.exportExcel", "Export Excel")}
          </button>
        </div>
      </div>

      {err ? <div style={{ color: "#b42318" }}>{err}</div> : null}

      <div style={{ display: "grid", gap: 14, padding: 20, background: "#fff", borderRadius: 8, border: "1px solid #e7e9ee" }}>
        <div style={{ display: "grid", gridTemplateColumns: meta?.canChooseRestaurants ? "1.2fr 1fr 1fr" : "1.4fr 1fr 1fr", gap: 12 }}>
          <label style={{ display: "grid", gap: 6 }}>
            <span>{t("reports.reportType", "Report type")}</span>
            <select value={reportKey} onChange={(event) => setReportKey(event.target.value)}>
              {availableReports.map((option) => (
                <option key={option.key} value={option.key}>
                  {t(option.labelKey, option.fallback)}
                </option>
              ))}
            </select>
          </label>

          <label style={{ display: "grid", gap: 6 }}>
            <span>{t("reports.fromDate", "From")}</span>
            <input type="date" value={from} onChange={(event) => setFrom(event.target.value)} />
          </label>

          <label style={{ display: "grid", gap: 6 }}>
            <span>{t("reports.toDate", "To")}</span>
            <input type="date" value={to} onChange={(event) => setTo(event.target.value)} />
          </label>
        </div>

        {meta?.canChooseRestaurants ? (
          <div style={{ display: "grid", gap: 8 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, flexWrap: "wrap" }}>
              <div style={{ fontWeight: 700 }}>{t("reports.restaurants", "Restaurants")}</div>
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button type="button" onClick={selectAllRestaurants} style={{ background: "#fff", color: "#344054" }}>
                  {t("reports.selectAllRestaurants", "Select all")}
                </button>
                <button type="button" onClick={clearRestaurantSelection} style={{ background: "#fff", color: "#344054" }}>
                  {t("reports.clearRestaurantSelection", "Clear selection")}
                </button>
              </div>
            </div>
            <input
              placeholder={t("reports.searchRestaurants", "Search restaurants")}
              value={restaurantSearch}
              onChange={(event) => setRestaurantSearch(event.target.value)}
            />
            <div style={{ border: "1px solid #d0d5dd", borderRadius: 8, padding: 12, maxHeight: 240, overflowY: "auto", display: "grid", gap: 8 }}>
              {filteredRestaurantOptions.map((restaurant) => (
                <label key={restaurant.id} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                  <input
                    type="checkbox"
                    checked={selectedRestaurantIds.includes(restaurant.id)}
                    onChange={() => toggleRestaurant(restaurant.id)}
                  />
                  <span>{restaurant.name}</span>
                </label>
              ))}
              {filteredRestaurantOptions.length === 0 ? (
                <div style={{ color: "#667085" }}>{t("reports.noRestaurantsMatch", "No restaurants match the search.")}</div>
              ) : null}
            </div>
            <div style={{ color: "#667085", fontSize: "0.92rem" }}>
              {t("reports.restaurantHint", "Leave everything unchecked to include all accessible restaurants.")}
            </div>
          </div>
        ) : null}

        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={() => void loadReport()} disabled={loading}>
            {loading ? t("reports.loading", "Loading...") : t("reports.generate", "Generate report")}
          </button>
          <button onClick={resetFilters} style={{ background: "#fff", color: "#344054" }}>
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
      </div>

      {report ? (
        <>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
            {report.summary.map((metric) => (
              <div key={metric.labelKey} style={{ padding: 16, borderRadius: 8, background: "#fff", border: "1px solid #e7e9ee" }}>
                <div style={{ color: "#667085", fontSize: "0.9rem", marginBottom: 8 }}>
                  {t(metric.labelKey, metric.label)}
                </div>
                <div style={{ fontSize: "1.35rem", fontWeight: 800, color: "#101828" }}>{metric.value}</div>
              </div>
            ))}
          </div>

          <div style={{ padding: 20, background: "#fff", borderRadius: 8, border: "1px solid #e7e9ee" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, marginBottom: 14, flexWrap: "wrap" }}>
              <h3 style={{ margin: 0 }}>{t(report.titleKey, report.title)}</h3>
              <div style={{ color: "#667085" }}>
                {t("reports.rowCount", "Rows")}: {report.rows.length}
              </div>
            </div>

            {report.rows.length === 0 ? (
              <div style={{ color: "#667085" }}>{t("reports.noData", "No data found for the selected filters.")}</div>
            ) : (
              <div style={{ overflowX: "auto" }}>
                <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ borderBottom: "1px solid #e7e9ee" }}>
                      {report.columns
                        .filter((column) => meta?.canChooseRestaurants || (column.key !== "RestaurantId" && column.key !== "RestaurantName"))
                        .map((column) => (
                        <th key={column.key} align="left" style={{ whiteSpace: "nowrap" }}>
                          {t(column.labelKey, column.label)}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {report.rows.map((row, rowIndex) => (
                      <tr key={rowIndex} style={{ borderBottom: "1px solid #f2f4f7" }}>
                        {report.columns
                          .filter((column) => meta?.canChooseRestaurants || (column.key !== "RestaurantId" && column.key !== "RestaurantName"))
                          .map((column) => (
                          <td key={`${rowIndex}-${column.key}`}>{formatCell(row[column.key])}</td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      ) : null}
    </div>
  );
}
