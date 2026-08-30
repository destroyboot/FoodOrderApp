import { Modal, Pressable, Text, TextInput } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { CartPreview } from "../../types/api";
import type { TFunction } from "./types";

type Props = {
  visible: boolean;
  busy: boolean;
  cardName: string;
  cardNumber: string;
  cardExpiry: string;
  preview: CartPreview | null;
  onCardNameChange: (value: string) => void;
  onCardNumberChange: (value: string) => void;
  onCardExpiryChange: (value: string) => void;
  onConfirm: () => void;
  onCancel: () => void;
  t: TFunction;
};

export function PaymentModal({
  visible,
  busy,
  cardName,
  cardNumber,
  cardExpiry,
  preview,
  onCardNameChange,
  onCardNumberChange,
  onCardExpiryChange,
  onConfirm,
  onCancel,
  t,
}: Props) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable onPress={onCancel} style={sharedStyles.modalBackdrop}>
        <Pressable onPress={() => undefined} style={sharedStyles.modalCard}>
          <Text style={sharedStyles.modalTitle}>{t("cart.mockPayment", "Mock Payment")}</Text>
          <Text style={sharedStyles.bodyMuted}>
            {t("cart.mockPaymentHint", "This is a demo payment page. Enter mock card details to continue.")}
          </Text>
          <TextInput placeholder={t("cart.cardholderName", "Cardholder name")} value={cardName} onChangeText={onCardNameChange} style={inputStyle} />
          <TextInput
            placeholder={t("cart.cardNumber", "Card number")}
            value={cardNumber}
            onChangeText={onCardNumberChange}
            style={inputStyle}
            keyboardType="number-pad"
          />
          <TextInput
            placeholder={t("cart.expiry", "Expiry (MM/YY)")}
            value={cardExpiry}
            onChangeText={onCardExpiryChange}
            style={inputStyle}
            keyboardType="number-pad"
          />
          {preview ? <Text style={sharedStyles.title}>{t("cart.chargeAmount", "Charge amount")}: {preview.total.toFixed(2)}</Text> : null}
          <PrimaryButton label={busy ? t("cart.processing", "Processing...") : t("cart.payNow", "Pay now")} onPress={onConfirm} disabled={busy} />
          <PrimaryButton label={t("cart.cancelPayment", "Cancel payment")} onPress={onCancel} />
        </Pressable>
      </Pressable>
    </Modal>
  );
}
