import { Text, View } from "react-native";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import type { CartPreview } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  preview: CartPreview | null;
  t: TFunction;
};

export function CartTotalsSection({ preview, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("orders.total", "Totals")}</Text>
        {preview ? (
          <>
            <Text>{t("orders.subtotal", "Subtotal")}: {preview.subtotal.toFixed(2)}</Text>
            <Text>{t("menu.customizationExtra", "Customization extra")}: {preview.extraChargeTotal.toFixed(2)}</Text>
            <Text>{t("orders.deliveryFee", "Delivery fee")}: {preview.deliveryFee.toFixed(2)}</Text>
            <Text style={sharedStyles.title}>{t("orders.total", "Total")}: {preview.total.toFixed(2)}</Text>
            <Text>{t("cart.estimatedPrep", "Estimated prep")}: {preview.estimatedPreparationMinutes} min</Text>
          </>
        ) : (
          <Text style={sharedStyles.mutedText}>{t("cart.totalsAuto", "Totals will appear automatically as the cart refreshes.")}</Text>
        )}
      </View>
    </SectionCard>
  );
}
