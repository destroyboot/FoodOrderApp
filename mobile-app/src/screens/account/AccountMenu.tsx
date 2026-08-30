import { View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import type { SignedInMode, TFunction } from "./types";

type Props = {
  onModeChange: (mode: SignedInMode) => void;
  onSignOut: () => void;
  t: TFunction;
};

export function AccountMenu({ onModeChange, onSignOut, t }: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <PrimaryButton label={t("account.invoiceDetails", "Invoice Details")} onPress={() => onModeChange("invoice")} />
        <PrimaryButton label={t("account.deliveryDetails", "Delivery Details")} onPress={() => onModeChange("delivery")} />
        <PrimaryButton label={t("account.passwordChange", "Password Change")} onPress={() => onModeChange("password")} />
        <PrimaryButton label={t("account.accountRemoval", "Account Removal")} onPress={() => onModeChange("remove")} />
        <PrimaryButton label={t("auth.signOut", "Sign out")} onPress={onSignOut} />
      </View>
    </SectionCard>
  );
}
