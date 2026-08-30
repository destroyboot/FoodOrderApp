import Constants from "expo-constants";
import * as Device from "expo-device";
import * as Notifications from "expo-notifications";
import { Platform } from "react-native";

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

export type PushRegistrationResult =
  | { ok: true; expoPushToken: string }
  | { ok: false; reason: string };

function getProjectId() {
  const expoConfigProjectId = (Constants.expoConfig?.extra as any)?.eas?.projectId;
  const easConfigProjectId = (Constants as any)?.easConfig?.projectId;
  const projectId = expoConfigProjectId || easConfigProjectId;
  return typeof projectId === "string" && projectId.trim().length > 0 ? projectId.trim() : null;
}

export async function registerForPushNotificationsAsync(): Promise<PushRegistrationResult> {
  if (Platform.OS === "web") {
    return { ok: false, reason: "Remote push notifications are not supported in the web build." };
  }

  if (Platform.OS === "android") {
    await Notifications.setNotificationChannelAsync("order-status", {
      name: "Order status updates",
      importance: Notifications.AndroidImportance.MAX,
      vibrationPattern: [0, 250, 250, 250],
      lightColor: "#111827",
    });
  }

  if (Constants.executionEnvironment === "storeClient" && Platform.OS === "android") {
    return { ok: false, reason: "Android remote push requires a development build or production build, not Expo Go." };
  }

  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;
  if (existingStatus !== "granted") {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  if (finalStatus !== "granted") {
    return { ok: false, reason: "Notification permission was not granted." };
  }

  const projectId = getProjectId();
  if (!projectId) {
    return { ok: false, reason: "Missing Expo projectId. Set extra.eas.projectId before testing remote push." };
  }

  try {
    const token = await Notifications.getExpoPushTokenAsync({ projectId });
    return { ok: true, expoPushToken: token.data };
  } catch (error: any) {
    return { ok: false, reason: error?.message || "Could not register for Expo push notifications." };
  }
}

export function getDeviceLabel() {
  return Device.modelName || `${Platform.OS} device`;
}
