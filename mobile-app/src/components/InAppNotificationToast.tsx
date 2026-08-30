import { Pressable, StyleSheet, Text, View } from "react-native";
import { theme } from "../lib/theme";

type Props = {
  title: string;
  body: string;
  visible: boolean;
  onPress: () => void;
};

export function InAppNotificationToast({ title, body, visible, onPress }: Props) {
  if (!visible) {
    return null;
  }

  return (
    <View
      pointerEvents="box-none"
      style={styles.wrapper}
    >
      <Pressable
        onPress={onPress}
        style={styles.toast}
      >
        <Text style={styles.title}>
          {title}
        </Text>
        <Text style={styles.body}>{body}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    position: "absolute",
    top: 18,
    left: 16,
    right: 16,
    zIndex: 1000,
  },
  toast: {
    backgroundColor: theme.colors.navy,
    borderLeftWidth: 5,
    borderLeftColor: theme.colors.accent,
    borderRadius: theme.radius.medium,
    paddingHorizontal: 16,
    paddingVertical: 14,
    shadowColor: theme.colors.navy,
    shadowOpacity: 0.2,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 6 },
    elevation: 8,
  },
  title: {
    color: "#f9fafb",
    fontSize: 16,
    fontWeight: "700",
    marginBottom: 4,
  },
  body: {
    color: "#e5e7eb",
    lineHeight: 20,
  },
});
