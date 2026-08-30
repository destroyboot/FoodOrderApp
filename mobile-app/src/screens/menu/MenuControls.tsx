import { ScrollView, TextInput, View } from "react-native";
import { PickerField } from "../../components/PickerField";
import { PrimaryButton } from "../../components/PrimaryButton";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { MenuCategory } from "../../types/api";
import type { MenuSortMode, TFunction } from "./types";

type Props = {
  search: string;
  categoryId: number | null;
  sortMode: MenuSortMode;
  categories: MenuCategory[];
  onSearchChange: (value: string) => void;
  onCategoryChange: (value: number | null) => void;
  onSortModeChange: (value: MenuSortMode) => void;
  t: TFunction;
};

export function MenuControls({
  search,
  categoryId,
  sortMode,
  categories,
  onSearchChange,
  onCategoryChange,
  onSortModeChange,
  t,
}: Props) {
  return (
    <>
      <TextInput
        placeholder={t("menu.search", "Search dishes")}
        value={search}
        onChangeText={onSearchChange}
        style={inputStyle}
      />

      <PickerField
        label={t("menu.sortMenu", "Sort menu")}
        placeholder={t("menu.chooseSorting", "Choose sorting")}
        value={sortMode}
        options={[
          { label: t("menu.sortDefault", "Default order"), value: "default" },
          { label: t("menu.sortName", "Name"), value: "name" },
          { label: t("menu.sortPriceAsc", "Price low to high"), value: "priceAsc" },
          { label: t("menu.sortPriceDesc", "Price high to low"), value: "priceDesc" },
        ]}
        onChange={(value) => onSortModeChange(value as MenuSortMode)}
      />

      <ScrollView horizontal showsHorizontalScrollIndicator={false}>
        <View style={sharedStyles.rowTop}>
          <PrimaryButton label={t("common.all", "All")} onPress={() => onCategoryChange(null)} disabled={categoryId === null} />
          {categories.map((category) => (
            <PrimaryButton
              key={category.id}
              label={category.name}
              onPress={() => onCategoryChange(category.id)}
              disabled={categoryId === category.id}
            />
          ))}
        </View>
      </ScrollView>
    </>
  );
}
