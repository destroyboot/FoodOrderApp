import type { ItemDto, TFunction } from "./types";

type MenuItemsTableProps = {
  items: ItemDto[];
  categoryMap: Record<number, string>;
  getDisplayedTranslation: (item: ItemDto) => { name: string; description: string; allergens: string };
  getDisplayedAllergens: (item: ItemDto) => string;
  onEdit: (item: ItemDto) => void;
  onDelete: (id: number) => void;
  t: TFunction;
};

export function MenuItemsTable({
  items,
  categoryMap,
  getDisplayedTranslation,
  getDisplayedAllergens,
  onEdit,
  onDelete,
  t,
}: MenuItemsTableProps) {
  return (
    <table width="100%" cellPadding={8}>
      <thead>
        <tr>
          <th align="left">{t("common.id", "ID")}</th>
          <th align="left">{t("ui.category", "Category")}</th>
          <th align="left">{t("common.name", "Name")}</th>
          <th align="left">{t("ui.description", "Description")}</th>
          <th align="left">{t("ui.allergens", "Allergens")}</th>
          <th align="left">{t("ui.order", "Order")}</th>
          <th align="left">{t("common.price", "Price")}</th>
          <th align="left">{t("menuItems.swap", "Swap")}</th>
          <th align="left">{t("ui.available", "Available")}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.id}>
            <td>{item.id}</td>
            <td>{categoryMap[item.menuCategoryId] ?? item.menuCategoryId}</td>
            <td>{getDisplayedTranslation(item).name || "-"}</td>
            <td>{getDisplayedTranslation(item).description || "-"}</td>
            <td>{getDisplayedAllergens(item) || "-"}</td>
            <td>{item.sortOrder ?? 0}</td>
            <td>{item.currentPrice}</td>
            <td>{item.enableIngredientSwap ? t("common.yes", "Yes") : t("common.no", "No")}</td>
            <td>{item.isAvailable ? t("common.yes", "Yes") : t("common.no", "No")}</td>
            <td>
              <div style={{ display: "flex", gap: 8 }}>
                <button onClick={() => onEdit(item)}>{t("common.edit", "Edit")}</button>
                <button onClick={() => onDelete(item.id)} className="button-danger">
                  {t("common.delete", "Delete")}
                </button>
              </div>
            </td>
          </tr>
        ))}
        {items.length === 0 ? (
          <tr>
            <td colSpan={10} style={{ color: "#666", padding: 16 }}>
              {t("menuItems.none", "No items match this search.")}
            </td>
          </tr>
        ) : null}
      </tbody>
    </table>
  );
}
