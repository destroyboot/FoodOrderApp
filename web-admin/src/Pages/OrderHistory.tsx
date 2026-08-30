import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../api";
import { orderTypeLabel } from "../orderPresentation";
import { orderStatusDisplayLabel } from "../orderStatus";
import { matchesTokenizedSearch } from "../tokenSearch";
import { useI18n } from "../i18n";

type OrderHistoryRow = {
  id: number;
  status: number;
  orderType: number;
  customerUserId: string | null;
  customerEmail: string | null;
  isAnonymousCustomer: boolean;
  tableNumber: string | null;
  pickupContactName: string | null;
  pickupPhone: string | null;
  deliveryContactName: string | null;
  deliveryPhone: string | null;
  deliveryAddressLine1: string | null;
  deliveryCity: string | null;
  paymentMethod: number;
  paymentStatus: number;
  receiptEmail: string | null;
  scheduledFor: string | null;
  total: number;
  createdAt: string;
};

export default function OrderHistory() {
  const { t } = useI18n();
  const nav = useNavigate();
  const [rows, setRows] = useState<OrderHistoryRow[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchText, setSearchText] = useState("");
  const [emailFilter, setEmailFilter] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [totalMin, setTotalMin] = useState("");
  const [totalMax, setTotalMax] = useState("");

  async function load() {
    setErr(null);
    setLoading(true);

    try {
      const query = new URLSearchParams();
      if (emailFilter.trim()) query.set("email", emailFilter.trim());
      if (fromDate) query.set("from", new Date(`${fromDate}T00:00:00`).toISOString());
      if (toDate) query.set("to", new Date(`${toDate}T23:59:59`).toISOString());
      if (statusFilter) query.set("status", statusFilter);
      if (typeFilter) query.set("orderType", typeFilter);
      query.set("take", "300");

      const result = await api<OrderHistoryRow[]>(`/api/admin/orders/history?${query.toString()}`);
      setRows(result);
    } catch (e: any) {
      setErr(e.message || t("orders.history.loadFailed", "Failed to load order history"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filteredRows = useMemo(
    () =>
      rows.filter((row) =>
        matchesTokenizedSearch(
          [
            row.id,
            row.isAnonymousCustomer ? t("orders.customer.anonymous", "anonymous") : t("orders.customer.user", "user"),
            row.customerEmail ?? "",
            row.receiptEmail ?? "",
            orderStatusDisplayLabel(row.status, row.orderType, t),
            orderTypeLabel(row.orderType, t),
            row.tableNumber ?? "",
            row.pickupContactName ?? "",
            row.pickupPhone ?? "",
            row.deliveryContactName ?? "",
            row.deliveryPhone ?? "",
            row.deliveryAddressLine1 ?? "",
            row.deliveryCity ?? "",
            row.total.toFixed(2),
            new Date(row.createdAt).toLocaleString(),
          ].join(" "),
          searchText
        ) && (!totalMin || row.total >= Number(totalMin)) && (!totalMax || row.total <= Number(totalMax))
      ),
    [rows, searchText, totalMin, totalMax]
  );

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto" }}>
      <div style={{ display: "flex", alignItems: "baseline", gap: 12, flexWrap: "wrap" }}>
        <h2 style={{ marginBottom: 0 }}>{t("nav.orderHistory", "Order History")}</h2>
        <span style={{ color: "#666" }}>{t("orders.history.subtitle", "Completed and cancelled orders")}</span>
      </div>

      {err && <div className="alert-error spaced-top-md">{err}</div>}

      <div style={{ display: "grid", gap: 12, marginTop: 16 }}>
        <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8 }}>
          <input placeholder={t("orders.history.search", "Search history")} value={searchText} onChange={(e) => setSearchText(e.target.value)} />
          <button
            onClick={() => {
              setSearchText("");
              setEmailFilter("");
              setFromDate("");
              setToDate("");
              setStatusFilter("");
              setTypeFilter("");
              setTotalMin("");
              setTotalMax("");
            }}
          >
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(5, minmax(0, 1fr))", gap: 8 }}>
          <input placeholder={t("orders.history.customerEmail", "Customer email")} value={emailFilter} onChange={(e) => setEmailFilter(e.target.value)} />
          <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          <div />
          <button onClick={() => void load()} disabled={loading}>{loading ? t("common.loading", "Loading...") : t("orders.history.applyFilters", "Apply filters")}</button>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(5, minmax(0, 220px))", gap: 8 }}>
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          <option value="">{t("orders.history.allStatuses", "All statuses")}</option>
          <option value="5">{t("orders.status.completed", "Completed")}</option>
          <option value="6">{t("orders.status.cancelled", "Cancelled")}</option>
        </select>
        <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
          <option value="">{t("orders.history.allTypes", "All order types")}</option>
          <option value="0">{t("mobile.restaurants.toTable", "To table")}</option>
          <option value="1">{t("mobile.restaurants.pickup", "Pick up")}</option>
          <option value="2">{t("mobile.restaurants.delivery", "Delivery")}</option>
        </select>
        <input placeholder={t("orders.totalFrom", "Total from")} value={totalMin} onChange={(e) => setTotalMin(e.target.value)} />
        <input placeholder={t("orders.totalTo", "Total to")} value={totalMax} onChange={(e) => setTotalMax(e.target.value)} />
      </div>
      </div>

      <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse", marginTop: 16 }}>
        <thead>
          <tr style={{ borderBottom: "1px solid #ccc", background: "#f9fafb" }}>
            <th align="left">ID</th>
            <th align="left">{t("orders.customer", "Customer")}</th>
            <th align="left">{t("orders.receiptEmail", "Receipt email")}</th>
            <th align="left">{t("orders.type", "Type")}</th>
            <th align="left">{t("orders.status", "Status")}</th>
            <th align="right">{t("orders.total", "Total")}</th>
            <th align="left">{t("orders.created", "Created")}</th>
          </tr>
        </thead>
        <tbody>
          {filteredRows.map((row) => (
            <tr
              key={row.id}
              style={{ borderBottom: "1px solid #eee", cursor: "pointer" }}
              onClick={() => nav(`/orders/${row.id}`, { state: { historyView: true } })}
            >
              <td><Link to={`/orders/${row.id}`} state={{ historyView: true }} onClick={(e) => e.stopPropagation()}>{row.id}</Link></td>
              <td>
                {row.isAnonymousCustomer || !row.customerUserId ? (
                  t("orders.customer.anonymousDisplay", "Anonymous")
                ) : (
                  <Link to={`/users?edit=${encodeURIComponent(row.customerUserId)}`} onClick={(e) => e.stopPropagation()}>
                    {row.customerEmail ?? t("orders.customer.userDisplay", "User")}
                  </Link>
                )}
              </td>
              <td>{row.receiptEmail ?? row.customerEmail ?? "-"}</td>
              <td>{orderTypeLabel(row.orderType, t)}</td>
              <td>{orderStatusDisplayLabel(row.status, row.orderType, t)}</td>
              <td align="right">{row.total.toFixed(2)}</td>
              <td>{new Date(row.createdAt).toLocaleString()}</td>
            </tr>
          ))}
          {!loading && filteredRows.length === 0 ? (
            <tr>
              <td colSpan={7} style={{ color: "#666", padding: 16 }}>
                {t("orders.history.none", "No history entries match the current filters.")}
              </td>
            </tr>
          ) : null}
        </tbody>
      </table>
    </div>
  );
}
