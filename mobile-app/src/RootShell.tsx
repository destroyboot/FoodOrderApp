import { useEffect, useMemo, useState } from "react";
import { ActivityIndicator, Modal, Platform, Pressable, ScrollView, Text, View } from "react-native";
import * as Notifications from "expo-notifications";
import { InAppNotificationToast } from "./components/InAppNotificationToast";
import { PrimaryButton } from "./components/PrimaryButton";
import { TabButton } from "./components/TabButton";
import { useAppSession } from "./context/AppSessionContext";
import { getDisplayUsername } from "./lib/authIdentity";
import { API_BASE_URL } from "./lib/config";
import { sharedStyles, theme } from "./lib/theme";
import { AccountScreen } from "./screens/AccountScreen";
import { CartScreen } from "./screens/CartScreen";
import { MenuScreen } from "./screens/MenuScreen";
import { MyOrdersScreen } from "./screens/MyOrdersScreen";
import { ReservationsScreen } from "./screens/ReservationsScreen";
import { RestaurantsScreen } from "./screens/RestaurantsScreen";

type TabKey = "restaurants" | "menu" | "cart" | "reservations" | "orders" | "account";

export function RootShell() {
  const {
    booting,
    bootError,
    bootstrap,
    token,
    selectedRestaurantId,
    selectedRestaurant,
    activeCarts,
    transientNotification,
    dismissTransientNotification,
    appLanguages,
    currentCulture,
    setPreferredCulture,
    t,
  } = useAppSession();
  const [tab, setTab] = useState<TabKey>("menu");
  const [hadToken, setHadToken] = useState(!!token);
  const [restaurantPickerSignal, setRestaurantPickerSignal] = useState(0);
  const [languagePickerOpen, setLanguagePickerOpen] = useState(false);
  const [message, setMessage] = useState<{ title: string; body: string } | null>(null);
  const [confirmation, setConfirmation] = useState<{ title: string; body: string; resolve: (result: boolean) => void } | null>(null);

  const totalCartQuantity = useMemo(
    () => activeCarts.reduce((sum, cart) => sum + cart.totalQuantity, 0),
    [activeCarts]
  );
  const username = useMemo(() => getDisplayUsername(token), [token]);
  const displayRestaurantName = useMemo(() => {
    if (selectedRestaurant?.name) {
      return selectedRestaurant.name;
    }

    if (selectedRestaurantId) {
      const fromActiveCart = activeCarts.find((cart) => cart.restaurantId === selectedRestaurantId)?.restaurantName;
      if (fromActiveCart) {
        return fromActiveCart;
      }
    }

    return null;
  }, [activeCarts, selectedRestaurant?.name, selectedRestaurantId]);

  const displayRestaurantSubtitle = useMemo(() => {
    const cuisine = selectedRestaurant?.cuisineTypeDisplay || selectedRestaurant?.cuisineType;
    if (cuisine) {
      return `${t("mobile.restaurants.cuisine", "Cuisine")}: ${cuisine}${selectedRestaurant?.address ? ` - ${selectedRestaurant.address}` : ""}`;
    }

    if (selectedRestaurant?.address) {
      return selectedRestaurant.address;
    }

    return t("restaurant.openDetails", "Open the restaurant page to view details or change it.");
  }, [selectedRestaurant?.address, selectedRestaurant?.cuisineType, selectedRestaurant?.cuisineTypeDisplay, t]);

  useEffect(() => {
    if (!hadToken && token) {
      setTab("menu");
    }
    setHadToken(!!token);
  }, [hadToken, token]);

  useEffect(() => {
    if (Platform.OS === "web") {
      return;
    }

    const lastResponse = Notifications.getLastNotificationResponse();
    const initialUrl = lastResponse?.notification.request.content.data?.url;
    if (initialUrl === "/orders") setTab("orders");
    if (initialUrl === "/reservations") setTab("reservations");

    const subscription = Notifications.addNotificationResponseReceivedListener((response) => {
      const url = response.notification.request.content.data?.url;
      if (url === "/orders") setTab("orders");
      if (url === "/reservations") setTab("reservations");
    });

    return () => {
      subscription.remove();
    };
  }, []);

  useEffect(() => {
    if (Platform.OS !== "web" || typeof window === "undefined") return;
    const handleMessage = (event: Event) => {
      const detail = (event as CustomEvent<{ title?: string; message?: string }>).detail;
      setMessage({ title: detail?.title ?? t("common.message", "Message"), body: detail?.message ?? "" });
    };
    const handleConfirmation = (event: Event) => {
      const detail = (event as CustomEvent<{ title?: string; message?: string; resolve?: (result: boolean) => void }>).detail;
      if (detail?.resolve) {
        setConfirmation({ title: detail.title ?? t("common.confirm", "Confirm"), body: detail.message ?? "", resolve: detail.resolve });
      }
    };
    window.addEventListener("foodorderapp:message", handleMessage);
    window.addEventListener("foodorderapp:confirm", handleConfirmation);
    return () => {
      window.removeEventListener("foodorderapp:message", handleMessage);
      window.removeEventListener("foodorderapp:confirm", handleConfirmation);
    };
  }, [t]);

  if (booting) {
    return (
      <View style={sharedStyles.centerFill}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (bootError) {
    return (
      <View style={[sharedStyles.flexOne, { padding: 24, justifyContent: "center", gap: 12 }]}>
        <Text style={sharedStyles.modalTitle}>{t("app.bootFailed", "App could not start")}</Text>
        <Text style={sharedStyles.bodyMuted}>{bootError}</Text>
        <Text style={sharedStyles.mutedText}>{t("app.apiBaseUrl", "API base URL")}: {API_BASE_URL}</Text>
        <PrimaryButton label={t("common.tryAgain", "Try again")} onPress={bootstrap} />
      </View>
    );
  }

  return (
    <View style={[sharedStyles.flexOne, sharedStyles.screen]}>
      <InAppNotificationToast
        visible={!!transientNotification}
        title={transientNotification?.title ?? ""}
        body={transientNotification?.body ?? ""}
        onPress={() => {
          dismissTransientNotification();
          setTab("orders");
        }}
      />

      <View style={{ paddingHorizontal: 16, paddingTop: 12, paddingBottom: 12, borderBottomWidth: 1, borderBottomColor: "#26344d", backgroundColor: theme.colors.navy }}>
        <View style={{ width: "100%", flexDirection: "row", alignItems: "stretch", gap: 12 }}>
          <Pressable
            onPress={() => {
              setRestaurantPickerSignal((current) => current + 1);
              setTab("restaurants");
            }}
            style={{ flex: 1, minWidth: 0, borderWidth: 1, borderColor: "#3a4a65", borderRadius: 8, paddingHorizontal: 12, paddingVertical: 10, backgroundColor: "#202c42" }}
          >
            <Text style={{ fontSize: 12, fontWeight: "700", color: "#b9c5d6", textTransform: "uppercase", marginBottom: 5 }}>{t("restaurant.selected", "Selected restaurant")}</Text>
            <Text style={{ fontWeight: "700", color: "#fff" }}>
              {displayRestaurantName ?? t("restaurant.choose", "Choose a restaurant")}
            </Text>
            <Text style={{ color: "#c4cfdd", marginTop: 3, fontSize: 13 }}>
              {displayRestaurantSubtitle}
            </Text>
          </Pressable>
          {appLanguages.length > 0 ? (
            <Pressable
              onPress={() => {
                setLanguagePickerOpen(true);
              }}
              accessibilityLabel={t("common.language", "Language")}
              style={{ width: 68, borderWidth: 1, borderColor: "#3a4a65", backgroundColor: "#202c42", borderRadius: 8, paddingHorizontal: 8, alignItems: "center", justifyContent: "center" }}
            >
              <Text style={{ fontSize: 18 }}>🌐</Text>
              <Text style={{ color: "#fff", fontWeight: "700", marginTop: 3 }}>{currentCulture.slice(0, 2).toUpperCase()}</Text>
            </Pressable>
          ) : null}
        </View>
      </View>

      <Modal visible={languagePickerOpen} transparent animationType="fade" onRequestClose={() => setLanguagePickerOpen(false)}>
        <Pressable onPress={() => setLanguagePickerOpen(false)} style={sharedStyles.modalBackdrop}>
          <Pressable onPress={() => undefined} style={sharedStyles.modalCardCompact}>
            <Text style={[sharedStyles.modalTitle, { marginBottom: 12 }]}>{t("common.language", "Language")}</Text>
            <ScrollView contentContainerStyle={sharedStyles.stackMd}>
              {appLanguages.map((language) => (
                <Pressable
                  key={language.culture}
                  onPress={() => {
                    void setPreferredCulture(language.culture);
                    setLanguagePickerOpen(false);
                  }}
                  style={{ borderWidth: 1, borderColor: currentCulture === language.culture ? theme.colors.accent : theme.colors.border, borderRadius: 8, paddingHorizontal: 12, paddingVertical: 12, backgroundColor: currentCulture === language.culture ? theme.colors.accentSoft : theme.colors.surface }}
                >
                  <Text style={{ fontWeight: "600" }}>{language.nativeName}</Text>
                </Pressable>
              ))}
            </ScrollView>
          </Pressable>
        </Pressable>
      </Modal>

      <Modal visible={!!message} transparent animationType="fade" onRequestClose={() => setMessage(null)}>
        <Pressable onPress={() => setMessage(null)} style={sharedStyles.modalBackdrop}>
          <Pressable onPress={() => undefined} style={[sharedStyles.modalCard, { maxWidth: 480, width: "100%", alignSelf: "center" }]}>
            <Text style={sharedStyles.modalTitle}>{message?.title}</Text>
            <Text style={sharedStyles.modalText}>{message?.body}</Text>
            <Pressable onPress={() => setMessage(null)} style={{ alignSelf: "flex-end", paddingVertical: 8, paddingHorizontal: 12 }}><Text style={{ color: theme.colors.accentPressed, fontWeight: "700" }}>{t("common.close", "Close")}</Text></Pressable>
          </Pressable>
        </Pressable>
      </Modal>

      <Modal visible={!!confirmation} transparent animationType="fade" onRequestClose={() => {
        confirmation?.resolve(false);
        setConfirmation(null);
      }}>
        <Pressable onPress={() => { confirmation?.resolve(false); setConfirmation(null); }} style={sharedStyles.modalBackdrop}>
          <Pressable onPress={() => undefined} style={[sharedStyles.modalCard, { gap: 14, maxWidth: 480, width: "100%", alignSelf: "center" }]}>
            <Text style={sharedStyles.modalTitle}>{confirmation?.title}</Text>
            <Text style={sharedStyles.modalText}>{confirmation?.body}</Text>
            <View style={sharedStyles.modalActions}>
              <Pressable onPress={() => { confirmation?.resolve(false); setConfirmation(null); }} style={{ borderWidth: 1, borderColor: theme.colors.border, borderRadius: 8, paddingHorizontal: 15, paddingVertical: 10 }}><Text style={{ color: theme.colors.ink, fontWeight: "700" }}>{t("common.cancel", "Cancel")}</Text></Pressable>
              <Pressable onPress={() => { confirmation?.resolve(true); setConfirmation(null); }} style={{ backgroundColor: theme.colors.navy, borderRadius: 8, paddingHorizontal: 15, paddingVertical: 10 }}><Text style={{ color: "#fff", fontWeight: "700" }}>{t("common.confirm", "Confirm")}</Text></Pressable>
            </View>
          </Pressable>
        </Pressable>
      </Modal>

      <View style={{ flex: 1 }}>
        {tab === "restaurants" ? <RestaurantsScreen onOpenMenu={() => setTab("menu")} forcePickerSignal={restaurantPickerSignal} /> : null}
        {tab === "menu" ? <MenuScreen /> : null}
        {tab === "cart" ? <CartScreen onOrderPlaced={() => setTab("orders")} /> : null}
        {tab === "reservations" ? <ReservationsScreen /> : null}
        {tab === "orders" ? <MyOrdersScreen /> : null}
        {tab === "account" ? <AccountScreen onSignedIn={() => setTab("menu")} onConfirmedRegistration={() => setTab("account")} /> : null}
      </View>

      <View style={{ flexDirection: "row", flexWrap: "wrap" }}>
        <TabButton active={tab === "restaurants"} label={t("nav.restaurants", "Restaurants")} icon="🍽" onPress={() => setTab("restaurants")} />
        <TabButton active={tab === "menu"} label={t("nav.menu", "Menu")} icon="📋" onPress={() => setTab("menu")} />
        <TabButton active={tab === "cart"} label={t("nav.cart", "Cart")} icon="🛒" leftBadge={activeCarts.length} rightBadge={totalCartQuantity} onPress={() => setTab("cart")} />
        <TabButton active={tab === "reservations"} label={t("nav.reservations", "Reservations")} icon="📅" onPress={() => setTab("reservations")} />
        <TabButton active={tab === "orders"} label={t("nav.orders", "My Orders")} icon="🧾" onPress={() => setTab("orders")} />
        <TabButton active={tab === "account"} label={token ? username : t("nav.account", "Account")} icon="👤" onPress={() => setTab("account")} />
      </View>
    </View>
  );
}
