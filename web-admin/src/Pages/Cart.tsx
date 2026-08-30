import { useEffect, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { PageShell } from "../Components/PageShell";
import { AvailableMenuItems } from "./cart/AvailableMenuItems";
import { CartItemsEditor } from "./cart/CartItemsEditor";
import { CartMetaForm } from "./cart/CartMetaForm";
import { CartPreviewPanel } from "./cart/CartPreviewPanel";
import type { CartCreateResponseDto, CartDto, CartItemDto, CartPreviewResponseDto, MenuItemDto, RestaurantDto, RestaurantSettingsDto, RestaurantTableDto } from "./cart/types";

function getOrderTypeOptions(settings: RestaurantSettingsDto | null, t: (key: string, fallback: string) => string) {
  const allOrderTypeOptions = [
    { value: 0, label: t("mobile.restaurants.toTable", "To table") },
    { value: 1, label: t("mobile.restaurants.pickup", "Pick up") },
    { value: 2, label: t("mobile.restaurants.delivery", "Delivery") },
  ];
  if (!settings) return allOrderTypeOptions;

  return allOrderTypeOptions.filter((option) => {
    if (option.value === 0) return settings.enableTableOrders;
    if (option.value === 1) return settings.enableTakeawayOrders;
    if (option.value === 2) return settings.enableDeliveryOrders;
    return false;
  });
}

function getPaymentMethodOptions(settings: RestaurantSettingsDto | null, t: (key: string, fallback: string) => string) {
  const allPaymentMethodOptions = [
    { value: 0, label: t("restaurant.payInApp", "Pay in app") },
    { value: 1, label: t("restaurant.payAtCounter", "Pay at counter") },
  ];
  if (!settings) return allPaymentMethodOptions;

  return allPaymentMethodOptions.filter((option) => {
    if (option.value === 0) return settings.enablePayInApp;
    if (option.value === 1) return settings.enablePayAtCounter;
    return false;
  });
}

export default function Cart() {
  const { t } = useI18n();
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [menuItems, setMenuItems] = useState<MenuItemDto[]>([]);
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [restaurantTables, setRestaurantTables] = useState<RestaurantTableDto[]>([]);
  const [restaurantSettings, setRestaurantSettings] = useState<RestaurantSettingsDto | null>(null);
  const [cartId, setCartId] = useState<number | null>(null);
  const [cartItems, setCartItems] = useState<CartItemDto[]>([]);
  const [preview, setPreview] = useState<CartPreviewResponseDto | null>(null);

  const [orderType, setOrderType] = useState(0);
  const [restaurantId, setRestaurantId] = useState("");
  const [restaurantTableId, setRestaurantTableId] = useState("");
  const [paymentMethod, setPaymentMethod] = useState(1);
  const [receiptEmail, setReceiptEmail] = useState("");
  const orderTypeOptions = getOrderTypeOptions(restaurantSettings, t);
  const paymentMethodOptions = getPaymentMethodOptions(restaurantSettings, t);

  async function ensureCart() {
    const stored = localStorage.getItem("cartId");
    if (stored) {
      const parsed = Number(stored);
      if (!Number.isNaN(parsed) && parsed > 0) {
        setCartId(parsed);
        return parsed;
      }
    }

    const res = await api<CartCreateResponseDto>("/api/cart", {
      method: "POST",
      body: JSON.stringify({}),
    });

    localStorage.setItem("cartId", String(res.cartId));
    setCartId(res.cartId);
    return res.cartId;
  }

  async function loadRestaurants() {
    const res = await api<RestaurantDto[]>("/api/restaurants");
    setRestaurants(res ?? []);
    return res ?? [];
  }

  async function loadTables(nextRestaurantId: string | number) {
    if (!nextRestaurantId) {
      setRestaurantTables([]);
      return [];
    }

    const res = await api<RestaurantTableDto[]>(`/api/restaurants/${nextRestaurantId}/tables`);
    setRestaurantTables(res ?? []);
    return res ?? [];
  }

  async function loadSettings(nextRestaurantId: string | number) {
    if (!nextRestaurantId) {
      setRestaurantSettings(null);
      return null;
    }

    const res = await api<RestaurantSettingsDto>(`/api/restaurants/${nextRestaurantId}/settings`);
    setRestaurantSettings(res);

    const enabledOrderTypes = getOrderTypeOptions(res, t);
    if (!enabledOrderTypes.some((option) => option.value === orderType)) {
      setOrderType(enabledOrderTypes[0]?.value ?? 0);
    }

    const enabledPaymentMethods = getPaymentMethodOptions(res, t);
    if (!enabledPaymentMethods.some((option) => option.value === paymentMethod)) {
      setPaymentMethod(enabledPaymentMethods[0]?.value ?? 1);
    }

    return res;
  }

  async function loadMenu(nextRestaurantId: string | number) {
    if (!nextRestaurantId) {
      setMenuItems([]);
      return [];
    }

    const res = await api<MenuItemDto[]>(`/api/menu/items?restaurantId=${nextRestaurantId}`);
    setMenuItems((res ?? []).filter(x => x.isAvailable));
    return res ?? [];
  }

  async function loadCart(id?: number) {
    const resolvedId = id ?? cartId;
    if (!resolvedId) throw new Error("Cart id is required.");

    const res = await api<CartDto>(`/api/cart/${resolvedId}`);

    setCartItems(res.items ?? []);
    setOrderType(res.orderType ?? 0);
    setRestaurantId(
      res.restaurantId === null || res.restaurantId === undefined
        ? ""
        : String(res.restaurantId)
    );
    setRestaurantTableId(
      res.restaurantTableId === null || res.restaurantTableId === undefined
        ? ""
        : String(res.restaurantTableId)
    );
    setPaymentMethod(res.paymentMethod ?? 1);
    setReceiptEmail(res.receiptEmail ?? "");

    return res;
  }

  async function init() {
    setErr(null);
    setLoading(true);
  
    try {
      const restaurantList = await loadRestaurants();
  
      try {
        const id = await ensureCart();
        const cart = await loadCart(id);
        const selectedRestaurantId = cart.restaurantId ?? restaurantList[0]?.id ?? "";

        if (selectedRestaurantId) {
          setRestaurantId(String(selectedRestaurantId));
          await Promise.all([
            loadTables(selectedRestaurantId),
            loadMenu(selectedRestaurantId),
            loadSettings(selectedRestaurantId),
          ]);
        }
      } catch (e: any) {
        if ((e.message ?? "").includes("Not allowed to access this cart")) {
          localStorage.removeItem("cartId");
          setCartId(null);
  
          const newId = await ensureCart();
          const cart = await loadCart(newId);
          const selectedRestaurantId = cart.restaurantId ?? restaurantList[0]?.id ?? "";

          if (selectedRestaurantId) {
            setRestaurantId(String(selectedRestaurantId));
            await Promise.all([
              loadTables(selectedRestaurantId),
              loadMenu(selectedRestaurantId),
              loadSettings(selectedRestaurantId),
            ]);
          }
        } else {
          throw e;
        }
      }
    } catch (e: any) {
      setErr(e.message || t("cart.loadFailed", "Failed to load cart."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    init();
  }, []);

  function addToCart(menuItemId: number) {
    setCartItems(prev => {
      const existing = prev.find(x => x.menuItemId === menuItemId);

      if (existing) {
        return prev.map(x =>
          x.menuItemId === menuItemId
            ? { ...x, quantity: x.quantity + 1 }
            : x
        );
      }

      return [...prev, { menuItemId, quantity: 1, note: "" }];
    });
  }

  async function changeRestaurant(value: string) {
    setRestaurantId(value);
    setRestaurantTableId("");
    setCartItems([]);
    setPreview(null);

    if (!value) {
      setRestaurantTables([]);
      setRestaurantSettings(null);
      setMenuItems([]);
      return;
    }

    await Promise.all([
      loadTables(value),
      loadMenu(value),
      loadSettings(value),
    ]);
  }

  function updateQuantity(menuItemId: number, quantity: number) {
    if (quantity <= 0) {
      removeItem(menuItemId);
      return;
    }

    setCartItems(prev =>
      prev.map(x =>
        x.menuItemId === menuItemId
          ? { ...x, quantity }
          : x
      )
    );
  }

  function updateNote(menuItemId: number, note: string) {
    setCartItems(prev =>
      prev.map(x =>
        x.menuItemId === menuItemId
          ? { ...x, note }
          : x
      )
    );
  }

  function removeItem(menuItemId: number) {
    setCartItems(prev => prev.filter(x => x.menuItemId !== menuItemId));
  }

  async function saveItems() {
    try {
      setErr(null);
      const id = await ensureCart();

      await api(`/api/cart/${id}/items`, {
        method: "PUT",
        body: JSON.stringify({
          items: cartItems.map(x => ({
            menuItemId: x.menuItemId,
            quantity: x.quantity,
            note: x.note || null,
          })),
        }),
      });

      await loadCart(id);
    } catch (e: any) {
      setErr(e.message || t("cart.saveItemsFailed", "Failed to save cart items."));
    }
  }

  async function saveMeta() {
    try {
      setErr(null);
      const id = await ensureCart();

      await api(`/api/cart/${id}/meta`, {
        method: "PUT",
        body: JSON.stringify({
          orderType,
          restaurantId: restaurantId ? Number(restaurantId) : null,
          restaurantTableId:
            orderType === 0 && restaurantTableId
              ? Number(restaurantTableId)
              : null,
          tableNumber: null,
          paymentMethod,
          scheduledFor: null,
          receiptEmail: receiptEmail || null,
        }),
      });

      await loadCart(id);
    } catch (e: any) {
      setErr(e.message || t("cart.saveMetaFailed", "Failed to save cart meta."));
    }
  }

  async function runPreview() {
    try {
      setErr(null);
      const id = await ensureCart();

      const res = await api<CartPreviewResponseDto>(`/api/cart/${id}/preview`, {
        method: "POST",
      });

      setPreview(res);
    } catch (e: any) {
      setErr(e.message || t("cart.previewFailed", "Failed to preview cart."));
    }
  }

  async function finalizeCart() {
    try {
      setErr(null);
      const id = await ensureCart();

      await api(`/api/cart/${id}/finalize`, {
        method: "POST",
      });

      alert(t("cart.finalized", "Cart finalized."));
      localStorage.removeItem("cartId");
      setCartId(null);
      setCartItems([]);
      setPreview(null);
      await init();
    } catch (e: any) {
      setErr(e.message || t("cart.finalizeFailed", "Failed to finalize cart."));
    }
  }

  return (
    <PageShell title={t("nav.cart", "Cart")} error={err} maxWidth={1200}>
      {loading && <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div>}

      <div style={{ display: "grid", gridTemplateColumns: "1.2fr 1fr", gap: 24 }}>
        <AvailableMenuItems menuItems={menuItems} onAddToCart={addToCart} t={t} />

        <div>
          <h3>{t("cart.details", "Cart details")}</h3>

          <CartMetaForm
            orderType={orderType}
            restaurantId={restaurantId}
            restaurantTableId={restaurantTableId}
            paymentMethod={paymentMethod}
            receiptEmail={receiptEmail}
            restaurants={restaurants}
            restaurantTables={restaurantTables}
            orderTypeOptions={orderTypeOptions}
            paymentMethodOptions={paymentMethodOptions}
            onOrderTypeChange={setOrderType}
            onRestaurantChange={(value) => void changeRestaurant(value)}
            onRestaurantTableChange={setRestaurantTableId}
            onPaymentMethodChange={setPaymentMethod}
            onReceiptEmailChange={setReceiptEmail}
            onSaveMeta={() => void saveMeta()}
            t={t}
          />

          <CartItemsEditor
            cartItems={cartItems}
            menuItems={menuItems}
            onQuantityChange={updateQuantity}
            onNoteChange={updateNote}
            onRemoveItem={removeItem}
            t={t}
          />

          <CartPreviewPanel
            preview={preview}
            onSaveItems={() => void saveItems()}
            onPreview={() => void runPreview()}
            onFinalize={() => void finalizeCart()}
            t={t}
          />
        </div>
      </div>
    </PageShell>
  );
}
