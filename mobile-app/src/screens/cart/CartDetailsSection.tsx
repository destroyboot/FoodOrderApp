import type React from "react";
import { Text, TextInput, View } from "react-native";
import { PickerField } from "../../components/PickerField";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles, theme } from "../../lib/theme";
import type { CartResponse, Restaurant, RestaurantTable } from "../../types/api";
import { OrderType } from "../../types/api";
import type { TFunction } from "./types";

type Option = {
  value: string;
  label: string;
};

type Props = {
  cart: CartResponse;
  selectedRestaurant: Restaurant | null;
  tables: RestaurantTable[];
  orderTypeOptions: Option[];
  tableOptions: Option[];
  pickupContactName: string;
  pickupPhone: string;
  pickupNote: string;
  pickupHour: string;
  pickupMinute: string;
  pickupHourOptions: Option[];
  pickupMinuteOptions: Option[];
  deliveryContactName: string;
  deliveryPhone: string;
  deliveryAddressLine1: string;
  deliveryAddressLine2: string;
  deliveryCity: string;
  deliveryPostalCode: string;
  deliveryCountry: string;
  deliveryNote: string;
  onOrderTypeChange: (orderType: OrderType) => void;
  onTableChange: (tableId: number) => void;
  onPickupContactNameChange: (value: string) => void;
  onPickupPhoneChange: (value: string) => void;
  onPickupNoteChange: (value: string) => void;
  onPickupTimePartChange: (part: "hour" | "minute", value: string) => void;
  onDeliveryContactNameChange: (value: string) => void;
  onDeliveryPhoneChange: (value: string) => void;
  onDeliveryAddressLine1Change: (value: string) => void;
  onDeliveryAddressLine2Change: (value: string) => void;
  onDeliveryCityChange: (value: string) => void;
  onDeliveryPostalCodeChange: (value: string) => void;
  onDeliveryCountryChange: (value: string) => void;
  onDeliveryNoteChange: (value: string) => void;
  onRemoveCart: () => void;
  inputErrorStyle: (field: string) => object | null;
  FieldError: (props: { field: string }) => React.ReactNode;
  withRequired: (label: string, required?: boolean) => string;
  t: TFunction;
};

