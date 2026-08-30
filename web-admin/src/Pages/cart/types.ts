export type MenuItemDto = {
  id: number;
  restaurantId?: number;
  menuCategoryId: number;
  currentPrice: number;
  isAvailable: boolean;
  photoAssetId?: number | null;
  name?: string | null;
  description?: string | null;
  allergens?: string | null;
};

export type CartItemDto = {
  menuItemId: number;
  quantity: number;
  note?: string | null;
};

export type CartDto = {
  cartId: number;
  customerId?: string | null;
  orderType?: number;
  restaurantTableId?: number | null;
  tableNumber?: string | null;
  restaurantId?: number | null;
  paymentMethod?: number;
  receiptEmail?: string | null;
  items: CartItemDto[];
};

export type CartCreateResponseDto = {
  cartId: number;
};

export type CartPreviewResponseDto = {
  subtotal: number;
  deliveryFee: number;
  total: number;
  estimatedPreparationMinutes: number;
  estimatedReadyAt?: string | null;
};

export type RestaurantDto = {
  id: number;
  name: string;
  address?: string | null;
  isActive: boolean;
};

export type RestaurantTableDto = {
  id: number;
  restaurantId: number;
  label: string;
  seats?: number | null;
  isActive: boolean;
  sortOrder: number;
};

export type RestaurantSettingsDto = {
  restaurantId: number;
  enableTableOrders: boolean;
  enableTakeawayOrders: boolean;
  enableDeliveryOrders: boolean;
  enablePayInApp: boolean;
  enablePayAtCounter: boolean;
  supportedCultures: string;
  defaultCulture: string;
};

export type SelectOption = {
  value: number;
  label: string;
};

export type TFunction = (key: string, fallback: string) => string;
