import { Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import type { CartResponse, MenuItem } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  cart: CartResponse;
  itemMap: Record<number, MenuItem>;
  onCustomizeLine: (lineId: number, menuItem: MenuItem, removedIngredientIds: number[], addedIngredientIds: number[]) => void;
  onDecreaseQuantity: (lineId: number, quantity: number) => void;
  onIncreaseQuantity: (lineId: number, quantity: number) => void;
  t: TFunction;
};

export function CartItemsSection({
  cart,
  itemMap,
  onCustomizeLine,
  onDecreaseQuantity,
  onIncreaseQuantity,
  t,
}: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("nav.items", "Items")}</Text>
        {cart.items.length ? (
          cart.items.map((line) => {
            const menuItem = itemMap[line.menuItemId];
            return (
              <View key={line.lineId} style={[sharedStyles.stackMd, sharedStyles.dividerBottom]}>
                <Text style={sharedStyles.title}>{menuItem?.name ?? line.menuItemName ?? `${t("orders.item", "Item")} ${line.menuItemId}`}</Text>
                <Text style={sharedStyles.bodyMuted}>{line.quantity} x {line.unitPrice.toFixed(2)} = {line.lineTotal.toFixed(2)}</Text>
                {line.extraCharge > 0 ? (
                  <Text style={{ color: "#2563eb" }}>
                    {t("menu.customizationExtra", "Customization extra")}: {line.extraCharge.toFixed(2)}
                  </Text>
                ) : null}
                {line.note ? <Text style={sharedStyles.mutedText}>{line.note}</Text> : null}
                {menuItem?.enableIngredientSwap ? (
                  <PrimaryButton
                    label={t("menu.customizeIngredients", "Customize ingredients")}
                    onPress={() => onCustomizeLine(line.lineId, menuItem, line.removedIngredientIds ?? [], line.addedIngredientIds ?? [])}
                  />
                ) : null}
                <View style={sharedStyles.row}>
                  <PrimaryButton label="-" onPress={() => onDecreaseQuantity(line.lineId, line.quantity)} />
                  <Text style={[sharedStyles.title, { minWidth: 24, textAlign: "center" }]}>{line.quantity}</Text>
                  <PrimaryButton label="+" onPress={() => onIncreaseQuantity(line.lineId, line.quantity)} />
                </View>
              </View>
            );
          })
        ) : (
          <Text style={sharedStyles.mutedText}>{t("cart.empty", "This cart is empty.")}</Text>
        )}
      </View>
    </SectionCard>
  );
}
