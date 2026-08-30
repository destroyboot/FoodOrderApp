import { Modal, Pressable, ScrollView, Text, View } from "react-native";
import type { MenuItemCustomization } from "../types/api";
import { PrimaryButton } from "./PrimaryButton";
import { sharedStyles, theme } from "../lib/theme";

type Props = {
  visible: boolean;
  title: string;
  basePrice: number;
  customization: MenuItemCustomization | null;
  removedIngredientIds: number[];
  addedIngredientIds: number[];
  onToggleRemoved: (ingredientId: number) => void;
  onToggleAdded: (ingredientId: number) => void;
  onConfirm: () => void | Promise<void>;
  onCancel: () => void;
  t: (key: string, fallback: string) => string;
  confirmLabel?: string;
};

export function IngredientCustomizationModal({
  visible,
  title,
  basePrice,
  customization,
  removedIngredientIds,
  addedIngredientIds,
  onToggleRemoved,
  onToggleAdded,
  onConfirm,
  onCancel,
  t,
  confirmLabel,
}: Props) {
  const extraCount = Math.max(0, addedIngredientIds.length - removedIngredientIds.length);
  const extraCharge = extraCount * (customization?.extraIngredientPrice ?? 0);
  const totalPrice = basePrice + extraCharge;
  const removableIds = new Set(customization?.removableIngredientIds ?? []);

  return (
    <Modal visible={visible && !!customization} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable
        onPress={onCancel}
        style={sharedStyles.modalBackdrop}
      >
        <Pressable
          onPress={() => undefined}
          style={sharedStyles.modalCard}
        >
          <Text style={sharedStyles.modalTitle}>{title}</Text>
          <Text style={sharedStyles.themeMutedText}>{t("menu.customizeIngredients", "Customize ingredients")}</Text>

          <ScrollView contentContainerStyle={{ gap: 12 }} keyboardShouldPersistTaps="handled">
            <View style={sharedStyles.stackMd}>
              <Text style={sharedStyles.title}>{t("menu.dishIngredients", "Dish ingredients")}</Text>
              {(customization?.dishIngredients ?? []).length === 0 ? (
                <Text style={sharedStyles.mutedText}>{t("menu.noDishIngredients", "No dish ingredients configured.")}</Text>
              ) : (
                (customization?.dishIngredients ?? []).map((ingredient) => {
                  const canRemove = removableIds.has(ingredient.ingredientId);
                  const selected = removedIngredientIds.includes(ingredient.ingredientId);
                  return (
                    <Pressable
                      key={`remove-${ingredient.ingredientId}`}
                      onPress={() => {
                        if (canRemove) {
                          onToggleRemoved(ingredient.ingredientId);
                        }
                      }}
                      style={{
                        borderWidth: 1,
                        borderColor: selected ? theme.colors.danger : canRemove ? theme.colors.border : "#e5e7eb",
                        backgroundColor: selected ? theme.colors.dangerSoft : canRemove ? theme.colors.surface : theme.colors.surfaceMuted,
                        borderRadius: 8,
                        paddingHorizontal: 12,
                        paddingVertical: 10,
                        opacity: canRemove ? 1 : 0.75,
                      }}
                    >
                      <Text style={{ fontWeight: "600", color: selected ? "#991b1b" : "#111827" }}>
                        {selected
                          ? `${t("menu.removed", "Removed")}: `
                          : canRemove
                            ? ""
                            : `${t("menu.fixedIngredient", "Fixed")}: `}
                        {ingredient.name}
                      </Text>
                    </Pressable>
                  );
                })
              )}
            </View>

            <View style={sharedStyles.stackMd}>
              <Text style={sharedStyles.title}>{t("menu.addIngredients", "Add ingredients")}</Text>
              {(customization?.substituteIngredients ?? []).length === 0 ? (
                <Text style={sharedStyles.mutedText}>{t("menu.noExtraIngredients", "No extra ingredients available.")}</Text>
              ) : (
                (customization?.substituteIngredients ?? []).map((ingredient) => {
                  const selected = addedIngredientIds.includes(ingredient.ingredientId);
                  return (
                    <Pressable
                      key={`add-${ingredient.ingredientId}`}
                      onPress={() => onToggleAdded(ingredient.ingredientId)}
                      style={{
                        borderWidth: 1,
                        borderColor: selected ? theme.colors.success : theme.colors.border,
                        backgroundColor: selected ? theme.colors.successSoft : theme.colors.surface,
                        borderRadius: 8,
                        paddingHorizontal: 12,
                        paddingVertical: 10,
                      }}
                    >
                      <Text style={{ fontWeight: "600", color: selected ? "#166534" : "#111827" }}>
                        {selected ? `${t("common.selected", "Selected")}: ` : ""}{ingredient.name}
                      </Text>
                    </Pressable>
                  );
                })
              )}
            </View>

            <Text style={sharedStyles.bodyMuted}>
              {t("menu.basePrice", "Base price")}: {basePrice.toFixed(2)}
            </Text>
            <Text style={sharedStyles.bodyMuted}>
              {t("menu.extraIngredientCount", "Charged extra ingredients")}: {extraCount}
            </Text>
            <Text style={sharedStyles.bodyMuted}>
              {t("menu.extraIngredientUnitPrice", "Price per extra ingredient")}: {(customization?.extraIngredientPrice ?? 0).toFixed(2)}
            </Text>
            <Text style={sharedStyles.title}>
              {t("menu.extraIngredientsTotal", "Extra ingredients total")}: {extraCharge.toFixed(2)}
            </Text>
            <Text style={sharedStyles.title}>
              {t("orders.total", "Total")}: {totalPrice.toFixed(2)}
            </Text>

            <PrimaryButton
              label={confirmLabel ?? t("menu.confirmCustomization", "Add customized item")}
              onPress={onConfirm}
            />
            <PrimaryButton label={t("common.cancel", "Cancel")} onPress={onCancel} />
          </ScrollView>
        </Pressable>
      </Pressable>
    </Modal>
  );
}
