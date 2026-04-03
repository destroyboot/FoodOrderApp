import { useEffect, useState } from "react";
import { api } from "../api";

type MenuItemRow = {
  id: number;
  menuCategoryId: number;
  currentPrice: number;
  isAvailable: boolean;
  photoAssetId?: number | null;
  name: string;
  description?: string | null;
};

export default function MenuItems() {
  const [items, setItems] = useState<MenuItemRow[]>([]);
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    const data = await api<MenuItemRow[]>("/api/admin/menu/items?culture=en-US");
    setItems(data);
  }

  useEffect(() => {
    load().catch((e: any) => setErr(e.message));
  }, []);

  return (
    <div style={{ maxWidth: 1100, margin: "20px auto" }}>
      <h2>Menu Items</h2>
      {err && <div style={{ color: "crimson" }}>{err}</div>}

      <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
        <thead>
          <tr style={{ borderBottom: "1px solid #ccc" }}>
            <th align="left">Id</th>
            <th align="left">Name</th>
            <th align="left">CategoryId</th>
            <th align="right">Price</th>
            <th align="left">Available</th>
          </tr>
        </thead>
        <tbody>
          {items.map((m) => (
            <tr key={m.id} style={{ borderBottom: "1px solid #eee" }}>
              <td>{m.id}</td>
              <td>{m.name}</td>
              <td>{m.menuCategoryId}</td>
              <td align="right">{Number(m.currentPrice).toFixed(2)}</td>
              <td>{m.isAvailable ? "Yes" : "No"}</td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={5} style={{ padding: 16, color: "#666" }}>
                No items.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}