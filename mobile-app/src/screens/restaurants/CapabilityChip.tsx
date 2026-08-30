import { StyleSheet, Text, View } from "react-native";
import { theme } from "../../lib/theme";

type Props = {
  label: string;
  kind: "table" | "pickup" | "delivery" | "reservation";
};

export function CapabilityChip({ label, kind }: Props) {
  const palette = {
    table: { background: "#e8eef6", border: "#b7c8e0", text: "#244e80" },
    pickup: { background: "#f2ebfb", border: "#d8c4ee", text: "#6e3ea3" },
    delivery: { background: theme.colors.successSoft, border: "#b7dfc4", text: theme.colors.success },
    reservation: { background: "#e8f5f5", border: "#b8dfe0", text: "#176c70" },
  }[kind];

  return (
    <View style={[styles.chip, { borderColor: palette.border, backgroundColor: palette.background }]}>
      <Text style={[styles.label, { color: palette.text }]}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  chip: {
    borderWidth: 1,
    borderRadius: theme.radius.small,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  label: {
    fontWeight: "700",
    fontSize: 12,
  },
});
