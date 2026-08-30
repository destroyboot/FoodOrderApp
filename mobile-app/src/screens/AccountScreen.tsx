import { useEffect, useState } from "react";
import { ScrollView, Text } from "react-native";
import { PrimaryButton } from "../components/PrimaryButton";
import { useAppSession } from "../context/AppSessionContext";
import { sharedStyles } from "../lib/theme";
import { AccountMenu } from "./account/AccountMenu";
import { DeliveryProfileForm } from "./account/DeliveryProfileForm";
import { GuestAccountForms } from "./account/GuestAccountForms";
import { InvoiceProfileForm } from "./account/InvoiceProfileForm";
import { AccountRemovalForm, PasswordChangeForm } from "./account/SecurityForms";
import type { GuestMode, SignedInMode } from "./account/types";

export function AccountScreen({
  onSignedIn,
  onConfirmedRegistration,
}: {
  onSignedIn?: () => void;
  onConfirmedRegistration?: () => void;
}) {
  const {
    token,
    billingProfile,
    signIn,
    signOut,
    register,
    confirmRegistration,
    saveBillingProfile,
    changePassword,
    requestAccountDeletion,
    confirmAccountDeletion,
    t,
  } = useAppSession();
  const [guestMode, setGuestMode] = useState<GuestMode>("login");
  const [signedInMode, setSignedInMode] = useState<SignedInMode>("menu");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmCode, setConfirmCode] = useState("");
  const [info, setInfo] = useState<string | null>(null);
  const [profileCustomerType, setProfileCustomerType] = useState<"person" | "company">(
    (billingProfile?.customerType ?? 0) === 1 ? "company" : "person"
  );
  const [profileReceiptEmail, setProfileReceiptEmail] = useState(billingProfile?.receiptEmail ?? "");
  const [profilePersonName, setProfilePersonName] = useState(billingProfile?.personName ?? "");
  const [profileCompanyName, setProfileCompanyName] = useState(billingProfile?.companyName ?? "");
  const [profileTaxId, setProfileTaxId] = useState(billingProfile?.taxId ?? "");
  const [profileAddressLine1, setProfileAddressLine1] = useState(billingProfile?.billingAddressLine1 ?? "");
  const [profileAddressLine2, setProfileAddressLine2] = useState(billingProfile?.billingAddressLine2 ?? "");
  const [profileCity, setProfileCity] = useState(billingProfile?.billingCity ?? "");
  const [profilePostalCode, setProfilePostalCode] = useState(billingProfile?.billingPostalCode ?? "");
  const [profileCountry, setProfileCountry] = useState(billingProfile?.billingCountry ?? "");
  const [profileDeliveryContactName, setProfileDeliveryContactName] = useState(billingProfile?.deliveryContactName ?? "");
  const [profileDeliveryPhone, setProfileDeliveryPhone] = useState(billingProfile?.deliveryPhone ?? "");
  const [profileDeliveryAddressLine1, setProfileDeliveryAddressLine1] = useState(billingProfile?.deliveryAddressLine1 ?? "");
  const [profileDeliveryAddressLine2, setProfileDeliveryAddressLine2] = useState(billingProfile?.deliveryAddressLine2 ?? "");
  const [profileDeliveryCity, setProfileDeliveryCity] = useState(billingProfile?.deliveryCity ?? "");
  const [profileDeliveryPostalCode, setProfileDeliveryPostalCode] = useState(billingProfile?.deliveryPostalCode ?? "");
  const [profileDeliveryCountry, setProfileDeliveryCountry] = useState(billingProfile?.deliveryCountry ?? "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmDeleteCode, setConfirmDeleteCode] = useState("");

  useEffect(() => {
    setProfileCustomerType((billingProfile?.customerType ?? 0) === 1 ? "company" : "person");
    setProfileReceiptEmail(billingProfile?.receiptEmail ?? "");
    setProfilePersonName(billingProfile?.personName ?? "");
    setProfileCompanyName(billingProfile?.companyName ?? "");
    setProfileTaxId(billingProfile?.taxId ?? "");
    setProfileAddressLine1(billingProfile?.billingAddressLine1 ?? "");
    setProfileAddressLine2(billingProfile?.billingAddressLine2 ?? "");
    setProfileCity(billingProfile?.billingCity ?? "");
    setProfilePostalCode(billingProfile?.billingPostalCode ?? "");
    setProfileCountry(billingProfile?.billingCountry ?? "");
    setProfileDeliveryContactName(billingProfile?.deliveryContactName ?? "");
    setProfileDeliveryPhone(billingProfile?.deliveryPhone ?? "");
    setProfileDeliveryAddressLine1(billingProfile?.deliveryAddressLine1 ?? "");
    setProfileDeliveryAddressLine2(billingProfile?.deliveryAddressLine2 ?? "");
    setProfileDeliveryCity(billingProfile?.deliveryCity ?? "");
    setProfileDeliveryPostalCode(billingProfile?.deliveryPostalCode ?? "");
    setProfileDeliveryCountry(billingProfile?.deliveryCountry ?? "");
  }, [billingProfile]);

  async function run(action: () => Promise<void>, successMessage: string) {
    try {
      setInfo(null);
      await action();
      setInfo(successMessage);
    } catch (error: any) {
      setInfo(formatAccountError(error?.message));
    }
  }

  function formatAccountError(message?: string) {
    const value = message?.trim() || "";
    if (/incorrect password/i.test(value)) return t("account.incorrectPassword", "The current password is incorrect.");
    if (/current password is required/i.test(value)) return t("validation.currentPasswordRequired", "Current password is required.");
    if (/new password is required/i.test(value)) return t("validation.newPasswordRequired", "New password is required.");
    if (/passwords must|password.*uppercase|password.*digit|password.*non alphanumeric/i.test(value)) {
      return t("account.passwordRequirementsError", "The new password does not meet the password requirements.");
    }
    if (/confirmation code/i.test(value)) return t("account.invalidConfirmationCode", "The confirmation code is invalid or expired.");
    if (/user not found/i.test(value)) return t("account.accountUnavailable", "This account is no longer available.");
    return value || t("common.genericError", "Something went wrong");
  }

  function validateRegistration() {
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      return t("validation.emailInvalid", "Enter a valid email address.");
    }

    const missing: string[] = [];
    if (password.length < 8) missing.push(t("validation.passwordMinLength", "at least 8 characters"));
    if (!/[A-Z]/.test(password)) missing.push(t("validation.passwordUppercase", "one uppercase letter"));
    if (!/\d/.test(password)) missing.push(t("validation.passwordDigit", "one digit"));
    if (!/[^A-Za-z0-9]/.test(password)) missing.push(t("validation.passwordSpecial", "one special character"));
    return missing.length > 0
      ? `${t("validation.passwordRequirements", "Password must contain")}: ${missing.join(", ")}.`
      : null;
  }

  if (!token) {
    return (
      <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
        <Text style={sharedStyles.pageTitle}>
          {guestMode === "login" ? t("nav.account", "Account") : guestMode === "register" ? t("auth.createAccount", "Create account") : t("auth.confirmRegistration", "Confirm registration")}
        </Text>
        {info ? <Text style={sharedStyles.infoText}>{info}</Text> : null}

        <GuestAccountForms
          mode={guestMode}
          email={email}
          password={password}
          confirmCode={confirmCode}
          onEmailChange={setEmail}
          onPasswordChange={setPassword}
          onConfirmCodeChange={setConfirmCode}
          onModeChange={setGuestMode}
          onLogin={() =>
            void run(async () => {
              await signIn(email, password);
              onSignedIn?.();
            }, t("auth.signedIn", "Signed in."))
          }
          onRegister={() =>
            void run(async () => {
              const validationMessage = validateRegistration();
              if (validationMessage) {
                throw new Error(validationMessage);
              }
              await register(email, password);
              setGuestMode("confirm");
            }, t("auth.registrationCodeSent", "Confirmation code sent to your email."))
          }
          onConfirmRegistration={() =>
            void run(async () => {
              await confirmRegistration(email, confirmCode);
              setGuestMode("login");
              onConfirmedRegistration?.();
            }, t("auth.accountConfirmed", "Account confirmed. You can log in now."))
          }
          t={t}
        />
      </ScrollView>
    );
  }

  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <Text style={sharedStyles.pageTitle}>{t("nav.account", "Account")}</Text>
      {info ? <Text style={sharedStyles.infoText}>{info}</Text> : null}

      {signedInMode !== "menu" ? (
        <PrimaryButton label={t("common.back", "Back")} onPress={() => setSignedInMode("menu")} />
      ) : null}

      {signedInMode === "menu" ? (
        <AccountMenu
          onModeChange={setSignedInMode}
          onSignOut={() => void run(signOut, t("auth.signedOut", "Signed out."))}
          t={t}
        />
      ) : null}

      {signedInMode === "invoice" ? (
        <InvoiceProfileForm
          customerType={profileCustomerType}
          receiptEmail={profileReceiptEmail}
          personName={profilePersonName}
          companyName={profileCompanyName}
          taxId={profileTaxId}
          addressLine1={profileAddressLine1}
          addressLine2={profileAddressLine2}
          city={profileCity}
          postalCode={profilePostalCode}
          country={profileCountry}
          deliveryContactName={profileDeliveryContactName}
          deliveryPhone={profileDeliveryPhone}
          deliveryAddressLine1={profileDeliveryAddressLine1}
          deliveryAddressLine2={profileDeliveryAddressLine2}
          deliveryCity={profileDeliveryCity}
          deliveryPostalCode={profileDeliveryPostalCode}
          deliveryCountry={profileDeliveryCountry}
          onCustomerTypeChange={setProfileCustomerType}
          onReceiptEmailChange={setProfileReceiptEmail}
          onPersonNameChange={setProfilePersonName}
          onCompanyNameChange={setProfileCompanyName}
          onTaxIdChange={setProfileTaxId}
          onAddressLine1Change={setProfileAddressLine1}
          onAddressLine2Change={setProfileAddressLine2}
          onCityChange={setProfileCity}
          onPostalCodeChange={setProfilePostalCode}
          onCountryChange={setProfileCountry}
          onSave={(profile) => void run(() => saveBillingProfile(profile), t("account.invoiceDetailsSaved", "Invoice details saved."))}
          t={t}
        />
      ) : null}

      {signedInMode === "delivery" ? (
        <DeliveryProfileForm
          customerType={profileCustomerType}
          receiptEmail={profileReceiptEmail}
          personName={profilePersonName}
          companyName={profileCompanyName}
          taxId={profileTaxId}
          billingAddressLine1={profileAddressLine1}
          billingAddressLine2={profileAddressLine2}
          billingCity={profileCity}
          billingPostalCode={profilePostalCode}
          billingCountry={profileCountry}
          contactName={profileDeliveryContactName}
          phone={profileDeliveryPhone}
          addressLine1={profileDeliveryAddressLine1}
          addressLine2={profileDeliveryAddressLine2}
          city={profileDeliveryCity}
          postalCode={profileDeliveryPostalCode}
          country={profileDeliveryCountry}
          onContactNameChange={setProfileDeliveryContactName}
          onPhoneChange={setProfileDeliveryPhone}
          onAddressLine1Change={setProfileDeliveryAddressLine1}
          onAddressLine2Change={setProfileDeliveryAddressLine2}
          onCityChange={setProfileDeliveryCity}
          onPostalCodeChange={setProfileDeliveryPostalCode}
          onCountryChange={setProfileDeliveryCountry}
          onSave={(profile) => void run(() => saveBillingProfile(profile), t("account.deliveryDetailsSaved", "Delivery details saved."))}
          t={t}
        />
      ) : null}

      {signedInMode === "password" ? (
        <PasswordChangeForm
          currentPassword={currentPassword}
          newPassword={newPassword}
          onCurrentPasswordChange={setCurrentPassword}
          onNewPasswordChange={setNewPassword}
          onSubmit={() =>
            void run(async () => {
              await changePassword(currentPassword, newPassword);
              setCurrentPassword("");
              setNewPassword("");
            }, t("account.passwordChanged", "Password changed."))
          }
          t={t}
        />
      ) : null}

      {signedInMode === "remove" ? (
        <AccountRemovalForm
          confirmationCode={confirmDeleteCode}
          onConfirmationCodeChange={setConfirmDeleteCode}
          onRequestDeletion={() => void run(requestAccountDeletion, t("account.confirmationEmailSent", "Confirmation email sent."))}
          onConfirmDeletion={() =>
            void run(async () => {
              await confirmAccountDeletion(confirmDeleteCode);
            }, t("account.accountRemoved", "Account removed."))
          }
          t={t}
        />
      ) : null}
    </ScrollView>
  );
}
