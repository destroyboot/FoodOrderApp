import { Text, View } from "react-native";
import { PrimaryButton } from "../../components/PrimaryButton";
import { SectionCard } from "../../components/SectionCard";
import { sharedStyles } from "../../lib/theme";
import { Reservation, ReservationStatus } from "../../types/api";
import type { TFunction } from "./types";

export function formatReservationStatus(status: ReservationStatus, t: TFunction) {
  switch (status) {
    case ReservationStatus.Requested:
      return t("mobile.reservations.status.requested", "Requested");
    case ReservationStatus.Confirmed:
      return t("mobile.reservations.status.confirmed", "Confirmed");
    case ReservationStatus.Seated:
      return t("mobile.reservations.status.seated", "Seated");
    case ReservationStatus.Completed:
      return t("mobile.reservations.status.completed", "Completed");
    case ReservationStatus.Cancelled:
      return t("mobile.reservations.status.cancelled", "Cancelled");
    case ReservationStatus.NoShow:
      return t("mobile.reservations.status.noShow", "No show");
    default:
      return t("nav.reservations", "Reservations");
  }
}

export function canCancelReservation(status: ReservationStatus) {
  return ![
    ReservationStatus.Cancelled,
    ReservationStatus.Completed,
    ReservationStatus.NoShow,
  ].includes(status);
}

type Props = {
  reservations: Reservation[];
  restaurantNameById: Record<number, string>;
  currentCulture: string;
  busy: boolean;
  onCancelReservation: (reservationId: number) => void;
  t: TFunction;
};

export function ReservationListSection({
  reservations,
  restaurantNameById,
  currentCulture,
  busy,
  onCancelReservation,
  t,
}: Props) {
  return (
    <SectionCard>
      <View style={sharedStyles.stackMd}>
        <Text style={sharedStyles.sectionTitle}>{t("mobile.reservations.activeReservations", "Active reservations")}</Text>
        {reservations.length === 0 ? (
          <Text style={sharedStyles.mutedText}>{t("common.none", "None")}</Text>
        ) : (
          reservations.map((reservation) => (
            <View key={reservation.id} style={[sharedStyles.dividerTop, sharedStyles.stackSm]}>
              <Text style={sharedStyles.title}>
                {reservation.restaurantName ?? restaurantNameById[reservation.restaurantId] ?? `Restaurant #${reservation.restaurantId}`}
              </Text>
              <Text>{new Date(reservation.startAt).toLocaleString(currentCulture)}</Text>
              <Text>
                {reservation.tableLabel ?? "-"} | {formatReservationStatus(reservation.status, t)}
              </Text>
              {reservation.note ? <Text style={sharedStyles.mutedText}>{t("mobile.reservations.note", "Note")}: {reservation.note}</Text> : null}
              {canCancelReservation(reservation.status) ? (
                <PrimaryButton label={t("mobile.reservations.cancel", "Cancel reservation")} onPress={() => onCancelReservation(reservation.id)} disabled={busy} />
              ) : null}
            </View>
          ))
        )}
      </View>
    </SectionCard>
  );
}
