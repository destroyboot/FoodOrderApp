import type React from "react";
import { Modal, Pressable, ScrollView, Text, TextInput, View } from "react-native";
import { PickerField } from "../../components/PickerField";
import { PrimaryButton } from "../../components/PrimaryButton";
import { inputStyle, sharedStyles, theme } from "../../lib/theme";
import type { CartPreview, CartResponse, MenuItem, Restaurant, RestaurantTable } from "../../types/api";
import { OrderType, PaymentMethod } from "../../types/api";
import type { TFunction } from "./types";

type Option = {
  value: string;
  label: string;
};

type Props = {
  visible: boolean;
  busy: boolean;
  cart: CartResponse | null;
  selectedRestaurant: Restaurant | null;
  tables: RestaurantTable[];
  itemMap: Record<number, MenuItem>;
  token: string | null;
  preview: CartPreview | null;
  pickupContactName: string;
  pickupPhone: string;
  pickupNote: string;
  pickupTime: string;
  deliveryContactName: string;
  deliveryPhone: string;
  deliveryAddressLine1: string;
  deliveryAddressLine2: string;
  deliveryCity: string;
  deliveryPostalCode: string;
  deliveryCountry: string;
  deliveryNote: string;
  paymentOptions: Option[];
  selectedPaymentMethod: PaymentMethod;
  billingMode: "receipt" | "invoice";
  invoiceCustomerType: "person" | "company";
  invoiceReceiptEmail: string;
  confirmEmail: string;
  invoicePersonName: string;
  invoiceCompanyName: string;
  invoiceTaxId: string;
  invoiceAddressLine1: string;
  invoiceAddressLine2: string;
  invoiceCity: string;
  invoicePostalCode: string;
  invoiceCountry: string;
  saveInvoiceProfileEnabled: boolean;
  onSelectedPaymentMethodChange: (value: PaymentMethod) => void;
  onBillingModeChange: (value: "receipt" | "invoice") => void;
  onInvoiceCustomerTypeChange: (value: "person" | "company") => void;
  onInvoiceReceiptEmailChange: (value: string) => void;
  onConfirmEmailChange: (value: string) => void;
  onInvoicePersonNameChange: (value: string) => void;
  onInvoiceCompanyNameChange: (value: string) => void;
  onInvoiceTaxIdChange: (value: string) => void;
  onInvoiceAddressLine1Change: (value: string) => void;
  onInvoiceAddressLine2Change: (value: string) => void;
  onInvoiceCityChange: (value: string) => void;
  onInvoicePostalCodeChange: (value: string) => void;
  onInvoiceCountryChange: (value: string) => void;
  onSaveInvoiceProfileEnabledChange: (value: boolean) => void;
  clearFieldError: (field: string) => void;
  inputErrorStyle: (field: string) => object | null;
  FieldError: (props: { field: string }) => React.ReactNode;
  withRequired: (label: string, required?: boolean) => string;
  buildTodayIsoFromTime: (input: string) => string | null;
  onConfirm: () => void;
  onCancel: () => void;
  t: TFunction;
};

