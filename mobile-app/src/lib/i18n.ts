import i18next from "i18next";

export const appI18n = i18next.createInstance();

void appI18n.init({
  lng: "pl-PL",
  fallbackLng: "pl-PL",
  resources: {},
  keySeparator: false,
  interpolation: { escapeValue: false },
});
