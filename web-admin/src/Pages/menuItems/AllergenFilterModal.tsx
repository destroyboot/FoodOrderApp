import { ModalShell } from "../../Components/ModalShell";
import type { TFunction } from "./types";

type Props = {
  allergens: string[];
  selectedAllergens: string[];
  search: string;
  onSearchChange: (value: string) => void;
  onSelectedAllergensChange: (value: string[]) => void;
  onClose: () => void;
  t: TFunction;
};

export function AllergenFilterModal({
  allergens,
  selectedAllergens,
  search,
  onSearchChange,
  onSelectedAllergensChange,
  onClose,
  t,
}: Props) {
  return (
    <ModalShell title={t("ingredients.filterAllergens", "Filter allergens")} onClose={onClose} minWidth="min(700px, calc(100vw - 32px))" maxWidth={900}>
      <div style={{ display: "grid", gap: 12 }}>
        <input
          placeholder={t("ingredients.searchAllergens", "Search allergens")}
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          style={{ width: "100%", padding: 8 }}
        />
        <div style={{ display: "grid", gap: 8, maxHeight: 420, overflowY: "auto" }}>
          {allergens.map((allergen) => (
            <label key={`filter-${allergen}`} style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <input
                type="checkbox"
                checked={selectedAllergens.includes(allergen)}
                onChange={() =>
                  onSelectedAllergensChange(
                    selectedAllergens.includes(allergen)
                      ? selectedAllergens.filter((entry) => entry !== allergen)
                      : [...selectedAllergens, allergen]
                  )
                }
              />
              <span>{allergen}</span>
            </label>
          ))}
          {allergens.length === 0 ? (
            <div style={{ color: "#666" }}>{t("ingredients.noAllergensMatch", "No allergens match this search.")}</div>
          ) : null}
        </div>
        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button type="button" onClick={() => onSelectedAllergensChange([])}>{t("common.clear", "Clear")}</button>
          <button type="button" onClick={onClose}>{t("common.done", "Done")}</button>
        </div>
      </div>
    </ModalShell>
  );
}
