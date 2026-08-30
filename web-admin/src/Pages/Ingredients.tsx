import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { matchesTokenizedSearch } from "../tokenSearch";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";

type RestaurantDto = { id: number; name: string };
type TranslationDto = { culture: string; name: string };
type RestaurantSettingsDto = { supportedCultures: string; defaultCulture: string };
type AllergenDto = {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
};
type IngredientDto = {
  id: number;
  restaurantId: number;
  unit: number;
  costPerUnit: number;
  isActive: boolean;
  allergenCode?: string | null;
  allergenCodes?: string[];
  name?: string | null;
  translations: TranslationDto[];
};

type IngredientFormState = {
  id: number;
  restaurantId: number;
  unit: number;
  costPerUnit: number;
  isActive: boolean;
  allergenCodes: string[];
  translations: TranslationDto[];
};

const emptyForm = (restaurantId = 0, cultures: string[] = ["pl-PL", "en-US"]): IngredientFormState => ({
  id: 0,
  restaurantId,
  unit: 0,
  costPerUnit: 0,
  isActive: true,
  allergenCodes: [],
  translations: cultures.map((culture) => ({ culture, name: "" })),
});

export default function Ingredients() {
  const { culture, t } = useI18n();
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [data, setData] = useState<IngredientDto[]>([]);
  const [allergens, setAllergens] = useState<AllergenDto[]>([]);
  const [form, setForm] = useState<IngredientFormState>(emptyForm());
  const [restaurantCultures, setRestaurantCultures] = useState<Record<number, string[]>>({});
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [selectedCulture, setSelectedCulture] = useState(culture);
  const [unitFilter, setUnitFilter] = useState("all");
  const [activeFilter, setActiveFilter] = useState("all");
  const [costMin, setCostMin] = useState("");
  const [costMax, setCostMax] = useState("");
  const [allergenFilter, setAllergenFilter] = useState<string[]>([]);
  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [allergenSearch, setAllergenSearch] = useState("");
  const [showAllergenModal, setShowAllergenModal] = useState(false);
  const [showAllergenFilterModal, setShowAllergenFilterModal] = useState(false);
  const dataCulture = selectedCulture;

  async function load() {
    setLoading(true);
    setErr(null);
    try {
      const [restaurantList, ingredients, allergenList] = await Promise.all([
        api<RestaurantDto[]>("/api/admin/restaurants"),
        api<IngredientDto[]>(`/api/admin/ingredients?culture=${encodeURIComponent(dataCulture)}`),
        api<AllergenDto[]>(`/api/admin/ingredients/allergens?culture=${encodeURIComponent(dataCulture)}`),
      ]);

      setRestaurants(restaurantList ?? []);
      setData(ingredients ?? []);
      setAllergens(allergenList ?? []);

      const settingsEntries = await Promise.all(
        (restaurantList ?? []).map(async (restaurant) => {
          const settings = await api<RestaurantSettingsDto>(`/api/restaurants/${restaurant.id}/settings`);
          return [restaurant.id, settings.supportedCultures.split(",").map((value) => value.trim()).filter(Boolean)] as const;
        })
      );
      setRestaurantCultures(Object.fromEntries(settingsEntries));

      if (!form.restaurantId && (restaurantList?.length ?? 0) > 0) {
        const firstRestaurantId = restaurantList[0].id;
        const cultures = settingsEntries.find(([restaurantId]) => restaurantId === firstRestaurantId)?.[1] ?? ["pl-PL", "en-US"];
        setForm(emptyForm(firstRestaurantId, cultures));
      }
    } catch (e: any) {
      setErr(e.message || t("ingredients.loadFailed", "Failed to load ingredients."));
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

  const allergenMap = useMemo(
    () => Object.fromEntries(allergens.map((allergen) => [allergen.code, allergen.name])),
    [allergens]
  );

  const cultureOptions = useMemo(() => {
    const values = new Set<string>();
    Object.values(restaurantCultures).forEach((cultures) => cultures.forEach((value) => values.add(value)));
    return Array.from(values).sort((a, b) => a.localeCompare(b));
  }, [restaurantCultures]);

  function unitLabel(unit: number) {
    return unit === 0
      ? t("ui.gram", "Gram")
      : unit === 1
        ? t("ui.milliliter", "Milliliter")
        : unit === 2
          ? t("ui.piece", "Piece")
          : String(unit);
  }

  const filteredAllergens = useMemo(
    () =>
      allergens.filter((allergen) =>
        matchesTokenizedSearch(`${allergen.code} ${allergen.name}`, allergenSearch)
      ),
    [allergenSearch, allergens]
  );

  function getDisplayedName(ingredient: IngredientDto) {
    const match = ingredient.translations.find((translation) => translation.culture === selectedCulture);
    if (match) {
      return match.name;
    }

    return ingredient.name ?? ingredient.translations[0]?.name ?? `${t("ingredients.add", "Ingredient")} ${ingredient.id}`;
  }

  function getAllergenNames(codes: string[] | undefined) {
    return (codes ?? [])
      .map((code) => allergenMap[code] ?? code)
      .join(", ");
  }

  const filteredData = data.filter((ingredient) => {
    const matchesSearch = matchesTokenizedSearch(
      [
        ingredient.id,
        getDisplayedName(ingredient),
        unitLabel(ingredient.unit),
        ingredient.costPerUnit,
        getAllergenNames(ingredient.allergenCodes),
        ingredient.isActive ? "active yes" : "active no",
        restaurantMap[ingredient.restaurantId] ?? "",
      ].join(" "),
      searchText
    );
    const matchesUnit = unitFilter === "all" || String(ingredient.unit) === unitFilter;
    const matchesActive = activeFilter === "all" || String(ingredient.isActive) === activeFilter;
    const matchesCostMin = !costMin || ingredient.costPerUnit >= Number(costMin);
    const matchesCostMax = !costMax || ingredient.costPerUnit <= Number(costMax);
    const codes = ingredient.allergenCodes ?? [];
    const matchesAllergens = allergenFilter.length === 0 || allergenFilter.every((code) => codes.includes(code));
    return matchesSearch && matchesUnit && matchesActive && matchesCostMin && matchesCostMax && matchesAllergens;
  });

  function resetForm(restaurantId?: number) {
    const resolvedRestaurantId = restaurantId ?? restaurants[0]?.id ?? 0;
    setForm(emptyForm(resolvedRestaurantId, restaurantCultures[resolvedRestaurantId] ?? ["pl-PL", "en-US"]));
    setAllergenSearch("");
  }

  function setTranslation(index: number, value: string) {
    setForm((prev) => {
      const translations = [...prev.translations];
      translations[index] = { ...translations[index], name: value };
      return { ...prev, translations };
    });
  }

  function openCreate() {
    resetForm();
    setShowCreate(true);
  }

  function openEdit(ingredient: IngredientDto) {
    const cultures = restaurantCultures[ingredient.restaurantId] ?? ingredient.translations.map((translation) => translation.culture) ?? ["pl-PL", "en-US"];
    setForm({
      id: ingredient.id,
      restaurantId: ingredient.restaurantId,
      unit: ingredient.unit,
      costPerUnit: ingredient.costPerUnit,
      isActive: ingredient.isActive,
      allergenCodes: ingredient.allergenCodes ?? [],
      translations: cultures.map((culture) => ingredient.translations.find((translation) => translation.culture === culture) ?? { culture, name: "" }),
    });
    setAllergenSearch("");
    setShowEdit(true);
  }

  async function save() {
    if (!form.restaurantId) {
      setErr(t("restaurant.selectFirst", "Select a restaurant first."));
      return;
    }

    if (form.translations.some((translation) => !translation.name.trim())) {
      setErr(t("ingredients.nameRequired", "Every translation must have a name."));
      return;
    }

    const payload = {
      restaurantId: form.restaurantId,
      unit: Number(form.unit),
      costPerUnit: Number(form.costPerUnit),
      isActive: form.isActive,
      allergenCodes: form.allergenCodes,
      translations: form.translations.map((translation) => ({
        culture: translation.culture,
        name: translation.name.trim(),
      })),
    };

    const method = form.id ? "PUT" : "POST";
    const url = form.id ? `/api/admin/ingredients/${form.id}` : "/api/admin/ingredients";
    await api(url, {
      method,
      body: JSON.stringify(payload),
    });

    setShowCreate(false);
    setShowEdit(false);
    resetForm();
    await load();
  }

  function toggleAllergen(code: string) {
    setForm((prev) => ({
      ...prev,
      allergenCodes: prev.allergenCodes.includes(code)
        ? prev.allergenCodes.filter((item) => item !== code)
        : [...prev.allergenCodes, code],
    }));
  }

  function renderForm() {
    return (
      <div>
        {err && <div className="alert-error">{err}</div>}

        {form.id > 0 ? (
          <div style={{ marginBottom: 16, fontWeight: 700 }}>
            {`${form.translations[0]?.name || t("ingredients.add", "Ingredient")} (ID: ${form.id})`}
          </div>
        ) : null}

        <div style={{ marginBottom: 16 }}>
          <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm((prev) => ({ ...prev, isActive: e.target.checked }))} />
            &nbsp;{t("common.active", "Active")}
          </label>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginBottom: 12 }}>
          <div>
            <label>{t("ui.unit", "Unit")}</label>
            <br />
            <select value={form.unit} onChange={(e) => setForm((prev) => ({ ...prev, unit: Number(e.target.value) }))} style={{ width: "100%", padding: 8 }}>
              <option value={0}>{t("ui.gram", "Gram")}</option>
              <option value={1}>{t("ui.milliliter", "Milliliter")}</option>
              <option value={2}>{t("ui.piece", "Piece")}</option>
            </select>
          </div>
          <div>
            <label>{t("ui.costPerUnit", "Cost per unit")}</label>
            <br />
            <input type="number" step="0.0001" value={form.costPerUnit} onChange={(e) => setForm((prev) => ({ ...prev, costPerUnit: Number(e.target.value) }))} style={{ width: "100%", padding: 8 }} />
          </div>
        </div>

        <div style={{ marginBottom: 16, border: "1px solid #ddd", borderRadius: 6, padding: 12 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8 }}>
            <strong>{t("ui.allergens", "Allergens")}</strong>
            <button type="button" onClick={() => setShowAllergenModal(true)}>{t("ui.chooseAllergens", "Choose allergens")}</button>
          </div>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginTop: 12 }}>
            {form.allergenCodes.length === 0 ? (
              <span style={{ color: "#666" }}>{t("ui.noAllergensSelected", "No allergens selected.")}</span>
            ) : (
              form.allergenCodes.map((code) => (
                <span key={code} style={{ display: "inline-flex", alignItems: "center", gap: 6, border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px" }}>
                  {allergenMap[code] ?? code}
                  <button type="button" onClick={() => toggleAllergen(code)} style={{ padding: "0 4px" }}>x</button>
                </span>
              ))
            )}
          </div>
        </div>

        <details>
          <summary style={{ cursor: "pointer", fontWeight: 700, marginBottom: 12 }}>{t("ui.translations", "Translations")}</summary>
          <div style={{ marginTop: 12 }}>
            {form.translations.map((translation, index) => (
              <div key={`${translation.culture}-${index}`} style={{ border: "1px solid #ddd", borderRadius: 6, padding: 12, marginBottom: 12 }}>
                <div style={{ marginBottom: 8 }}>
                  <label>{t("ui.culture", "Culture")}</label>
                  <input value={translation.culture} readOnly style={{ width: "100%", padding: 8, background: "#f9fafb" }} />
                </div>
                <div style={{ marginBottom: 8 }}>
                  <label>{t("common.name", "Name")}</label>
                  <input value={translation.name} onChange={(e) => setTranslation(index, e.target.value)} style={{ width: "100%", padding: 8 }} />
                </div>
              </div>
            ))}
          </div>
        </details>

        <div style={{ marginTop: 12, display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button type="button" onClick={() => void save()}>{t("common.save", "Save")}</button>
          <button type="button" onClick={() => { setShowCreate(false); setShowEdit(false); resetForm(); }}>{t("common.cancel", "Cancel")}</button>
        </div>
      </div>
    );
  }

  return (
    <PageShell title={t("nav.ingredients", "Ingredients")} error={err} maxWidth={1100}>
      <div style={{ marginBottom: 16, display: "grid", gap: 12 }}>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
          <button onClick={openCreate}>{t("ingredients.add", "Add ingredient")}</button>
          <button onClick={() => void load()}>{t("common.reload", "Reload")}</button>
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
            placeholder={t("ui.searchIngredients", "Search ingredients")}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            style={{ width: "100%" }}
          />
          <button type="button" onClick={() => { setUnitFilter("all"); setActiveFilter("all"); setCostMin(""); setCostMax(""); setAllergenFilter([]); setSearchText(""); setSelectedCulture(culture); }}>
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "auto auto auto auto auto", gap: 8, alignItems: "start" }}>
          <select value={unitFilter} onChange={(e) => setUnitFilter(e.target.value)}>
            <option value="all">{t("ui.allUnits", "All units")}</option>
            <option value="0">{t("ui.gram", "Gram")}</option>
            <option value="1">{t("ui.milliliter", "Milliliter")}</option>
            <option value="2">{t("ui.piece", "Piece")}</option>
          </select>
          <input placeholder={t("ui.costFrom", "Cost from")} value={costMin} onChange={(e) => setCostMin(e.target.value)} />
          <input placeholder={t("ui.costTo", "Cost to")} value={costMax} onChange={(e) => setCostMax(e.target.value)} />
          <select value={activeFilter} onChange={(e) => setActiveFilter(e.target.value)}>
            <option value="all">{t("ui.allActivity", "All activity")}</option>
            <option value="true">{t("common.active", "Active")}</option>
            <option value="false">{t("common.inactive", "Inactive")}</option>
          </select>
          <button type="button" onClick={() => setShowAllergenFilterModal(true)}>
            {t("ingredients.filterAllergens", "Filter allergens")}
          </button>
        </div>
        {allergenFilter.length > 0 ? (
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            {allergenFilter.map((code) => (
              <span key={`filter-${code}`} style={{ display: "inline-flex", alignItems: "center", gap: 6, border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px" }}>
                {allergenMap[code] ?? code}
                <button type="button" onClick={() => setAllergenFilter((prev) => prev.filter((entry) => entry !== code))} style={{ padding: "0 4px" }}>x</button>
              </span>
            ))}
          </div>
        ) : null}
      </div>

      {loading ? (
        <div>{t("common.loading", "Loading...")}</div>
      ) : (
        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("common.name", "Name")}</th>
              <th align="left">{t("ui.unit", "Unit")}</th>
              <th align="left">{t("ui.cost", "Cost")}</th>
              <th align="left">{t("ui.allergens", "Allergens")}</th>
              <th align="left">{t("common.active", "Active")}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {filteredData.map((ingredient) => (
              <tr key={ingredient.id}>
                <td>{getDisplayedName(ingredient)}</td>
                <td>{unitLabel(ingredient.unit)}</td>
                <td>{ingredient.costPerUnit}</td>
                <td>{getAllergenNames(ingredient.allergenCodes) || "-"}</td>
                <td>{ingredient.isActive ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                <td>
                  <div style={{ display: "flex", gap: 8 }}>
                    <button onClick={() => openEdit(ingredient)}>{t("common.edit", "Edit")}</button>
                    <button onClick={async () => { if (!confirm(t("ingredients.deleteConfirm", "Delete ingredient?"))) return; await api(`/api/admin/ingredients/${ingredient.id}`, { method: "DELETE" }); await load(); }} className="button-danger">{t("common.delete", "Delete")}</button>
                  </div>
                </td>
              </tr>
            ))}
            {filteredData.length === 0 && (
              <tr>
                <td colSpan={6} style={{ color: "#666", padding: 16 }}>
                  {t("ingredients.none", "No ingredients match this search.")}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}

      {showCreate ? (
        <ModalShell
          title={t("ingredients.add", "Add ingredient")}
          onClose={() => {
            setShowCreate(false);
            resetForm();
          }}
          minWidth="min(700px, calc(100vw - 32px))"
          maxWidth={900}
        >
          {renderForm()}
        </ModalShell>
      ) : null}

      {showEdit ? (
        <ModalShell
          title={`${t("ingredients.edit", "Edit ingredient")} #${form.id}`}
          onClose={() => {
            setShowEdit(false);
            resetForm();
          }}
          minWidth="min(700px, calc(100vw - 32px))"
          maxWidth={900}
        >
          {renderForm()}
        </ModalShell>
      ) : null}

      {showAllergenModal ? (
        <ModalShell title={t("ui.chooseAllergens", "Choose allergens")} onClose={() => { setShowAllergenModal(false); setAllergenSearch(""); }} minWidth="min(700px, calc(100vw - 32px))" maxWidth={900}>
          <div style={{ display: "grid", gap: 12 }}>
            <input
              placeholder={t("ui.searchAllergens", "Search allergens")}
              value={allergenSearch}
              onChange={(e) => setAllergenSearch(e.target.value)}
              style={{ width: "100%", padding: 8 }}
            />
            <div style={{ display: "grid", gap: 8, maxHeight: 420, overflowY: "auto" }}>
              {filteredAllergens.map((allergen) => (
                <label key={allergen.code} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <input
                    type="checkbox"
                    checked={form.allergenCodes.includes(allergen.code)}
                    onChange={() => toggleAllergen(allergen.code)}
                  />
                  <span>{allergen.name}</span>
                  <span style={{ color: "#6b7280" }}>({allergen.code})</span>
                </label>
              ))}
            </div>
            <div>
              <button type="button" onClick={() => setShowAllergenModal(false)}>{t("common.done", "Done")}</button>
            </div>
          </div>
        </ModalShell>
      ) : null}

      {showAllergenFilterModal ? (
        <ModalShell title={t("ingredients.filterAllergens", "Filter allergens")} onClose={() => setShowAllergenFilterModal(false)} minWidth="min(700px, calc(100vw - 32px))" maxWidth={900}>
          <div style={{ display: "grid", gap: 12 }}>
            <input placeholder={t("ui.searchAllergens", "Search allergens")} value={allergenSearch} onChange={(e) => setAllergenSearch(e.target.value)} />
            <div style={{ display: "grid", gap: 8, maxHeight: 420, overflowY: "auto" }}>
              {filteredAllergens.map((allergen) => (
                <label key={`filter-${allergen.code}`} style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <input
                    type="checkbox"
                    checked={allergenFilter.includes(allergen.code)}
                    onChange={() =>
                      setAllergenFilter((prev) =>
                        prev.includes(allergen.code)
                          ? prev.filter((entry) => entry !== allergen.code)
                          : [...prev, allergen.code]
                      )
                    }
                  />
                  <span>{allergen.name}</span>
                  <span style={{ color: "#6b7280" }}>({allergen.code})</span>
                </label>
              ))}
            </div>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button type="button" onClick={() => setShowAllergenFilterModal(false)}>{t("common.done", "Done")}</button>
              <button type="button" onClick={() => { setAllergenFilter([]); setShowAllergenFilterModal(false); }}>{t("common.clear", "Clear")}</button>
            </div>
          </div>
        </ModalShell>
      ) : null}
    </PageShell>
  );
}
