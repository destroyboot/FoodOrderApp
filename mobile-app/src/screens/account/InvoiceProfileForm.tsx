import { TextInput, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { BillingProfile } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  customerType: "person" | "company";
  receiptEmail: string;
  personName: string;
  companyName: string;
  taxId: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  postalCode: string;
  country: string;
  deliveryContactName: string;
  deliveryPhone: string;
  deliveryAddressLine1: string;
  deliveryAddressLine2: string;
  deliveryCity: string;
  deliveryPostalCode: string;
  deliveryCountry: string;
  onCustomerTypeChange: (value: "person" | "company") => void;
  onReceiptEmailChange: (value: string) => void;
  onPersonNameChange: (value: string) => void;
  onCompanyNameChange: (value: string) => void;
  onTaxIdChange: (value: string) => void;
  onAddressLine1Change: (value: string) => void;
  onAddressLine2Change: (value: string) => void;
  onCityChange: (value: string) => void;
  onPostalCodeChange: (value: string) => void;
  onCountryChange: (value: string) => void;
  onSave: (profile: BillingProfile) => void;
  t: TFunction;
};

export function InvoiceProfileForm({
  customerType,
  receiptEmail,
  personName,
  companyName,
  taxId,
  addressLine1,
  addressLine2,
  city,
  postalCode,
  country,
  deliveryContactName,
  deliveryPhone,
  deliveryAddressLine1,
  deliveryAddressLine2,
  deliveryCity,
  deliveryPostalCode,
  deliveryCountry,
  onCustomerTypeChange,
  onReceiptEmailChange,
  onPersonNameChange,
  onCompanyNameChange,
  onTaxIdChange,
  onAddressLine1Change,
  onAddressLine2Change,
  onCityChange,
  onPostalCodeChange,
  onCountryChange,
  onSave,
  t,
}: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <TextInput placeholder={t("account.receiptInvoiceEmail", "Receipt / invoice email")} value={receiptEmail} onChangeText={onReceiptEmailChange} autoCapitalize="none" style={inputStyle} />
        <View style={sharedStyles.rowTop}>
          <PrimaryButton label={customerType === "person" ? t("cart.person", "Person") : t("account.invoiceForPerson", "Invoice for person")} onPress={() => onCustomerTypeChange("person")} />
          <PrimaryButton label={customerType === "company" ? t("cart.company", "Company") : t("account.invoiceForCompany", "Invoice for company")} onPress={() => onCustomerTypeChange("company")} />
        </View>
        {customerType === "person" ? (
          <TextInput placeholder={t("cart.fullName", "Full name")} value={personName} onChangeText={onPersonNameChange} style={inputStyle} />
        ) : (
          <>
            <TextInput placeholder={t("cart.companyName", "Company name")} value={companyName} onChangeText={onCompanyNameChange} style={inputStyle} />
            <TextInput placeholder={t("cart.taxId", "Tax ID / NIP")} value={taxId} onChangeText={onTaxIdChange} style={inputStyle} />
          </>
        )}
        <TextInput placeholder={t("cart.billingAddressLine1", "Billing address line 1")} value={addressLine1} onChangeText={onAddressLine1Change} style={inputStyle} />
        <TextInput placeholder={t("cart.billingAddressLine2Optional", "Billing address line 2 (optional)")} value={addressLine2} onChangeText={onAddressLine2Change} style={inputStyle} />
        <TextInput placeholder={t("cart.billingCity", "Billing city")} value={city} onChangeText={onCityChange} style={inputStyle} />
        <TextInput placeholder={t("cart.billingPostalCode", "Billing postal code")} value={postalCode} onChangeText={onPostalCodeChange} style={inputStyle} />
        <TextInput placeholder={t("cart.billingCountryOptional", "Billing country (optional)")} value={country} onChangeText={onCountryChange} style={inputStyle} />
        <PrimaryButton
          label={t("account.saveInvoiceDetails", "Save invoice details")}
          onPress={() =>
            onSave({
              customerType: customerType === "company" ? 1 : 0,
              receiptEmail,
              personName,
              companyName,
              taxId,
              billingAddressLine1: addressLine1,
              billingAddressLine2: addressLine2,
              billingCity: city,
              billingPostalCode: postalCode,
              billingCountry: country,
              deliveryContactName,
              deliveryPhone,
              deliveryAddressLine1,
              deliveryAddressLine2,
              deliveryCity,
              deliveryPostalCode,
              deliveryCountry,
            })
          }
        />
      </View>
    </SectionCard>
  );
}
