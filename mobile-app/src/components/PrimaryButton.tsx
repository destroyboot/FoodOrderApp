import { Pressable, Text } from "react-native";
import { theme } from "../lib/theme";

export function PrimaryButton({
  label,
  onPress,
  disabled,
}: {
  label: string;
  onPress: () => void | Promise<void>;
  disabled?: boolean;
}) {
  return (
    <Pressable
      onPress={() => void onPress()}
      disabled={disabled}
      style={{
        backgroundColor: disabled ? "#c7cdd6" : theme.colors.navy,
        paddingVertical: 13,
        paddingHorizontal: 14,
        borderRadius: theme.radius.small,
        alignItems: "center",
      }}
    >
      <Text style={{ color: disabled ? "#667085" : "#fff", fontWeight: "700" }}>{label}</Text>
    </Pressable>
  );
}
