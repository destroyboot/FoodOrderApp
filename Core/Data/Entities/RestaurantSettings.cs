namespace Core.Data.Entities
{
    public enum DeliveryAssignmentMode
    {
        Manual = 0,
        Automatic = 1
    }

    public class RestaurantSettings
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }

        public bool EnableTableOrders { get; set; } = true;
        public bool EnableTakeawayOrders { get; set; } = true;
        public bool EnableDeliveryOrders { get; set; } = false;

        public bool EnablePayInApp { get; set; } = true;
        public bool EnablePayAtCounter { get; set; } = true;
        public bool EnablePayOnDelivery { get; set; } = true;

        public bool EnableReservations { get; set; } = false;
        public bool AllowUserTableSelectionForReservations { get; set; } = true;
        public bool ReservationRequiresInAppPayment { get; set; } = true;
        public int ReservationPreorderMinOffsetMinutes { get; set; } = 5;
        public int ReservationPreorderMaxAfterStartMinutes { get; set; } = 60;
        public int ReservationStartMinuteOfDay { get; set; } = 17 * 60;
        public int ReservationLastStartMinuteOfDay { get; set; } = 23 * 60;
        public int DefaultReservationDurationMinutes { get; set; } = 90;
        public bool ReservationHoldsTableUntilClose { get; set; }
        public int ReservationGracePeriodMinutes { get; set; } = 15;

        public decimal DeliveryFee { get; set; } = 8.00m;
        public decimal DeliveryRadiusKm { get; set; } = 5.00m;
        public decimal MinimumDeliveryOrder { get; set; } = 0m;
        public int DeliveryStartMinuteOfDay { get; set; } = 11 * 60;
        public int DeliveryEndMinuteOfDay { get; set; } = 22 * 60;
        public int DeliveryLeadTimeMinutes { get; set; } = 30;
        public DeliveryAssignmentMode DeliveryAssignmentMode { get; set; } = DeliveryAssignmentMode.Manual;
        public int EstimatedPreparationBaseMinutes { get; set; } = 10;
        public int EstimatedPreparationPerItemMinutes { get; set; } = 2;
        public decimal ExtraIngredientPrice { get; set; } = 0m;

        public string SupportedCultures { get; set; } = "pl-PL,en-US";
        public string DefaultCulture { get; set; } = "pl-PL";

        public Restaurant? Restaurant { get; set; }
    }
}
