import type { MenuItemDto, TFunction } from "./types";

type Props = {
  menuItems: MenuItemDto[];
  onAddToCart: (menuItemId: number) => void;
  t: TFunction;
};

export function AvailableMenuItems({ menuItems, onAddToCart, t }: Props) {
  return (
    <div>
      <h3>{t("cart.availableMenuItems", "Available menu items")}</h3>

      <table width="100%" cellPadding={8}>
        <thead>
          <tr>
            <th align="left">{t("common.id", "Id")}</th>
            <th align="left">{t("common.name", "Name")}</th>
            <th align="left">{t("common.price", "Price")}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {menuItems.map((item) => (
            <tr key={item.id}>
              <td>{item.id}</td>
              <td>{item.name ?? "-"}</td>
              <td>{item.currentPrice}</td>
              <td>
                <button onClick={() => onAddToCart(item.id)}>{t("common.add", "Add")}</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
