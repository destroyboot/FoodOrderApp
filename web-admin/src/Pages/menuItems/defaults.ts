import type { ItemFormState, ItemTranslationDto } from "./types";

export function defaultTranslations(cultures: string[]): ItemTranslationDto[] {
  return cultures.map((culture) => ({ culture, name: "", description: "", allergens: "" }));
}

export const emptyForm = (cultures: string[] = ["pl-PL", "en-US"]): ItemFormState => ({
  menuCategoryId: 0,
  currentPrice: 0,
  isAvailable: true,
  sortOrder: 0,
  photoAssetId: "",
  photoPath: "",
  photoUrl: "",
  enableIngredientSwap: false,
  dishIngredientIds: [],
  removableIngredientIds: [],
  substituteIngredientIds: [],
  translations: defaultTranslations(cultures),
});
