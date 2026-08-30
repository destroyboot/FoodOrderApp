export type RestaurantDto = {
  id: number;
  name: string;
  address?: string | null;
  city?: string | null;
  street?: string | null;
  postalCode?: string | null;
  houseNumber?: string | null;
  cuisineType?: string | null;
  cuisineTypes?: string[];
  cuisineTypeDisplay?: string | null;
  cuisineTypeDisplays?: string[];
  isActive: boolean;
};

export type CuisineDto = {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
};

export type RestaurantTableDto = {
  id: number;
  restaurantId: number;
  label: string;
  seats?: number | null;
  isActive: boolean;
  isReservable: boolean;
  sortOrder: number;
};

export type RestaurantUserRoleDto = {
  id: number;
  restaurantId: number;
  userId: string;
  email?: string | null;
  role: string;
  isPendingInvite?: boolean;
  isAwaitingAssignment?: boolean;
  inviteId?: number | null;
};

export type RestaurantSettingsDto = {
  restaurantId: number;
  enableTableOrders: boolean;
  enableTakeawayOrders: boolean;
  enableDeliveryOrders: boolean;
  enablePayInApp: boolean;
  enablePayAtCounter: boolean;
  enablePayOnDelivery: boolean;
  enableReservations: boolean;
  allowUserTableSelectionForReservations: boolean;
  reservationRequiresInAppPayment: boolean;
  reservationPreorderMinOffsetMinutes: number;
  reservationPreorderMaxAfterStartMinutes: number;
  reservationStartMinuteOfDay: number;
  reservationLastStartMinuteOfDay: number;
  defaultReservationDurationMinutes: number;
  reservationHoldsTableUntilClose: boolean;
  reservationGracePeriodMinutes: number;
  deliveryFee: number;
  deliveryRadiusKm: number;
  minimumDeliveryOrder: number;
  deliveryStartMinuteOfDay: number;
  deliveryEndMinuteOfDay: number;
  deliveryLeadTimeMinutes: number;
  deliveryAssignmentMode: number;
  estimatedPreparationBaseMinutes: number;
  estimatedPreparationPerItemMinutes: number;
  extraIngredientPrice: number;
  supportedCultures: string;
  defaultCulture: string;
};

export type RestaurantFormState = {
  id?: number;
  name: string;
  city: string;
  street: string;
  postalCode: string;
  houseNumber: string;
  cuisineTypes: string[];
  isActive: boolean;
};

export type TableFormState = {
  id?: number;
  label: string;
  seats: string;
  sortOrder: number;
  isActive: boolean;
  isReservable: boolean;
};

export type SettingsTab = "payment" | "delivery" | "reservations" | "languages" | "tables" | "roles";

export type AppLanguageDto = {
  culture: string;
  displayName: string;
  nativeName: string;
  isActive: boolean;
};

export type AppLanguagePayload = {
  defaultCulture: string;
  languages: AppLanguageDto[];
};

export type TFunction = (key: string, fallback: string) => string;
