import { View } from "react-native";
import { theme } from "../lib/theme";

export function SectionCard({ children }: { children: React.ReactNode }) {
  return (
    <View
      style={{
        borderWidth: 1,
        borderColor: theme.colors.border,
        borderRadius: theme.radius.medium,
        padding: 16,
        backgroundColor: theme.colors.surface,
        shadowColor: "#172033",
        shadowOpacity: 0.05,
        shadowRadius: 10,
        shadowOffset: { width: 0, height: 3 },
        elevation: 1,
      }}
    >
      {children}
    </View>
  );
}
