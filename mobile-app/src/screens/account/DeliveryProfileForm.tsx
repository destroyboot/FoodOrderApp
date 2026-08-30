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
  billingAddressLine1: string;
  billingAddressLine2: string;
  billingCity: string;
  billingPostalCode: string;
  billingCountry: string;
  contactName: string;
  phone: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  postalCode: string;
  country: string;
  onContactNameChange: (value: string) => void;
  onPhoneChange: (value: string) => void;
  onAddressLine1Change: (value: string) => void;
  onAddressLine2Change: (value: string) => void;
  onCityChange: (value: string) => void;
  onPostalCodeChange: (value: string) => void;
  onCountryChange: (value: string) => void;
  onSave: (profile: BillingProfile) => void;
  t: TFunction;
};

export function DeliveryProfileForm({
  customerType,
  receiptEmail,
  personName,
  companyName,
  taxId,
  billingAddressLine1,
  billingAddressLine2,
  billingCity,
  billingPostalCode,
  billingCountry,
  contactName,
  phone,
  addressLine1,
  addressLine2,
  city,
  postalCode,
  country,
  onContactNameChange,
  onPhoneChange,
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
        <TextInput placeholder={t("account.deliveryContactName", "Delivery contact name")} value={contactName} onChangeText={onContactNameChange} style={inputStyle} />
        <TextInput placeholder={t("account.deliveryPhone", "Delivery phone")} value={phone} onChangeText={onPhoneChange} style={inputStyle} />
        <TextInput placeholder={t("cart.addressLine1", "Address line 1")} value={addressLine1} onChangeText={onAddressLine1Change} style={inputStyle} />
        <TextInput placeholder={t("cart.addressLine2Optional", "Address line 2 (optional)")} value={addressLine2} onChangeText={onAddressLine2Change} style={inputStyle} />
        <TextInput placeholder={t("common.city", "City")} value={city} onChangeText={onCityChange} style={inputStyle} />
        <TextInput placeholder={t("common.postalCode", "Postal code")} value={postalCode} onChangeText={onPostalCodeChange} style={inputStyle} />
        <TextInput placeholder={t("cart.countryOptional", "Country (optional)")} value={country} onChangeText={onCountryChange} style={inputStyle} />
        <PrimaryButton
          label={t("account.saveDeliveryDetails", "Save delivery details")}
          onPress={() =>
            onSave({
              customerType: customerType === "company" ? 1 : 0,
              receiptEmail,
              personName,
              companyName,
              taxId,
              billingAddressLine1,
              billingAddressLine2,
              billingCity,
              billingPostalCode,
              billingCountry,
              deliveryContactName: contactName,
              deliveryPhone: phone,
              deliveryAddressLine1: addressLine1,
              deliveryAddressLine2: addressLine2,
              deliveryCity: city,
              deliveryPostalCode: postalCode,
              deliveryCountry: country,
            })
          }
        />
      </View>
    </SectionCard>
  );
}
