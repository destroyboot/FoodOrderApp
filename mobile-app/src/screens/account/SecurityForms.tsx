import { Text, TextInput, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { TFunction } from "./types";

type PasswordProps = {
  currentPassword: string;
  newPassword: string;
  onCurrentPasswordChange: (value: string) => void;
  onNewPasswordChange: (value: string) => void;
  onSubmit: () => void;
  t: TFunction;
};

export function PasswordChangeForm({
  currentPassword,
  newPassword,
  onCurrentPasswordChange,
  onNewPasswordChange,
  onSubmit,
  t,
}: PasswordProps) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <TextInput placeholder={t("account.currentPassword", "Current password")} value={currentPassword} onChangeText={onCurrentPasswordChange} secureTextEntry style={inputStyle} />
        <TextInput placeholder={t("account.newPassword", "New password")} value={newPassword} onChangeText={onNewPasswordChange} secureTextEntry style={inputStyle} />
        <PrimaryButton label={t("account.changePasswordAction", "Change password")} onPress={onSubmit} />
      </View>
    </SectionCard>
  );
}

type RemovalProps = {
  confirmationCode: string;
  onConfirmationCodeChange: (value: string) => void;
  onRequestDeletion: () => void;
  onConfirmDeletion: () => void;
  t: TFunction;
};

export function AccountRemovalForm({
  confirmationCode,
  onConfirmationCodeChange,
  onRequestDeletion,
  onConfirmDeletion,
  t,
}: RemovalProps) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <Text style={sharedStyles.mutedText}>{t("account.removeAccountHint", "Request a confirmation email first, then enter the code here to remove the account.")}</Text>
        <PrimaryButton label={t("account.sendConfirmationEmail", "Send confirmation email")} onPress={onRequestDeletion} />
        <TextInput placeholder={t("common.confirmationCode", "Confirmation code")} value={confirmationCode} onChangeText={onConfirmationCodeChange} style={inputStyle} />
        <PrimaryButton label={t("account.removeAccount", "Remove account")} onPress={onConfirmDeletion} />
      </View>
    </SectionCard>
  );
}
