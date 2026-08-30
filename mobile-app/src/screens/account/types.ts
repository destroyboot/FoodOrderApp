export type GuestMode = "login" | "register" | "confirm";

export type SignedInMode = "menu" | "invoice" | "delivery" | "password" | "remove";

export type TFunction = (key: string, fallback: string) => string;
