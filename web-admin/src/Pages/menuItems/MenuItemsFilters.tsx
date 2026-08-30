import type { CategoryDto, TFunction } from "./types";

type Props = {
  categories: CategoryDto[];
  cultureOptions: string[];
  selectedCulture: string;
  searchText: string;
  categoryFilter: string;
  availableFilter: string;
  swapFilter: string;
  priceMin: string;
  priceMax: string;
  allergenFilter: string[];
  onOpenCreate: () => void;
  onReload: () => void;
  onSelectedCultureChange: (value: string) => void;
  onSearchTextChange: (value: string) => void;
  onCategoryFilterChange: (value: string) => void;
  onAvailableFilterChange: (value: string) => void;
  onSwapFilterChange: (value: string) => void;
  onPriceMinChange: (value: string) => void;
  onPriceMaxChange: (value: string) => void;
  onAllergenFilterChange: (value: string[]) => void;
  onOpenAllergenFilter: () => void;
  onResetFilters: () => void;
  t: TFunction;
};

export function MenuItemsFilters({
  categories,
  cultureOptions,
  selectedCulture,
  searchText,
  categoryFilter,
  availableFilter,
  swapFilter,
  priceMin,
  priceMax,
  allergenFilter,
  onOpenCreate,
  onReload,
  onSelectedCultureChange,
  onSearchTextChange,
  onCategoryFilterChange,
  onAvailableFilterChange,
  onSwapFilterChange,
  onPriceMinChange,
  onPriceMaxChange,
  onAllergenFilterChange,
  onOpenAllergenFilter,
  onResetFilters,
  t,
}: Props) {
  return (
    <div style={{ marginBottom: 20, display: "grid", gap: 12 }}>
      <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
        <button onClick={onOpenCreate}>{t("menuItems.add", "Add item")}</button>
        <button onClick={onReload}>{t("common.reload", "Reload")}</button>
        <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span>{t("menuItems.displayLanguage", "Display language")}</span>
          <select value={selectedCulture} onChange={(e) => onSelectedCultureChange(e.target.value)}>
            {cultureOptions.map((culture) => (
              <option key={culture} value={culture}>{culture}</option>
            ))}
          </select>
        </label>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 5fr) minmax(140px, 1fr)", gap: 8, alignItems: "center" }}>
        <input
          placeholder={t("ui.searchItems", "Search items")}
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
          style={{ minWidth: 280, width: "100%" }}
        />
        <button type="button" onClick={onResetFilters}>
          {t("common.resetFilters", "Reset Filters")}
        </button>
      </div>
      <div style={{ display: "grid", gridTemplateColumns: "repeat(6, minmax(0, 1fr))", gap: 8 }}>
        <select value={categoryFilter} onChange={(e) => onCategoryFilterChange(e.target.value)}>
          <option value="all">{t("ui.allCategories", "All categories")}</option>
          {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
        </select>
        <select value={availableFilter} onChange={(e) => onAvailableFilterChange(e.target.value)}>
          <option value="all">{t("ui.allAvailability", "All availability")}</option>
          <option value="true">{t("ui.available", "Available")}</option>
          <option value="false">{t("ui.unavailable", "Unavailable")}</option>
        </select>
        <select value={swapFilter} onChange={(e) => onSwapFilterChange(e.target.value)}>
          <option value="all">{t("ui.allSwapModes", "All swap modes")}</option>
          <option value="true">{t("ui.swapEnabled", "Swap enabled")}</option>
          <option value="false">{t("ui.swapDisabled", "Swap disabled")}</option>
        </select>
        <input placeholder={t("ui.priceFrom", "Price from")} value={priceMin} onChange={(e) => onPriceMinChange(e.target.value)} />
        <input placeholder={t("ui.priceTo", "Price to")} value={priceMax} onChange={(e) => onPriceMaxChange(e.target.value)} />
        <button type="button" onClick={onOpenAllergenFilter}>
          {t("ingredients.filterAllergens", "Filter allergens")}
        </button>
      </div>
      {allergenFilter.length > 0 ? (
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          {allergenFilter.map((allergen) => (
            <span key={`filter-${allergen}`} style={{ display: "inline-flex", alignItems: "center", gap: 6, border: "1px solid #d1d5db", borderRadius: 6, padding: "6px 10px" }}>
              {allergen}
              <button type="button" onClick={() => onAllergenFilterChange(allergenFilter.filter((entry) => entry !== allergen))} style={{ padding: "0 4px" }}>x</button>
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}
