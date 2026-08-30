import { useEffect, useMemo, useState } from "react";
import { useAppSession } from "../context/AppSessionContext";
import { SelectedRestaurantView } from "./restaurants/SelectedRestaurantView";
import { RestaurantPickerView } from "./restaurants/RestaurantPickerView";

function formatMinuteOfDay(totalMinutes: number) {
  const normalized = Math.max(0, Math.min(24 * 60, totalMinutes || 0));
  const hours = String(Math.floor(normalized / 60)).padStart(2, "0");
  const minutes = String(normalized % 60).padStart(2, "0");
  return `${hours}:${minutes}`;
}

export function RestaurantsScreen({ onOpenMenu, forcePickerSignal = 0 }: { onOpenMenu?: () => void; forcePickerSignal?: number }) {
  const { restaurants, selectedRestaurant, selectedRestaurantId, selectRestaurant, restaurantSettings, t } = useAppSession();
  const [mode, setMode] = useState<"home" | "picker">(selectedRestaurantId ? "home" : "picker");
  const [search, setSearch] = useState("");
  const [deliveryOnly, setDeliveryOnly] = useState(false);
  const [pickupOnly, setPickupOnly] = useState(false);
  const [tableOnly, setTableOnly] = useState(false);
  const [reservationsOnly, setReservationsOnly] = useState(false);

  useEffect(() => {
    if (!selectedRestaurantId) {
      setMode("picker");
    }
  }, [selectedRestaurantId]);

  useEffect(() => {
    if (forcePickerSignal > 0) {
      setMode("picker");
    }
  }, [forcePickerSignal]);

  const filteredRestaurants = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();
    return restaurants.filter((restaurant) => {
      const haystack = `${restaurant.name} ${restaurant.address ?? ""} ${restaurant.cuisineType ?? ""} ${restaurant.cuisineTypeDisplay ?? ""}`.toLowerCase();
      const searchMatch =
        normalizedSearch.length === 0 ||
        normalizedSearch.split(/\s+/).every((term) => haystack.includes(term));
      const deliveryMatch = !deliveryOnly || restaurant.enableDeliveryOrders;
      const pickupMatch = !pickupOnly || restaurant.enableTakeawayOrders;
      const tableMatch = !tableOnly || restaurant.enableTableOrders;
      const reservationMatch = !reservationsOnly || restaurant.enableReservations;
      return searchMatch && deliveryMatch && pickupMatch && tableMatch && reservationMatch;
    });
  }, [deliveryOnly, pickupOnly, reservationsOnly, restaurants, search, tableOnly]);

  if (mode === "home" && selectedRestaurant) {
    return (
      <SelectedRestaurantView
        restaurant={selectedRestaurant}
        settings={restaurantSettings}
        onOpenMenu={onOpenMenu}
        onChangeRestaurant={() => setMode("picker")}
        formatMinuteOfDay={formatMinuteOfDay}
        t={t}
      />
    );
  }

  return (
    <RestaurantPickerView
      restaurants={filteredRestaurants}
      selectedRestaurantId={selectedRestaurantId}
      search={search}
      deliveryOnly={deliveryOnly}
      pickupOnly={pickupOnly}
      tableOnly={tableOnly}
      reservationsOnly={reservationsOnly}
      onSearchChange={setSearch}
      onDeliveryOnlyChange={() => setDeliveryOnly((current) => !current)}
      onPickupOnlyChange={() => setPickupOnly((current) => !current)}
      onTableOnlyChange={() => setTableOnly((current) => !current)}
      onReservationsOnlyChange={() => setReservationsOnly((current) => !current)}
      onSelectRestaurant={(restaurantId) => {
        void selectRestaurant(restaurantId).then(() => setMode("home"));
      }}
      t={t}
    />
  );
}