export function CartDetailsSection({
  cart,
  selectedRestaurant,
  tables,
  orderTypeOptions,
  tableOptions,
  pickupContactName,
  pickupPhone,
  pickupNote,
  pickupHour,
  pickupMinute,
  pickupHourOptions,
  pickupMinuteOptions,
  deliveryContactName,
  deliveryPhone,
  deliveryAddressLine1,
  deliveryAddressLine2,
  deliveryCity,
  deliveryPostalCode,
  deliveryCountry,
  deliveryNote,
  onOrderTypeChange,
  onTableChange,
  onPickupContactNameChange,
  onPickupPhoneChange,
  onPickupNoteChange,
  onPickupTimePartChange,
  onDeliveryContactNameChange,
  onDeliveryPhoneChange,
  onDeliveryAddressLine1Change,
  onDeliveryAddressLine2Change,
  onDeliveryCityChange,
  onDeliveryPostalCodeChange,
  onDeliveryCountryChange,
  onDeliveryNoteChange,
  onRemoveCart,
  inputErrorStyle,
  FieldError,
  withRequired,
  t,
}: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("orders.details", "Order details")}</Text>
        <Text style={sharedStyles.bodyMuted}>
          {selectedRestaurant ? `${t("cart.editingCartFor", "Editing cart for")} ${selectedRestaurant.name}.` : t("mobile.reservations.chooseRestaurantFirst", "Choose a restaurant first.")}
        </Text>
        <View style={{ borderWidth: 1, borderColor: "#b7c8e0", backgroundColor: theme.colors.navySoft, borderRadius: theme.radius.medium, padding: 12 }}>
          <PickerField
            label={t("orders.type", "Order type")}
            placeholder={t("cart.chooseOrderType", "Choose order type")}
            value={String(cart.orderType)}
            options={orderTypeOptions}
            onChange={(value) => onOrderTypeChange(Number(value) as OrderType)}
          />
        </View>

        {cart.orderType === OrderType.Table ? (
          <PickerField
            label={t("mobile.reservations.table", "Table")}
            placeholder={t("mobile.reservations.chooseTable", "Choose table")}
            value={cart.restaurantTableId ? String(cart.restaurantTableId) : ""}
            options={tableOptions}
            onChange={(value) => onTableChange(Number(value))}
          />
        ) : null}

        {cart.orderType === OrderType.Takeaway ? (
          <View style={sharedStyles.stackLg}>
            <TextInput
              placeholder={withRequired(t("orders.pickupName", "Pickup name"), true)}
              value={pickupContactName}
              onChangeText={onPickupContactNameChange}
              style={[inputStyle, inputErrorStyle("pickupContactName")]}
            />
            <FieldError field="pickupContactName" />
            <TextInput
              placeholder={withRequired(t("orders.pickupPhone", "Pickup phone"), true)}
              value={pickupPhone}
              onChangeText={onPickupPhoneChange}
              style={[inputStyle, inputErrorStyle("pickupPhone")]}
            />
            <FieldError field="pickupPhone" />
            <Text style={sharedStyles.title}>{t("cart.requestedPickupTimeToday", "Requested pickup time for today")}</Text>
            <View style={sharedStyles.rowTop}>
              <View style={sharedStyles.flexOne}>
                <PickerField
                  label={t("cart.hour", "Hour")}
                  placeholder="HH"
                  value={pickupHour}
                  options={pickupHourOptions}
                  onChange={(value) => onPickupTimePartChange("hour", value)}
                />
              </View>
              <View style={sharedStyles.flexOne}>
                <PickerField
                  label={t("cart.minute", "Minute")}
                  placeholder="MM"
                  value={pickupMinute}
                  options={pickupMinuteOptions}
                  onChange={(value) => onPickupTimePartChange("minute", value)}
                />
              </View>
            </View>
            <FieldError field="pickupTime" />
            <TextInput
              placeholder={t("cart.pickupNoteOptional", "Pickup note (optional)")}
              value={pickupNote}
              onChangeText={onPickupNoteChange}
              style={[inputStyle, { minHeight: 88, textAlignVertical: "top" }]}
              multiline
            />
          </View>
        ) : null}

        {cart.orderType === OrderType.Delivery ? (
          <View style={sharedStyles.stackLg}>
            <TextInput
              placeholder={withRequired(t("orders.deliveryContact", "Delivery contact name"), true)}
              value={deliveryContactName}
              onChangeText={onDeliveryContactNameChange}
              style={[inputStyle, inputErrorStyle("deliveryContactName")]}
            />
            <FieldError field="deliveryContactName" />
            <TextInput
              placeholder={withRequired(t("orders.deliveryPhone", "Delivery phone"), true)}
              value={deliveryPhone}
              onChangeText={onDeliveryPhoneChange}
              style={[inputStyle, inputErrorStyle("deliveryPhone")]}
            />
            <FieldError field="deliveryPhone" />
            <TextInput
              placeholder={withRequired(t("cart.addressLine1", "Address line 1"), true)}
              value={deliveryAddressLine1}
              onChangeText={onDeliveryAddressLine1Change}
              style={[inputStyle, inputErrorStyle("deliveryAddressLine1")]}
            />
            <FieldError field="deliveryAddressLine1" />
            <TextInput
              placeholder={t("cart.addressLine2Optional", "Address line 2 (optional)")}
              value={deliveryAddressLine2}
              onChangeText={onDeliveryAddressLine2Change}
              style={inputStyle}
            />
            <TextInput
              placeholder={withRequired(t("common.city", "City"), true)}
              value={deliveryCity}
              onChangeText={onDeliveryCityChange}
              style={[inputStyle, inputErrorStyle("deliveryCity")]}
            />
            <FieldError field="deliveryCity" />
            <TextInput
              placeholder={withRequired(t("common.postalCode", "Postal code"), true)}
              value={deliveryPostalCode}
              onChangeText={onDeliveryPostalCodeChange}
              style={[inputStyle, inputErrorStyle("deliveryPostalCode")]}
            />
            <FieldError field="deliveryPostalCode" />
            <TextInput
              placeholder={t("cart.countryOptional", "Country (optional)")}
              value={deliveryCountry}
              onChangeText={onDeliveryCountryChange}
              style={inputStyle}
            />
            <TextInput
              placeholder={t("cart.deliveryNoteOptional", "Delivery note (optional)")}
              value={deliveryNote}
              onChangeText={onDeliveryNoteChange}
              style={[inputStyle, { minHeight: 88, textAlignVertical: "top" }]}
              multiline
            />
          </View>
        ) : null}

        <PrimaryButton label={t("cart.removeCart", "Remove cart")} onPress={onRemoveCart} />
      </View>
    </SectionCard>
  );
}
