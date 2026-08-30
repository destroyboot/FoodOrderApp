import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import { AppState, Platform } from "react-native";
import * as Notifications from "expo-notifications";
import { appI18n } from "../lib/i18n";
import { apiRequest } from "../lib/api";
import { createGuestToken } from "../lib/guest";
import { getDeviceLabel, registerForPushNotificationsAsync } from "../lib/push";
import { getAuthToken, getCartId, getGuestToken, getRestaurantId, setAuthToken, setCartId, setGuestToken, setRestaurantId } from "../lib/storage";
import type { ActiveCartSummary, AppLanguage, AppLanguagePayload, AppTextDictionary, AuthResponse, BillingProfile, CartCreateResponse, CartPreview, CartResponse, FinalizeResponse, MenuCategory, MenuItem, MenuItemCustomization, NotificationItem, OrderDetails, OrderSummary, Reservation, ReservationAvailability, Restaurant, RestaurantSettings, RestaurantTable } from "../types/api";
import { NotificationType, OrderType, PaymentMethod } from "../types/api";

function logBackgroundError(context: string, error: unknown) {
  if (__DEV__) {
    console.warn(`[AppSession] ${context}`, error);
  }
}

type SessionContextValue = {
  booting: boolean;
  bootError: string | null;
  token: string | null;
  guestToken: string | null;
  cartId: number | null;
  cart: CartResponse | null;
  activeCarts: ActiveCartSummary[];
  preview: CartPreview | null;
  restaurants: Restaurant[];
  selectedRestaurantId: number | null;
  selectedRestaurant: Restaurant | null;
  restaurantSettings: RestaurantSettings | null;
  tables: RestaurantTable[];
  categories: MenuCategory[];
  items: MenuItem[];
  activeOrders: OrderSummary[];
  orderHistory: OrderSummary[];
  orderDetailsById: Record<number, OrderDetails | undefined>;
  notifications: NotificationItem[];
  transientNotification: { title: string; body: string } | null;
  pushStatus: string;
  reservationAvailability: ReservationAvailability | null;
  reservations: Reservation[];
  billingProfile: BillingProfile | null;
  appLanguages: AppLanguage[];
  currentCulture: string;
  t: (key: string, fallback: string) => string;
  bootstrap: () => Promise<void>;
  refreshRestaurants: () => Promise<void>;
  selectRestaurant: (restaurantId: number) => Promise<void>;
  refreshMenu: () => Promise<void>;
  refreshCart: () => Promise<void>;
  loadActiveCarts: () => Promise<void>;
  openCart: (cartId: number) => Promise<void>;
  deleteCart: (cartId: number) => Promise<void>;
  setCartMeta: (input: {
    orderType?: OrderType;
    paymentMethod?: PaymentMethod;
    restaurantTableId?: number | null;
    scheduledFor?: string | null;
    pickupContactName?: string | null;
    pickupPhone?: string | null;
    pickupNote?: string | null;
    deliveryContactName?: string | null;
    deliveryPhone?: string | null;
    deliveryAddressLine1?: string | null;
    deliveryAddressLine2?: string | null;
    deliveryCity?: string | null;
    deliveryPostalCode?: string | null;
    deliveryCountry?: string | null;
    deliveryNote?: string | null;
    receiptEmail?: string | null;
    billingDetails?: CartResponse["billingDetails"] | null;
  }) => Promise<void>;
  addItemToCart: (menuItemId: number, customization?: { removedIngredientIds?: number[]; addedIngredientIds?: number[] }) => Promise<void>;
  setItemQuantity: (lineId: number, quantity: number) => Promise<void>;
  updateItemCustomization: (lineId: number, customization: { removedIngredientIds?: number[]; addedIngredientIds?: number[] }) => Promise<void>;
  previewCart: () => Promise<void>;
  finalizeCart: () => Promise<FinalizeResponse | null>;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  confirmRegistration: (email: string, code: string) => Promise<void>;
  loadMyActiveOrders: () => Promise<void>;
  loadMyOrderHistory: () => Promise<void>;
  loadOrderDetails: (orderId: number) => Promise<void>;
  loadNotifications: (announceNew?: boolean) => Promise<void>;
  dismissTransientNotification: () => void;
  loadReservationAvailability: (restaurantId: number, date: string) => Promise<void>;
  createReservation: (input: { restaurantId: number; restaurantTableId?: number | null; partySize: number; startAt: string; note?: string | null }) => Promise<void>;
  cancelReservation: (reservationId: number) => Promise<void>;
  loadMyReservations: () => Promise<void>;
  loadBillingProfile: () => Promise<void>;
  saveBillingProfile: (profile: BillingProfile) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  setPreferredCulture: (culture: string) => Promise<void>;
  requestAccountDeletion: () => Promise<void>;
  confirmAccountDeletion: (code: string) => Promise<void>;
};

const AppSessionContext = createContext<SessionContextValue | null>(null);

