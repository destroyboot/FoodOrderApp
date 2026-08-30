import { Text, TextInput, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { GuestMode, TFunction } from "./types";

type Props = {
  mode: GuestMode;
  email: string;
  password: string;
  confirmCode: string;
  onEmailChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onConfirmCodeChange: (value: string) => void;
  onModeChange: (mode: GuestMode) => void;
  onLogin: () => void;
  onRegister: () => void;
  onConfirmRegistration: () => void;
  t: TFunction;
};

export function GuestAccountForms({
  mode,
  email,
  password,
  confirmCode,
  onEmailChange,
  onPasswordChange,
  onConfirmCodeChange,
  onModeChange,
  onLogin,
  onRegister,
  onConfirmRegistration,
  t,
}: Props) {
  if (mode === "login") {
    return (
      <SectionCard>
        <View style={sharedStyles.stackMd}>
          <Text style={sharedStyles.mutedText}>
            {t("account.guestInfo", "Order on-site as a guest, or sign in when you want reservations, delivery, and account-based history.")}
          </Text>
          <TextInput placeholder={t("common.email", "Email")} value={email} onChangeText={onEmailChange} autoCapitalize="none" style={inputStyle} />
          <TextInput placeholder={t("common.password", "Password")} value={password} onChangeText={onPasswordChange} secureTextEntry style={inputStyle} />
          <PrimaryButton label={t("auth.login", "Log in")} onPress={onLogin} />
          <PrimaryButton label={t("auth.createAccount", "Create account")} onPress={() => onModeChange("register")} />
        </View>
      </SectionCard>
    );
  }

  if (mode === "register") {
    return (
      <SectionCard>
        <View style={sharedStyles.stackMd}>
          <TextInput placeholder={t("common.email", "Email")} value={email} onChangeText={onEmailChange} autoCapitalize="none" style={inputStyle} />
          <TextInput placeholder={t("common.password", "Password")} value={password} onChangeText={onPasswordChange} secureTextEntry style={inputStyle} />
          <Text style={sharedStyles.mutedTextComfortable}>{t("auth.passwordRequirementsHint", "Password must have at least 8 characters, an uppercase letter, a digit, and a special character.")}</Text>
          <PrimaryButton label={t("auth.register", "Register")} onPress={onRegister} />
          <PrimaryButton label={t("auth.backToLogin", "Back to login")} onPress={() => onModeChange("login")} />
        </View>
      </SectionCard>
    );
  }

  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <Text>{t("auth.enterConfirmationCode", "Enter the confirmation code that was sent to your email address.")}</Text>
        <TextInput placeholder={t("common.email", "Email")} value={email} onChangeText={onEmailChange} autoCapitalize="none" style={inputStyle} />
        <TextInput placeholder={t("common.confirmationCode", "Confirmation code")} value={confirmCode} onChangeText={onConfirmCodeChange} style={inputStyle} />
        <PrimaryButton label={t("auth.confirmRegistration", "Confirm registration")} onPress={onConfirmRegistration} />
        <PrimaryButton label={t("auth.backToLogin", "Back to login")} onPress={() => onModeChange("login")} />
      </View>
    </SectionCard>
  );
}
