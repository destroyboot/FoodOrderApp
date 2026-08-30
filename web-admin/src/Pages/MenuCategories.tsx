import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { matchesTokenizedSearch } from "../tokenSearch";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";

type CategoryTranslationDto = {
  culture: string;
  name: string;
  description?: string | null;
};

type CategoryDto = {
  id: number;
  restaurantId: number;
  name?: string | null;
  description?: string | null;
  isActive?: boolean;
  sortOrder?: number;
  translations?: CategoryTranslationDto[];
};

type CategoryFormState = {
  restaurantId: number;
  isActive: boolean;
  sortOrder: number;
  translations: CategoryTranslationDto[];
};

type RestaurantDto = {
  id: number;
  name: string;
};

type RestaurantSettingsDto = {
  supportedCultures: string;
  defaultCulture: string;
};

type RestaurantCultureConfig = {
  supportedCultures: string[];
  defaultCulture: string;
};

function defaultTranslations(cultures: string[]): CategoryTranslationDto[] {
  return cultures.map((culture) => ({ culture, name: "", description: "" }));
}

const emptyForm = (cultures: string[] = ["pl-PL", "en-US"]): CategoryFormState => ({
  restaurantId: 0,
  isActive: true,
  sortOrder: 0,
  translations: defaultTranslations(cultures),
});

