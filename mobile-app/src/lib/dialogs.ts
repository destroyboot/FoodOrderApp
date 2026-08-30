import { Alert, Platform } from "react-native";
import { appI18n } from "./i18n";

export function showMessage(title: string, message: string) {
  if (Platform.OS === "web" && typeof window !== "undefined") {
    window.dispatchEvent(new CustomEvent("foodorderapp:message", { detail: { title, message } }));
    return;
  }

  Alert.alert(title, message);
}

export function confirmMessage(title: string, message: string) {
  if (Platform.OS === "web" && typeof window !== "undefined") {
    return new Promise<boolean>((resolve) => {
      window.dispatchEvent(new CustomEvent("foodorderapp:confirm", { detail: { title, message, resolve } }));
    });
  }

  return new Promise<boolean>((resolve) => {
    Alert.alert(title, message, [
      { text: appI18n.t("common.no", "No"), style: "cancel", onPress: () => resolve(false) },
      { text: appI18n.t("common.yes", "Yes"), style: "destructive", onPress: () => resolve(true) },
    ]);
  });
}
