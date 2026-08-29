namespace Core.Contracts.Restaurants
{
    public class RestaurantSettingsDto
    {
        public int RestaurantId { get; set; }

        public bool EnableTableOrders { get; set; }
        public bool EnableTakeawayOrders { get; set; }
        public bool EnableDeliveryOrders { get; set; }

        public bool EnablePayInApp { get; set; }
        public bool EnablePayAtCounter { get; set; }
        public bool EnablePayOnDelivery { get; set; }

        public bool EnableReservations { get; set; }
        public bool AllowUserTableSelectionForReservations { get; set; }
        public bool ReservationRequiresInAppPayment { get; set; }
        public int ReservationPreorderMinOffsetMinutes { get; set; }
        public int ReservationPreorderMaxAfterStartMinutes { get; set; }
        public int ReservationStartMinuteOfDay { get; set; }
        public int ReservationLastStartMinuteOfDay { get; set; }
        public int DefaultReservationDurationMinutes { get; set; }
        public bool ReservationHoldsTableUntilClose { get; set; }
        public int ReservationGracePeriodMinutes { get; set; }

        public decimal DeliveryFee { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinimumDeliveryOrder { get; set; }
        public int DeliveryStartMinuteOfDay { get; set; }
        public int DeliveryEndMinuteOfDay { get; set; }
        public int DeliveryLeadTimeMinutes { get; set; }
        public int DeliveryAssignmentMode { get; set; }
        public int EstimatedPreparationBaseMinutes { get; set; }
        public int EstimatedPreparationPerItemMinutes { get; set; }
        public decimal ExtraIngredientPrice { get; set; }

        public string SupportedCultures { get; set; } = "pl-PL,en-US";
        public string DefaultCulture { get; set; } = "pl-PL";
    }
}
