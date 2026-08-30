export type CategoryDto = {
  id: number;
  restaurantId: number;
  name: string;
};

export type RestaurantDto = {
  id: number;
  name: string;
};

export type RestaurantSettingsDto = {
  supportedCultures: string;
  defaultCulture: string;
};

export type IngredientDto = {
  id: number;
  restaurantId: number;
  name?: string | null;
  allergenCodes?: string[];
};

export type ItemCustomizationDto = {
  menuItemId: number;
  enableIngredientSwap: boolean;
  extraIngredientPrice: number;
  dishIngredients: Array<{ ingredientId: number; name: string }>;
  removableIngredientIds: number[];
  substituteIngredients: Array<{ ingredientId: number; name: string }>;
};

export type ItemTranslationDto = {
  culture: string;
  name: string;
  description?: string | null;
  allergens?: string | null;
};

export type ItemDto = {
  id: number;
  menuCategoryId: number;
  restaurantId: number;
  currentPrice: number;
  isAvailable: boolean;
  sortOrder?: number;
  enableIngredientSwap?: boolean;
  photoAssetId?: number | null;
  photoPath?: string | null;
  photoUrl?: string | null;
  name?: string | null;
  description?: string | null;
  allergens?: string | null;
  translations?: ItemTranslationDto[];
};

export type ItemFormState = {
  menuCategoryId: number;
  currentPrice: number;
  isAvailable: boolean;
  sortOrder: number;
  photoAssetId: string;
  photoPath: string;
  photoUrl: string;
  enableIngredientSwap: boolean;
  dishIngredientIds: number[];
  removableIngredientIds: number[];
  substituteIngredientIds: number[];
  translations: ItemTranslationDto[];
};

export type IngredientPickerMode = "dish" | "removable" | "substitute";

export type TFunction = (key: string, fallback: string) => string;
