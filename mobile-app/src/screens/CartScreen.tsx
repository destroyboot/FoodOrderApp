import { useEffect, useMemo, useState } from "react";
import { ScrollView, Text } from "react-native";
import { IngredientCustomizationModal } from "../components/IngredientCustomizationModal";
import { PrimaryButton } from "../components/PrimaryButton";
import { useAppSession } from "../context/AppSessionContext";
import { apiRequest } from "../lib/api";
import { tryGetEmailFromToken } from "../lib/authIdentity";
import { confirmMessage, showMessage } from "../lib/dialogs";
import { sharedStyles } from "../lib/theme";
import type { MenuItem, MenuItemCustomization } from "../types/api";
import { OrderType, PaymentMethod } from "../types/api";
import { ActiveCartPicker } from "./cart/ActiveCartPicker";
import { CartDetailsSection } from "./cart/CartDetailsSection";
import { CartItemsSection } from "./cart/CartItemsSection";
import { CartTotalsSection } from "./cart/CartTotalsSection";
import { EmptyCartState } from "./cart/EmptyCartState";
import { PaymentModal } from "./cart/PaymentModal";
import { PlaceOrderModal } from "./cart/PlaceOrderModal";

function getLocalTimeValue(input: string | null | undefined) {
  if (!input) {
    return "";
  }

  const value = new Date(input);
  if (Number.isNaN(value.getTime())) {
    return "";
  }

  return `${String(value.getHours()).padStart(2, "0")}:${String(value.getMinutes()).padStart(2, "0")}`;
}

