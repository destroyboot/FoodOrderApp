export enum OrderType {
  Table = 0,
  Takeaway = 1,
  Delivery = 2,
}

export enum PaymentMethod {
  InApp = 0,
  AtCounter = 1,
}

export enum OrderStatus {
  Draft = 0,
  Pending = 1,
  Accepted = 2,
  Preparing = 3,
  Ready = 4,
  Completed = 5,
  Cancelled = 6,
  SentToKitchen = 7,
  ReadyForWaiter = 8,
  Delivered = 9,
  OutForDelivery = 10,
}

export enum ReservationStatus {
  Requested = 0,
  Confirmed = 1,
  Seated = 2,
  Completed = 3,
  Cancelled = 4,
  NoShow = 5,
}

export enum NotificationType {
  OrderCreated = 1,
  OrderStatusChanged = 2,
  ReservationNoShow = 3,
  DeliveryAssigned = 4,
}

export type Restaurant = {
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
  enableTableOrders: boolean;
  enableTakeawayOrders: boolean;
  enableDeliveryOrders: boolean;
  enableReservations: boolean;
  deliveryFee: number;
  deliveryRadiusKm: number;
  minimumDeliveryOrder: number;
  deliveryStartMinuteOfDay: number;
  deliveryEndMinuteOfDay: number;
  deliveryLeadTimeMinutes: number;
};

export type RestaurantTable = {
  id: number;
  restaurantId: number;
  label: string;
  seats: number;
  isActive: boolean;
  isReservable: boolean;
  sortOrder: number;
};

