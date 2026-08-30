import { Modal, Pressable, Text } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { sharedStyles } from "../../lib/theme";
import type { MenuItem } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  item: MenuItem | null;
  onAddAsIs: (item: MenuItem) => void;
  onCustomize: (item: MenuItem) => void;
  onCancel: () => void;
  t: TFunction;
};

export function CustomizeDecisionModal({ item, onAddAsIs, onCustomize, onCancel, t }: Props) {
  return (
    <Modal visible={!!item} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable onPress={onCancel} style={sharedStyles.modalBackdrop}>
        <Pressable onPress={() => undefined} style={sharedStyles.modalCardCompact}>
          <Text style={sharedStyles.modalTitle}>{item?.name ?? t("menu.addToCart", "Add to cart")}</Text>
          <Text style={sharedStyles.bodyMuted}>
            {t("menu.askCustomizeBeforeAdd", "Do you want to customize this dish before adding it to the cart?")}
          </Text>
          <PrimaryButton
            label={t("menu.addAsIs", "Add as is")}
            onPress={() => {
              if (item) {
                onAddAsIs(item);
              }
            }}
          />
          <PrimaryButton
            label={t("menu.customizeIngredients", "Customize ingredients")}
            onPress={() => {
              if (item) {
                onCustomize(item);
              }
            }}
          />
          <PrimaryButton label={t("common.cancel", "Cancel")} onPress={onCancel} />
        </Pressable>
      </Pressable>
    </Modal>
  );
}