export default function MenuCategories() {
  const { culture, t } = useI18n();
  const [data, setData] = useState<CategoryDto[]>([]);
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [selectedCulture, setSelectedCulture] = useState(culture);
  const [activeFilter, setActiveFilter] = useState("all");

  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [restaurantCultures, setRestaurantCultures] = useState<Record<number, RestaurantCultureConfig>>({});

  const [form, setForm] = useState<CategoryFormState>(emptyForm());
  const dataCulture = selectedCulture;

  async function load() {
    setLoading(true);
    setErr(null);

    try {
      const [restaurantList, categories] = await Promise.all([
        api<RestaurantDto[]>("/api/admin/restaurants"),
        api<CategoryDto[]>(`/api/admin/menu/categories?culture=${encodeURIComponent(dataCulture)}`),
      ]);

      setRestaurants(restaurantList ?? []);
      setData(categories ?? []);
      const settingsEntries = await Promise.all(
        (restaurantList ?? []).map(async (restaurant) => {
          const settings = await api<RestaurantSettingsDto>(`/api/restaurants/${restaurant.id}/settings`);
          return [
            restaurant.id,
            {
              supportedCultures: settings.supportedCultures.split(",").map((value) => value.trim()).filter(Boolean),
              defaultCulture: settings.defaultCulture,
            },
          ] as const;
        })
      );
      setRestaurantCultures(Object.fromEntries(settingsEntries));
      if (!form.restaurantId && (restaurantList?.length ?? 0) > 0) {
        const firstRestaurantId = restaurantList[0].id;
        const firstCultures =
          settingsEntries.find(([restaurantId]) => restaurantId === firstRestaurantId)?.[1].supportedCultures ?? ["pl-PL", "en-US"];
        setForm({ ...emptyForm(firstCultures), restaurantId: firstRestaurantId });
      }
    } catch (e: any) {
      setErr(e.message || t("categories.loadFailed", "Failed to load categories."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [dataCulture]);

  useEffect(() => {
    setSelectedCulture(culture);
  }, [culture]);

  const restaurantMap = useMemo(
    () => Object.fromEntries(restaurants.map((restaurant) => [restaurant.id, restaurant.name])),
    [restaurants]
  );

  const cultureOptions = useMemo(() => {
    const values = new Set<string>();
    Object.values(restaurantCultures).forEach((config) => config.supportedCultures.forEach((value) => values.add(value)));
    return Array.from(values).sort((a, b) => a.localeCompare(b));
  }, [restaurantCultures]);

  function getDisplayedTranslation(category: CategoryDto) {
    const translations = category.translations ?? [];
    const config = restaurantCultures[category.restaurantId];
    const preferredCulture =
      selectedCulture !== "all" && config && !config.supportedCultures.includes(selectedCulture)
        ? config.defaultCulture
        : selectedCulture;

    if (preferredCulture !== "all") {
      const match = translations.find((translation) => translation.culture === preferredCulture);
      if (match) {
        return {
          name: match.name,
          description: match.description ?? "",
        };
      }
    }

    if (config?.defaultCulture) {
      const fallback = translations.find((translation) => translation.culture === config.defaultCulture);
      if (fallback) {
        return {
          name: fallback.name,
          description: fallback.description ?? "",
        };
      }
    }

    const first = translations[0];
    return {
      name: category.name ?? first?.name ?? "",
      description: category.description ?? first?.description ?? "",
    };
  }

  const filteredData = data.filter((category) => {
    const displayed = getDisplayedTranslation(category);
    const matchesSearch = matchesTokenizedSearch(
      [
        category.id,
        displayed.name,
        displayed.description,
        category.sortOrder ?? 0,
        category.isActive ? "active yes" : "active no",
        restaurantMap[category.restaurantId] ?? "",
      ].join(" "),
      searchText
    );
    const matchesActive = activeFilter === "all" || String(!!category.isActive) === activeFilter;
    return matchesSearch && matchesActive;
  });

  function resetForm() {
    const cultures = restaurantCultures[restaurants[0]?.id ?? 0]?.supportedCultures ?? ["pl-PL", "en-US"];
    setForm({
      ...emptyForm(cultures),
      restaurantId: restaurants[0]?.id ?? 0,
    });
    setEditingId(null);
  }

  function setTranslationField(index: number, field: keyof CategoryTranslationDto, value: string) {
    setForm((prev) => {
      const next = [...prev.translations];
      next[index] = { ...next[index], [field]: value };
      return { ...prev, translations: next };
    });
  }

  function validateForm() {
    if (!form.restaurantId) {
      return t("restaurant.selectFirst", "Select a restaurant first.");
    }

    if (!form.translations.length) {
      return t("validation.translationRequired", "At least one translation is required.");
    }

    const cultures = form.translations.map((t) => t.culture.trim().toLowerCase());
    const duplicates = cultures.filter((c, i) => cultures.indexOf(c) !== i);
    if (duplicates.length > 0) {
      return t("validation.duplicateCultures", "Duplicate cultures are not allowed.");
    }

    if (form.translations.some((t) => !t.name.trim())) {
      return t("validation.translationNameRequired", "Each translation must have a name.");
    }

    return null;
  }

  function openCreate() {
    resetForm();
    setShowCreate(true);
  }

  async function openEdit(category: CategoryDto) {
    try {
      setErr(null);

      const full = await api<CategoryDto>(`/api/admin/menu/categories/${category.id}`);

      setEditingId(full.id);
      const cultures =
        restaurantCultures[full.restaurantId]?.supportedCultures ?? full.translations?.map((t) => t.culture) ?? ["pl-PL", "en-US"];
      setForm({
        restaurantId: full.restaurantId,
        isActive: full.isActive ?? true,
        sortOrder: full.sortOrder ?? 0,
        translations: cultures.map((culture) => {
          const existing = full.translations?.find((translation) => translation.culture === culture);
          return {
            culture,
            name: existing?.name ?? "",
            description: existing?.description ?? "",
          };
        }),
      });

      setShowEdit(true);
    } catch (e: any) {
      setErr(e.message || t("categories.loadFailed", "Failed to load categories."));
    }
  }

  async function create() {
    const validation = validateForm();
    if (validation) {
      setErr(validation);
      return;
    }

    setErr(null);

    await api("/api/admin/menu/categories", {
      method: "POST",
      body: JSON.stringify({
        restaurantId: form.restaurantId,
        isActive: form.isActive,
        sortOrder: Number(form.sortOrder),
        translations: form.translations.map((t) => ({
          culture: t.culture.trim(),
          name: t.name.trim(),
          description: t.description?.trim() || null,
        })),
      }),
    });

    setShowCreate(false);
    resetForm();
    await load();
  }

  async function update() {
    if (editingId == null) return;

    const validation = validateForm();
    if (validation) {
      setErr(validation);
      return;
    }

    setErr(null);

    await api(`/api/admin/menu/categories/${editingId}`, {
      method: "PUT",
      body: JSON.stringify({
        restaurantId: form.restaurantId,
        isActive: form.isActive,
        sortOrder: Number(form.sortOrder),
        translations: form.translations.map((t) => ({
          culture: t.culture.trim(),
          name: t.name.trim(),
          description: t.description?.trim() || null,
        })),
      }),
    });

    setShowEdit(false);
    resetForm();
    await load();
  }

  async function remove(id: number) {
    if (!confirm(t("categories.deleteConfirm", "Delete category?"))) return;

    await api(`/api/admin/menu/categories/${id}`, { method: "DELETE" });
    await load();
  }

  function renderForm(onSave: () => void) {
    return (
      <div>
        {err && <div className="alert-error">{err}</div>}

        <div style={{ marginBottom: 12 }}>
          <label>{t("ui.sortOrder", "Sort order")}</label>
          <input
            type="number"
            value={form.sortOrder}
            onChange={(e) => setForm((prev) => ({ ...prev, sortOrder: Number(e.target.value) }))}
            style={{ width: "100%", padding: 8 }}
          />
        </div>

        <div style={{ marginBottom: 16, display: "flex", alignItems: "center", gap: 8 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))}
            />
            &nbsp;{t("common.active", "Active")}
          </label>
        </div>

        <details>
          <summary style={{ cursor: "pointer", fontWeight: 700, marginBottom: 12 }}>{t("ui.translations", "Translations")}</summary>
          <div style={{ marginTop: 12 }}>
            {form.translations.map((translation, index) => (
              <div
                key={`${translation.culture}-${index}`}
                style={{ border: "1px solid #ddd", borderRadius: 6, padding: 12, marginBottom: 12 }}
              >
                <div style={{ marginBottom: 8 }}>
                  <label>{t("ui.culture", "Culture")}</label>
                  <input value={translation.culture} readOnly style={{ width: "100%", padding: 8, background: "#f9fafb" }} />
                </div>

                <div style={{ marginBottom: 8 }}>
                  <label>{t("common.name", "Name")}</label>
                  <input
                    value={translation.name}
                    onChange={(e) => setTranslationField(index, "name", e.target.value)}
                    style={{ width: "100%", padding: 8 }}
                  />
                </div>

                <div style={{ marginBottom: 8 }}>
                  <label>{t("ui.description", "Description")}</label>
                  <textarea
                    value={translation.description ?? ""}
                    onChange={(e) => setTranslationField(index, "description", e.target.value)}
                    style={{ width: "100%", padding: 8, minHeight: 70 }}
                  />
                </div>
              </div>
            ))}
          </div>
        </details>

        <div style={{ marginTop: 12, display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button type="button" onClick={onSave}>
            {t("common.save", "Save")}
          </button>
          <button type="button" onClick={() => { setShowCreate(false); setShowEdit(false); resetForm(); }}>
            {t("common.cancel", "Cancel")}
          </button>
        </div>
      </div>
    );
  }

  return (
    <PageShell title={t("nav.categories", "Categories")} error={err} maxWidth={1100}>
      <div style={{ marginBottom: 20, display: "grid", gap: 12 }}>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
          <button onClick={openCreate}>{t("categories.add", "Add category")}</button>
          <button onClick={() => load()}>{t("common.reload", "Reload")}</button>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <span>{t("menuItems.displayLanguage", "Display language")}</span>
          <select value={selectedCulture} onChange={(e) => setSelectedCulture(e.target.value)}>
            {cultureOptions.map((culture) => (
              <option key={culture} value={culture}>{culture}</option>
            ))}
          </select>
          </label>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8, alignItems: "center" }}>
          <input
            placeholder={t("ui.searchCategories", "Search categories")}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ width: "100%" }}
          />
          <button type="button" onClick={() => { setSelectedCulture(culture); setActiveFilter("all"); setSearchText(""); }}>
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <select value={activeFilter} onChange={(e) => setActiveFilter(e.target.value)}>
            <option value="all">{t("ui.allActivity", "All activity")}</option>
            <option value="true">{t("common.active", "Active")}</option>
            <option value="false">{t("common.inactive", "Inactive")}</option>
          </select>
        </div>
      </div>

      {loading ? (
        <div>{t("common.loading", "Loading...")}</div>
      ) : (
        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("common.id", "ID")}</th>
              <th align="left">{t("common.name", "Name")}</th>
              <th align="left">{t("ui.description", "Description")}</th>
              <th align="left">{t("common.active", "Active")}</th>
              <th align="left">{t("ui.sortOrder", "Sort order")}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {filteredData.map((category) => (
              <tr key={category.id}>
                <td>{category.id}</td>
                <td>{getDisplayedTranslation(category).name || "-"}</td>
                <td>{getDisplayedTranslation(category).description || "-"}</td>
                <td>{category.isActive ?? true ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                <td>{category.sortOrder ?? 0}</td>
                <td>
                  <button onClick={() => openEdit(category)}>{t("common.edit", "Edit")}</button>
                  <button onClick={() => remove(category.id)} className="button-danger" style={{ marginLeft: 6 }}>
                    {t("common.delete", "Delete")}
                  </button>
                </td>
              </tr>
            ))}
            {filteredData.length === 0 && (
              <tr>
                <td colSpan={6} style={{ color: "#666", padding: 16 }}>
                  {t("categories.none", "No categories match this search.")}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}

      {showCreate && (
        <ModalShell
          title={t("categories.add", "Add category")}
          onClose={() => {
            setShowCreate(false);
            resetForm();
          }}
          minWidth="min(650px, calc(100vw - 32px))"
          maxWidth={850}
        >
          {renderForm(create)}
        </ModalShell>
      )}

      {showEdit && (
        <ModalShell
          title={`${t("categories.edit", "Edit category")} ${data.find((category) => category.id === editingId)?.name ?? ""} (ID: ${editingId})`}
          onClose={() => {
            setShowEdit(false);
            resetForm();
          }}
          minWidth="min(650px, calc(100vw - 32px))"
          maxWidth={850}
        >
          {renderForm(update)}
        </ModalShell>
      )}
    </PageShell>
  );
}
