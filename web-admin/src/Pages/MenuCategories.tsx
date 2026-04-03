import { useEffect, useState } from "react";
import { api } from "../api";

type CategoryDto = {
  id: number;
  name: string;
  description?: string | null;
};

export default function MenuCategories() {
  const [data, setData] = useState<CategoryDto[]>([]);
  const [name, setName] = useState("");
  const [desc, setDesc] = useState("");
  const [err, setErr] = useState<string | null>(null);

  async function load() {
    const res = await api<CategoryDto[]>("/api/admin/menu/categories");
    setData(res);
  }

  useEffect(() => {
    load().catch((e: any) => setErr(e.message));
  }, []);

  async function create() {
    setErr(null);
    await api("/api/admin/menu/categories", {
      method: "POST",
      body: JSON.stringify({ name, description: desc }),
    });
    setName("");
    setDesc("");
    await load();
  }

  async function update(c: CategoryDto) {
    const newName = prompt("New name:", c.name);
    if (!newName) return;

    // For now hardcode culture:
    const culture = "en-US";

    await api(`/api/admin/menu/categories/${c.id}/translations`, {
      method: "PUT",
      body: JSON.stringify({ culture, name: newName, description: c.description ?? null }),
    });

    await load();
  }

  async function remove(id: number) {
    if (!confirm("Delete category?")) return;

    await api(`/api/admin/menu/categories/${id}`, {
      method: "DELETE",
    });

    await load();
  }

  return (
    <div style={{ maxWidth: 900, margin: "20px auto" }}>
      <h2>Menu Categories</h2>

      {err && <div style={{ color: "crimson" }}>{err}</div>}

      <div style={{ marginBottom: 20 }}>
        <input
          placeholder="Category name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <input
          placeholder="Description"
          value={desc}
          onChange={(e) => setDesc(e.target.value)}
          style={{ marginLeft: 8 }}
        />
        <button onClick={create} style={{ marginLeft: 8 }}>
          Add
        </button>
      </div>

      <table width="100%" cellPadding={8}>
        <thead>
          <tr>
            <th align="left">Id</th>
            <th align="left">Name</th>
            <th align="left">Description</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data.map((c) => (
            <tr key={c.id}>
              <td>{c.id}</td>
              <td>{c.name}</td>
              <td>{c.description ?? "-"}</td>
              <td>
                <button onClick={() => update(c)}>Edit</button>
                <button onClick={() => remove(c.id)} style={{ marginLeft: 6 }}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}