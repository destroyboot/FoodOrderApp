import { Pressable, Text, View } from "react-native";
import { theme } from "../lib/theme";

export function TabButton({
  active,
  label,
  onPress,
  icon,
  leftBadge,
  rightBadge,
}: {
  active: boolean;
  label: string;
  onPress: () => void;
  icon?: string;
  leftBadge?: number;
  rightBadge?: number;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={{
        width: "33.3333%",
        paddingVertical: 10,
        borderTopWidth: 1,
        borderRightWidth: 1,
        borderColor: theme.colors.border,
        backgroundColor: active ? theme.colors.navySoft : theme.colors.surface,
        alignItems: "center",
        justifyContent: "center",
        gap: 2,
        position: "relative",
      }}
    >
      {icon ? <Text style={{ fontSize: 18 }}>{icon}</Text> : null}
      <Text style={{ fontSize: 12, fontWeight: active ? "700" : "600", color: active ? theme.colors.navy : theme.colors.ink }}>{label}</Text>
      {typeof leftBadge === "number" && leftBadge > 0 ? (
        <View
          style={{
            position: "absolute",
            bottom: 6,
            left: 10,
            minWidth: 18,
            height: 18,
            borderRadius: 9,
            paddingHorizontal: 4,
            alignItems: "center",
            justifyContent: "center",
            backgroundColor: theme.colors.info,
          }}
        >
          <Text style={{ color: "#fff", fontSize: 11, fontWeight: "700" }}>{leftBadge}</Text>
        </View>
      ) : null}
      {typeof rightBadge === "number" && rightBadge > 0 ? (
        <View
          style={{
            position: "absolute",
            bottom: 6,
            right: 10,
            minWidth: 18,
            height: 18,
            borderRadius: 9,
            paddingHorizontal: 4,
            alignItems: "center",
            justifyContent: "center",
            backgroundColor: theme.colors.success,
          }}
        >
          <Text style={{ color: "#fff", fontSize: 11, fontWeight: "700" }}>{rightBadge}</Text>
        </View>
      ) : null}
    </Pressable>
  );
}
