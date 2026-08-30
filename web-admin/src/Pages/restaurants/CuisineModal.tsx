import { ModalShell } from "../../Components/ModalShell";
import type { CuisineDto, TFunction } from "./types";

type Props = {
  cuisineSearch: string;
  selectedCuisineToAdd: string;
  filteredCuisineOptions: CuisineDto[];
  onClose: () => void;
  onCuisineSearchChange: (value: string) => void;
  onSelectedCuisineToAddChange: (value: string) => void;
  onAddCuisine: () => void;
  t: TFunction;
};

export function CuisineModal({
  cuisineSearch,
  selectedCuisineToAdd,
  filteredCuisineOptions,
  onClose,
  onCuisineSearchChange,
  onSelectedCuisineToAddChange,
  onAddCuisine,
  t,
}: Props) {
  return (
    <ModalShell title={t("restaurant.addCuisine", "Add cuisine")} onClose={onClose} className="modal-card-medium">
      <div className="stack-sm">
        <input
          placeholder={t("restaurant.searchCuisines", "Search cuisines")}
          value={cuisineSearch}
          onChange={(e) => onCuisineSearchChange(e.target.value)}
        />
        <select
          value={selectedCuisineToAdd}
          onChange={(e) => onSelectedCuisineToAddChange(e.target.value)}
          size={Math.min(Math.max(filteredCuisineOptions.length, 4), 10)}
          style={{ width: "100%" }}
        >
          {filteredCuisineOptions.map((option) => <option key={option.code} value={option.code}>{option.name}</option>)}
        </select>
        <div className="cluster-sm">
          <button type="button" onClick={onAddCuisine}>{t("common.add", "Add")}</button>
          <button type="button" onClick={onClose}>{t("common.cancel", "Cancel")}</button>
        </div>
      </div>
    </ModalShell>
  );
}