export function AppSessionProvider({ children }: { children: React.ReactNode }) {
  const [booting, setBooting] = useState(true);
  const [bootError, setBootError] = useState<string | null>(null);
  const [token, setTokenState] = useState<string | null>(null);
  const [guestToken, setGuestTokenState] = useState<string | null>(null);
  const [cartId, setCartIdState] = useState<number | null>(null);
  const [cart, setCart] = useState<CartResponse | null>(null);
  const [activeCarts, setActiveCarts] = useState<ActiveCartSummary[]>([]);
  const [preview, setPreview] = useState<CartPreview | null>(null);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantIdState] = useState<number | null>(null);
  const [restaurantSettings, setRestaurantSettings] = useState<RestaurantSettings | null>(null);
  const [tables, setTables] = useState<RestaurantTable[]>([]);
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [items, setItems] = useState<MenuItem[]>([]);
  const [activeOrders, setActiveOrders] = useState<OrderSummary[]>([]);
  const [orderHistory, setOrderHistory] = useState<OrderSummary[]>([]);
  const [orderDetailsById, setOrderDetailsById] = useState<Record<number, OrderDetails | undefined>>({});
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [transientNotification, setTransientNotification] = useState<{ title: string; body: string } | null>(null);
  const [pushStatus, setPushStatus] = useState("Checking push notification support...");
  const [reservationAvailability, setReservationAvailability] = useState<ReservationAvailability | null>(null);
  const [reservations, setReservations] = useState<Reservation[]>([]);
  const [billingProfile, setBillingProfile] = useState<BillingProfile | null>(null);
  const [appLanguages, setAppLanguages] = useState<AppLanguage[]>([]);
  const [currentCulture, setCurrentCulture] = useState("pl-PL");
  const lastSeenNotificationIdRef = useRef<number>(0);
  const notificationsReadyRef = useRef(false);
  const remotePushReadyRef = useRef(false);
  const transientNotificationTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const transientNotificationQueueRef = useRef<Array<{ title: string; body: string }>>([]);
  const appStateRef = useRef(AppState.currentState);

  const selectedRestaurant = useMemo(
    () => restaurants.find((restaurant) => restaurant.id === selectedRestaurantId) ?? null,
    [restaurants, selectedRestaurantId]
  );

  useEffect(() => {
    void bootstrap();
  }, []);

  useEffect(() => {
    return () => {
      if (transientNotificationTimerRef.current) {
        clearTimeout(transientNotificationTimerRef.current);
      }
    };
  }, []);

  useEffect(() => {
    const subscription = AppState.addEventListener("change", (nextState) => {
      appStateRef.current = nextState;
    });

    return () => {
      subscription.remove();
    };
  }, []);

  useEffect(() => {
    if (booting || !guestToken) {
      return;
    }

    void syncPushRegistration();
  }, [booting, guestToken, token]);

  useEffect(() => {
    if (booting) {
      return;
    }

    if (activeCarts.length === 0) {
      if (cartId !== null || cart !== null || preview !== null) {
        void (async () => {
          await setCartId(null);
          setCartIdState(null);
          setCart(null);
          setPreview(null);
        })();
      }
      return;
    }

    if (activeCarts.length === 1) {
      const onlyCart = activeCarts[0];
      if (cartId !== onlyCart.cartId || cart?.cartId !== onlyCart.cartId) {
        void openCart(onlyCart.cartId, false);
      }
    }
  }, [booting, activeCarts, cartId, cart?.cartId, preview]);

  useEffect(() => {
    if (Platform.OS === "web") {
      return;
    }

    const receivedSubscription = Notifications.addNotificationReceivedListener(() => {
      void Promise.all([
        loadMyActiveOrders(),
        loadMyOrderHistory(),
        loadNotifications(false),
      ]);
    });

    const responseSubscription = Notifications.addNotificationResponseReceivedListener(() => {
      void Promise.all([
        loadMyActiveOrders(),
        loadMyOrderHistory(),
        loadNotifications(false),
      ]);
    });

    return () => {
      receivedSubscription.remove();
      responseSubscription.remove();
    };
  }, [guestToken, token]);

  useEffect(() => {
    const shouldPoll = !!token || activeOrders.length > 0;
    if (booting || !guestToken || !shouldPoll) {
      return;
    }

    let cancelled = false;

    async function tick(announceNew: boolean) {
      try {
        if (cancelled || appStateRef.current !== "active") return;
        await Promise.all([
          loadMyActiveOrders(),
          loadNotifications(announceNew),
        ]);
      } catch (error) {
        logBackgroundError("Order and notification polling failed.", error);
      }
    }

    void tick(false);

    const handle = setInterval(() => {
      void tick(true);
    }, 30000);

    return () => {
      cancelled = true;
      clearInterval(handle);
    };
  }, [activeOrders.length, booting, guestToken, token]);

  async function loadActiveCartsInternal(overrideToken?: string | null, overrideGuestToken?: string | null) {
    const response = await apiRequest<ActiveCartSummary[]>("/api/cart/active", getOwnerHeaders(overrideToken, overrideGuestToken));
    setActiveCarts(response);
    return response;
  }

  async function loadActiveCarts() {
    await loadActiveCartsInternal(token, guestToken);
  }

  function showNextTransientNotification() {
    if (transientNotificationTimerRef.current) {
      clearTimeout(transientNotificationTimerRef.current);
      transientNotificationTimerRef.current = null;
    }

    const next = transientNotificationQueueRef.current.shift() ?? null;
    setTransientNotification(next);

    if (!next) {
      return;
    }

    transientNotificationTimerRef.current = setTimeout(() => {
      setTransientNotification(null);
      transientNotificationTimerRef.current = null;
      showNextTransientNotification();
    }, 3000);
  }

  function enqueueTransientNotification(title: string, body: string) {
    transientNotificationQueueRef.current.push({ title, body });
    if (!transientNotification) {
      showNextTransientNotification();
    }
  }

  function dismissTransientNotification() {
    if (transientNotificationTimerRef.current) {
      clearTimeout(transientNotificationTimerRef.current);
      transientNotificationTimerRef.current = null;
    }

    setTransientNotification(null);
    showNextTransientNotification();
  }

  async function bootstrap() {
    setBooting(true);
    setBootError(null);

    try {
      const [storedToken, storedGuestToken, storedCartId, storedRestaurantId] = await Promise.all([
        getAuthToken(),
        getGuestToken(),
        getCartId(),
        getRestaurantId(),
      ]);

      const nextGuestToken = storedToken
        ? (storedGuestToken || createGuestToken())
        : createGuestToken();
      if (!storedGuestToken || !storedToken) {
        await setGuestToken(nextGuestToken);
      }
      if (!storedToken) {
        await setCartId(null);
        await setRestaurantId(null);
      }

      setTokenState(storedToken);
      setGuestTokenState(nextGuestToken);
      setCartIdState(storedToken ? storedCartId : null);
      setSelectedRestaurantIdState(storedToken ? storedRestaurantId : null);

      const bootstrapCulture = await loadLocalization(storedToken);
      await refreshRestaurants(bootstrapCulture);
      const activeDrafts = await loadActiveCartsInternal(storedToken, nextGuestToken);
      if (storedToken && storedRestaurantId) {
        await loadRestaurantContext(storedRestaurantId, bootstrapCulture);
      }

      const preferredStoredCartId = storedToken && storedCartId && activeDrafts.some((draft) => draft.cartId === storedCartId)
        ? storedCartId
        : null;

      const preferredCartId = preferredStoredCartId
        ?? activeDrafts.find((draft) => draft.restaurantId === storedRestaurantId)?.cartId
        ?? activeDrafts[0]?.cartId
        ?? null;

      if (preferredCartId) {
        try {
          setCartIdState(preferredCartId);
          await setCartId(preferredCartId);
          await loadCart(preferredCartId, storedToken, nextGuestToken);
        } catch {
          await setCartId(null);
          setCartIdState(null);
          setCart(null);
        }
      }

      await Promise.all([
        loadMyActiveOrdersInternal(storedToken, nextGuestToken),
        loadMyOrderHistoryInternal(storedToken, nextGuestToken),
        loadNotificationsInternal(false, storedToken, nextGuestToken),
      ]);
      if (storedToken) {
        await loadBillingProfileInternal(storedToken);
      }
    } catch (error: any) {
      setBootError(error?.message || "App startup failed.");
    } finally {
      setBooting(false);
    }
  }

  async function refreshRestaurants(cultureOverride?: string) {
    const cultureQuery = encodeURIComponent(cultureOverride || currentCulture);
    const data = await apiRequest<Restaurant[]>(`/api/restaurants?culture=${cultureQuery}`);
    setRestaurants(data);
  }

  async function loadRestaurantContext(restaurantId: number, cultureOverride?: string) {
    const cultureQuery = encodeURIComponent(cultureOverride || currentCulture);
    const [settings, restaurantTables, menuCategories, menuItems] = await Promise.all([
      apiRequest<RestaurantSettings>(`/api/restaurants/${restaurantId}/settings`),
      apiRequest<RestaurantTable[]>(`/api/restaurants/${restaurantId}/tables`),
      apiRequest<MenuCategory[]>(`/api/menu/categories?restaurantId=${restaurantId}&lang=${cultureQuery}`),
      apiRequest<MenuItem[]>(`/api/menu/items?restaurantId=${restaurantId}&lang=${cultureQuery}`),
    ]);

    setRestaurantSettings(settings);
    setTables(restaurantTables);
    setCategories(menuCategories);
    setItems(menuItems);
  }

  async function selectRestaurant(restaurantId: number) {
    setSelectedRestaurantIdState(restaurantId);
    await setRestaurantId(restaurantId);
    await loadRestaurantContext(restaurantId);
  }

  async function refreshMenu() {
    if (!selectedRestaurantId) return;
    await loadRestaurantContext(selectedRestaurantId);
  }

  function getOwnerHeaders(overrideToken?: string | null, overrideGuestToken?: string | null) {
    return {
      token: overrideToken ?? token,
      guestToken: overrideGuestToken ?? guestToken,
    };
  }

  async function syncPushRegistration() {
    try {
      const result = await registerForPushNotificationsAsync();
      if (!result.ok) {
        remotePushReadyRef.current = false;
        setPushStatus(result.reason);
        return;
      }

      await apiRequest<void>("/api/push/devices/register", {
        method: "POST",
        ...getOwnerHeaders(),
        body: {
          expoPushToken: result.expoPushToken,
          platform: token ? "signed-in" : "guest",
          deviceName: getDeviceLabel(),
        },
      });

      remotePushReadyRef.current = true;
      setPushStatus("Remote push is registered for this device.");
    } catch (error: any) {
      remotePushReadyRef.current = false;
      setPushStatus(error?.message || "Could not register this device for remote push notifications.");
    }
  }

  async function ensureCart() {
    if (cartId && cart?.restaurantId === selectedRestaurantId) return cartId;

    if (!selectedRestaurantId) {
      throw new Error(appI18n.t("restaurant.selectFirst", "Choose a restaurant first."));
    }

    const existing = activeCarts.find((draft) => draft.restaurantId === selectedRestaurantId);
    if (existing) {
      setCartIdState(existing.cartId);
      await setCartId(existing.cartId);
      return existing.cartId;
    }

    const response = await apiRequest<CartCreateResponse>("/api/cart", {
      method: "POST",
      ...getOwnerHeaders(),
      body: {
        restaurantId: selectedRestaurantId,
      },
    });

    setCartIdState(response.cartId);
    await setCartId(response.cartId);
    await loadActiveCarts();
    return response.cartId;
  }

  async function getWorkingCart(targetCartId: number) {
    if (cart && cart.cartId === targetCartId) {
      return cart;
    }

    return loadCart(targetCartId);
  }

  async function loadCart(targetCartId: number, overrideToken?: string | null, overrideGuestToken?: string | null) {
    const response = await apiRequest<CartResponse>(`/api/cart/${targetCartId}`, getOwnerHeaders(overrideToken, overrideGuestToken));
    setCart(response);
    return response;
  }

  async function refreshCart() {
    if (!cartId) {
      setCart(null);
      setPreview(null);
      return;
    }

    await loadCart(cartId);
    try {
      await previewCartAction();
    } catch {
      setPreview(null);
    }
  }

  async function openCart(nextCartId: number, syncRestaurant = true) {
    setCartIdState(nextCartId);
    await setCartId(nextCartId);
    const openedCart = await loadCart(nextCartId);
    if (syncRestaurant && openedCart.restaurantId && openedCart.restaurantId !== selectedRestaurantId) {
      await selectRestaurant(openedCart.restaurantId);
    }
    try {
      const response = await apiRequest<CartPreview>(`/api/cart/${nextCartId}/preview`, {
        method: "POST",
        ...getOwnerHeaders(),
      });
      setPreview(response);
    } catch {
      setPreview(null);
    }
  }

  async function deleteCart(targetCartId: number) {
    await apiRequest<void>(`/api/cart/${targetCartId}`, {
      method: "DELETE",
      ...getOwnerHeaders(),
    });

    if (cartId === targetCartId) {
      await setCartId(null);
      setCartIdState(null);
      setCart(null);
      setPreview(null);
    }

    await loadActiveCarts();
  }

  async function loadMyActiveOrders() {
    await loadMyActiveOrdersInternal(token, guestToken);
  }

  async function loadMyActiveOrdersInternal(overrideToken?: string | null, overrideGuestToken?: string | null) {
    const response = await apiRequest<OrderSummary[]>("/api/orders/my/active", getOwnerHeaders(overrideToken, overrideGuestToken));
    setActiveOrders(response);
  }

  async function loadMyOrderHistory() {
    await loadMyOrderHistoryInternal(token, guestToken);
  }

  async function loadMyOrderHistoryInternal(overrideToken?: string | null, overrideGuestToken?: string | null) {
    const response = await apiRequest<OrderSummary[]>("/api/orders/my/history", getOwnerHeaders(overrideToken, overrideGuestToken));
    setOrderHistory(response);
  }

  async function loadOrderDetails(orderId: number) {
    const response = await apiRequest<OrderDetails>(`/api/orders/${orderId}`, getOwnerHeaders());
    setOrderDetailsById((current) => ({ ...current, [orderId]: response }));
  }

  async function loadNotifications(announceNew = true) {
    await loadNotificationsInternal(announceNew, token, guestToken);
  }

  async function loadNotificationsInternal(
    announceNew = true,
    overrideToken?: string | null,
    overrideGuestToken?: string | null
  ) {
    const response = await apiRequest<NotificationItem[]>("/api/notifications?take=50", getOwnerHeaders(overrideToken, overrideGuestToken));
    setNotifications(response);

    const newestId = response.reduce((max, item) => Math.max(max, item.id), 0);
    if (!notificationsReadyRef.current) {
      const launchNotice = response.find((item) => !item.isRead && item.type === NotificationType.ReservationNoShow);
      if (launchNotice && !remotePushReadyRef.current) {
        const launchText = getNotificationText(launchNotice);
        enqueueTransientNotification(launchText.title, launchText.body);
      }
      lastSeenNotificationIdRef.current = newestId;
      notificationsReadyRef.current = true;
      return;
    }

    const newItems = response
      .filter((item) =>
        item.id > lastSeenNotificationIdRef.current &&
        (item.type === NotificationType.OrderStatusChanged || item.type === NotificationType.ReservationNoShow))
      .sort((a, b) => a.id - b.id);

    if (announceNew && !remotePushReadyRef.current) {
      for (const item of newItems) {
        const itemText = getNotificationText(item);
        enqueueTransientNotification(itemText.title, itemText.body);
      }
    }

    lastSeenNotificationIdRef.current = Math.max(lastSeenNotificationIdRef.current, newestId);
  }

  function getNotificationText(item: NotificationItem) {
    if (!item.payloadJson) {
      return { title: item.title, body: item.body };
    }

    try {
      const payload = JSON.parse(item.payloadJson) as { orderId?: number; newStatus?: string };
      if (item.type === NotificationType.OrderStatusChanged && payload.newStatus) {
        const orderLabel = payload.orderId ? `${t("orders.order", "Order")} #${payload.orderId}` : t("orders.order", "Order");
        return {
          title: t("orders.statusUpdated", "Order status updated"),
          body: `${orderLabel}: ${translateStatusName(payload.newStatus)}`,
        };
      }
    } catch {
    }

    return { title: item.title, body: item.body };
  }

  function translateStatusName(status: string) {
    switch (status) {
      case "Pending":
        return t("orders.status.pending", "Pending");
      case "Accepted":
        return t("orders.status.accepted", "Accepted");
      case "SentToKitchen":
        return t("orders.status.sentToKitchen", "Sent to kitchen");
      case "Preparing":
        return t("orders.status.preparing", "Preparing");
      case "Ready":
        return t("orders.status.ready", "Ready");
      case "ReadyForWaiter":
        return t("orders.status.readyForWaiter", "Ready for waiter");
      case "Delivered":
        return t("orders.status.delivered", "Delivered");
      case "OutForDelivery":
        return t("orders.status.outForDelivery", "Out for delivery");
      case "Completed":
        return t("orders.status.completed", "Completed");
      case "Cancelled":
        return t("orders.status.cancelled", "Cancelled");
      default:
        return status;
    }
  }

  async function setCartMeta(input: {
    cartId?: number;
    orderType?: OrderType;
    paymentMethod?: PaymentMethod;
    restaurantTableId?: number | null;
    scheduledFor?: string | null;
    pickupContactName?: string | null;
    pickupPhone?: string | null;
    pickupNote?: string | null;
    deliveryContactName?: string | null;
    deliveryPhone?: string | null;
    deliveryAddressLine1?: string | null;
    deliveryAddressLine2?: string | null;
    deliveryCity?: string | null;
    deliveryPostalCode?: string | null;
    deliveryCountry?: string | null;
    deliveryNote?: string | null;
    receiptEmail?: string | null;
    billingDetails?: CartResponse["billingDetails"] | null;
  }) {
    const nextCartId = input.cartId ?? await ensureCart();
    const current = await getWorkingCart(nextCartId);

    const orderType = input.orderType ?? current.orderType ?? OrderType.Table;
    const paymentMethod = input.paymentMethod ?? current.paymentMethod ?? PaymentMethod.AtCounter;
    const restaurantId = current.restaurantId ?? selectedRestaurantId ?? null;

    if (!restaurantId) {
      throw new Error(appI18n.t("restaurant.selectFirst", "Choose a restaurant first."));
    }

    const firstActiveTable = tables.find((table) => table.isActive)?.id ?? null;
    const restaurantTableId =
      orderType === OrderType.Table
        ? input.restaurantTableId ?? current.restaurantTableId ?? firstActiveTable
        : null;

    await apiRequest<void>(`/api/cart/${nextCartId}/meta`, {
      method: "PUT",
      ...getOwnerHeaders(),
      body: {
        orderType,
        restaurantId,
        restaurantTableId,
        paymentMethod,
        reservationId: current.reservationId ?? null,
        scheduledFor: input.scheduledFor !== undefined ? input.scheduledFor : current.scheduledFor ?? null,
        pickupContactName: input.pickupContactName !== undefined ? input.pickupContactName : current.pickupContactName ?? null,
        pickupPhone: input.pickupPhone !== undefined ? input.pickupPhone : current.pickupPhone ?? null,
        pickupNote: input.pickupNote !== undefined ? input.pickupNote : current.pickupNote ?? null,
        deliveryContactName: input.deliveryContactName !== undefined ? input.deliveryContactName : current.deliveryContactName ?? null,
        deliveryPhone: input.deliveryPhone !== undefined ? input.deliveryPhone : current.deliveryPhone ?? null,
        deliveryAddressLine1: input.deliveryAddressLine1 !== undefined ? input.deliveryAddressLine1 : current.deliveryAddressLine1 ?? null,
        deliveryAddressLine2: input.deliveryAddressLine2 !== undefined ? input.deliveryAddressLine2 : current.deliveryAddressLine2 ?? null,
        deliveryCity: input.deliveryCity !== undefined ? input.deliveryCity : current.deliveryCity ?? null,
        deliveryPostalCode: input.deliveryPostalCode !== undefined ? input.deliveryPostalCode : current.deliveryPostalCode ?? null,
        deliveryCountry: input.deliveryCountry !== undefined ? input.deliveryCountry : current.deliveryCountry ?? null,
        deliveryNote: input.deliveryNote !== undefined ? input.deliveryNote : current.deliveryNote ?? null,
        receiptEmail: input.receiptEmail !== undefined ? input.receiptEmail : current.receiptEmail ?? null,
        billingDetails: input.billingDetails !== undefined ? input.billingDetails : current.billingDetails ?? null,
      },
    });

    await refreshCart();
  }

  async function addItemToCart(menuItemId: number, customization?: { removedIngredientIds?: number[]; addedIngredientIds?: number[] }) {
    const nextCartId = await ensureCart();
    const current = await getWorkingCart(nextCartId);
    if (!selectedRestaurantId) {
      throw new Error(appI18n.t("restaurant.selectFirst", "Choose a restaurant first."));
    }

    await setCartMeta({
      cartId: nextCartId,
      orderType: current.orderType ?? OrderType.Table,
      paymentMethod: current.paymentMethod ?? PaymentMethod.AtCounter,
      restaurantTableId: current.restaurantTableId ?? tables[0]?.id ?? null,
    });

    const removedIngredientIds = (customization?.removedIngredientIds ?? []).filter((value) => value > 0);
    const addedIngredientIds = (customization?.addedIngredientIds ?? []).filter((value) => value > 0);
    const existingLine = current.items.find(
      (line) =>
        line.menuItemId === menuItemId &&
        compareNumberLists(line.removedIngredientIds, removedIngredientIds) &&
        compareNumberLists(line.addedIngredientIds, addedIngredientIds) &&
        !line.note
    );
    const nextItems = existingLine
      ? current.items.map((line) =>
          line.lineId === existingLine.lineId
            ? { ...line, quantity: line.quantity + 1 }
            : line
        )
      : [...current.items, { lineId: 0, menuItemId, quantity: 1, note: null, extraCharge: 0, removedIngredientIds, addedIngredientIds }];

    await apiRequest<void>(`/api/cart/${nextCartId}/items`, {
      method: "PUT",
      ...getOwnerHeaders(),
      body: { items: nextItems },
    });

    await loadActiveCarts();
    await refreshCart();
  }

  async function setItemQuantity(lineId: number, quantity: number) {
    if (!cartId || !cart) return;

    const nextItems = cart.items
      .map((line) => (line.lineId === lineId ? { ...line, quantity } : line))
      .filter((line) => line.quantity > 0);

    await apiRequest<void>(`/api/cart/${cartId}/items`, {
      method: "PUT",
      ...getOwnerHeaders(),
      body: { items: nextItems },
    });

    await loadActiveCarts();
    await refreshCart();
  }

  async function updateItemCustomization(
    lineId: number,
    customization: { removedIngredientIds?: number[]; addedIngredientIds?: number[] }
  ) {
    if (!cartId || !cart) return;

    const removedIngredientIds = (customization.removedIngredientIds ?? []).filter((value) => value > 0);
    const addedIngredientIds = (customization.addedIngredientIds ?? []).filter((value) => value > 0);
    const nextItems = cart.items.map((line) =>
      line.lineId === lineId
        ? {
            ...line,
            removedIngredientIds,
            addedIngredientIds,
          }
        : line
    );

    await apiRequest<void>(`/api/cart/${cartId}/items`, {
      method: "PUT",
      ...getOwnerHeaders(),
      body: { items: nextItems },
    });

    await loadActiveCarts();
    await refreshCart();
  }

  async function previewCartAction() {
    if (!cartId) {
      setPreview(null);
      return;
    }

    const nextCartId = cartId;
    const response = await apiRequest<CartPreview>(`/api/cart/${nextCartId}/preview`, {
      method: "POST",
      ...getOwnerHeaders(),
    });
    setPreview(response);
  }

  async function finalizeCart() {
    if (!selectedRestaurantId) {
      throw new Error(appI18n.t("restaurant.selectFirst", "Choose a restaurant first."));
    }

    const nextCartId = await ensureCart();
    const response = await apiRequest<FinalizeResponse>(`/api/cart/${nextCartId}/finalize`, {
      method: "POST",
      ...getOwnerHeaders(),
    });

    await setCartId(null);
    setCartIdState(null);
    setCart(null);
    setPreview(null);
    await Promise.all([
      loadActiveCarts(),
      loadMyActiveOrders(),
      loadMyOrderHistory(),
      loadNotifications(false),
    ]);
    return response;
  }

  function compareNumberLists(left: number[] | undefined, right: number[] | undefined) {
    const normalizedLeft = [...(left ?? [])].filter((value) => value > 0).sort((a, b) => a - b);
    const normalizedRight = [...(right ?? [])].filter((value) => value > 0).sort((a, b) => a - b);
    if (normalizedLeft.length !== normalizedRight.length) {
      return false;
    }

    for (let index = 0; index < normalizedLeft.length; index += 1) {
      if (normalizedLeft[index] !== normalizedRight[index]) {
        return false;
      }
    }

    return true;
  }

  async function claimGuestOrders(authToken: string) {
    if (!guestToken) return;

    await apiRequest<{ movedOrders: number }>("/api/account/claim", {
      method: "POST",
      token: authToken,
      guestToken,
    });
  }

  async function signIn(email: string, password: string) {
    const response = await apiRequest<AuthResponse>("/api/auth/login", {
      method: "POST",
      body: { email, password },
    });

    await setAuthToken(response.token);
    setTokenState(response.token);
    const signInCulture = await loadLocalization(response.token);

    if (cartId) {
      try {
        await loadCart(cartId, response.token, guestToken);
      } catch {
        await setCartId(null);
        setCartIdState(null);
        setCart(null);
      }
    }

      const drafts = await loadActiveCartsInternal(response.token, guestToken);
      if (drafts.length > 0) {
        const preferredCartId = drafts[0].cartId;
        setCartIdState(preferredCartId);
        await setCartId(preferredCartId);
        await loadCart(preferredCartId, response.token, guestToken);
        if (selectedRestaurantId) {
          await loadRestaurantContext(selectedRestaurantId, signInCulture);
        }
      }

      await Promise.all([
        loadMyActiveOrders(),
        loadMyOrderHistory(),
        loadNotifications(false),
      ]);
      await loadBillingProfileInternal(response.token);
    }

  async function signOut() {
    const nextGuestToken = createGuestToken();

    await setAuthToken(null);
    await setCartId(null);
    await setRestaurantId(null);
    await setGuestToken(nextGuestToken);
    setTokenState(null);
    setGuestTokenState(nextGuestToken);
    setCartIdState(null);
    setSelectedRestaurantIdState(null);
    setActiveCarts([]);
    setCart(null);
    setPreview(null);
    setActiveOrders([]);
    setOrderHistory([]);
    setOrderDetailsById({});
    setNotifications([]);
    setTransientNotification(null);
    setPushStatus("Checking push notification support...");
    setReservations([]);
    setBillingProfile(null);
    setAppLanguages([]);
    setCurrentCulture("pl-PL");
    appI18n.addResourceBundle("pl-PL", "translation", {}, true, true);
    await appI18n.changeLanguage("pl-PL");
    lastSeenNotificationIdRef.current = 0;
    notificationsReadyRef.current = false;
    remotePushReadyRef.current = false;
    transientNotificationQueueRef.current = [];
    if (transientNotificationTimerRef.current) {
      clearTimeout(transientNotificationTimerRef.current);
      transientNotificationTimerRef.current = null;
    }

    await loadLocalization(null);
  }

  async function register(email: string, password: string) {
    await apiRequest<{ message: string }>("/api/auth/register", {
      method: "POST",
      body: { email, password },
    });
  }

  async function confirmRegistration(email: string, code: string) {
    await apiRequest<{ message: string }>("/api/auth/confirm-registration", {
      method: "POST",
      body: { email, code },
    });
  }

  async function loadMyReservations() {
    if (!token) {
      setReservations([]);
      return;
    }

    const response = await apiRequest<Reservation[]>("/api/reservations/mine", {
      token,
    });
    setReservations(response);
  }

  async function loadBillingProfile() {
    if (!token) {
      setBillingProfile(null);
      return;
    }

    await loadBillingProfileInternal(token);
  }

  async function loadBillingProfileInternal(currentToken: string) {
    try {
      const response = await apiRequest<BillingProfile>("/api/account/billing-profile", {
        token: currentToken,
      });
      setBillingProfile(response);
    } catch (error: any) {
      const message = String(error?.message ?? "");
      if (message.includes("401")) {
        await setAuthToken(null);
        setTokenState(null);
        setBillingProfile(null);
        return;
      }

      throw error;
    }
  }

  async function saveBillingProfile(profile: BillingProfile) {
    if (!token) {
      throw new Error(appI18n.t("account.signInToSaveBilling", "Sign in to save invoice details."));
    }

    await apiRequest<void>("/api/account/billing-profile", {
      method: "PUT",
      token,
      body: profile,
    });
    setBillingProfile(profile);
  }

  async function loadLocalization(currentToken?: string | null) {
    const languagesPayload = await apiRequest<AppLanguagePayload>("/api/platform/languages");
    setAppLanguages(languagesPayload.languages);

    let nextCulture = languagesPayload.defaultCulture || languagesPayload.languages[0]?.culture || "pl-PL";
    if (currentToken) {
      try {
        const preference = await apiRequest<{ culture?: string | null }>("/api/account/preferences/culture", {
          token: currentToken,
        });
        if (preference.culture) {
          nextCulture = preference.culture;
        }
      } catch {
      }
    }

    const texts = await apiRequest<AppTextDictionary>(`/api/platform/texts?culture=${encodeURIComponent(nextCulture)}`);
    const resourceCulture = texts.culture || nextCulture;
    appI18n.addResourceBundle(resourceCulture, "translation", texts.texts || {}, true, true);
    await appI18n.changeLanguage(resourceCulture);
    setCurrentCulture(resourceCulture);
    return resourceCulture;
  }

  async function setPreferredCulture(culture: string) {
    const nextCulture = culture.trim();
    if (!nextCulture) {
      return;
    }

    if (token) {
      await apiRequest<void>("/api/account/preferences/culture", {
        method: "PUT",
        token,
        body: { culture: nextCulture },
      });
    }

    const texts = await apiRequest<AppTextDictionary>(`/api/platform/texts?culture=${encodeURIComponent(nextCulture)}`);
    const resourceCulture = texts.culture || nextCulture;
    appI18n.addResourceBundle(resourceCulture, "translation", texts.texts || {}, true, true);
    await appI18n.changeLanguage(resourceCulture);
    setCurrentCulture(resourceCulture);
    await refreshRestaurants(resourceCulture);

    if (selectedRestaurantId) {
      await loadRestaurantContext(selectedRestaurantId, resourceCulture);
    }
  }

  function t(key: string, fallback: string) {
    const translated = appI18n.t(key, { defaultValue: fallback });
    return typeof translated === "string" ? translated : fallback;
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    if (!token) throw new Error(appI18n.t("auth.signInFirst", "Sign in first."));
    await apiRequest<{ message: string }>("/api/account/change-password", {
      method: "POST",
      token,
      body: { currentPassword, newPassword },
    });
  }

  async function requestAccountDeletion() {
    if (!token) throw new Error(appI18n.t("auth.signInFirst", "Sign in first."));
    await apiRequest<{ message: string }>("/api/account/delete-request", {
      method: "POST",
      token,
    });
  }

  async function confirmAccountDeletion(code: string) {
    if (!token) throw new Error(appI18n.t("auth.signInFirst", "Sign in first."));
    await apiRequest<{ message: string }>("/api/account/delete-confirm", {
      method: "POST",
      token,
      body: { code },
    });
    await signOut();
  }

  async function loadReservationAvailability(restaurantId: number, date: string) {
    if (!token) {
      throw new Error(appI18n.t("mobile.reservations.signInRequired", "Sign in to view reservation availability."));
    }

    const encodedDate = encodeURIComponent(date);
    const response = await apiRequest<ReservationAvailability>(
      `/api/reservations/availability?restaurantId=${restaurantId}&date=${encodedDate}`,
      { token }
    );
    setReservationAvailability(response);
  }

  async function createReservation(input: { restaurantId: number; restaurantTableId?: number | null; partySize: number; startAt: string; note?: string | null }) {
    if (!token) {
      throw new Error(appI18n.t("mobile.reservations.signInRequired", "Sign in to create a reservation."));
    }

    await apiRequest<Reservation>("/api/reservations", {
      method: "POST",
      token,
      body: {
        restaurantId: input.restaurantId,
        restaurantTableId: input.restaurantTableId ?? null,
        partySize: input.partySize,
        startAt: input.startAt,
        note: input.note ?? null,
      },
    });

    await loadMyReservations();
  }

  async function cancelReservation(reservationId: number) {
    if (!token) {
      throw new Error(appI18n.t("mobile.reservations.signInRequired", "Sign in to cancel a reservation."));
    }

    await apiRequest<void>(`/api/reservations/${reservationId}/cancel`, {
      method: "PATCH",
      token,
    });

    await loadMyReservations();
  }

  const value = useMemo<SessionContextValue>(
    () => ({
      booting,
      bootError,
      token,
      guestToken,
      cartId,
      cart,
      activeCarts,
      preview,
      restaurants,
      selectedRestaurantId,
      selectedRestaurant,
      restaurantSettings,
      tables,
      categories,
      items,
      activeOrders,
      orderHistory,
      orderDetailsById,
      notifications,
      transientNotification,
      pushStatus,
      reservationAvailability,
      reservations,
      billingProfile,
      appLanguages,
      currentCulture,
      t,
      bootstrap,
      refreshRestaurants,
      selectRestaurant,
      refreshMenu,
      refreshCart,
      loadActiveCarts,
      openCart,
      deleteCart,
      setCartMeta,
        addItemToCart,
        setItemQuantity,
        updateItemCustomization,
        previewCart: previewCartAction,
      finalizeCart,
      signIn,
      signOut,
      register,
      confirmRegistration,
      loadMyActiveOrders,
      loadMyOrderHistory,
      loadOrderDetails,
      loadNotifications,
      dismissTransientNotification,
      loadReservationAvailability,
      createReservation,
      cancelReservation,
      loadMyReservations,
      loadBillingProfile,
      saveBillingProfile,
      changePassword,
      setPreferredCulture,
      requestAccountDeletion,
      confirmAccountDeletion,
    }),
    [booting, bootError, token, guestToken, cartId, cart, activeCarts, preview, restaurants, selectedRestaurantId, selectedRestaurant, restaurantSettings, tables, categories, items, activeOrders, orderHistory, orderDetailsById, notifications, transientNotification, pushStatus, reservationAvailability, reservations, billingProfile, appLanguages, currentCulture]
  );

  return <AppSessionContext.Provider value={value}>{children}</AppSessionContext.Provider>;
}

export function useAppSession() {
  const context = useContext(AppSessionContext);
  if (!context) {
    throw new Error("useAppSession must be used within AppSessionProvider");
  }

  return context;
}
