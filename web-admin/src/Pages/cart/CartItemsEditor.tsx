import type { CartItemDto, MenuItemDto, TFunction } from "./types";

type Props = {
  cartItems: CartItemDto[];
  menuItems: MenuItemDto[];
  onQuantityChange: (menuItemId: number, quantity: number) => void;
  onNoteChange: (menuItemId: number, note: string) => void;
  onRemoveItem: (menuItemId: number) => void;
  t: TFunction;
};

export function CartItemsEditor({
  cartItems,
  menuItems,
  onQuantityChange,
  onNoteChange,
  onRemoveItem,
  t,
}: Props) {
  return (
    <>
      <h4 style={{ marginTop: 20 }}>{t("cart.itemsInCart", "Items in cart")}</h4>

      {cartItems.length === 0 ? (
        <div>{t("cart.noItems", "No items in cart.")}</div>
      ) : (
        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("common.item", "Item")}</th>
              <th align="left">{t("common.quantity", "Qty")}</th>
              <th align="left">{t("common.note", "Note")}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {cartItems.map((cartItem) => {
              const item = menuItems.find((entry) => entry.id === cartItem.menuItemId);

              return (
                <tr key={cartItem.menuItemId}>
                  <td>{item?.name ?? cartItem.menuItemId}</td>
                  <td>
                    <input
                      type="number"
                      min={1}
                      value={cartItem.quantity}
                      onChange={(e) => onQuantityChange(cartItem.menuItemId, Number(e.target.value))}
                      style={{ width: 70 }}
                    />
                  </td>
                  <td>
                    <input
                      value={cartItem.note ?? ""}
                      onChange={(e) => onNoteChange(cartItem.menuItemId, e.target.value)}
                    />
                  </td>
                  <td>
                    <button onClick={() => onRemoveItem(cartItem.menuItemId)}>{t("common.remove", "Remove")}</button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </>
  );
}
