import type { Dispatch, RefObject, SetStateAction } from "react";
import { matchesTokenizedSearch } from "../../tokenSearch";
import { IngredientPickerModal } from "./IngredientPickerModal";
import type {
  CategoryDto,
  IngredientDto,
  IngredientPickerMode,
  ItemFormState,
  ItemTranslationDto,
  TFunction,
} from "./types";

type Props = {
  error: string | null;
  form: ItemFormState;
  categories: CategoryDto[];
  restaurantCultures: Record<number, string[]>;
  ingredients: IngredientDto[];
  pickerMode: IngredientPickerMode | null;
  pickerSearch: string;
  uploadInputRef: RefObject<HTMLInputElement | null>;
  onFormChange: Dispatch<SetStateAction<ItemFormState>>;
  onErrorChange: (value: string | null) => void;
  onPickerModeChange: (value: IngredientPickerMode | null) => void;
  onPickerSearchChange: (value: string) => void;
  onTranslationFieldChange: (index: number, field: keyof ItemTranslationDto, value: string) => void;
  onIngredientSelectionChange: (mode: IngredientPickerMode, selectedIds: number[]) => void;
  onUploadPhoto: (file: File) => Promise<void>;
  onSave: () => void;
  onCancel: () => void;
  t: TFunction;
};

