import { Text, TextInput, View } from "react-native";
import { PickerField } from "../../components/PickerField";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { inputStyle, sharedStyles } from "../../lib/theme";
import type { Restaurant, RestaurantSettings, ReservationAvailability } from "../../types/api";
import type { TFunction } from "./types";

type PickerOption = {
  value: string;
  label: string;
};

type Props = {
  selectedRestaurant: Restaurant | null;
  restaurantSettings: RestaurantSettings | null;
  reservationAvailability: ReservationAvailability | null;
  date: string;
  partySize: string;
  selectedTableId: string;
  selectedStartTime: string;
  note: string;
  busy: boolean;
  dateOptions: PickerOption[];
  partySizeOptions: PickerOption[];
  tableOptions: PickerOption[];
  slotOptions: PickerOption[];
  pooledSlotOptions: PickerOption[];
  onDateChange: (value: string) => void;
  onPartySizeChange: (value: string) => void;
  onTableChange: (value: string) => void;
  onStartTimeChange: (value: string) => void;
  onNoteChange: (value: string) => void;
  onSubmit: () => void;
  t: TFunction;
};

export function ReservationBookingForm({
  selectedRestaurant,
  restaurantSettings,
  reservationAvailability,
  date,
  partySize,
  selectedTableId,
  selectedStartTime,
  note,
  busy,
  dateOptions,
  partySizeOptions,
  tableOptions,
  slotOptions,
  pooledSlotOptions,
  onDateChange,
  onPartySizeChange,
  onTableChange,
  onStartTimeChange,
  onNoteChange,
  onSubmit,
  t,
}: Props) {
  const tableSelectionEnabled = restaurantSettings?.allowUserTableSelectionForReservations !== false;
  const availableSlotOptions = tableSelectionEnabled ? slotOptions : pooledSlotOptions;

  return (
    <SectionCard>
      <View style={sharedStyles.stackLg}>
        <Text style={sharedStyles.sectionTitle}>{t("mobile.reservations.bookFor", "Book for")} {selectedRestaurant?.name ?? t("restaurant.current", "selected restaurant")}</Text>
        <Text style={sharedStyles.bodyMuted}>
          {selectedRestaurant
            ? t("mobile.reservations.pickHint", "Pick a date, choose a table and available slot, then send a reservation request.")
            : t("mobile.reservations.chooseRestaurantFirst", "Choose a restaurant from the selector first.")}
        </Text>

        {selectedRestaurant && restaurantSettings?.enableReservations === false ? (
          <Text style={{ color: "#92400e" }}>{t("mobile.reservations.notAccepting", "This restaurant is not accepting reservations right now.")}</Text>
        ) : null}

        <PickerField
          label={t("mobile.reservations.date", "Reservation date")}
          placeholder={t("mobile.reservations.chooseDate", "Choose date")}
          value={date}
          options={dateOptions}
          onChange={onDateChange}
          disabled={!selectedRestaurant || restaurantSettings?.enableReservations === false}
        />

        {reservationAvailability ? (
          <>
            <PickerField
              label={t("mobile.reservations.people", "People")}
              placeholder={t("mobile.reservations.choosePartySize", "Choose party size")}
              value={partySize}
              options={partySizeOptions}
              onChange={onPartySizeChange}
              disabled={busy}
            />

            {tableSelectionEnabled ? (
              <PickerField
                label={t("mobile.reservations.table", "Table")}
                placeholder={busy ? t("mobile.reservations.loadingTables", "Loading tables...") : t("mobile.reservations.chooseTable", "Choose table")}
                value={selectedTableId}
                options={tableOptions}
                onChange={onTableChange}
                disabled={busy || tableOptions.length === 0}
              />
            ) : null}

            <PickerField
              label={t("mobile.reservations.slot", "Available slot")}
              placeholder={!tableSelectionEnabled || selectedTableId ? t("mobile.reservations.chooseSlot", "Choose slot") : t("mobile.reservations.chooseTableFirst", "Choose table first")}
              value={selectedStartTime}
              options={availableSlotOptions}
              onChange={onStartTimeChange}
              disabled={(tableSelectionEnabled && !selectedTableId) || availableSlotOptions.length === 0}
            />

            <TextInput
              placeholder={t("mobile.reservations.optionalNote", "Optional note for the restaurant")}
              value={note}
              onChangeText={onNoteChange}
              multiline
              style={[inputStyle, { minHeight: 88, textAlignVertical: "top" }]}
            />

            <PrimaryButton
              label={busy ? t("mobile.reservations.sending", "Sending...") : t("mobile.reservations.send", "Send reservation request")}
              onPress={onSubmit}
              disabled={busy || !selectedStartTime || (tableSelectionEnabled && !selectedTableId)}
            />

            {!busy && tableOptions.length === 0 ? (
              <Text style={sharedStyles.mutedText}>{t("mobile.reservations.noSlots", "No reservation slots are available for this date.")}</Text>
            ) : null}
          </>
        ) : null}
      </View>
    </SectionCard>
  );
}
