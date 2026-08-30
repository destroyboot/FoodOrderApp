import { Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles, theme } from "../../lib/theme";
import type { Restaurant, RestaurantSettings } from "../../types/api";
import { CapabilityChip } from "./CapabilityChip";
import type { TFunction } from "./types";

type Props = {
  restaurant: Restaurant;
  settings: RestaurantSettings | null;
  onOpenMenu?: () => void;
  onChangeRestaurant: () => void;
  formatMinuteOfDay: (totalMinutes: number) => string;
  t: TFunction;
};

export function SelectedRestaurantView({
  restaurant,
  settings,
  onOpenMenu,
  onChangeRestaurant,
  formatMinuteOfDay,
  t,
}: Props) {
  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <View style={styles.selectedHero}>
        <Text style={styles.selectedEyebrow}>{t("restaurant.selected", "Selected restaurant")}</Text>
        <Text style={styles.selectedName}>{restaurant.name}</Text>
        <Text style={styles.selectedAddress}>{restaurant.address || t("mobile.restaurants.addressPlaceholder", "Address will appear here.")}</Text>
      </View>

      <SectionCard>
        <View style={sharedStyles.stackLg}>
          <Text style={sharedStyles.themeMutedText}>{t("mobile.restaurants.cuisine", "Cuisine")}</Text>
          <Text style={sharedStyles.sectionTitle}>{restaurant.cuisineTypeDisplay || restaurant.cuisineType || t("mobile.restaurants.notSetYet", "Not set yet")}</Text>
          <Text>
            {t("mobile.restaurants.paymentOptions", "Payment options")}: {[
              settings?.enablePayInApp ? t("restaurant.payInApp", "Pay in app") : null,
              settings?.enablePayAtCounter ? t("restaurant.payAtCounter", "Pay at counter") : null,
              settings?.enablePayOnDelivery ? t("restaurant.payOnDelivery", "Pay on delivery") : null,
            ].filter(Boolean).join(", ") || t("mobile.restaurants.notSet", "Not set")}
          </Text>
          {settings?.enableDeliveryOrders ? (
            <>
              <Text style={sharedStyles.themeMutedText}>{t("mobile.restaurants.deliveryFee", "Delivery fee")}: {settings.deliveryFee.toFixed(2)}</Text>
              <Text>{t("mobile.restaurants.deliveryRadius", "Delivery radius")}: {settings.deliveryRadiusKm.toFixed(1)} km</Text>
              <Text>{t("mobile.restaurants.minimumDeliveryOrder", "Minimum delivery order")}: {settings.minimumDeliveryOrder.toFixed(2)}</Text>
              <Text>
                {t("mobile.restaurants.deliveryHours", "Delivery hours")}: {formatMinuteOfDay(settings.deliveryStartMinuteOfDay)} - {formatMinuteOfDay(settings.deliveryEndMinuteOfDay)}
              </Text>
              <Text>{t("mobile.restaurants.leadTime", "Minimum delivery lead time")}: {settings.deliveryLeadTimeMinutes} minutes</Text>
            </>
          ) : null}
          <View style={sharedStyles.rowWrap}>
            {restaurant.enableTableOrders ? <CapabilityChip kind="table" label={t("mobile.restaurants.toTable", "To table")} /> : null}
            {restaurant.enableTakeawayOrders ? <CapabilityChip kind="pickup" label={t("mobile.restaurants.pickup", "Pick up")} /> : null}
            {restaurant.enableDeliveryOrders ? <CapabilityChip kind="delivery" label={t("mobile.restaurants.delivery", "Delivery")} /> : null}
            {restaurant.enableReservations ? <CapabilityChip kind="reservation" label={t("mobile.restaurants.reservations", "Reservations")} /> : null}
          </View>
          <View style={sharedStyles.rowTop}>
            <Pressable onPress={() => onOpenMenu?.()} style={styles.primaryAction}><Text style={styles.primaryActionText}>{t("mobile.restaurants.menu", "Menu")}</Text></Pressable>
            <Pressable onPress={onChangeRestaurant} style={styles.secondaryAction}><Text style={styles.secondaryActionText}>{t("mobile.restaurants.changeRestaurant", "Change restaurant")}</Text></Pressable>
          </View>
        </View>
      </SectionCard>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  selectedHero: {
    backgroundColor: theme.colors.navy,
    borderRadius: theme.radius.medium,
    padding: 18,
    gap: 6,
  },
  selectedEyebrow: {
    fontSize: 13,
    fontWeight: "700",
    color: "#c4cfdd",
    textTransform: "uppercase",
  },
  selectedName: {
    fontSize: 28,
    fontWeight: "700",
    color: "#fff",
  },
  selectedAddress: {
    color: "#d6deea",
  },
  primaryAction: {
    flex: 1,
    borderRadius: theme.radius.medium,
    backgroundColor: theme.colors.navy,
    alignItems: "center",
    paddingVertical: 13,
  },
  primaryActionText: {
    color: "#fff",
    fontWeight: "700",
  },
  secondaryAction: {
    flex: 1,
    borderWidth: 1,
    borderColor: theme.colors.border,
    borderRadius: theme.radius.medium,
    backgroundColor: theme.colors.surface,
    alignItems: "center",
    paddingVertical: 13,
  },
  secondaryActionText: {
    color: theme.colors.ink,
    fontWeight: "700",
  },
});