export function MenuItemForm({
  error,
  form,
  categories,
  restaurantCultures,
  ingredients,
  pickerMode,
  pickerSearch,
  uploadInputRef,
  onFormChange,
  onErrorChange,
  onPickerModeChange,
  onPickerSearchChange,
  onTranslationFieldChange,
  onIngredientSelectionChange,
  onUploadPhoto,
  onSave,
  onCancel,
  t,
}: Props) {
  const selectedCategory = categoryListById(form.menuCategoryId, categories);
  const ingredientOptions = ingredients.filter((ingredient) => ingredient.restaurantId === selectedCategory?.restaurantId);
  const ingredientNameMap = Object.fromEntries(
    ingredientOptions.map((ingredient) => [ingredient.id, ingredient.name ?? `Ingredient #${ingredient.id}`])
  );
  const derivedAllergens = Array.from(new Set(
    ingredientOptions
      .filter((ingredient) => form.dishIngredientIds.includes(ingredient.id))
      .flatMap((ingredient) => ingredient.allergenCodes ?? [])
  ));
  const pickerOptions =
    pickerMode === "removable"
      ? ingredientOptions.filter((ingredient) => form.dishIngredientIds.includes(ingredient.id))
      : pickerMode === "substitute"
        ? ingredientOptions.filter((ingredient) => !form.dishIngredientIds.includes(ingredient.id))
        : ingredientOptions;
  const selectedIdsForPicker =
    pickerMode === "dish"
      ? form.dishIngredientIds
      : pickerMode === "removable"
        ? form.removableIngredientIds
        : form.substituteIngredientIds;
  const filteredPickerOptions = pickerOptions.filter((ingredient) =>
    matchesTokenizedSearch(`${ingredient.id} ${ingredient.name ?? ""}`, pickerSearch)
  );

  return (
    <div>
      {error && <div className="alert-error">{error}</div>}

      <div style={{ marginBottom: 16 }}>
        <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <input
            type="checkbox"
            checked={form.isAvailable}
            onChange={(e) => onFormChange((prev) => ({ ...prev, isAvailable: e.target.checked }))}
          />
          {t("common.available", "Available")}
        </label>
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("ui.category", "Category")}</label>
        <br />
        <select
          value={form.menuCategoryId}
          onChange={(e) => {
            const menuCategoryId = Number(e.target.value);
            const nextCategory = categoryListById(menuCategoryId, categories);
            const cultures = restaurantCultures[nextCategory?.restaurantId ?? 0] ?? ["pl-PL", "en-US"];
            onFormChange((prev) => ({
              ...prev,
              menuCategoryId,
              translations: cultures.map((culture) => {
                const existing = prev.translations.find((translation) => translation.culture === culture);
                return existing ?? { culture, name: "", description: "", allergens: "" };
              }),
            }));
          }}
          style={{ width: "100%", padding: 8 }}
        >
          <option value={0}>-- select category --</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("ui.sortOrder", "Display order")}</label>
        <input
          type="number"
          value={form.sortOrder}
          onChange={(e) => onFormChange((prev) => ({ ...prev, sortOrder: Number(e.target.value) }))}
          style={{ width: "100%", padding: 8 }}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("common.price", "Current price")}</label>
        <input
          type="number"
          step="0.01"
          value={form.currentPrice}
          onChange={(e) => onFormChange((prev) => ({ ...prev, currentPrice: Number(e.target.value) }))}
          style={{ width: "100%", padding: 8 }}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("ui.photoAssetId", "Photo asset ID")}</label>
        <input
          value={form.photoAssetId}
          onChange={(e) => onFormChange((prev) => ({ ...prev, photoAssetId: e.target.value }))}
          style={{ width: "100%", padding: 8 }}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <label>{t("ui.photoUpload", "Photo upload")}</label>
        <div style={{ display: "flex", gap: 8, alignItems: "center", marginTop: 8, flexWrap: "wrap" }}>
          <button type="button" onClick={() => uploadInputRef.current?.click()}>
            {t("ui.photoUpload", "Upload photo")}
          </button>
          {form.photoPath ? (
            <button
              type="button"
              onClick={() => onFormChange((prev) => ({ ...prev, photoPath: "", photoUrl: "" }))}
            >
              Remove photo
            </button>
          ) : null}
          <span style={{ color: "#6b7280" }}>{form.photoPath || t("menuItems.noPhotoSelected", "No uploaded photo selected.")}</span>
        </div>
        <input
          ref={uploadInputRef}
          type="file"
          accept="image/png,image/jpeg,image/webp"
          style={{ display: "none" }}
          onChange={async (e) => {
            const file = e.target.files?.[0];
            if (!file) return;
            try {
              onErrorChange(null);
              await onUploadPhoto(file);
            } catch (uploadError: any) {
              onErrorChange(uploadError?.message || t("menuItems.photoUploadFailed", "Photo upload failed."));
            } finally {
              e.currentTarget.value = "";
            }
          }}
        />
        {form.photoUrl ? (
          <div style={{ marginTop: 12 }}>
            <img
              src={form.photoUrl}
              alt={t("menuItems.photoPreview", "Menu item preview")}
              style={{ width: 140, height: 140, objectFit: "cover", borderRadius: 8, border: "1px solid #ddd" }}
            />
          </div>
        ) : null}
      </div>

      <div style={{ marginBottom: 16, border: "1px solid #ddd", borderRadius: 6, padding: 12 }}>
        <div style={{ marginBottom: 10, fontWeight: 700 }}>{t("menuItems.ingredientSwapSettings", "Ingredient swap settings")}</div>
        <div style={{ display: "grid", gap: 12, marginBottom: 12 }}>
          <div style={{ border: "1px solid #e5e7eb", borderRadius: 6, padding: 12 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12 }}>
              <div>
                <div style={{ fontWeight: 600 }}>{t("menuItems.dishIngredients", "Dish ingredients")}</div>
                <div style={{ color: "#6b7280", marginTop: 4 }}>
                  {form.dishIngredientIds.length > 0
                    ? form.dishIngredientIds.map((id) => ingredientNameMap[id] ?? `${t("nav.ingredients", "Ingredient")} #${id}`).join(", ")
                    : t("menuItems.noDishIngredientsSelected", "No dish ingredients selected yet.")}
                </div>
              </div>
              <button type="button" onClick={() => { onPickerModeChange("dish"); onPickerSearchChange(""); }}>
                {t("menuItems.selectIngredients", "Select ingredients")}
              </button>
            </div>
          </div>
        </div>
        <label>
          <input
            type="checkbox"
            checked={form.enableIngredientSwap}
            onChange={(e) =>
              onFormChange((prev) => ({
                ...prev,
                enableIngredientSwap: e.target.checked,
                removableIngredientIds: e.target.checked ? prev.removableIngredientIds : [],
                substituteIngredientIds: e.target.checked ? prev.substituteIngredientIds : [],
              }))
            }
          />
          &nbsp;{t("menuItems.enableIngredientSwap", "Enable ingredient swap")}
        </label>

        {!form.enableIngredientSwap ? (
          <div style={{ marginTop: 10, color: "#6b7280" }}>
            {t("menuItems.turnOnIngredientSwapHint", "Turn this on to choose default dish ingredients and allowed substitute ingredients.")}
          </div>
        ) : null}

        {form.enableIngredientSwap ? (
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 12 }}>
            <div>
              <label>{t("menuItems.swappableDishIngredients", "Swappable dish ingredients")}</label>
              <div style={{ color: "#6b7280", marginTop: 8 }}>
                {form.removableIngredientIds.length > 0
                  ? form.removableIngredientIds.map((id) => ingredientNameMap[id] ?? `${t("nav.ingredients", "Ingredient")} #${id}`).join(", ")
                  : t("menuItems.noSwappableIngredientsSelected", "No swappable ingredients selected.")}
              </div>
              <button type="button" onClick={() => { onPickerModeChange("removable"); onPickerSearchChange(""); }} style={{ marginTop: 8 }}>
                {t("menuItems.chooseSwappableIngredients", "Choose swappable ingredients")}
              </button>
            </div>
            <div>
              <label>{t("menuItems.allowedSubstituteIngredients", "Allowed substitute ingredients")}</label>
              <div style={{ color: "#6b7280", marginTop: 8 }}>
                {form.substituteIngredientIds.length > 0
                  ? form.substituteIngredientIds.map((id) => ingredientNameMap[id] ?? `${t("nav.ingredients", "Ingredient")} #${id}`).join(", ")
                  : t("menuItems.noSubstituteIngredientsSelected", "No substitute ingredients selected.")}
              </div>
              <button type="button" onClick={() => { onPickerModeChange("substitute"); onPickerSearchChange(""); }} style={{ marginTop: 8 }}>
                {t("menuItems.chooseSubstituteIngredients", "Choose substitute ingredients")}
              </button>
            </div>
          </div>
        ) : null}
      </div>

      <div style={{ marginBottom: 16, border: "1px solid #ddd", borderRadius: 6, padding: 12 }}>
        <div style={{ fontWeight: 700, marginBottom: 8 }}>{t("menuItems.derivedAllergens", "Derived allergens")}</div>
        <div style={{ color: "#6b7280" }}>
          {derivedAllergens.length > 0 ? derivedAllergens.join(", ") : t("menuItems.noDerivedAllergens", "No allergens derived from the selected ingredients yet.")}
        </div>
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
                  onChange={(e) => onTranslationFieldChange(index, "name", e.target.value)}
                  style={{ width: "100%", padding: 8 }}
                />
              </div>

              <div style={{ marginBottom: 8 }}>
                <label>{t("ui.description", "Description")}</label>
                <textarea
                  value={translation.description ?? ""}
                  onChange={(e) => onTranslationFieldChange(index, "description", e.target.value)}
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
        <button type="button" onClick={onCancel}>
          {t("common.cancel", "Cancel")}
        </button>
      </div>

      {pickerMode ? (
        <IngredientPickerModal
          mode={pickerMode}
          title={
            pickerMode === "dish"
              ? t("menuItems.selectDishIngredients", "Select dish ingredients")
              : pickerMode === "removable"
                ? t("menuItems.selectSwappableDishIngredients", "Select swappable dish ingredients")
                : t("menuItems.selectSubstituteIngredients", "Select substitute ingredients")
          }
          search={pickerSearch}
          selectedIds={selectedIdsForPicker}
          ingredients={filteredPickerOptions}
          onSearchChange={onPickerSearchChange}
          onSelectionChange={(selectedIds) => onIngredientSelectionChange(pickerMode, selectedIds)}
          onClose={() => onPickerModeChange(null)}
          t={t}
        />
      ) : null}
    </div>
  );
}

function categoryListById(categoryId: number, categories: CategoryDto[]) {
  return categories.find((category) => category.id === categoryId);
}