function buildTodayIsoFromTime(input: string) {
  if (!/^\d{2}:\d{2}$/.test(input)) {
    return null;
  }

  const [hours, minutes] = input.split(":").map(Number);
  if (Number.isNaN(hours) || Number.isNaN(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null;
  }

  const next = new Date();
  next.setSeconds(0, 0);
  next.setHours(hours, minutes, 0, 0);
  return next.toISOString();
}

export function CartScreen({ onOrderPlaced }: { onOrderPlaced?: () => void }) {
  const {
    cart,
    activeCarts,
    items,
    preview,
    token,
    billingProfile,
    currentCulture,
    tables,
    selectedRestaurant,
    restaurantSettings,
    loadActiveCarts,
    openCart,
    deleteCart,
    refreshCart,
    selectRestaurant,
    setCartMeta,
    setItemQuantity,
    updateItemCustomization,
    finalizeCart,
    saveBillingProfile,
    t,
  } = useAppSession();
  const [busy, setBusy] = useState(false);
  const [focusedCartId, setFocusedCartId] = useState<number | null>(null);
  const [showPlaceOrderModal, setShowPlaceOrderModal] = useState(false);
  const [pickupContactName, setPickupContactName] = useState("");
  const [pickupPhone, setPickupPhone] = useState("");
  const [pickupNote, setPickupNote] = useState("");
  const [pickupTime, setPickupTime] = useState("");
  const [deliveryContactName, setDeliveryContactName] = useState("");
  const [deliveryPhone, setDeliveryPhone] = useState("");
  const [deliveryAddressLine1, setDeliveryAddressLine1] = useState("");
  const [deliveryAddressLine2, setDeliveryAddressLine2] = useState("");
  const [deliveryCity, setDeliveryCity] = useState("");
  const [deliveryPostalCode, setDeliveryPostalCode] = useState("");
  const [deliveryCountry, setDeliveryCountry] = useState("");
  const [deliveryNote, setDeliveryNote] = useState("");
  const [billingMode, setBillingMode] = useState<"receipt" | "invoice">("receipt");
  const [invoiceCustomerType, setInvoiceCustomerType] = useState<"person" | "company">("person");
  const [invoiceReceiptEmail, setInvoiceReceiptEmail] = useState("");
  const [invoicePersonName, setInvoicePersonName] = useState("");
  const [invoiceCompanyName, setInvoiceCompanyName] = useState("");
  const [invoiceTaxId, setInvoiceTaxId] = useState("");
  const [invoiceAddressLine1, setInvoiceAddressLine1] = useState("");
  const [invoiceAddressLine2, setInvoiceAddressLine2] = useState("");
  const [invoiceCity, setInvoiceCity] = useState("");
  const [invoicePostalCode, setInvoicePostalCode] = useState("");
  const [invoiceCountry, setInvoiceCountry] = useState("");
  const [saveInvoiceProfileEnabled, setSaveInvoiceProfileEnabled] = useState(false);
  const [confirmEmail, setConfirmEmail] = useState("");
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [mockCardName, setMockCardName] = useState("");
  const [mockCardNumber, setMockCardNumber] = useState("");
  const [mockCardExpiry, setMockCardExpiry] = useState("");
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<PaymentMethod>(PaymentMethod.AtCounter);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});
  const [editingLine, setEditingLine] = useState<{ lineId: number; menuItem: MenuItem } | null>(null);
  const [customization, setCustomization] = useState<MenuItemCustomization | null>(null);
  const [removedIngredientIds, setRemovedIngredientIds] = useState<number[]>([]);
  const [addedIngredientIds, setAddedIngredientIds] = useState<number[]>([]);

  useEffect(() => {
    void Promise.all([loadActiveCarts(), refreshCart()]);
  }, []);

  useEffect(() => {
    if (activeCarts.length <= 1) {
      setFocusedCartId(activeCarts[0]?.cartId ?? null);
    } else if (focusedCartId && !activeCarts.some((draft) => draft.cartId === focusedCartId)) {
      setFocusedCartId(null);
    }
  }, [activeCarts, focusedCartId]);

  const itemMap = useMemo(
    () => Object.fromEntries(items.map((item) => [item.id, item])),
    [items]
  );

  const displayCart =
    cart && cart.items.length > 0 && activeCarts.length <= 1
      ? cart
      : cart && cart.items.length > 0 && focusedCartId === cart.cartId
        ? cart
        : null;

  useEffect(() => {
    if (showPlaceOrderModal || showPaymentModal) {
      return;
    }

    setPickupContactName(displayCart?.pickupContactName ?? "");
    setPickupPhone(displayCart?.pickupPhone ?? "");
    setPickupNote(displayCart?.pickupNote ?? "");
    setPickupTime(getLocalTimeValue(displayCart?.scheduledFor));
    setDeliveryContactName(displayCart?.deliveryContactName ?? billingProfile?.deliveryContactName ?? "");
    setDeliveryPhone(displayCart?.deliveryPhone ?? billingProfile?.deliveryPhone ?? "");
    setDeliveryAddressLine1(displayCart?.deliveryAddressLine1 ?? billingProfile?.deliveryAddressLine1 ?? "");
    setDeliveryAddressLine2(displayCart?.deliveryAddressLine2 ?? billingProfile?.deliveryAddressLine2 ?? "");
    setDeliveryCity(displayCart?.deliveryCity ?? billingProfile?.deliveryCity ?? "");
    setDeliveryPostalCode(displayCart?.deliveryPostalCode ?? billingProfile?.deliveryPostalCode ?? "");
    setDeliveryCountry(displayCart?.deliveryCountry ?? billingProfile?.deliveryCountry ?? "");
    setDeliveryNote(displayCart?.deliveryNote ?? "");
    setSelectedPaymentMethod(displayCart?.paymentMethod ?? PaymentMethod.AtCounter);
    const hasInvoice = (displayCart?.billingDetails?.invoiceStatus ?? 0) !== 0;
    setBillingMode(hasInvoice ? "invoice" : "receipt");
    setInvoiceCustomerType((displayCart?.billingDetails?.customerType ?? billingProfile?.customerType ?? 0) === 1 ? "company" : "person");
    const fallbackEmail = tryGetEmailFromToken(token);
    setInvoiceReceiptEmail(displayCart?.billingDetails?.receiptEmail ?? displayCart?.receiptEmail ?? billingProfile?.receiptEmail ?? fallbackEmail);
    setInvoicePersonName(displayCart?.billingDetails?.personName ?? billingProfile?.personName ?? "");
    setInvoiceCompanyName(displayCart?.billingDetails?.companyName ?? billingProfile?.companyName ?? "");
    setInvoiceTaxId(displayCart?.billingDetails?.taxId ?? billingProfile?.taxId ?? "");
    setInvoiceAddressLine1(displayCart?.billingDetails?.billingAddressLine1 ?? billingProfile?.billingAddressLine1 ?? "");
    setInvoiceAddressLine2(displayCart?.billingDetails?.billingAddressLine2 ?? billingProfile?.billingAddressLine2 ?? "");
    setInvoiceCity(displayCart?.billingDetails?.billingCity ?? billingProfile?.billingCity ?? "");
    setInvoicePostalCode(displayCart?.billingDetails?.billingPostalCode ?? billingProfile?.billingPostalCode ?? "");
    setInvoiceCountry(displayCart?.billingDetails?.billingCountry ?? billingProfile?.billingCountry ?? "");
    setSaveInvoiceProfileEnabled(false);
    setConfirmEmail("");
  }, [
    displayCart?.cartId,
    displayCart?.pickupContactName,
    displayCart?.pickupPhone,
    displayCart?.pickupNote,
    displayCart?.scheduledFor,
    displayCart?.deliveryContactName,
    displayCart?.deliveryPhone,
    displayCart?.deliveryAddressLine1,
    displayCart?.deliveryAddressLine2,
    displayCart?.deliveryCity,
    displayCart?.deliveryPostalCode,
    displayCart?.deliveryCountry,
    displayCart?.deliveryNote,
    displayCart?.billingDetails,
    displayCart?.receiptEmail,
    billingProfile,
    showPaymentModal,
    showPlaceOrderModal,
    token,
  ]);

  function clearFieldError(field: string) {
    setValidationErrors((current) => {
      if (!current[field]) {
        return current;
      }

      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  function toggleRemoved(ingredientId: number) {
    setRemovedIngredientIds((current) =>
      current.includes(ingredientId) ? current.filter((value) => value !== ingredientId) : [...current, ingredientId]
    );
  }

  function toggleAdded(ingredientId: number) {
    setAddedIngredientIds((current) =>
      current.includes(ingredientId) ? current.filter((value) => value !== ingredientId) : [...current, ingredientId]
    );
  }

  const orderTypeOptions = useMemo(() => {
    const options: Array<{ value: string; label: string }> = [];
    if (restaurantSettings?.enableTableOrders) {
      options.push({ value: String(OrderType.Table), label: t("mobile.restaurants.toTable", "To table") });
    }
    if (restaurantSettings?.enableTakeawayOrders) {
      options.push({ value: String(OrderType.Takeaway), label: t("mobile.restaurants.pickup", "Pick up") });
    }
    if (restaurantSettings?.enableDeliveryOrders) {
      options.push({ value: String(OrderType.Delivery), label: t("mobile.restaurants.delivery", "Delivery") });
    }
    return options;
  }, [restaurantSettings]);

  const paymentOptions = useMemo(() => {
    const options: Array<{ value: string; label: string }> = [];
    if (displayCart?.orderType === OrderType.Delivery) {
      if (restaurantSettings?.enablePayOnDelivery) {
        options.push({ value: String(PaymentMethod.AtCounter), label: t("restaurant.payOnDelivery", "Pay on delivery") });
      }
    } else if (restaurantSettings?.enablePayAtCounter) {
      options.push({ value: String(PaymentMethod.AtCounter), label: t("restaurant.payAtCounter", "Pay at counter") });
    }
    if (restaurantSettings?.enablePayInApp) {
      options.push({ value: String(PaymentMethod.InApp), label: t("restaurant.payInApp", "Pay in app") });
    }
    return options;
  }, [displayCart?.orderType, restaurantSettings]);

  const tableOptions = useMemo(
    () =>
      tables
        .filter((table) => table.isActive)
        .map((table) => ({
          value: String(table.id),
          label: `${table.label}${table.seats ? ` (${table.seats} seats)` : ""}`,
        })),
    [tables]
  );
  const pickupHourOptions = useMemo(
    () => Array.from({ length: 24 }, (_, index) => ({ value: String(index).padStart(2, "0"), label: String(index).padStart(2, "0") })),
    []
  );
  const pickupMinuteOptions = useMemo(
    () => Array.from({ length: 60 }, (_, index) => ({ value: String(index).padStart(2, "0"), label: String(index).padStart(2, "0") })),
    []
  );
  const pickupHour = pickupTime ? pickupTime.split(":")[0] ?? "" : "";
  const pickupMinute = pickupTime ? pickupTime.split(":")[1] ?? "" : "";

  function choosePickupTimePart(part: "hour" | "minute", value: string) {
    const next = part === "hour"
      ? `${value}:${pickupMinute || "00"}`
      : `${pickupHour || "00"}:${value}`;

    setPickupTime(next);
    clearFieldError("pickupTime");
  }

  async function openRestaurantCart(cartId: number, restaurantId?: number | null) {
    setFocusedCartId(cartId);
    if (restaurantId) {
      await selectRestaurant(restaurantId);
    }
    await openCart(cartId);
  }

  async function removeCartNow(targetCartId: number) {
    await deleteCart(targetCartId);
    setFocusedCartId(null);
    setShowPlaceOrderModal(false);
  }

  async function confirmRemoveCart(targetCartId: number) {
    const confirmed = await confirmMessage(t("cart.removeCart", "Remove cart"), t("cart.removeCartConfirm", "Do you want to remove this cart?"));
    if (!confirmed) {
      return;
    }

    await removeCartNow(targetCartId);
  }

  async function openCustomizationEditor(lineId: number, menuItem: MenuItem, initialRemoved: number[], initialAdded: number[]) {
    const response = await apiRequest<MenuItemCustomization>(`/api/menu/items/${menuItem.id}/customization?lang=${encodeURIComponent(currentCulture)}`);
    setEditingLine({ lineId, menuItem });
    setCustomization(response);
    setRemovedIngredientIds(initialRemoved);
    setAddedIngredientIds(initialAdded);
  }

  async function saveCustomizationEditor() {
    if (!editingLine) {
      return;
    }

    await updateItemCustomization(editingLine.lineId, {
      removedIngredientIds,
      addedIngredientIds,
    });

    setEditingLine(null);
    setCustomization(null);
    setRemovedIngredientIds([]);
    setAddedIngredientIds([]);
  }

  async function completeOrder() {
    try {
      setBusy(true);
      const response = await finalizeCart();
      if (response) {
        await loadActiveCarts();
        const message = selectedPaymentMethod === PaymentMethod.AtCounter
          ? displayCart?.orderType === OrderType.Delivery
            ? `${t("cart.orderPlacedMessage", "Your order")} #${response.orderId} ${t("cart.orderPlacedSuffix", "has been placed.")}\n\n${t("cart.payOnDeliveryHint", "Please be ready to pay on delivery.")} ${t("cart.trackOrderHint", "You can track the status on the My Orders page.")}`
            : `${t("cart.orderPlacedMessage", "Your order")} #${response.orderId} ${t("cart.orderPlacedSuffix", "has been placed.")}\n\n${t("cart.payAtCounterHint", "Please go to the counter and refer to this order number when paying.")} ${t("cart.trackOrderHint", "You can track the status on the My Orders page.")}`
          : `${t("cart.orderPlacedMessage", "Your order")} #${response.orderId} ${t("cart.orderPlacedSuffix", "has been placed.")}\n\n${t("cart.trackOrderHint", "You can track the status on the My Orders page.")}`;
        showMessage(t("cart.orderPlaced", "Order placed"), message);
        onOrderPlaced?.();
        return true;
      }
    } catch (error: any) {
      showMessage(t("cart.placeOrderFailed", "Could not place order"), error.message || t("common.unknownError", "Unknown error"));
    } finally {
      setBusy(false);
    }

    return false;
  }

  function buildBillingDetails() {
    if (billingMode !== "invoice") {
      return null;
    }

    return {
      customerType: invoiceCustomerType === "company" ? 1 : 0,
      invoiceStatus: 1,
      receiptEmail: token ? null : invoiceReceiptEmail || null,
      personName: invoiceCustomerType === "person" ? invoicePersonName || null : null,
      companyName: invoiceCustomerType === "company" ? invoiceCompanyName || null : null,
      taxId: invoiceCustomerType === "company" ? invoiceTaxId || null : null,
      billingAddressLine1: invoiceAddressLine1 || null,
      billingAddressLine2: invoiceAddressLine2 || null,
      billingCity: invoiceCity || null,
      billingPostalCode: invoicePostalCode || null,
      billingCountry: invoiceCountry || null,
    };
  }

  function validateCheckout() {
    const errors: Record<string, string> = {};
    const email = invoiceReceiptEmail.trim();
    const confirm = confirmEmail.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const requiresReceiptEmail = !token;

    if (requiresReceiptEmail && !email) {
      errors.invoiceReceiptEmail = t("validation.emailRequired", "Email address is required.");
    } else if (email && !emailRegex.test(email)) {
      errors.invoiceReceiptEmail = t("validation.emailInvalid", "Email address is invalid.");
    }

    if (requiresReceiptEmail && !confirm) {
      errors.confirmEmail = t("cart.emailConfirmationRequired", "Confirm your email address.");
    } else if (confirm && email !== confirm) {
      errors.confirmEmail = t("cart.emailMismatch", "Email addresses do not match.");
    }

    if (selectedPaymentMethod === undefined || selectedPaymentMethod === null) {
      errors.paymentMethod = t("cart.paymentRequired", "Choose a payment option.");
    }

    if (billingMode === "invoice") {
      if (invoiceCustomerType === "person" && !invoicePersonName.trim()) {
        errors.invoicePersonName = t("cart.invoicePersonNameRequired", "Full name is required for invoice.");
      }

      if (invoiceCustomerType === "company") {
        if (!invoiceCompanyName.trim()) {
          errors.invoiceCompanyName = t("cart.invoiceCompanyRequired", "Company name is required for invoice.");
        }

        if (!invoiceTaxId.trim()) {
          errors.invoiceTaxId = t("cart.invoiceTaxIdRequired", "Tax ID / NIP is required for invoice.");
        } else if (!/^\d+$/.test(invoiceTaxId.trim())) {
          errors.invoiceTaxId = t("cart.invoiceTaxIdDigits", "Tax ID / NIP must contain only digits.");
        }
      }

      if (!invoiceAddressLine1.trim()) {
        errors.invoiceAddressLine1 = t("cart.billingAddressRequired", "Billing address line 1 is required.");
      }
      if (!invoiceCity.trim()) {
        errors.invoiceCity = t("cart.billingCityRequired", "Billing city is required.");
      }
      if (!invoicePostalCode.trim()) {
        errors.invoicePostalCode = t("cart.billingPostalCodeRequired", "Billing postal code is required.");
      }
    }

    addFulfillmentValidationErrors(errors);

    setValidationErrors(errors);
    return errors;
  }

  function addFulfillmentValidationErrors(errors: Record<string, string>) {
    if (displayCart?.orderType === OrderType.Takeaway) {
      if (!pickupContactName.trim()) {
        errors.pickupContactName = t("cart.pickupNameRequired", "Pickup name is required.");
      }
      if (!pickupPhone.trim()) {
        errors.pickupPhone = t("cart.pickupPhoneRequired", "Pickup phone is required.");
      } else if (!/^[+\d\s()-]{6,20}$/.test(pickupPhone.trim())) {
        errors.pickupPhone = t("cart.pickupPhoneInvalid", "Pickup phone looks invalid.");
      }
      if (!pickupTime) {
        errors.pickupTime = t("cart.pickupTimeRequired", "Choose a pickup time.");
      } else {
        const scheduledFor = buildTodayIsoFromTime(pickupTime);
        if (!scheduledFor) {
          errors.pickupTime = t("cart.pickupTimeInvalid", "Choose a valid pickup time.");
        } else if (new Date(scheduledFor).getTime() <= Date.now()) {
          errors.pickupTime = t("cart.pickupTimeFuture", "Pickup time must be later than the current time.");
        }
      }
    }

    if (displayCart?.orderType === OrderType.Delivery) {
      if (!deliveryContactName.trim()) {
        errors.deliveryContactName = t("cart.deliveryContactRequired", "Delivery contact name is required.");
      }
      if (!deliveryPhone.trim()) {
        errors.deliveryPhone = t("cart.deliveryPhoneRequired", "Delivery phone is required.");
      } else if (!/^[+\d\s()-]{6,20}$/.test(deliveryPhone.trim())) {
        errors.deliveryPhone = t("cart.deliveryPhoneInvalid", "Delivery phone looks invalid.");
      }
      if (!deliveryAddressLine1.trim()) {
        errors.deliveryAddressLine1 = t("cart.deliveryAddressRequired", "Delivery address line 1 is required.");
      }
      if (!deliveryCity.trim()) {
        errors.deliveryCity = t("cart.deliveryCityRequired", "Delivery city is required.");
      }
      if (!deliveryPostalCode.trim()) {
        errors.deliveryPostalCode = t("cart.deliveryPostalCodeRequired", "Delivery postal code is required.");
      }
    }
  }

  function openPlaceOrderModal() {
    const errors: Record<string, string> = {};
    addFulfillmentValidationErrors(errors);
    setValidationErrors(errors);

    if (Object.keys(errors).length > 0) {
      showMessage(t("cart.checkoutDetails", "Checkout details"), t("cart.fixHighlighted", "Please fix the highlighted fields before placing the order."));
      return;
    }

    setShowPlaceOrderModal(true);
  }

  async function persistCheckoutMeta() {
    const billingDetails = buildBillingDetails();

    if (displayCart?.orderType === OrderType.Takeaway) {
      const scheduledFor = pickupTime ? buildTodayIsoFromTime(pickupTime) : null;
      await setCartMeta({
        orderType: OrderType.Takeaway,
        pickupContactName,
        pickupPhone,
        pickupNote,
        scheduledFor,
        paymentMethod: selectedPaymentMethod,
        receiptEmail: token ? null : invoiceReceiptEmail.trim() || null,
        billingDetails,
      });
    }

    if (displayCart?.orderType === OrderType.Delivery) {
      await setCartMeta({
        orderType: OrderType.Delivery,
        deliveryContactName,
        deliveryPhone,
        deliveryAddressLine1,
        deliveryAddressLine2,
        deliveryCity,
        deliveryPostalCode,
        deliveryCountry,
        deliveryNote,
        paymentMethod: selectedPaymentMethod,
        receiptEmail: token ? null : invoiceReceiptEmail.trim() || null,
        billingDetails,
      });
    }

    if (displayCart?.orderType === OrderType.Table) {
      await setCartMeta({
        orderType: OrderType.Table,
        restaurantTableId: displayCart.restaurantTableId ?? null,
        paymentMethod: selectedPaymentMethod,
        receiptEmail: token ? null : invoiceReceiptEmail.trim() || null,
        billingDetails,
      });
    }

    if (billingMode === "invoice" && saveInvoiceProfileEnabled && token && billingDetails) {
      await saveBillingProfile(billingDetails);
    }
  }

  async function beginOrderPlacement() {
    try {
      const errors = validateCheckout();
      if (Object.keys(errors).length > 0) {
        showMessage(t("cart.checkoutDetails", "Checkout details"), t("cart.fixHighlighted", "Please fix the highlighted fields before placing the order."));
        return;
      }

      await persistCheckoutMeta();

      if (selectedPaymentMethod === PaymentMethod.InApp) {
        setShowPaymentModal(true);
        return;
      }

      if (await completeOrder()) {
        setShowPlaceOrderModal(false);
        setFocusedCartId(null);
      }
    } catch (error: any) {
      showMessage(
        t("cart.checkoutDetails", "Checkout details"),
        error?.message || t("common.unknownError", "Unknown error")
      );
    }
  }

  async function confirmMockPayment() {
    if (!mockCardName.trim() || !/^\d{12,19}$/.test(mockCardNumber.replace(/\D+/g, "")) || !/^\d{2}\/\d{2}$/.test(mockCardExpiry.trim())) {
      showMessage(t("cart.paymentDetails", "Payment details"), t("cart.paymentDetailsHint", "Enter a card name, a numeric card number, and expiry in MM/YY format."));
      return;
    }

    setShowPaymentModal(false);
    if (await completeOrder()) {
      setShowPlaceOrderModal(false);
      setFocusedCartId(null);
    }
  }

  function withRequired(label: string, required?: boolean) {
    return required ? `${label} *` : label;
  }

  function formatCardNumber(value: string) {
    const digits = value.replace(/\D+/g, "").slice(0, 16);
    return digits.replace(/(\d{4})(?=\d)/g, "$1-");
  }

  function formatCardExpiry(value: string) {
    const digits = value.replace(/\D+/g, "").slice(0, 4);
    if (digits.length <= 2) {
      return digits;
    }

    return `${digits.slice(0, 2)}/${digits.slice(2)}`;
  }

  function inputErrorStyle(field: string) {
    return validationErrors[field] ? { borderColor: "#dc2626" } : null;
  }

  function FieldError({ field }: { field: string }) {
    if (!validationErrors[field]) {
      return null;
    }

    return <Text style={sharedStyles.fieldError}>{validationErrors[field]}</Text>;
  }

  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <Text style={sharedStyles.pageTitle}>{t("nav.cart", "Cart")}</Text>
      <Text style={sharedStyles.themeMutedComfortable}>
        {token
          ? t("cart.maxActiveHint", "You can keep up to 4 active carts, one per restaurant.")
          : t("cart.guestMaxActiveHint", "You can keep a maximum of 1 active cart.")}
      </Text>

      {activeCarts.length > 1 && !displayCart ? (
        <ActiveCartPicker
          activeCarts={activeCarts}
          onOpenCart={(cartId, restaurantId) => void openRestaurantCart(cartId, restaurantId)}
          onRemoveCart={(cartId) => void confirmRemoveCart(cartId)}
          t={t}
        />
      ) : null}

      {!displayCart ? (
        <EmptyCartState hasMultipleCarts={activeCarts.length > 1} t={t} />
      ) : (
        <>
          {activeCarts.length > 1 ? (
            <PrimaryButton
              label={t("cart.backToCarts", "Back to carts")}
              onPress={() => {
                setFocusedCartId(null);
                setShowPlaceOrderModal(false);
              }}
            />
          ) : null}

          <CartDetailsSection
            cart={displayCart}
            selectedRestaurant={selectedRestaurant}
            tables={tables}
            orderTypeOptions={orderTypeOptions}
            tableOptions={tableOptions}
            pickupContactName={pickupContactName}
            pickupPhone={pickupPhone}
            pickupNote={pickupNote}
            pickupHour={pickupHour}
            pickupMinute={pickupMinute}
            pickupHourOptions={pickupHourOptions}
            pickupMinuteOptions={pickupMinuteOptions}
            deliveryContactName={deliveryContactName}
            deliveryPhone={deliveryPhone}
            deliveryAddressLine1={deliveryAddressLine1}
            deliveryAddressLine2={deliveryAddressLine2}
            deliveryCity={deliveryCity}
            deliveryPostalCode={deliveryPostalCode}
            deliveryCountry={deliveryCountry}
            deliveryNote={deliveryNote}
            onOrderTypeChange={(nextOrderType) => {
              void setCartMeta({
                orderType: nextOrderType,
                restaurantTableId:
                  nextOrderType === OrderType.Table
                    ? displayCart.restaurantTableId ?? tables.find((table) => table.isActive)?.id ?? null
                    : null,
              });
            }}
            onTableChange={(tableId) => {
              void setCartMeta({
                orderType: OrderType.Table,
                restaurantTableId: tableId,
              });
            }}
            onPickupContactNameChange={(value) => {
              setPickupContactName(value);
              clearFieldError("pickupContactName");
            }}
            onPickupPhoneChange={(value) => {
              setPickupPhone(value);
              clearFieldError("pickupPhone");
            }}
            onPickupNoteChange={setPickupNote}
            onPickupTimePartChange={choosePickupTimePart}
            onDeliveryContactNameChange={(value) => {
              setDeliveryContactName(value);
              clearFieldError("deliveryContactName");
            }}
            onDeliveryPhoneChange={(value) => {
              setDeliveryPhone(value);
              clearFieldError("deliveryPhone");
            }}
            onDeliveryAddressLine1Change={(value) => {
              setDeliveryAddressLine1(value);
              clearFieldError("deliveryAddressLine1");
            }}
            onDeliveryAddressLine2Change={setDeliveryAddressLine2}
            onDeliveryCityChange={(value) => {
              setDeliveryCity(value);
              clearFieldError("deliveryCity");
            }}
            onDeliveryPostalCodeChange={(value) => {
              setDeliveryPostalCode(value);
              clearFieldError("deliveryPostalCode");
            }}
            onDeliveryCountryChange={setDeliveryCountry}
            onDeliveryNoteChange={setDeliveryNote}
            onRemoveCart={() => void confirmRemoveCart(displayCart.cartId)}
            inputErrorStyle={inputErrorStyle}
            FieldError={FieldError}
            withRequired={withRequired}
            t={t}
          />

          <CartItemsSection
            cart={displayCart}
            itemMap={itemMap}
            onCustomizeLine={(lineId, menuItem, initialRemoved, initialAdded) => {
              void openCustomizationEditor(lineId, menuItem, initialRemoved, initialAdded).catch((error: any) => {
                showMessage(t("menu.customizationUnavailable", "Customization unavailable"), error?.message || t("common.unknownError", "Unknown error"));
              });
            }}
            onDecreaseQuantity={(lineId, quantity) => {
              void (async () => {
                if (displayCart.items.length === 1 && quantity === 1) {
                  const confirmed = await confirmMessage(
                    t("cart.removeLastItem", "Remove last item"),
                    t("cart.removeLastItemConfirm", "This is the last item in the cart. Do you want to remove the whole cart?")
                  );
                  if (confirmed) {
                    await removeCartNow(displayCart.cartId);
                  }
                  return;
                }

                await setItemQuantity(lineId, quantity - 1);
              })();
            }}
            onIncreaseQuantity={(lineId, quantity) => void setItemQuantity(lineId, quantity + 1)}
            t={t}
          />

          <CartTotalsSection preview={preview} t={t} />

          <PrimaryButton
            label={t("cart.placeOrder", "Place order")}
            onPress={openPlaceOrderModal}
            disabled={busy || !displayCart.items.length}
          />
        </>
      )}

      <PlaceOrderModal
        visible={showPlaceOrderModal}
        busy={busy}
        cart={displayCart}
        selectedRestaurant={selectedRestaurant}
        tables={tables}
        itemMap={itemMap}
        token={token}
        preview={preview}
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
        paymentOptions={paymentOptions}
        selectedPaymentMethod={selectedPaymentMethod}
        billingMode={billingMode}
        invoiceCustomerType={invoiceCustomerType}
        invoiceReceiptEmail={invoiceReceiptEmail}
        confirmEmail={confirmEmail}
        invoicePersonName={invoicePersonName}
        invoiceCompanyName={invoiceCompanyName}
        invoiceTaxId={invoiceTaxId}
        invoiceAddressLine1={invoiceAddressLine1}
        invoiceAddressLine2={invoiceAddressLine2}
        invoiceCity={invoiceCity}
        invoicePostalCode={invoicePostalCode}
        invoiceCountry={invoiceCountry}
        saveInvoiceProfileEnabled={saveInvoiceProfileEnabled}
        onSelectedPaymentMethodChange={setSelectedPaymentMethod}
        onBillingModeChange={setBillingMode}
        onInvoiceCustomerTypeChange={setInvoiceCustomerType}
        onInvoiceReceiptEmailChange={setInvoiceReceiptEmail}
        onConfirmEmailChange={setConfirmEmail}
        onInvoicePersonNameChange={setInvoicePersonName}
        onInvoiceCompanyNameChange={setInvoiceCompanyName}
        onInvoiceTaxIdChange={setInvoiceTaxId}
        onInvoiceAddressLine1Change={setInvoiceAddressLine1}
        onInvoiceAddressLine2Change={setInvoiceAddressLine2}
        onInvoiceCityChange={setInvoiceCity}
        onInvoicePostalCodeChange={setInvoicePostalCode}
        onInvoiceCountryChange={setInvoiceCountry}
        onSaveInvoiceProfileEnabledChange={setSaveInvoiceProfileEnabled}
        clearFieldError={clearFieldError}
        inputErrorStyle={inputErrorStyle}
        FieldError={FieldError}
        withRequired={withRequired}
        buildTodayIsoFromTime={buildTodayIsoFromTime}
        onConfirm={() => void beginOrderPlacement()}
        onCancel={() => setShowPlaceOrderModal(false)}
        t={t}
      />

      <PaymentModal
        visible={showPaymentModal}
        busy={busy}
        cardName={mockCardName}
        cardNumber={mockCardNumber}
        cardExpiry={mockCardExpiry}
        preview={preview}
        onCardNameChange={setMockCardName}
        onCardNumberChange={(value) => setMockCardNumber(formatCardNumber(value))}
        onCardExpiryChange={(value) => setMockCardExpiry(formatCardExpiry(value))}
        onConfirm={() => void confirmMockPayment()}
        onCancel={() => setShowPaymentModal(false)}
        t={t}
      />

      <IngredientCustomizationModal
        visible={!!editingLine && !!customization}
        title={editingLine?.menuItem.name ?? ""}
        basePrice={editingLine?.menuItem.currentPrice ?? 0}
        customization={customization}
        removedIngredientIds={removedIngredientIds}
        addedIngredientIds={addedIngredientIds}
        onToggleRemoved={toggleRemoved}
        onToggleAdded={toggleAdded}
        onConfirm={async () => {
          try {
            await saveCustomizationEditor();
          } catch (error: any) {
            showMessage(
              t("menu.customizationSaveFailed", "Could not update ingredients"),
              error?.message || t("common.unknownError", "Unknown error")
            );
          }
        }}
        onCancel={() => {
          setEditingLine(null);
          setCustomization(null);
          setRemovedIngredientIds([]);
          setAddedIngredientIds([]);
        }}
        t={t}
      />
    </ScrollView>
  );
}
