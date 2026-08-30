import { ModalShell } from "../../Components/ModalShell";
import type { IngredientDto, IngredientPickerMode, TFunction } from "./types";

type Props = {
  mode: IngredientPickerMode;
  title: string;
  search: string;
  selectedIds: number[];
  ingredients: IngredientDto[];
  onSearchChange: (value: string) => void;
  onSelectionChange: (selectedIds: number[]) => void;
  onClose: () => void;
  t: TFunction;
};

export function IngredientPickerModal({
  mode,
  title,
  search,
  selectedIds,
  ingredients,
  onSearchChange,
  onSelectionChange,
  onClose,
  t,
}: Props) {
  return (
    <ModalShell title={title} onClose={onClose} minWidth="min(700px, calc(100vw - 32px))" maxWidth={900}>
      <div style={{ display: "grid", gap: 12 }}>
        <input
          placeholder={t("menuItems.searchIngredients", "Search ingredients")}
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          style={{ width: "100%", padding: 8 }}
        />
        <div style={{ maxHeight: 420, overflowY: "auto", border: "1px solid #e5e7eb", borderRadius: 6, padding: 12 }}>
          {ingredients.length === 0 ? (
            <div style={{ color: "#6b7280" }}>{t("menuItems.noIngredientsMatch", "No ingredients match this search.")}</div>
          ) : (
            ingredients.map((ingredient) => {
              const checked = selectedIds.includes(ingredient.id);
              return (
                <label key={`${mode}-${ingredient.id}`} style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8 }}>
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={(e) => {
                      const next = e.target.checked
                        ? [...selectedIds, ingredient.id]
                        : selectedIds.filter((value) => value !== ingredient.id);
                      onSelectionChange(next);
                    }}
                  />
                  <span>{ingredient.name ?? `Ingredient #${ingredient.id}`}</span>
                </label>
              );
            })
          )}
        </div>
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8 }}>
          <button type="button" onClick={onClose}>{t("common.done", "Done")}</button>
        </div>
      </div>
    </ModalShell>
  );
}
