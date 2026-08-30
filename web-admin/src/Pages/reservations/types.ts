export type Reservation = {
  id: number;
  restaurantId: number;
  restaurantName?: string | null;
  restaurantTableId?: number | null;
  tableLabel?: string | null;
  customerId?: string | null;
  guestName: string;
  guestEmail?: string | null;
  guestPhone?: string | null;
  partySize: number;
  startAt: string;
  endAt: string;
  status: number;
  cancelledAt?: string | null;
  releasedAt?: string | null;
  note?: string | null;
};

export type RestaurantOption = {
  id: number;
  name: string;
};

export type RestaurantSettingsDto = {
  restaurantId: number;
  enableReservations: boolean;
  allowUserTableSelectionForReservations: boolean;
  reservationStartMinuteOfDay: number;
  reservationLastStartMinuteOfDay: number;
  defaultReservationDurationMinutes: number;
  reservationHoldsTableUntilClose: boolean;
  reservationGracePeriodMinutes: number;
  enableTableOrders: boolean;
  enableTakeawayOrders: boolean;
  enableDeliveryOrders: boolean;
  enablePayInApp: boolean;
  enablePayAtCounter: boolean;
  reservationRequiresInAppPayment: boolean;
  reservationPreorderMinOffsetMinutes: number;
  reservationPreorderMaxAfterStartMinutes: number;
  deliveryFee: number;
  estimatedPreparationBaseMinutes: number;
  estimatedPreparationPerItemMinutes: number;
  supportedCultures: string;
  defaultCulture: string;
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

export type ReservationSchedule = {
  id: number;
  restaurantId: number;
  restaurantTableId: number;
  tableLabel?: string | null;
  seats?: number | null;
  startMinuteOfDay: number;
  endMinuteOfDay: number;
  intervalMinutes: number;
  isActive: boolean;
  createdAt: string;
};

export type ReservationCalendarDay = {
  date: string;
  reservationCount: number;
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

export type TFunction = (key: string, fallback: string) => string;