export function PlaceOrderModal({
  visible,
  busy,
  cart,
  selectedRestaurant,
  tables,
  itemMap,
  token,
  preview,
  pickupContactName,
  pickupPhone,
  pickupNote,
  pickupTime,
  deliveryContactName,
  deliveryPhone,
  deliveryAddressLine1,
  deliveryAddressLine2,
  deliveryCity,
  deliveryPostalCode,
  deliveryCountry,
  deliveryNote,
  paymentOptions,
  selectedPaymentMethod,
  billingMode,
  invoiceCustomerType,
  invoiceReceiptEmail,
  confirmEmail,
  invoicePersonName,
  invoiceCompanyName,
  invoiceTaxId,
  invoiceAddressLine1,
  invoiceAddressLine2,
  invoiceCity,
  invoicePostalCode,
  invoiceCountry,
  saveInvoiceProfileEnabled,
  onSelectedPaymentMethodChange,
  onBillingModeChange,
  onInvoiceCustomerTypeChange,
  onInvoiceReceiptEmailChange,
  onConfirmEmailChange,
  onInvoicePersonNameChange,
  onInvoiceCompanyNameChange,
  onInvoiceTaxIdChange,
  onInvoiceAddressLine1Change,
  onInvoiceAddressLine2Change,
  onInvoiceCityChange,
  onInvoicePostalCodeChange,
  onInvoiceCountryChange,
  onSaveInvoiceProfileEnabledChange,
  clearFieldError,
  inputErrorStyle,
  FieldError,
  withRequired,
  buildTodayIsoFromTime,
  onConfirm,
  onCancel,
  t,
}: Props) {
  return (
    <Modal visible={visible && !!cart} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable onPress={onCancel} style={sharedStyles.modalBackdrop}>
        <Pressable onPress={() => undefined} style={[sharedStyles.modalCard, { width: "100%", maxWidth: 560, alignSelf: "center" }]}>
          <ScrollView style={{ flexGrow: 0 }} contentContainerStyle={{ gap: 12, paddingBottom: 16 }} keyboardShouldPersistTaps="handled">
            <Text style={sharedStyles.modalTitle}>{t("cart.placeOrder", "Place Order")}</Text>
            {cart ? (
              <>
                <Text style={sharedStyles.bodyMuted}>
                  {t("cart.confirmOrderFor", "Confirm your order for")} {selectedRestaurant?.name ?? t("cart.thisRestaurant", "this restaurant")}.
                </Text>
                <OrderSummaryPanel cart={cart} itemMap={itemMap} t={t} />
                <FulfillmentSummary
                  cart={cart}
                  tables={tables}
                  pickupContactName={pickupContactName}
                  pickupPhone={pickupPhone}
                  pickupNote={pickupNote}
                  pickupTime={pickupTime}
                  deliveryContactName={deliveryContactName}
                  deliveryPhone={deliveryPhone}
                  deliveryAddressLine1={deliveryAddressLine1}
                  deliveryAddressLine2={deliveryAddressLine2}
                  deliveryCity={deliveryCity}
                  deliveryPostalCode={deliveryPostalCode}
                  deliveryCountry={deliveryCountry}
                  deliveryNote={deliveryNote}
                  buildTodayIsoFromTime={buildTodayIsoFromTime}
                  t={t}
                />
                <PickerField
                  label={withRequired(t("cart.paymentOption", "Payment option"), true)}
                  placeholder={t("cart.choosePaymentOption", "Choose payment option")}
                  value={String(selectedPaymentMethod)}
                  options={paymentOptions}
                  onChange={(value) => {
                    clearFieldError("paymentMethod");
                    onSelectedPaymentMethodChange(Number(value) as PaymentMethod);
                  }}
                />
                <FieldError field="paymentMethod" />
                {!token ? (
                  <>
                    <TextInput
                      placeholder={withRequired(t("cart.emailAddress", "Email address"), true)}
                      value={invoiceReceiptEmail}
                      onChangeText={(value) => {
                        onInvoiceReceiptEmailChange(value);
                        clearFieldError("invoiceReceiptEmail");
                      }}
                      style={[inputStyle, inputErrorStyle("invoiceReceiptEmail")]}
                      autoCapitalize="none"
                    />
                    <FieldError field="invoiceReceiptEmail" />
                    <TextInput
                      placeholder={withRequired(t("cart.confirmEmailAddress", "Confirm email address"), true)}
                      value={confirmEmail}
                      onChangeText={(value) => {
                        onConfirmEmailChange(value);
                        clearFieldError("confirmEmail");
                      }}
                      style={[inputStyle, inputErrorStyle("confirmEmail")]}
                      autoCapitalize="none"
                    />
                    <FieldError field="confirmEmail" />
                  </>
                ) : null}
                <PickerField
                  label={t("cart.document", "Document")}
                  placeholder={t("cart.chooseDocumentType", "Choose document type")}
                  value={billingMode}
                  options={[
                    { value: "receipt", label: t("cart.regularReceipt", "Regular receipt") },
                    { value: "invoice", label: t("cart.invoice", "Invoice") },
                  ]}
                  onChange={(value) => onBillingModeChange(value === "invoice" ? "invoice" : "receipt")}
                />
                {billingMode === "invoice" ? (
                  <InvoiceDetailsFields
                    token={token}
                    invoiceCustomerType={invoiceCustomerType}
                    invoiceReceiptEmail={invoiceReceiptEmail}
                    invoicePersonName={invoicePersonName}
                    invoiceCompanyName={invoiceCompanyName}
                    invoiceTaxId={invoiceTaxId}
                    invoiceAddressLine1={invoiceAddressLine1}
                    invoiceAddressLine2={invoiceAddressLine2}
                    invoiceCity={invoiceCity}
                    invoicePostalCode={invoicePostalCode}
                    invoiceCountry={invoiceCountry}
                    saveInvoiceProfileEnabled={saveInvoiceProfileEnabled}
                    onInvoiceCustomerTypeChange={onInvoiceCustomerTypeChange}
                    onInvoiceReceiptEmailChange={onInvoiceReceiptEmailChange}
                    onInvoicePersonNameChange={onInvoicePersonNameChange}
                    onInvoiceCompanyNameChange={onInvoiceCompanyNameChange}
                    onInvoiceTaxIdChange={onInvoiceTaxIdChange}
                    onInvoiceAddressLine1Change={onInvoiceAddressLine1Change}
                    onInvoiceAddressLine2Change={onInvoiceAddressLine2Change}
                    onInvoiceCityChange={onInvoiceCityChange}
                    onInvoicePostalCodeChange={onInvoicePostalCodeChange}
                    onInvoiceCountryChange={onInvoiceCountryChange}
                    onSaveInvoiceProfileEnabledChange={onSaveInvoiceProfileEnabledChange}
                    clearFieldError={clearFieldError}
                    inputErrorStyle={inputErrorStyle}
                    FieldError={FieldError}
                    withRequired={withRequired}
                    t={t}
                  />
                ) : null}
                {preview ? (
                  <View style={sharedStyles.stackSm}>
                    <Text>{t("orders.subtotal", "Subtotal")}: {preview.subtotal.toFixed(2)}</Text>
                    <Text>{t("orders.deliveryFee", "Delivery fee")}: {preview.deliveryFee.toFixed(2)}</Text>
                    <Text style={sharedStyles.title}>{t("orders.total", "Total")}: {preview.total.toFixed(2)}</Text>
                  </View>
                ) : null}
                <Text style={sharedStyles.bodyMuted}>
                  {billingMode === "invoice"
                    ? t("cart.invoiceAttached", "Invoice details will be attached to this order and can be reused next time.")
                    : t("cart.receiptPhysical", "Regular receipt stays physical. Staff can still send an order summary by email later.")}
                </Text>
                <View style={sharedStyles.stackMd}>
                  <PrimaryButton
                    label={busy ? t("cart.placing", "Placing...") : t("cart.confirmOrder", "Confirm order")}
                    onPress={onConfirm}
                    disabled={busy || !cart.items.length}
                  />
                  <PrimaryButton label={t("common.cancel", "Cancel")} onPress={onCancel} />
                </View>
              </>
            ) : null}
          </ScrollView>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function OrderSummaryPanel({ cart, itemMap, t }: { cart: CartResponse; itemMap: Record<number, MenuItem>; t: TFunction }) {
  return (
    <View style={[sharedStyles.stackMd, sharedStyles.framedPanel]}>
      <Text style={sharedStyles.title}>{t("cart.orderSummary", "Order summary")}</Text>
      {cart.items.map((line) => (
        <View key={`summary-${line.lineId}`} style={sharedStyles.stackXs}>
          <Text style={sharedStyles.semibold}>
            {line.quantity}x {itemMap[line.menuItemId]?.name ?? line.menuItemName ?? `${t("orders.item", "Item")} ${line.menuItemId}`} = {line.lineTotal.toFixed(2)}
          </Text>
          {line.note ? <Text style={sharedStyles.mutedText}>{line.note}</Text> : null}
          {line.extraCharge > 0 ? (
            <Text style={{ color: "#2563eb" }}>
              {t("menu.customizationExtra", "Customization extra")}: {line.extraCharge.toFixed(2)}
            </Text>
          ) : null}
        </View>
      ))}
    </View>
  );
}

function FulfillmentSummary({
  cart,
  tables,
  pickupContactName,
  pickupPhone,
  pickupNote,
  pickupTime,
  deliveryContactName,
  deliveryPhone,
  deliveryAddressLine1,
  deliveryAddressLine2,
  deliveryCity,
  deliveryPostalCode,
  deliveryCountry,
  deliveryNote,
  buildTodayIsoFromTime,
  t,
}: {
  cart: CartResponse;
  tables: RestaurantTable[];
  pickupContactName: string;
  pickupPhone: string;
  pickupNote: string;
  pickupTime: string;
  deliveryContactName: string;
  deliveryPhone: string;
  deliveryAddressLine1: string;
  deliveryAddressLine2: string;
  deliveryCity: string;
  deliveryPostalCode: string;
  deliveryCountry: string;
  deliveryNote: string;
  buildTodayIsoFromTime: (input: string) => string | null;
  t: TFunction;
}) {
  return (
    <>
      <Text>{t("orders.type", "Order type")}: {cart.orderType === OrderType.Table ? t("mobile.restaurants.toTable", "To table") : cart.orderType === OrderType.Takeaway ? t("mobile.restaurants.pickup", "Pick up") : t("mobile.restaurants.delivery", "Delivery")}</Text>
      {cart.orderType === OrderType.Table ? (
        <Text>{t("mobile.reservations.table", "Table")}: {cart.tableNumber ?? tables.find((table) => table.id === cart.restaurantTableId)?.label ?? "-"}</Text>
      ) : null}
      {cart.orderType === OrderType.Takeaway ? (
        <View style={sharedStyles.stackSm}>
          <Text>Pickup name: {(cart.pickupContactName ?? pickupContactName) || "-"}</Text>
          <Text>Pickup phone: {(cart.pickupPhone ?? pickupPhone) || "-"}</Text>
          {cart.scheduledFor || pickupTime ? (
            <Text>
              Requested pickup: {new Date(cart.scheduledFor ?? buildTodayIsoFromTime(pickupTime) ?? "").toLocaleString()}
            </Text>
          ) : null}
          {(cart.pickupNote ?? pickupNote) ? <Text>Pickup note: {cart.pickupNote ?? pickupNote}</Text> : null}
        </View>
      ) : null}
      {cart.orderType === OrderType.Delivery ? (
        <View style={sharedStyles.stackSm}>
          <Text>Delivery contact: {(cart.deliveryContactName ?? deliveryContactName) || "-"}</Text>
          <Text>Delivery phone: {(cart.deliveryPhone ?? deliveryPhone) || "-"}</Text>
          <Text>
            Address: {[
              cart.deliveryAddressLine1 ?? deliveryAddressLine1,
              cart.deliveryAddressLine2 ?? deliveryAddressLine2,
              cart.deliveryCity ?? deliveryCity,
              cart.deliveryPostalCode ?? deliveryPostalCode,
              cart.deliveryCountry ?? deliveryCountry,
            ].filter(Boolean).join(", ") || "-"}
          </Text>
          {(cart.deliveryNote ?? deliveryNote) ? <Text>Delivery note: {cart.deliveryNote ?? deliveryNote}</Text> : null}
        </View>
      ) : null}
    </>
  );
}

function InvoiceDetailsFields({
  token,
  invoiceCustomerType,
  invoiceReceiptEmail,
  invoicePersonName,
  invoiceCompanyName,
  invoiceTaxId,
  invoiceAddressLine1,
  invoiceAddressLine2,
  invoiceCity,
  invoicePostalCode,
  invoiceCountry,
  saveInvoiceProfileEnabled,
  onInvoiceCustomerTypeChange,
  onInvoiceReceiptEmailChange,
  onInvoicePersonNameChange,
  onInvoiceCompanyNameChange,
  onInvoiceTaxIdChange,
  onInvoiceAddressLine1Change,
  onInvoiceAddressLine2Change,
  onInvoiceCityChange,
  onInvoicePostalCodeChange,
  onInvoiceCountryChange,
  onSaveInvoiceProfileEnabledChange,
  clearFieldError,
  inputErrorStyle,
  FieldError,
  withRequired,
  t,
}: Pick<Props,
  | "token"
  | "invoiceCustomerType"
  | "invoiceReceiptEmail"
  | "invoicePersonName"
  | "invoiceCompanyName"
  | "invoiceTaxId"
  | "invoiceAddressLine1"
  | "invoiceAddressLine2"
  | "invoiceCity"
  | "invoicePostalCode"
  | "invoiceCountry"
  | "saveInvoiceProfileEnabled"
  | "onInvoiceCustomerTypeChange"
  | "onInvoiceReceiptEmailChange"
  | "onInvoicePersonNameChange"
  | "onInvoiceCompanyNameChange"
  | "onInvoiceTaxIdChange"
  | "onInvoiceAddressLine1Change"
  | "onInvoiceAddressLine2Change"
  | "onInvoiceCityChange"
  | "onInvoicePostalCodeChange"
  | "onInvoiceCountryChange"
  | "onSaveInvoiceProfileEnabledChange"
  | "clearFieldError"
  | "inputErrorStyle"
  | "FieldError"
  | "withRequired"
  | "t"
>) {
  return (
    <View style={sharedStyles.stackLg}>
      <PickerField
        label={t("cart.invoiceFor", "Invoice for")}
        placeholder={t("cart.choosePersonOrCompany", "Choose person or company")}
        value={invoiceCustomerType}
        options={[
          { value: "person", label: t("cart.person", "Person") },
          { value: "company", label: t("cart.company", "Company") },
        ]}
        onChange={(value) => onInvoiceCustomerTypeChange(value === "company" ? "company" : "person")}
      />
      {!token ? (
        <TextInput
          placeholder={withRequired(t("cart.invoiceEmail", "Invoice email"), true)}
          value={invoiceReceiptEmail}
          onChangeText={(value) => {
            onInvoiceReceiptEmailChange(value);
            clearFieldError("invoiceReceiptEmail");
          }}
          style={[inputStyle, inputErrorStyle("invoiceReceiptEmail")]}
          autoCapitalize="none"
        />
      ) : null}
      {invoiceCustomerType === "person" ? (
        <>
          <TextInput
            placeholder={withRequired(t("cart.fullName", "Full name"), true)}
            value={invoicePersonName}
            onChangeText={(value) => {
              onInvoicePersonNameChange(value);
              clearFieldError("invoicePersonName");
            }}
            style={[inputStyle, inputErrorStyle("invoicePersonName")]}
          />
          <FieldError field="invoicePersonName" />
        </>
      ) : (
        <>
          <TextInput
            placeholder={withRequired(t("cart.companyName", "Company name"), true)}
            value={invoiceCompanyName}
            onChangeText={(value) => {
              onInvoiceCompanyNameChange(value);
              clearFieldError("invoiceCompanyName");
            }}
            style={[inputStyle, inputErrorStyle("invoiceCompanyName")]}
          />
          <FieldError field="invoiceCompanyName" />
          <TextInput
            placeholder={withRequired(t("cart.taxId", "Tax ID / NIP"), true)}
            value={invoiceTaxId}
            onChangeText={(value) => {
              onInvoiceTaxIdChange(value);
              clearFieldError("invoiceTaxId");
            }}
            style={[inputStyle, inputErrorStyle("invoiceTaxId")]}
          />
          <FieldError field="invoiceTaxId" />
        </>
      )}
      <TextInput
        placeholder={withRequired(t("cart.billingAddressLine1", "Billing address line 1"), true)}
        value={invoiceAddressLine1}
        onChangeText={(value) => {
          onInvoiceAddressLine1Change(value);
          clearFieldError("invoiceAddressLine1");
        }}
        style={[inputStyle, inputErrorStyle("invoiceAddressLine1")]}
      />
      <FieldError field="invoiceAddressLine1" />
      <TextInput placeholder={t("cart.billingAddressLine2Optional", "Billing address line 2 (optional)")} value={invoiceAddressLine2} onChangeText={onInvoiceAddressLine2Change} style={inputStyle} />
      <TextInput
        placeholder={withRequired(t("cart.billingCity", "Billing city"), true)}
        value={invoiceCity}
        onChangeText={(value) => {
          onInvoiceCityChange(value);
          clearFieldError("invoiceCity");
        }}
        style={[inputStyle, inputErrorStyle("invoiceCity")]}
      />
      <FieldError field="invoiceCity" />
      <TextInput
        placeholder={withRequired(t("cart.billingPostalCode", "Billing postal code"), true)}
        value={invoicePostalCode}
        onChangeText={(value) => {
          onInvoicePostalCodeChange(value);
          clearFieldError("invoicePostalCode");
        }}
        style={[inputStyle, inputErrorStyle("invoicePostalCode")]}
      />
      <FieldError field="invoicePostalCode" />
      <TextInput placeholder={t("cart.billingCountryOptional", "Billing country (optional)")} value={invoiceCountry} onChangeText={onInvoiceCountryChange} style={inputStyle} />
      {token ? (
        <Pressable
          onPress={() => onSaveInvoiceProfileEnabledChange(!saveInvoiceProfileEnabled)}
          style={sharedStyles.rowSm}
        >
          <View
            style={{
              width: 20,
              height: 20,
              borderRadius: 4,
              borderWidth: 1,
              borderColor: saveInvoiceProfileEnabled ? "#2563eb" : "#9ca3af",
              backgroundColor: saveInvoiceProfileEnabled ? "#2563eb" : "#fff",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            {saveInvoiceProfileEnabled ? <Text style={{ color: "#fff", fontWeight: "700", fontSize: 12 }}>✓</Text> : null}
          </View>
          <Text style={{ color: "#1d4ed8", fontWeight: "600" }}>
            {t("cart.saveInvoiceDataNextTime", "save invoice data for next time")}
          </Text>
        </Pressable>
      ) : null}
    </View>
  );
}