export type RestaurantSettings = {
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

export type MenuCategory = {
  id: number;
  restaurantId: number;
  name: string;
  sortOrder?: number;
};

export type MenuItem = {
  id: number;
  menuCategoryId: number;
  restaurantId: number;
  currentPrice: number;
  sortOrder: number;
  enableIngredientSwap: boolean;
  name: string;
  description?: string | null;
  allergens?: string | null;
  photoAssetId?: number | null;
  photoPath?: string | null;
  photoUrl?: string | null;
};

export type MenuItemIngredientOption = {
  ingredientId: number;
  name: string;
  quantity: number;
};

export type MenuItemCustomization = {
  menuItemId: number;
  enableIngredientSwap: boolean;
  extraIngredientPrice: number;
  dishIngredients: MenuItemIngredientOption[];
  removableIngredientIds: number[];
  substituteIngredients: MenuItemIngredientOption[];
};

export type CartCreateResponse = {
  cartId: number;
};

export type ActiveCartSummary = {
  cartId: number;
  restaurantId?: number | null;
  restaurantName?: string | null;
  itemCount: number;
  totalQuantity: number;
  createdAt: string;
};

export type CartLine = {
  lineId: number;
  menuItemId: number;
  menuItemName?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  extraCharge: number;
  note?: string | null;
  removedIngredientIds: number[];
  addedIngredientIds: number[];
};

export type OrderBillingDetails = {
  customerType?: number;
  invoiceStatus?: number;
  receiptEmail?: string | null;
  personName?: string | null;
  companyName?: string | null;
  taxId?: string | null;
  billingAddressLine1?: string | null;
  billingAddressLine2?: string | null;
  billingCity?: string | null;
  billingPostalCode?: string | null;
  billingCountry?: string | null;
};

export type BillingProfile = {
  customerType?: number;
  receiptEmail?: string | null;
  personName?: string | null;
  companyName?: string | null;
  taxId?: string | null;
  billingAddressLine1?: string | null;
  billingAddressLine2?: string | null;
  billingCity?: string | null;
  billingPostalCode?: string | null;
  billingCountry?: string | null;
  deliveryContactName?: string | null;
  deliveryPhone?: string | null;
  deliveryAddressLine1?: string | null;
  deliveryAddressLine2?: string | null;
  deliveryCity?: string | null;
  deliveryPostalCode?: string | null;
  deliveryCountry?: string | null;
};

export type CartResponse = {
  cartId: number;
  orderType: OrderType;
  restaurantTableId?: number | null;
  tableNumber?: string | null;
  restaurantId?: number | null;
  paymentMethod: PaymentMethod;
  scheduledFor?: string | null;
  reservationId?: number | null;
  pickupContactName?: string | null;
  pickupPhone?: string | null;
  pickupNote?: string | null;
  deliveryContactName?: string | null;
  deliveryPhone?: string | null;
  deliveryAddressLine1?: string | null;
  deliveryAddressLine2?: string | null;
  deliveryCity?: string | null;
  deliveryPostalCode?: string | null;
  deliveryCountry?: string | null;
  deliveryNote?: string | null;
  receiptEmail?: string | null;
  billingDetails?: OrderBillingDetails | null;
  items: CartLine[];
};

export type CartPreview = {
  subtotal: number;
  deliveryFee: number;
  extraChargeTotal: number;
  total: number;
  estimatedPreparationMinutes: number;
  estimatedReadyAt?: string | null;
};

export type FinalizeResponse = {
  orderId: number;
  status: OrderStatus;
  total: number;
  createdAt: string;
};

export type OrderSummary = {
  id: number;
  displayOrderNumber?: string;
  status: OrderStatus;
  orderType: OrderType;
  tableNumber?: string | null;
  pickupContactName?: string | null;
  pickupPhone?: string | null;
  deliveryContactName?: string | null;
  deliveryPhone?: string | null;
  deliveryAddressLine1?: string | null;
  deliveryCity?: string | null;
  scheduledFor?: string | null;
  total: number;
  createdAt: string;
  itemCount: number;
};

export type OrderDetailLine = {
  menuItemId: number;
  menuItemName?: string | null;
  quantity: number;
  unitPrice: number;
  note?: string | null;
};

export type OrderDetails = {
  id: number;
  displayOrderNumber?: string;
  status: OrderStatus;
  orderType: OrderType;
  receiptEmail?: string | null;
  invoiceNumber?: string | null;
  hasInvoiceDocument?: boolean;
  tableNumber?: string | null;
  pickupContactName?: string | null;
  pickupPhone?: string | null;
  pickupNote?: string | null;
  deliveryContactName?: string | null;
  deliveryPhone?: string | null;
  deliveryAddressLine1?: string | null;
  deliveryAddressLine2?: string | null;
  deliveryCity?: string | null;
  deliveryPostalCode?: string | null;
  deliveryCountry?: string | null;
  deliveryNote?: string | null;
  scheduledFor?: string | null;
  subtotal: number;
  deliveryFee: number;
  total: number;
  estimatedPreparationMinutes: number;
  estimatedReadyAt?: string | null;
  createdAt: string;
  items: OrderDetailLine[];
};

export type AuthResponse = {
  token: string;
};

export type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
};

export type NotificationItem = {
  id: number;
  type: NotificationType;
  title: string;
  body: string;
  payloadJson?: string | null;
  isRead: boolean;
  createdAt: string;
  readAt?: string | null;
};

export type ReservationBlockedRange = {
  startTime: string;
  endTime: string;
  label: string;
};

export type ReservationTableAvailability = {
  tableId: number;
  label: string;
  seats?: number | null;
  availableStartTimes: string[];
  blockedRanges: ReservationBlockedRange[];
};

export type ReservationAvailability = {
  restaurantId: number;
  date: string;
  slotMinutes: number;
  tables: ReservationTableAvailability[];
};

export type Reservation = {
  id: number;
  restaurantId: number;
  restaurantName?: string | null;
  restaurantTableId?: number | null;
  tableLabel?: string | null;
  guestName: string;
  guestEmail?: string | null;
  partySize: number;
  startAt: string;
  endAt: string;
  status: ReservationStatus;
  cancelledAt?: string | null;
  releasedAt?: string | null;
  note?: string | null;
  createdAt?: string;
};

export type AppLanguage = {
  id: number;
  culture: string;
  displayName: string;
  nativeName: string;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
};

export type AppLanguagePayload = {
  defaultCulture: string;
  languages: AppLanguage[];
};

export type AppTextDictionary = {
  culture: string;
  defaultCulture: string;
  texts: Record<string, string>;
};
