import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { api, getApiOrigin } from "./api";
import { getToken } from "./auth";

type AppLanguage = {
  id: number;
  culture: string;
  displayName: string;
  nativeName: string;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
};

type AppLanguagePayload = {
  defaultCulture: string;
  languages: AppLanguage[];
};

type AppTextDictionary = {
  culture: string;
  defaultCulture: string;
  texts: Record<string, string>;
};

type I18nContextValue = {
  languages: AppLanguage[];
  culture: string;
  t: (key: string, fallback: string) => string;
  setCulture: (culture: string) => Promise<void>;
  reloadTexts: () => Promise<void>;
  apiOrigin: string;
};

const i18n = i18next.createInstance();
void i18n.use(initReactI18next).init({
  lng: "pl-PL",
  fallbackLng: "pl-PL",
  resources: {},
  keySeparator: false,
  interpolation: { escapeValue: false },
});

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [languages, setLanguages] = useState<AppLanguage[]>([]);
  const [culture, setCultureState] = useState("pl-PL");
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    void bootstrap();
  }, []);

  async function bootstrap() {
    const payload = await fetchLanguages();
    let nextCulture = payload.defaultCulture || payload.languages[0]?.culture || "pl-PL";

    if (getToken()) {
      try {
        const preference = await api<{ culture?: string | null }>("/api/account/preferences/culture");
        if (preference.culture) {
          nextCulture = preference.culture;
        }
      } catch {
      }
    }

    await loadTexts(nextCulture);
  }

  async function fetchLanguages() {
    const response = await fetch(`${getApiOrigin()}/api/platform/languages`);
    const payload = (await response.json()) as AppLanguagePayload;
    setLanguages(payload.languages ?? []);
    return payload;
  }

  async function loadTexts(nextCulture: string) {
    const response = await fetch(`${getApiOrigin()}/api/platform/texts?culture=${encodeURIComponent(nextCulture)}`);
    const payload = (await response.json()) as AppTextDictionary;
    const resourceCulture = payload.culture || nextCulture;

    i18n.addResourceBundle(resourceCulture, "translation", payload.texts || {}, true, true);
    await i18n.changeLanguage(resourceCulture);

    setCultureState(resourceCulture);
    setRevision((current) => current + 1);
  }

  async function setCulture(cultureValue: string) {
    const nextCulture = cultureValue.trim();
    if (!nextCulture) {
      return;
    }

    if (getToken()) {
      await api("/api/account/preferences/culture", {
        method: "PUT",
        body: JSON.stringify({ culture: nextCulture }),
      });
    }

    await loadTexts(nextCulture);
  }

  async function reloadTexts() {
    await loadTexts(culture);
  }

  const value = useMemo<I18nContextValue>(() => ({
    languages,
    culture,
    t: (key, fallback) => {
      const translated = i18n.t(key, { defaultValue: fallback });
      return typeof translated === "string" ? translated : fallback;
    },
    setCulture,
    reloadTexts,
    apiOrigin: getApiOrigin(),
  }), [languages, culture, revision]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error("useI18n must be used within I18nProvider");
  }

  return context;
}
