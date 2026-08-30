import { Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles, theme } from "../../lib/theme";
import type { Restaurant } from "../../types/api";
import { CapabilityChip } from "./CapabilityChip";
import type { TFunction } from "./types";

type Props = {
  restaurants: Restaurant[];
  selectedRestaurantId: number | null;
  search: string;
  deliveryOnly: boolean;
  pickupOnly: boolean;
  tableOnly: boolean;
  reservationsOnly: boolean;
  onSearchChange: (value: string) => void;
  onDeliveryOnlyChange: () => void;
  onPickupOnlyChange: () => void;
  onTableOnlyChange: () => void;
  onReservationsOnlyChange: () => void;
  onSelectRestaurant: (restaurantId: number) => void;
  t: TFunction;
};

export function RestaurantPickerView({
  restaurants,
  selectedRestaurantId,
  search,
  deliveryOnly,
  pickupOnly,
  tableOnly,
  reservationsOnly,
  onSearchChange,
  onDeliveryOnlyChange,
  onPickupOnlyChange,
  onTableOnlyChange,
  onReservationsOnlyChange,
  onSelectRestaurant,
  t,
}: Props) {
  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <View style={sharedStyles.stackSm}>
        <Text style={sharedStyles.pageTitle}>{t("mobile.restaurants.title", "Restaurants")}</Text>
        <Text style={sharedStyles.themeMutedComfortable}>
          {t("mobile.restaurants.subtitle", "Guests can order on-site without an account. Sign in later for reservations, delivery, and order history.")}
        </Text>
      </View>

      <TextInput
        placeholder={t("mobile.restaurants.searchPlaceholder", "Search by restaurant, cuisine, or address")}
        value={search}
        onChangeText={onSearchChange}
        style={inputStyle}
      />

      <View style={sharedStyles.rowWrap}>
        <FilterButton label={t("mobile.restaurants.delivery", "Delivery")} active={deliveryOnly} onPress={onDeliveryOnlyChange} />
        <FilterButton label={t("mobile.restaurants.pickup", "Pick up")} active={pickupOnly} onPress={onPickupOnlyChange} />
        <FilterButton label={t("mobile.restaurants.toTable", "To table")} active={tableOnly} onPress={onTableOnlyChange} />
        <FilterButton label={t("mobile.restaurants.reservations", "Reservations")} active={reservationsOnly} onPress={onReservationsOnlyChange} />
      </View>

      {restaurants.map((restaurant) => (
        <RestaurantCard
          key={restaurant.id}
          restaurant={restaurant}
          selected={selectedRestaurantId === restaurant.id}
          onSelect={() => onSelectRestaurant(restaurant.id)}
          t={t}
        />
      ))}
    </ScrollView>
  );
}

function RestaurantCard({
  restaurant,
  selected,
  onSelect,
  t,
}: {
  restaurant: Restaurant;
  selected: boolean;
  onSelect: () => void;
  t: TFunction;
}) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <View style={sharedStyles.rowBetween}>
          <View style={[sharedStyles.flexOne, { gap: 3 }]}>
            <Text style={styles.restaurantName}>{restaurant.name}</Text>
            <Text style={sharedStyles.themeMutedText}>{restaurant.address || t("mobile.restaurants.addressPlaceholder", "Address will appear here.")}</Text>
          </View>
          {restaurant.enableDeliveryOrders ? <View style={styles.deliveryBadge}><Text style={styles.deliveryBadgeText}>{t("mobile.restaurants.delivery", "Delivery")}</Text></View> : null}
        </View>
        <Text style={sharedStyles.themeMutedText}>{t("mobile.restaurants.cuisine", "Cuisine")}: <Text style={styles.cuisineValue}>{restaurant.cuisineTypeDisplay || restaurant.cuisineType || t("mobile.restaurants.notSetYet", "Not set yet")}</Text></Text>
        <View style={sharedStyles.rowWrap}>
          {restaurant.enableTableOrders ? <CapabilityChip kind="table" label={t("mobile.restaurants.toTable", "To table")} /> : null}
          {restaurant.enableTakeawayOrders ? <CapabilityChip kind="pickup" label={t("mobile.restaurants.pickup", "Pick up")} /> : null}
          {restaurant.enableDeliveryOrders ? <CapabilityChip kind="delivery" label={`${t("mobile.restaurants.delivery", "Delivery")} ${restaurant.deliveryFee.toFixed(2)}`} /> : null}
          {restaurant.enableReservations ? <CapabilityChip kind="reservation" label={t("mobile.restaurants.reservations", "Reservations")} /> : null}
        </View>
        {restaurant.enableDeliveryOrders ? (
          <Text style={sharedStyles.themeMutedText}>
            {t("mobile.restaurants.deliveryRadius", "Delivery radius")} {restaurant.deliveryRadiusKm.toFixed(1)} km, {t("restaurant.minimumOrder", "Minimum order").toLowerCase()} {restaurant.minimumDeliveryOrder.toFixed(2)}
          </Text>
        ) : null}
        <PrimaryButton
          label={selected ? t("mobile.restaurants.openRestaurant", "Open restaurant") : t("mobile.restaurants.selectRestaurant", "Select restaurant")}
          onPress={onSelect}
        />
      </View>
    </SectionCard>
  );
}

function FilterButton({
  label,
  active,
  onPress,
}: {
  label: string;
  active: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={{
        borderWidth: 1,
        borderColor: active ? theme.colors.navy : theme.colors.border,
        backgroundColor: active ? theme.colors.navy : theme.colors.surface,
        borderRadius: theme.radius.medium,
        paddingHorizontal: 12,
        paddingVertical: 8,
      }}
    >
      <Text style={{ color: active ? "#fff" : theme.colors.inkMuted, fontWeight: "700" }}>{label}</Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  restaurantName: {
    fontSize: 19,
    fontWeight: "700",
    color: theme.colors.ink,
  },
  cuisineValue: {
    color: theme.colors.ink,
    fontWeight: "600",
  },
  deliveryBadge: {
    backgroundColor: theme.colors.successSoft,
    borderRadius: theme.radius.medium,
    paddingHorizontal: 8,
    paddingVertical: 5,
    alignSelf: "flex-start",
  },
  deliveryBadgeText: {
    color: theme.colors.success,
    fontWeight: "700",
    fontSize: 12,
  },
});
