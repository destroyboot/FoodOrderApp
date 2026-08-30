import { Image, Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { resolveApiUrl } from "../../lib/config";
import { sharedStyles, theme } from "../../lib/theme";
import type { MenuItem } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  item: MenuItem;
  onAddToCart: (item: MenuItem) => void;
  formatAllergens: (value: string) => string;
  t: TFunction;
};

export function MenuItemCard({ item, onAddToCart, formatAllergens, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.rowTop}>
        {item.photoUrl ? (
          <Image
            source={{ uri: resolveApiUrl(item.photoUrl) }}
            style={{ width: 92, height: 92, borderRadius: theme.radius.medium, backgroundColor: "#e5e7eb" }}
          />
        ) : null}
        <View style={[sharedStyles.stackMd, sharedStyles.flexOne]}>
          <Text style={sharedStyles.sectionTitle}>{item.name}</Text>
          <Text style={{ color: theme.colors.inkMuted, lineHeight: 19 }}>{item.description || t("menu.noDescription", "No description yet.")}</Text>
          {item.allergens ? <Text style={{ color: "#9a3412", fontSize: 12 }}>{t("menu.allergens", "Allergens")}: {formatAllergens(item.allergens)}</Text> : null}
          {item.enableIngredientSwap ? <Text style={{ color: theme.colors.info, fontWeight: "700", fontSize: 12 }}>{t("menu.ingredientSwapAvailable", "Ingredient changes available")}</Text> : null}
          <Text style={{ fontSize: 17, fontWeight: "700", color: theme.colors.info }}>{item.currentPrice.toFixed(2)}</Text>
        </View>
      </View>
      <View style={{ marginTop: 4 }}>
        <PrimaryButton label={t("menu.addToCart", "Add to cart")} onPress={() => onAddToCart(item)} />
      </View>
    </SectionCard>
  );
}
