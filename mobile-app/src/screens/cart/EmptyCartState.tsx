import { Text, View } from "react-native";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import type { TFunction } from "./types";

type Props = {
  hasMultipleCarts: boolean;
  t: TFunction;
};

export function EmptyCartState({ hasMultipleCarts, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <Text style={sharedStyles.title}>{hasMultipleCarts ? t("cart.chooseCart", "Choose a cart") : t("cart.noCartSelected", "No cart selected")}</Text>
        <Text style={sharedStyles.mutedText}>
          {hasMultipleCarts
            ? t("cart.openActiveCartHint", "Open one of your active carts above to see its items.")
            : t("cart.addSomethingHint", "Add something from the menu or open one of your active carts above.")}
        </Text>
      </View>
    </SectionCard>
  );
}
