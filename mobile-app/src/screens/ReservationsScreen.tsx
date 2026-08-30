import { useEffect, useMemo, useState } from "react";
import { ScrollView, Text, View } from "react-native";
import { PrimaryButton } from "../components/PrimaryButton";
import { SectionCard } from "../components/SectionCard";
import { useAppSession } from "../context/AppSessionContext";
import { confirmMessage, showMessage } from "../lib/dialogs";
import { ReservationStatus } from "../types/api";
import { sharedStyles } from "../lib/theme";
import { ReservationBookingForm } from "./reservations/ReservationBookingForm";
import { ReservationListSection } from "./reservations/ReservationListSection";

function getTodayDateString() {
  return formatDateOption(new Date());
}

function formatDateOption(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function ReservationsScreen() {
  const {
    token,
    restaurants,
    selectedRestaurant,
    restaurantSettings,
    currentCulture,
    reservationAvailability,
    reservations,
    loadReservationAvailability,
    createReservation,
    cancelReservation,
    loadMyReservations,
    t,
  } = useAppSession();
  const [mode, setMode] = useState<"list" | "book">("list");
  const [date, setDate] = useState(getTodayDateString());
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [partySize, setPartySize] = useState("2");
  const [selectedTableId, setSelectedTableId] = useState("");
  const [selectedStartTime, setSelectedStartTime] = useState("");

  useEffect(() => {
    if (token) {
      void loadMyReservations();
    }
  }, [token]);

  useEffect(() => {
    if (mode !== "book" || !token || !selectedRestaurant || restaurantSettings?.enableReservations === false) {
      return;
    }

    let cancelled = false;
    setBusy(true);
    setSelectedTableId("");
    setSelectedStartTime("");

    void loadReservationAvailability(selectedRestaurant.id, date)
      .catch((error: any) => {
        if (!cancelled) {
          showMessage(t("mobile.reservations.loadFailedTitle", "Could not load availability"), error?.message || t("common.unknownError", "Unknown error"));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setBusy(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [mode, token, selectedRestaurant?.id, date, restaurantSettings?.enableReservations]);

  const restaurantNameById = useMemo(
    () => Object.fromEntries(restaurants.map((restaurant) => [restaurant.id, restaurant.name])),
    [restaurants]
  );
  const activeReservations = useMemo(
    () => reservations.filter((reservation) => ![ReservationStatus.Cancelled, ReservationStatus.Completed, ReservationStatus.NoShow].includes(reservation.status)),
    [reservations]
  );

  const dateOptions = useMemo(
    () =>
      Array.from({ length: 30 }, (_, index) => {
        const dateValue = new Date();
        dateValue.setDate(dateValue.getDate() + index);
        const value = formatDateOption(dateValue);
        return {
          value,
          label: dateValue.toLocaleDateString(currentCulture, {
            weekday: "short",
            year: "numeric",
            month: "short",
            day: "numeric",
          }),
        };
      }),
    []
  );

  const selectedTable = useMemo(
    () => reservationAvailability?.tables.find((table) => String(table.tableId) === selectedTableId) ?? null,
    [reservationAvailability, selectedTableId]
  );

  const tableOptions = useMemo(
    () =>
      (reservationAvailability?.tables ?? [])
        .filter((table) => table.availableStartTimes.length > 0)
        .map((table) => ({
          value: String(table.tableId),
          label: `${table.label}${table.seats ? ` (${table.seats} seats)` : ""}`,
        })),
    [reservationAvailability]
  );

  const slotOptions = useMemo(
    () => (selectedTable?.availableStartTimes ?? []).map((slot) => ({ value: slot, label: slot })),
    [selectedTable]
  );
  const pooledSlotOptions = useMemo(
    () =>
      Array.from(new Set((reservationAvailability?.tables ?? []).flatMap((table) => table.availableStartTimes)))
        .sort()
        .map((slot) => ({ value: slot, label: slot })),
    [reservationAvailability]
  );
  const partySizeOptions = useMemo(
    () => Array.from({ length: 10 }, (_, index) => ({ value: String(index + 1), label: String(index + 1) })).concat([{ value: "10", label: "10+" }]),
    []
  );

  async function handleCreateReservation() {
    if (!selectedRestaurant || !selectedStartTime || (restaurantSettings?.allowUserTableSelectionForReservations !== false && !selectedTableId)) {
      showMessage(t("mobile.reservations.chooseSlotTitle", "Choose slot"), t("mobile.reservations.chooseSlotBody", "Pick the reservation details first."));
      return;
    }

    const startAt = new Date(`${date}T${selectedStartTime}:00`).toISOString();
    const confirmed = await confirmMessage(
      t("mobile.reservations.confirmTitle", "Confirm reservation"),
      `${t("mobile.reservations.confirmQuestion", "Send reservation request to")} ${selectedRestaurant.name} ${t("mobile.reservations.at", "at")} ${selectedStartTime}?`
    );
    if (!confirmed) {
      return;
    }

    try {
      setBusy(true);
      await createReservation({
        restaurantId: selectedRestaurant.id,
        restaurantTableId: selectedTableId ? Number(selectedTableId) : null,
        partySize: Math.max(1, Number(partySize) || 1),
        startAt,
        note: note.trim() || null,
      });
      await Promise.all([
        loadMyReservations(),
        loadReservationAvailability(selectedRestaurant.id, date),
      ]);
      setNote("");
      setSelectedTableId("");
      setSelectedStartTime("");
      setMode("list");
      showMessage(t("mobile.reservations.requestedTitle", "Reservation requested"), t("mobile.reservations.requestedBody", "The restaurant received your reservation request."));
    } catch (error: any) {
      showMessage(t("mobile.reservations.createFailed", "Could not create reservation"), error?.message || t("common.unknownError", "Unknown error"));
    } finally {
      setBusy(false);
    }
  }

  async function handleCancelReservation(reservationId: number) {
    const confirmed = await confirmMessage(
      t("mobile.reservations.cancelTitle", "Cancel reservation"),
      t("mobile.reservations.cancelBody", "Are you sure you want to cancel this reservation?")
    );
    if (!confirmed) {
      return;
    }

    try {
      setBusy(true);
      await cancelReservation(reservationId);
      if (selectedRestaurant) {
        await loadReservationAvailability(selectedRestaurant.id, date);
      }
      showMessage(t("mobile.reservations.cancelledTitle", "Reservation cancelled"), t("mobile.reservations.cancelledBody", "The table has been released."));
    } catch (error: any) {
      showMessage(t("mobile.reservations.cancelFailed", "Could not cancel reservation"), error?.message || t("common.unknownError", "Unknown error"));
    } finally {
      setBusy(false);
    }
  }

  if (!token) {
    return (
      <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
        <Text style={sharedStyles.pageTitle}>{t("mobile.reservations.title", "Reservations")}</Text>
        <SectionCard>
          <View style={sharedStyles.stackMd}>
            <Text style={sharedStyles.title}>{t("mobile.reservations.signInRequired", "Sign in required")}</Text>
            <Text style={sharedStyles.mutedText}>
              {t("mobile.reservations.signInHint", "Reservations are available only for logged in users. Create an account or log in from the Account tab.")}
            </Text>
          </View>
        </SectionCard>
      </ScrollView>
    );
  }

  return (
    <ScrollView style={sharedStyles.screen} contentContainerStyle={sharedStyles.screenContent}>
      <Text style={sharedStyles.pageTitle}>{t("mobile.reservations.title", "Reservations")}</Text>

      {mode === "list" ? (
        <>
          {selectedRestaurant && restaurantSettings?.enableReservations === false ? (
            <SectionCard>
              <Text style={{ color: "#92400e" }}>{t("mobile.reservations.notAccepting", "This restaurant is not accepting reservations right now.")}</Text>
            </SectionCard>
          ) : (
            <PrimaryButton label={t("mobile.reservations.bookTable", "Book a table")} onPress={() => setMode("book")} />
          )}

          <ReservationListSection
            reservations={activeReservations}
            restaurantNameById={restaurantNameById}
            currentCulture={currentCulture}
            busy={busy}
            onCancelReservation={(reservationId) => void handleCancelReservation(reservationId)}
            t={t}
          />
        </>
      ) : null}

      {mode === "book" ? (
        <>
          <PrimaryButton label={t("mobile.reservations.back", "Back to reservations")} onPress={() => setMode("list")} />
          <ReservationBookingForm
            selectedRestaurant={selectedRestaurant}
            restaurantSettings={restaurantSettings}
            reservationAvailability={reservationAvailability}
            date={date}
            partySize={partySize}
            selectedTableId={selectedTableId}
            selectedStartTime={selectedStartTime}
            note={note}
            busy={busy}
            dateOptions={dateOptions}
            partySizeOptions={partySizeOptions}
            tableOptions={tableOptions}
            slotOptions={slotOptions}
            pooledSlotOptions={pooledSlotOptions}
            onDateChange={setDate}
            onPartySizeChange={setPartySize}
            onTableChange={(value) => {
              setSelectedTableId(value);
              setSelectedStartTime("");
            }}
            onStartTimeChange={setSelectedStartTime}
            onNoteChange={setNote}
            onSubmit={() => void handleCreateReservation()}
            t={t}
          />
        </>
      ) : null}
    </ScrollView>
  );
}
