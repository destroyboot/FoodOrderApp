import { useMemo, useRef, useState } from "react";
import { Animated, ScrollView, Text, View } from "react-native";
import { IngredientCustomizationModal } from "../components/IngredientCustomizationModal";
import { useAppSession } from "../context/AppSessionContext";
import { apiRequest } from "../lib/api";
import { showMessage } from "../lib/dialogs";
import { sharedStyles } from "../lib/theme";
import type { MenuItem, MenuItemCustomization } from "../types/api";
import { CustomizeDecisionModal } from "./menu/CustomizeDecisionModal";
import { MenuControls } from "./menu/MenuControls";
import { MenuFeedbackToast } from "./menu/MenuFeedbackToast";
import { MenuItemCard } from "./menu/MenuItemCard";
import type { MenuSortMode } from "./menu/types";

export function MenuScreen() {
  const { categories, items, selectedRestaurant, currentCulture, addItemToCart, t } = useAppSession();
  const [search, setSearch] = useState("");
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [sortMode, setSortMode] = useState<MenuSortMode>("default");
  const [feedbackText, setFeedbackText] = useState("");
  const [pendingDecisionItem, setPendingDecisionItem] = useState<MenuItem | null>(null);
  const [customizingItem, setCustomizingItem] = useState<MenuItem | null>(null);
  const [customization, setCustomization] = useState<MenuItemCustomization | null>(null);
  const [removedIngredientIds, setRemovedIngredientIds] = useState<number[]>([]);
  const [addedIngredientIds, setAddedIngredientIds] = useState<number[]>([]);
  const animation = useRef(new Animated.Value(0)).current;

  function formatAllergens(value: string) {
    return value
      .split(",")
      .map((allergen) => {
        const original = allergen.trim();
        const key = original.toLowerCase().replace(/[^a-z0-9]+/g, "");
        return t(`menu.allergen.${key}`, original);
      })
      .filter(Boolean)
      .join(", ");
  }

  const filteredItems = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    const nextItems = items.filter((item) => {
      const matchesCategory = categoryId == null || item.menuCategoryId === categoryId;
      const haystack = `${item.name} ${item.description ?? ""} ${item.allergens ?? ""}`.toLowerCase();
      const matchesSearch = normalized.length === 0 || normalized.split(/\s+/).every((term) => haystack.includes(term));
      return matchesCategory && matchesSearch;
    });

    if (sortMode === "name") {
      return [...nextItems].sort((a, b) => a.name.localeCompare(b.name));
    }

    if (sortMode === "priceAsc") {
      return [...nextItems].sort((a, b) => a.currentPrice - b.currentPrice || a.name.localeCompare(b.name));
    }

    if (sortMode === "priceDesc") {
      return [...nextItems].sort((a, b) => b.currentPrice - a.currentPrice || a.name.localeCompare(b.name));
    }

    return nextItems;
  }, [items, categoryId, search, sortMode]);

  function animateFeedback(text: string) {
    setFeedbackText(text);
    animation.setValue(0);
    Animated.sequence([
      Animated.timing(animation, { toValue: 1, duration: 200, useNativeDriver: true }),
      Animated.delay(600),
      Animated.timing(animation, { toValue: 0, duration: 500, useNativeDriver: true }),
    ]).start(() => setFeedbackText(""));
  }

  async function addDefaultItem(item: MenuItem) {
    await addItemToCart(item.id);
    animateFeedback(`${item.name} ${t("menu.addedToCart", "added to cart")}`);
  }

  async function beginAddToCart(item: MenuItem) {
    if (!item.enableIngredientSwap) {
      await addDefaultItem(item);
      return;
    }

    setPendingDecisionItem(item);
  }

  async function openCustomization(item: MenuItem) {
    const response = await apiRequest<MenuItemCustomization>(`/api/menu/items/${item.id}/customization?lang=${encodeURIComponent(currentCulture)}`);
    setCustomizingItem(item);
    setCustomization(response);
    setRemovedIngredientIds([]);
    setAddedIngredientIds([]);
  }

  function toggleRemoved(id: number) {
    setRemovedIngredientIds((current) => (current.includes(id) ? current.filter((value) => value !== id) : [...current, id]));
  }

  function toggleAdded(id: number) {
    setAddedIngredientIds((current) => (current.includes(id) ? current.filter((value) => value !== id) : [...current, id]));
  }

  async function confirmCustomization() {
    if (!customizingItem) {
      return;
    }

    await addItemToCart(customizingItem.id, { removedIngredientIds, addedIngredientIds });
    animateFeedback(`${customizingItem.name} ${t("menu.addedToCart", "added to cart")}`);
    setCustomizingItem(null);
    setCustomization(null);
    setRemovedIngredientIds([]);
    setAddedIngredientIds([]);
  }

  return (
    <View style={[sharedStyles.flexOne, sharedStyles.screen]}>
      <MenuFeedbackToast text={feedbackText} animation={animation} />

      <ScrollView contentContainerStyle={sharedStyles.screenContent}>
        <View style={sharedStyles.stackSm}>
          <Text style={sharedStyles.pageTitle}>{t("nav.menu", "Menu")}</Text>
          <Text style={sharedStyles.themeMutedText}>
          {selectedRestaurant ? `${t("menu.orderingFrom", "Ordering from")} ${selectedRestaurant.name}.` : t("menu.chooseRestaurant", "Choose a restaurant from the Restaurants page to load the menu.")}
          </Text>
        </View>

        <MenuControls
          search={search}
          categoryId={categoryId}
          sortMode={sortMode}
          categories={categories}
          onSearchChange={setSearch}
          onCategoryChange={setCategoryId}
          onSortModeChange={setSortMode}
          t={t}
        />

        {filteredItems.map((item) => (
          <MenuItemCard
            key={item.id}
            item={item}
            formatAllergens={formatAllergens}
            t={t}
            onAddToCart={(nextItem) => {
              void beginAddToCart(nextItem).catch((error: any) => {
                showMessage(t("menu.addFailed", "Could not add to cart"), error?.message || t("common.unknownError", "Unknown error"));
              });
            }}
          />
        ))}
      </ScrollView>

      <CustomizeDecisionModal
        item={pendingDecisionItem}
        onCancel={() => setPendingDecisionItem(null)}
        onAddAsIs={(item) => {
          setPendingDecisionItem(null);
          void addDefaultItem(item);
        }}
        onCustomize={(item) => {
          setPendingDecisionItem(null);
          void openCustomization(item).catch((error: any) => {
            showMessage(t("menu.customizationUnavailable", "Customization unavailable"), error?.message || t("common.unknownError", "Unknown error"));
          });
        }}
        t={t}
      />

      <IngredientCustomizationModal
        visible={!!customizingItem && !!customization}
        title={customizingItem?.name ?? ""}
        basePrice={customizingItem?.currentPrice ?? 0}
        customization={customization}
        removedIngredientIds={removedIngredientIds}
        addedIngredientIds={addedIngredientIds}
        onToggleRemoved={toggleRemoved}
        onToggleAdded={toggleAdded}
        onConfirm={async () => {
          try {
            await confirmCustomization();
          } catch (error: any) {
            showMessage(
              t("menu.addFailed", "Could not add to cart"),
              error?.message || t("common.unknownError", "Unknown error")
            );
          }
        }}
        onCancel={() => {
          setCustomizingItem(null);
          setCustomization(null);
          setRemovedIngredientIds([]);
          setAddedIngredientIds([]);
        }}
        t={t}
      />
    </View>
  );
}
