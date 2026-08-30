import { StyleSheet } from "react-native";

export const theme = {
  colors: {
    background: "#f4f6f8",
    surface: "#ffffff",
    surfaceMuted: "#f8fafc",
    ink: "#172033",
    inkMuted: "#667085",
    border: "#d9e0e8",
    accent: "#f97316",
    accentPressed: "#ea580c",
    accentSoft: "#fff1e6",
    navy: "#172033",
    navySoft: "#e8eef6",
    success: "#16803c",
    successSoft: "#e9f7ee",
    danger: "#c7362f",
    dangerSoft: "#fff0ef",
    info: "#2563a9",
  },
  radius: { small: 8, medium: 8 },
  spacing: { page: 16, section: 16, gap: 12 },
} as const;

export const inputStyle = {
  borderWidth: 1,
  borderColor: theme.colors.border,
  borderRadius: theme.radius.small,
  paddingHorizontal: 13,
  paddingVertical: 12,
  backgroundColor: theme.colors.surface,
  color: theme.colors.ink,
} as const;

export const sharedStyles = StyleSheet.create({
  screen: {
    backgroundColor: theme.colors.background,
  },
  screenContent: {
    padding: theme.spacing.page,
    gap: 14,
  },
  centerFill: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
  },
  pageTitle: {
    fontSize: 30,
    fontWeight: "700",
    color: theme.colors.ink,
  },
  pageTitleCompact: {
    fontSize: 28,
    fontWeight: "700",
    color: theme.colors.ink,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: "700",
    color: theme.colors.ink,
  },
  title: {
    fontWeight: "700",
    color: theme.colors.ink,
  },
  semibold: {
    fontWeight: "600",
  },
  mutedText: {
    color: "#6b7280",
  },
  mutedTextComfortable: {
    color: "#6b7280",
    lineHeight: 20,
  },
  bodyMuted: {
    color: "#4b5563",
  },
  bodyMutedComfortable: {
    color: "#4b5563",
    lineHeight: 20,
  },
  themeMutedText: {
    color: theme.colors.inkMuted,
  },
  themeMutedComfortable: {
    color: theme.colors.inkMuted,
    lineHeight: 20,
  },
  infoText: {
    color: theme.colors.info,
  },
  errorText: {
    color: "#dc2626",
  },
  fieldError: {
    color: "#dc2626",
    marginTop: -4,
  },
  stackXs: {
    gap: 2,
  },
  stackSm: {
    gap: 4,
  },
  stackMd: {
    gap: 8,
  },
  stackLg: {
    gap: 10,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
  },
  rowSm: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
  },
  rowTop: {
    flexDirection: "row",
    gap: 8,
  },
  rowWrap: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
  },
  rowBetween: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: 12,
  },
  flexOne: {
    flex: 1,
  },
  dividerTop: {
    borderTopWidth: 1,
    borderTopColor: "#f3f4f6",
    paddingTop: 8,
  },
  dividerBottom: {
    borderBottomWidth: 1,
    borderBottomColor: "#f3f4f6",
    paddingBottom: 10,
  },
  modalBackdrop: {
    flex: 1,
    justifyContent: "center",
    padding: 16,
    backgroundColor: "rgba(23,32,51,0.48)",
  },
  modalCard: {
    backgroundColor: theme.colors.surface,
    borderRadius: theme.radius.medium,
    padding: 18,
    gap: 12,
    maxHeight: "88%",
  },
  modalCardCompact: {
    backgroundColor: theme.colors.surface,
    borderRadius: theme.radius.medium,
    padding: 16,
    gap: 12,
    maxHeight: "80%",
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: "700",
    color: theme.colors.ink,
  },
  modalText: {
    color: theme.colors.inkMuted,
    lineHeight: 22,
  },
  modalActions: {
    flexDirection: "row",
    justifyContent: "flex-end",
    gap: 10,
  },
  framedPanel: {
    borderWidth: 1,
    borderColor: "#e5e7eb",
    borderRadius: theme.radius.medium,
    padding: 12,
  },
});
