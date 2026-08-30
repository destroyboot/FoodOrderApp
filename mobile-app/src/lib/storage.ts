import AsyncStorage from "@react-native-async-storage/async-storage";
import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const GUEST_TOKEN_KEY = "foodapp_guest_token";
const CART_ID_KEY = "foodapp_cart_id";
const RESTAURANT_ID_KEY = "foodapp_restaurant_id";
const AUTH_TOKEN_KEY = "foodapp_auth_token";

function canUseSecureStore() {
  return Platform.OS !== "web" && typeof SecureStore.getItemAsync === "function";
}

export async function getGuestToken() {
  return AsyncStorage.getItem(GUEST_TOKEN_KEY);
}

export async function setGuestToken(value: string) {
  await AsyncStorage.setItem(GUEST_TOKEN_KEY, value);
}

export async function getCartId() {
  const raw = await AsyncStorage.getItem(CART_ID_KEY);
  return raw ? Number(raw) : null;
}

export async function setCartId(value: number | null) {
  if (value == null) {
    await AsyncStorage.removeItem(CART_ID_KEY);
    return;
  }

  await AsyncStorage.setItem(CART_ID_KEY, String(value));
}

export async function getRestaurantId() {
  const raw = await AsyncStorage.getItem(RESTAURANT_ID_KEY);
  return raw ? Number(raw) : null;
}

export async function setRestaurantId(value: number | null) {
  if (value == null) {
    await AsyncStorage.removeItem(RESTAURANT_ID_KEY);
    return;
  }

  await AsyncStorage.setItem(RESTAURANT_ID_KEY, String(value));
}

export async function getAuthToken() {
  if (!canUseSecureStore()) {
    return AsyncStorage.getItem(AUTH_TOKEN_KEY);
  }

  try {
    return await SecureStore.getItemAsync(AUTH_TOKEN_KEY);
  } catch {
    return AsyncStorage.getItem(AUTH_TOKEN_KEY);
  }
}

export async function setAuthToken(value: string | null) {
  if (!canUseSecureStore()) {
    if (!value) {
      await AsyncStorage.removeItem(AUTH_TOKEN_KEY);
      return;
    }

    await AsyncStorage.setItem(AUTH_TOKEN_KEY, value);
    return;
  }

  if (!value) {
    try {
      await SecureStore.deleteItemAsync(AUTH_TOKEN_KEY);
    } catch {
      await AsyncStorage.removeItem(AUTH_TOKEN_KEY);
    }
    return;
  }

  try {
    await SecureStore.setItemAsync(AUTH_TOKEN_KEY, value);
  } catch {
    await AsyncStorage.setItem(AUTH_TOKEN_KEY, value);
  }
}
