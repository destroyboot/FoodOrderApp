import { useEffect, useMemo, useRef, useState } from "react";
import { api, resolveApiUrl } from "../api";
import { useI18n } from "../i18n";
import { matchesTokenizedSearch } from "../tokenSearch";
import { ModalShell } from "../Components/ModalShell";
import { PageShell } from "../Components/PageShell";
import { AllergenFilterModal } from "./menuItems/AllergenFilterModal";
import { emptyForm } from "./menuItems/defaults";
import { MenuItemForm } from "./menuItems/MenuItemForm";
import { MenuItemsFilters } from "./menuItems/MenuItemsFilters";
import { MenuItemsTable } from "./menuItems/MenuItemsTable";
import type {
  CategoryDto,
  IngredientDto,
  IngredientPickerMode,
  ItemCustomizationDto,
  ItemDto,
  ItemFormState,
  ItemTranslationDto,
  RestaurantDto,
  RestaurantSettingsDto,
} from "./menuItems/types";

export default function MenuItems() {
  const { t, culture } = useI18n();
  const [data, setData] = useState<ItemDto[]>([]);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [selectedCulture, setSelectedCulture] = useState(culture);
  const [categoryFilter, setCategoryFilter] = useState("all");
  const [availableFilter, setAvailableFilter] = useState("all");
  const [swapFilter, setSwapFilter] = useState("all");
  const [priceMin, setPriceMin] = useState("");
  const [priceMax, setPriceMax] = useState("");
  const [allergenFilter, setAllergenFilter] = useState<string[]>([]);

  const [showCreate, setShowCreate] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const uploadInputRef = useRef<HTMLInputElement | null>(null);
  const [restaurantCultures, setRestaurantCultures] = useState<Record<number, string[]>>({});
  const [ingredients, setIngredients] = useState<IngredientDto[]>([]);
  const [pickerMode, setPickerMode] = useState<IngredientPickerMode | null>(null);
  const [pickerSearch, setPickerSearch] = useState("");
  const [showAllergenFilterModal, setShowAllergenFilterModal] = useState(false);
  const [allergenSearch, setAllergenSearch] = useState("");

  const [form, setForm] = useState<ItemFormState>(emptyForm());
  const dataCulture = selectedCulture;

  async function load() {
    setLoading(true);
    setErr(null);

    try {
      const [restaurantList, items, categoryList] = await Promise.all([
        api<RestaurantDto[]>("/api/admin/restaurants"),
        api<ItemDto[]>(`/api/admin/menu/items?culture=${encodeURIComponent(dataCulture)}`),
        api<CategoryDto[]>(`/api/admin/menu/categories?culture=${encodeURIComponent(dataCulture)}`),
      ]);

      setRestaurants(restaurantList ?? []);
      setData(items ?? []);
      setCategories(categoryList ?? []);
      const ingredientList = await api<IngredientDto[]>(`/api/admin/ingredients?culture=${encodeURIComponent(dataCulture)}`);
      setIngredients(ingredientList ?? []);
      const settingsEntries = await Promise.all(
        (restaurantList ?? []).map(async (restaurant) => {
          const settings = await api<RestaurantSettingsDto>(`/api/restaurants/${restaurant.id}/settings`);
          return [restaurant.id, settings.supportedCultures.split(",").map((value) => value.trim()).filter(Boolean)] as const;
        })
      );
      setRestaurantCultures(Object.fromEntries(settingsEntries));

      if (!form.menuCategoryId && (categoryList?.length ?? 0) > 0) {
        const firstCategory = categoryList[0];
        const cultures = settingsEntries.find(([restaurantId]) => restaurantId === firstCategory.restaurantId)?.[1] ?? ["pl-PL", "en-US"];
        setForm({ ...emptyForm(cultures), menuCategoryId: firstCategory.id });
      }
    } catch (e: any) {
      setErr(e.message || t("menuItems.loadFailed", "Failed to load items."));
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

  const categoryMap = useMemo(
    () => Object.fromEntries(categories.map((category) => [category.id, category.name])),
    [categories]
  );

  const restaurantMap = useMemo(
    () => Object.fromEntries(restaurants.map((restaurant) => [restaurant.id, restaurant.name])),
    [restaurants]
  );

  const cultureOptions = useMemo(() => {
    const values = new Set<string>();
    Object.values(restaurantCultures).forEach((cultures) => cultures.forEach((entry) => values.add(entry)));
    return Array.from(values).sort((a, b) => a.localeCompare(b));
  }, [restaurantCultures]);

  const allergenOptions = useMemo(() => {
    const values = new Set<string>();
    data.forEach((item) => {
      getDisplayedAllergens(item)
        .split(",")
        .map((entry) => entry.trim())
        .filter(Boolean)
        .forEach((entry) => values.add(entry));
    });
    return Array.from(values).sort((a, b) => a.localeCompare(b));
  }, [data, selectedCulture]);

  const filteredAllergenOptions = useMemo(
    () => allergenOptions.filter((allergen) => matchesTokenizedSearch(allergen, allergenSearch)),
    [allergenOptions, allergenSearch]
  );

  function getDisplayedTranslation(item: ItemDto) {
    const translations = item.translations ?? [];
    const match = translations.find((translation) => translation.culture === selectedCulture);
    if (match) {
      return {
        name: match.name,
        description: match.description ?? "",
        allergens: match.allergens ?? "",
      };
    }

    const first = translations[0];
    return {
      name: item.name ?? first?.name ?? "",
      description: item.description ?? first?.description ?? "",
      allergens: item.allergens ?? first?.allergens ?? "",
    };
  }

  function getDisplayedAllergens(item: ItemDto) {
    const displayed = getDisplayedTranslation(item);
    if ((displayed.allergens ?? "").trim()) {
      return displayed.allergens ?? "";
    }

    const customization = (item as ItemDto & { customization?: ItemCustomizationDto | null }).customization;
    const dishIngredientIds = customization?.dishIngredients?.map((entry) => entry.ingredientId) ?? [];
    const derived = Array.from(new Set(
      ingredients
        .filter((ingredient) => ingredient.restaurantId === item.restaurantId && dishIngredientIds.includes(ingredient.id))
        .flatMap((ingredient) => ingredient.allergenCodes ?? [])
    ));

    return derived.join(", ");
  }

  const filteredData = data.filter((item) => {
    const displayed = getDisplayedTranslation(item);
    const matchesSearch = matchesTokenizedSearch(
      [
        item.id,
        displayed.name,
        displayed.description,
        displayed.allergens,
        item.currentPrice,
        item.isAvailable ? "available yes" : "available no",
        categoryMap[item.menuCategoryId] ?? "",
        restaurantMap[item.restaurantId] ?? "",
      ].join(" "),
      searchText
    );
    const matchesCategory = categoryFilter === "all" || String(item.menuCategoryId) === categoryFilter;
    const matchesAvailable = availableFilter === "all" || String(item.isAvailable) === availableFilter;
    const matchesSwap = swapFilter === "all" || String(!!item.enableIngredientSwap) === swapFilter;
    const matchesMin = !priceMin || item.currentPrice >= Number(priceMin);
    const matchesMax = !priceMax || item.currentPrice <= Number(priceMax);
    const itemAllergens = getDisplayedAllergens(item)
      .split(",")
      .map((entry) => entry.trim().toLowerCase())
      .filter(Boolean);
    const matchesAllergens = allergenFilter.length === 0 || allergenFilter.every((entry) => itemAllergens.includes(entry.toLowerCase()));
    return matchesSearch && matchesCategory && matchesAvailable && matchesSwap && matchesMin && matchesMax && matchesAllergens;
  });

  function resetForm(categoryId?: number) {
    const resolvedCategoryId = categoryId ?? categories[0]?.id ?? 0;
    const category = categories.find((item) => item.id === resolvedCategoryId);
    const cultures = restaurantCultures[category?.restaurantId ?? 0] ?? ["pl-PL", "en-US"];
    setForm({
      ...emptyForm(cultures),
      menuCategoryId: resolvedCategoryId,
    });
  }

  function setTranslationField(index: number, field: keyof ItemTranslationDto, value: string) {
    setForm((prev) => {
      const next = [...prev.translations];
      next[index] = { ...next[index], [field]: value };
      return { ...prev, translations: next };
    });
  }

  function setIngredientSelection(mode: IngredientPickerMode, selectedIds: number[]) {
    setForm((prev) => {
      const normalized = Array.from(new Set(selectedIds.filter((value) => value > 0)));

      if (mode === "dish") {
        return {
          ...prev,
          dishIngredientIds: normalized,
          removableIngredientIds: prev.removableIngredientIds.filter((value) => normalized.includes(value)),
        };
      }

      if (mode === "removable") {
        return {
          ...prev,
          removableIngredientIds: normalized.filter((value) => prev.dishIngredientIds.includes(value)),
        };
      }

      return {
        ...prev,
        substituteIngredientIds: normalized.filter((value) => !prev.dishIngredientIds.includes(value)),
      };
    });
  }

  function validateForm() {
    if (!form.menuCategoryId || form.menuCategoryId <= 0) {
      return t("validation.categoryRequired", "Category is required.");
    }

    if (form.currentPrice < 0) {
      return t("validation.priceCannotBeNegative", "Price cannot be negative.");
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

  async function openCreate() {
    resetForm();
    setEditingId(null);
    setShowCreate(true);
  }

  async function openEdit(item: ItemDto) {
    try {
      setErr(null);
      const full = await api<ItemDto>(`/api/admin/menu/items/${item.id}`);
      const customization = await api<ItemCustomizationDto>(`/api/admin/ingredients/menu-items/${item.id}/customization`);

      setEditingId(item.id);
      const selectedCategory = categoryListById(full.menuCategoryId, categories);
      const cultures = restaurantCultures[selectedCategory?.restaurantId ?? 0] ?? full.translations?.map((translation) => translation.culture ?? "") ?? ["pl-PL", "en-US"];
      setForm({
        menuCategoryId: full.menuCategoryId,
        currentPrice: full.currentPrice,
        isAvailable: full.isAvailable,
        sortOrder: full.sortOrder ?? 0,
        photoAssetId:
          full.photoAssetId === null || full.photoAssetId === undefined
            ? ""
            : String(full.photoAssetId),
        photoPath: full.photoPath ?? "",
        photoUrl: full.photoUrl ? resolveApiUrl(full.photoUrl) : "",
        enableIngredientSwap: customization.enableIngredientSwap,
        dishIngredientIds: customization.dishIngredients.map((entry) => entry.ingredientId),
        removableIngredientIds: customization.removableIngredientIds,
        substituteIngredientIds: customization.substituteIngredients.map((entry) => entry.ingredientId),
        translations: cultures.map((culture) => {
          const existing = full.translations?.find((translation) => translation.culture === culture);
          return {
            culture,
            name: existing?.name ?? "",
            description: existing?.description ?? "",
            allergens: existing?.allergens ?? "",
          };
        }),
      });

      setShowEdit(true);
    } catch (e: any) {
      setErr(e.message || t("menuItems.loadFailed", "Failed to load items."));
    }
  }

  async function create() {
    const validation = validateForm();
    if (validation) {
      setErr(validation);
      return;
    }

    setErr(null);

    const created = await api<{ id: number }>("/api/admin/menu/items", {
      method: "POST",
      body: JSON.stringify({
        menuCategoryId: form.menuCategoryId,
        currentPrice: Number(form.currentPrice),
        sortOrder: Number(form.sortOrder),
        isAvailable: form.isAvailable,
        photoAssetId: form.photoAssetId ? Number(form.photoAssetId) : null,
        photoPath: form.photoPath.trim() || null,
        translations: form.translations.map((translation) => ({
          culture: translation.culture.trim(),
          name: translation.name.trim(),
          description: translation.description?.trim() || null,
          allergens: translation.allergens?.trim() || null,
        })),
      }),
    });

    await api(`/api/admin/ingredients/menu-items/${created.id}/customization`, {
      method: "PUT",
      body: JSON.stringify({
        enableIngredientSwap: form.enableIngredientSwap,
        dishIngredientIds: form.dishIngredientIds,
        removableIngredientIds: form.removableIngredientIds,
        substituteIngredientIds: form.substituteIngredientIds,
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

    await api(`/api/admin/menu/items/${editingId}`, {
      method: "PUT",
      body: JSON.stringify({
        menuCategoryId: form.menuCategoryId,
        sortOrder: Number(form.sortOrder),
        isAvailable: form.isAvailable,
        photoAssetId: form.photoAssetId ? Number(form.photoAssetId) : null,
        photoPath: form.photoPath.trim() || null,
        translations: form.translations.map((translation) => ({
          culture: translation.culture.trim(),
          name: translation.name.trim(),
          description: translation.description?.trim() || null,
          allergens: translation.allergens?.trim() || null,
        })),
      }),
    });

    await api(`/api/admin/menu/items/${editingId}/price?newPrice=${encodeURIComponent(Number(form.currentPrice))}`, {
      method: "PATCH",
    });

    await api(`/api/admin/ingredients/menu-items/${editingId}/customization`, {
      method: "PUT",
      body: JSON.stringify({
        enableIngredientSwap: form.enableIngredientSwap,
        dishIngredientIds: form.dishIngredientIds,
        removableIngredientIds: form.removableIngredientIds,
        substituteIngredientIds: form.substituteIngredientIds,
      }),
    });

    setShowEdit(false);
    setEditingId(null);
    resetForm();
    await load();
  }

  async function remove(id: number) {
    if (!confirm(t("menuItems.deleteConfirm", "Delete item?"))) return;

    await api(`/api/admin/menu/items/${id}`, { method: "DELETE" });
    await load();
  }

  async function uploadPhoto(file: File) {
    const body = new FormData();
    body.append("file", file);

    const result = await api<{ photoPath: string; photoUrl: string }>("/api/admin/menu/items/photo", {
      method: "POST",
      body,
    });

    setForm((prev) => ({
      ...prev,
      photoPath: result.photoPath,
      photoUrl: resolveApiUrl(result.photoUrl),
    }));
  }

  function renderForm(onSave: () => void) {
    return (
      <MenuItemForm
        error={err}
        form={form}
        categories={categories}
        restaurantCultures={restaurantCultures}
        ingredients={ingredients}
        pickerMode={pickerMode}
        pickerSearch={pickerSearch}
        uploadInputRef={uploadInputRef}
        onFormChange={setForm}
        onErrorChange={setErr}
        onPickerModeChange={setPickerMode}
        onPickerSearchChange={setPickerSearch}
        onTranslationFieldChange={setTranslationField}
        onIngredientSelectionChange={setIngredientSelection}
        onUploadPhoto={uploadPhoto}
        onSave={onSave}
        onCancel={() => {
          setShowCreate(false);
          setShowEdit(false);
          setEditingId(null);
          resetForm();
        }}
        t={t}
      />
    );
  }

  return (
    <PageShell title={t("nav.items", "Items")} error={err} maxWidth={1200}>
      <MenuItemsFilters
        categories={categories}
        cultureOptions={cultureOptions}
        selectedCulture={selectedCulture}
        searchText={searchText}
        categoryFilter={categoryFilter}
        availableFilter={availableFilter}
        swapFilter={swapFilter}
        priceMin={priceMin}
        priceMax={priceMax}
        allergenFilter={allergenFilter}
        onOpenCreate={() => void openCreate()}
        onReload={() => void load()}
        onSelectedCultureChange={setSelectedCulture}
        onSearchTextChange={setSearchText}
        onCategoryFilterChange={setCategoryFilter}
        onAvailableFilterChange={setAvailableFilter}
        onSwapFilterChange={setSwapFilter}
        onPriceMinChange={setPriceMin}
        onPriceMaxChange={setPriceMax}
        onAllergenFilterChange={setAllergenFilter}
        onOpenAllergenFilter={() => setShowAllergenFilterModal(true)}
        onResetFilters={() => {
          setCategoryFilter("all");
          setAvailableFilter("all");
          setSwapFilter("all");
          setPriceMin("");
          setPriceMax("");
          setAllergenFilter([]);
          setSearchText("");
        }}
        t={t}
      />

      {loading ? (
        <div>{t("common.loading", "Loading...")}</div>
      ) : (
        <MenuItemsTable
          items={filteredData}
          categoryMap={categoryMap}
          getDisplayedTranslation={getDisplayedTranslation}
          getDisplayedAllergens={getDisplayedAllergens}
          onEdit={(item) => void openEdit(item)}
          onDelete={(id) => void remove(id)}
          t={t}
        />
      )}

      {showCreate && (
        <ModalShell
          title={t("menuItems.add", "Add item")}
          onClose={() => {
            setShowCreate(false);
            resetForm();
          }}
          minWidth="min(700px, calc(100vw - 32px))"
          maxWidth={900}
        >
          {renderForm(create)}
        </ModalShell>
      )}

      {showEdit && (
        <ModalShell
          title={`${t("menuItems.edit", "Edit item")} ${data.find((item) => item.id === editingId)?.name ?? ""} (ID: ${editingId})`}
          onClose={() => {
            setShowEdit(false);
            setEditingId(null);
            resetForm();
          }}
          minWidth="min(700px, calc(100vw - 32px))"
          maxWidth={900}
        >
          {renderForm(update)}
        </ModalShell>
      )}

      {showAllergenFilterModal ? (
        <AllergenFilterModal
          allergens={filteredAllergenOptions}
          selectedAllergens={allergenFilter}
          search={allergenSearch}
          onSearchChange={setAllergenSearch}
          onSelectedAllergensChange={setAllergenFilter}
          onClose={() => { setShowAllergenFilterModal(false); setAllergenSearch(""); }}
          t={t}
        />
      ) : null}
    </PageShell>
  );
}

function categoryListById(categoryId: number, categories: CategoryDto[]) {
  return categories.find((category) => category.id === categoryId);
}
