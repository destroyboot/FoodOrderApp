import { Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import type { ActiveCartSummary } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  activeCarts: ActiveCartSummary[];
  onOpenCart: (cartId: number, restaurantId?: number | null) => void;
  onRemoveCart: (cartId: number) => void;
  t: TFunction;
};

export function ActiveCartPicker({ activeCarts, onOpenCart, onRemoveCart, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("cart.chooseCart", "Choose a cart")}</Text>
        {activeCarts.map((draft) => (
          <View key={draft.cartId} style={[sharedStyles.stackMd, sharedStyles.dividerTop]}>
            <Text style={sharedStyles.title}>{draft.restaurantName ?? `Restaurant #${draft.restaurantId}`}</Text>
            <Text style={sharedStyles.mutedText}>
              {draft.totalQuantity} {t("orders.items", "item")}{draft.totalQuantity === 1 ? "" : "s"} {t("cart.in", "in")} {draft.itemCount} {t("cart.lines", "line")}{draft.itemCount === 1 ? "" : "s"}
            </Text>
            <View style={sharedStyles.stackMd}>
              <PrimaryButton label={t("cart.openThisCart", "Open this cart")} onPress={() => onOpenCart(draft.cartId, draft.restaurantId)} />
              <PrimaryButton label={t("cart.removeCart", "Remove cart")} onPress={() => onRemoveCart(draft.cartId)} />
            </View>
          </View>
        ))}
      </View>
    </SectionCard>
  );
}
