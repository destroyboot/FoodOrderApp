import { Switch, Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles, theme } from "../../lib/theme";
import type { TFunction } from "./types";

type Props = {
  enabled: boolean;
  onEnabledChange: (value: boolean) => void;
  onSave: () => void;
  t: TFunction;
};

export function NotificationPreferenceForm({ enabled, onEnabledChange, onSave, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("account.notificationSettings", "Notification Settings")}</Text>
        <View style={[sharedStyles.rowBetween, { alignItems: "center" }]}>
          <View style={sharedStyles.flexOne}>
            <Text style={sharedStyles.title}>{t("account.orderStatusEmails", "Order status emails")}</Text>
            <Text style={sharedStyles.themeMutedComfortable}>
              {t("account.orderStatusEmailsHint", "Receive an email when restaurant staff changes the status of your order.")}
            </Text>
          </View>
          <Switch
            value={enabled}
            onValueChange={onEnabledChange}
            trackColor={{ false: theme.colors.border, true: theme.colors.accentSoft }}
            thumbColor={enabled ? theme.colors.accent : "#f4f6f8"}
          />
        </View>
        <PrimaryButton label={t("common.save", "Save")} onPress={onSave} />
      </View>
    </SectionCard>
  );
}
