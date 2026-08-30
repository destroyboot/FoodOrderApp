import type { RestaurantFormState, RestaurantSettingsDto, TableFormState } from "./types";

export const emptyRestaurantForm = (): RestaurantFormState => ({
  name: "",
  city: "",
  street: "",
  postalCode: "",
  houseNumber: "",
  cuisineTypes: [],
  isActive: true,
});

export const emptyTableForm = (): TableFormState => ({
  label: "",
  seats: "",
  sortOrder: 0,
  isActive: true,
  isReservable: true,
});

export const defaultSettings = (restaurantId = 0): RestaurantSettingsDto => ({
  restaurantId,
  enableTableOrders: true,
  enableTakeawayOrders: true,
  enableDeliveryOrders: false,
  enablePayInApp: true,
  enablePayAtCounter: true,
  enablePayOnDelivery: true,
  enableReservations: false,
  allowUserTableSelectionForReservations: true,
  reservationRequiresInAppPayment: true,
  reservationPreorderMinOffsetMinutes: 5,
  reservationPreorderMaxAfterStartMinutes: 60,
  reservationStartMinuteOfDay: 17 * 60,
  reservationLastStartMinuteOfDay: 23 * 60,
  defaultReservationDurationMinutes: 90,
  reservationHoldsTableUntilClose: false,
  reservationGracePeriodMinutes: 15,
  deliveryFee: 8,
  deliveryRadiusKm: 5,
  minimumDeliveryOrder: 0,
  deliveryStartMinuteOfDay: 11 * 60,
  deliveryEndMinuteOfDay: 22 * 60,
  deliveryLeadTimeMinutes: 30,
  deliveryAssignmentMode: 0,
  estimatedPreparationBaseMinutes: 10,
  estimatedPreparationPerItemMinutes: 2,
  extraIngredientPrice: 0,
  supportedCultures: "pl-PL,en-US",
  defaultCulture: "pl-PL",
});

export const restaurantRoles = ["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"];
