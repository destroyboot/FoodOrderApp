import type { RestaurantDto, TFunction } from "./types";

type Props = {
  restaurants: RestaurantDto[];
  selectedRestaurantId: number;
  loading: boolean;
  onSelectRestaurant: (restaurantId: number) => void;
  onNewRestaurant: () => void;
  onReload: () => void;
  t: TFunction;
};

export function RestaurantSelector({
  restaurants,
  selectedRestaurantId,
  loading,
  onSelectRestaurant,
  onNewRestaurant,
  onReload,
  t,
}: Props) {
  return (
    <div style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 20 }}>
      <select value={selectedRestaurantId} onChange={(e) => onSelectRestaurant(Number(e.target.value))}>
        <option value={0}>-- {t("restaurant.newRestaurant", "New restaurant").toLowerCase()} --</option>
        {restaurants.map((restaurant) => <option key={restaurant.id} value={restaurant.id}>{restaurant.name}</option>)}
      </select>
      <button style={{backgroundColor: "lightgreen"}} onClick={onNewRestaurant}>{t("restaurant.newRestaurant", "New restaurant")}</button>
      <button onClick={onReload}>{t("common.reload", "Reload")}</button>
      {loading && <span>{t("common.loading", "Loading...")}</span>}
    </div>
  );
}
