import { useState } from "react";
import { Modal, Pressable, ScrollView, Text, View } from "react-native";
import { sharedStyles, theme } from "../lib/theme";
import { appI18n } from "../lib/i18n";

export type PickerOption = {
  value: string;
  label: string;
};

export function PickerField({
  label,
  placeholder,
  value,
  options,
  onChange,
  disabled,
}: {
  label: string;
  placeholder: string;
  value: string;
  options: PickerOption[];
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const selectedLabel = options.find((option) => option.value === value)?.label ?? "";

  return (
    <View style={sharedStyles.stackMd}>
      <Text style={sharedStyles.title}>{label}</Text>
      <Pressable
        disabled={disabled}
        onPress={() => setOpen(true)}
        style={{
          borderWidth: 1,
          borderColor: theme.colors.border,
          borderRadius: 8,
          paddingHorizontal: 12,
          paddingVertical: 12,
          backgroundColor: disabled ? "#eef2f6" : theme.colors.surface,
        }}
      >
          <Text style={{ color: value ? theme.colors.ink : "#98a2b3" }}>
          {selectedLabel || placeholder}
        </Text>
      </Pressable>

      <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)}>
        <Pressable
          onPress={() => setOpen(false)}
          style={sharedStyles.modalBackdrop}
        >
          <Pressable
            onPress={() => undefined}
            style={sharedStyles.modalCardCompact}
          >
            <Text style={[sharedStyles.sectionTitle, { marginBottom: 12 }]}>{label}</Text>
            <ScrollView>
              <View style={sharedStyles.stackMd}>
                {options.map((option) => (
                  <Pressable
                    key={option.value}
                    onPress={() => {
                      onChange(option.value);
                      setOpen(false);
                    }}
                    style={{
                      borderWidth: 1,
                      borderColor: value === option.value ? theme.colors.navy : theme.colors.border,
                      borderRadius: 8,
                      paddingHorizontal: 12,
                      paddingVertical: 12,
                      backgroundColor: value === option.value ? theme.colors.navySoft : theme.colors.surface,
                    }}
                  >
                    <Text style={{ color: theme.colors.ink, fontWeight: value === option.value ? "700" : "500" }}>{option.label}</Text>
                  </Pressable>
                ))}
                {options.length === 0 ? (
                  <Text style={sharedStyles.mutedText}>{appI18n.t("ui.noOptionsAvailable", "No options available.")}</Text>
                ) : null}
              </View>
            </ScrollView>
            <Pressable onPress={() => setOpen(false)} style={{ marginTop: 12, alignSelf: "flex-end" }}>
              <Text style={{ color: theme.colors.accentPressed, fontWeight: "700" }}>{appI18n.t("common.close", "Close")}</Text>
            </Pressable>
          </Pressable>
        </Pressable>
      </Modal>
    </View>
  );
}
