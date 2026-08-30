import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { useI18n } from "../i18n";
import { ModalShell } from "../Components/ModalShell";

type AppLanguage = {
  id: number;
  culture: string;
  displayName: string;
  nativeName: string;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
};

type AppTextTranslation = {
  id: number;
  key: string;
  culture: string;
  value: string;
  description?: string | null;
  groupName?: string | null;
  isActive: boolean;
};

const emptyLanguage = (): AppLanguage => ({
  id: 0,
  culture: "",
  displayName: "",
  nativeName: "",
  isActive: true,
  isDefault: false,
  sortOrder: 0,
});

const emptyText = (): AppTextTranslation => ({
  id: 0,
  key: "",
  culture: "",
  value: "",
  description: "",
  groupName: "common",
  isActive: true,
});

export default function PlatformOptions() {
  const { t, reloadTexts } = useI18n();
  const [languages, setLanguages] = useState<AppLanguage[]>([]);
  const [texts, setTexts] = useState<AppTextTranslation[]>([]);
  const [languageDraft, setLanguageDraft] = useState<AppLanguage>(emptyLanguage());
  const [textDraft, setTextDraft] = useState<AppTextTranslation>(emptyText());
  const [search, setSearch] = useState("");
  const [selectedGroup, setSelectedGroup] = useState("all");
  const [selectedCulture, setSelectedCulture] = useState("all");
  const [info, setInfo] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [showLanguageModal, setShowLanguageModal] = useState(false);
  const [showTextModal, setShowTextModal] = useState(false);

  function mergeTextDraft(source: AppTextTranslation[]) {
    if (!textDraft.id || !textDraft.key.trim() || !textDraft.culture.trim() || !textDraft.value.trim()) {
      return source;
    }

    const next = [...source];
    const existingIndex = next.findIndex((item) => item.id === textDraft.id);
    if (existingIndex >= 0) {
      next[existingIndex] = { ...textDraft };
    }

    return next;
  }

  async function load() {
    setErr(null);
    const [languageItems, textItems] = await Promise.all([
      api<AppLanguage[]>("/api/admin/platform/languages"),
      api<AppTextTranslation[]>("/api/admin/platform/texts"),
    ]);
    setLanguages(languageItems ?? []);
    setTexts(textItems ?? []);
  }

  useEffect(() => {
    void load();
  }, []);

  const groups = useMemo(() => {
    const values = Array.from(
      new Set(
        texts
          .map((item) => item.groupName?.trim())
          .filter((value): value is string => !!value)
      )
    ).sort((a, b) => a.localeCompare(b));

    return values;
  }, [texts]);

  const cultures = useMemo(() => {
    const values = Array.from(
      new Set(
        texts
          .map((item) => item.culture?.trim())
          .filter((value): value is string => !!value)
      )
    ).sort((a, b) => a.localeCompare(b));

    return values;
  }, [texts]);

  const filteredTexts = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    return texts.filter((item) => {
      const matchesGroup = selectedGroup === "all" || (item.groupName ?? "") === selectedGroup;
      const matchesCulture = selectedCulture === "all" || item.culture === selectedCulture;
      if (!matchesGroup || !matchesCulture) {
        return false;
      }

      if (!normalized) {
        return true;
      }

      return [item.key, item.culture, item.value, item.description ?? "", item.groupName ?? ""]
        .join(" ")
        .toLowerCase()
        .includes(normalized);
    });
  }, [search, selectedCulture, selectedGroup, texts]);

  async function saveLanguages() {
    setErr(null);
    setInfo(null);
    await api("/api/admin/platform/languages", {
      method: "PUT",
      body: JSON.stringify({ items: languages }),
    });
    setInfo(t("platform.languagesSaved", "Languages saved."));
    await load();
  }

  async function saveTexts() {
    setErr(null);
    setInfo(null);

    const nextTexts = mergeTextDraft(texts);
    setTexts(nextTexts);

    await api("/api/admin/platform/texts", {
      method: "PUT",
      body: JSON.stringify({ items: nextTexts }),
    });
    await reloadTexts();
    await load();
    setInfo(t("platform.textsSaved", "Translations saved."));
  }

  function upsertLanguageDraft() {
    if (!languageDraft.culture.trim() || !languageDraft.displayName.trim() || !languageDraft.nativeName.trim()) {
      setErr(t("platform.languageRequired", "Culture, display name and native name are required."));
      return;
    }

    const next = [...languages];
    const existingIndex = next.findIndex((item) => item.id === languageDraft.id && item.id > 0);
    if (existingIndex >= 0) {
      next[existingIndex] = { ...languageDraft };
    } else {
      next.push({ ...languageDraft, id: 0 });
    }

    if (languageDraft.isDefault) {
      for (const item of next) {
        if (item.culture !== languageDraft.culture) {
          item.isDefault = false;
        }
      }
    }

    setLanguages(next);
    setLanguageDraft(emptyLanguage());
  }

  function upsertTextDraft() {
    if (!textDraft.id || !textDraft.key.trim() || !textDraft.culture.trim() || !textDraft.value.trim()) {
      setErr(t("platform.textRequired", "Key, culture and value are required."));
      return;
    }

    setTexts((prev) => prev.map((item) => item.id === textDraft.id ? { ...textDraft } : item));
    setTextDraft(emptyText());
    setInfo(null);
  }

  return (
    <div style={{ maxWidth: 1280, margin: "20px auto", fontFamily: "Arial" }}>
      <h2>{t("page.platformSettings", "Platform Settings")}</h2>
      {err ? <div className="alert-error">{err}</div> : null}
      {info ? <div className="alert-success">{info}</div> : null}

      <section style={{ marginBottom: 32 }}>
        <h3 style={{marginTop: 12, marginBottom: 12 }}>{t("platform.languages", "App languages")}</h3>

        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("platform.culture", "Culture")}</th>
              <th align="left">{t("platform.displayName", "Display name")}</th>
              <th align="left">{t("platform.nativeName", "Native name")}</th>
              <th align="left">{t("platform.sort", "Sort")}</th>
              <th align="left">{t("common.active", "Active")}</th>
              <th align="left">{t("platform.default", "Default")}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {languages.map((language, index) => (
              <tr key={`${language.culture}-${index}`}>
                <td>{language.culture}</td>
                <td>{language.displayName}</td>
                <td>{language.nativeName}</td>
                <td>{language.sortOrder}</td>
                <td>{language.isActive ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                <td>{language.isDefault ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                <td>
                  <div style={{ display: "flex", gap: 8 }}>
                    <button onClick={() => { setLanguageDraft(language); setShowLanguageModal(true); }}>{t("common.edit", "Edit")}</button>
                    <button onClick={() => setLanguages((prev) => prev.filter((item) => item !== language))} className="button-danger">{t("common.delete", "Delete")}</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12, marginTop: 12 }}>
          <button onClick={() => setShowLanguageModal(true)}>{t("platform.addLanguage", "Add language")}</button>
          <button onClick={saveLanguages}>{t("platform.saveLanguages", "Save languages")}</button>
        </div>
      </section>

      <section>
        <h3>{t("platform.texts", "App text resources")}</h3>
        <p style={{ marginTop: 0, color: "#4b5563" }}>
          {t("platform.textsHelp", "These translations are stored in JSON resource files and apply across the web and mobile apps.")}
        </p>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 8, marginBottom: 12 }}>
          <input placeholder={t("platform.searchKeysOrValues", "Search keys or values")} value={search} onChange={(e) => setSearch(e.target.value)} style={{ minWidth: 280 }} />
          <select value={selectedCulture} onChange={(e) => setSelectedCulture(e.target.value)}>
            <option value="all">{t("common.allCultures", "All cultures")}</option>
            {cultures.map((culture) => (
              <option key={culture} value={culture}>{culture}</option>
            ))}
          </select>
          <select value={selectedGroup} onChange={(e) => setSelectedGroup(e.target.value)}>
            <option value="all">{t("common.all", "All")}</option>
            {groups.map((group) => (
              <option key={group} value={group}>{group}</option>
            ))}
          </select>
        </div>
        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginBottom: 12 }}>
          <button type="button" onClick={() => void load()}>
            {t("common.reload", "Reload")}
          </button>
          <button
            onClick={() => {
              setSearch("");
              setSelectedCulture("all");
              setSelectedGroup("all");
            }}
          >
            {t("common.resetFilters", "Reset Filters")}
          </button>
        </div>
        <table width="100%" cellPadding={8}>
          <thead>
            <tr>
              <th align="left">{t("platform.key", "Key")}</th>
              <th align="left">{t("platform.culture", "Culture")}</th>
              <th align="left">{t("platform.value", "Value")}</th>
              <th align="left">{t("platform.group", "Group")}</th>
              <th align="left">{t("platform.description", "Description")}</th>
              <th align="left">{t("common.active", "Active")}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {filteredTexts.map((item, index) => (
              <tr key={`${item.key}-${item.culture}-${index}`}>
                <td>{item.key}</td>
                <td>{item.culture}</td>
                <td>{item.value}</td>
                <td>{item.groupName ?? "-"}</td>
                <td>{item.description ?? "-"}</td>
                <td>{item.isActive ? t("common.yes", "Yes") : t("common.no", "No")}</td>
                <td>
                  <div style={{ display: "flex", gap: 8 }}>
                    <button onClick={() => { setTextDraft(item); setShowTextModal(true); }}>{t("common.edit", "Edit")}</button>
                    <button onClick={() => setTexts((prev) => prev.filter((entry) => entry !== item))} className="button-danger">{t("common.delete", "Delete")}</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {showLanguageModal ? (
        <ModalShell title={languageDraft.id ? t("platform.updateDraft", "Update draft") : t("platform.addLanguage", "Add language")} onClose={() => { setShowLanguageModal(false); setLanguageDraft(emptyLanguage()); }} minWidth="min(760px, calc(100vw - 32px))" maxWidth={980}>
          <div style={{ display: "grid", gap: 12 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8 }}>
              <input placeholder={t("platform.culture", "Culture")} value={languageDraft.culture} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, culture: e.target.value }))} />
              <input placeholder={t("platform.displayName", "Display name")} value={languageDraft.displayName} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, displayName: e.target.value }))} />
              <input placeholder={t("platform.nativeName", "Native name")} value={languageDraft.nativeName} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, nativeName: e.target.value }))} />
              <input type="number" placeholder={t("platform.sort", "Sort")} value={languageDraft.sortOrder} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, sortOrder: Number(e.target.value) }))} />
            </div>
            <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
              <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={languageDraft.isActive} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, isActive: e.target.checked }))} /> {t("common.active", "Active")}</label>
              <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={languageDraft.isDefault} onChange={(e) => setLanguageDraft((prev) => ({ ...prev, isDefault: e.target.checked }))} /> {t("platform.default", "Default")}</label>
            </div>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button onClick={() => { upsertLanguageDraft(); setShowLanguageModal(false); }}>{languageDraft.id ? t("platform.updateDraft", "Update draft") : t("platform.addLanguage", "Add language")}</button>
              <button onClick={() => { setShowLanguageModal(false); setLanguageDraft(emptyLanguage()); }}>{t("common.cancel", "Cancel")}</button>
            </div>
          </div>
        </ModalShell>
      ) : null}

      {showTextModal ? (
        <ModalShell title={t("platform.updateText", "Update draft")} onClose={() => { setShowTextModal(false); setTextDraft(emptyText()); }} minWidth="min(760px, calc(100vw - 32px))" maxWidth={980}>
          <div style={{ display: "grid", gap: 12 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 8 }}>
              <input placeholder={t("platform.key", "Key")} value={textDraft.key} readOnly style={{ background: "#f3f4f6", color: "#6b7280" }} />
              <input placeholder={t("platform.culture", "Culture")} value={textDraft.culture} readOnly style={{ background: "#f3f4f6", color: "#6b7280" }} />
              <input placeholder={t("platform.group", "Group")} value={textDraft.groupName ?? ""} readOnly style={{ background: "#f3f4f6", color: "#6b7280" }} />
              <input placeholder={t("platform.description", "Description")} value={textDraft.description ?? ""} readOnly style={{ background: "#f3f4f6", color: "#6b7280" }} />
            </div>
            <textarea placeholder={t("platform.value", "Value")} value={textDraft.value} onChange={(e) => setTextDraft((prev) => ({ ...prev, value: e.target.value }))} style={{ minHeight: 120 }} />
            <label style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={textDraft.isActive} onChange={(e) => setTextDraft((prev) => ({ ...prev, isActive: e.target.checked }))} /> {t("common.active", "Active")}</label>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button onClick={async () => { upsertTextDraft(); setShowTextModal(false); await saveTexts(); }}>{t("platform.updateText", "Update draft")}</button>
              <button onClick={() => { setShowTextModal(false); setTextDraft(emptyText()); }}>{t("common.cancel", "Cancel")}</button>
            </div>
          </div>
        </ModalShell>
      ) : null}
    </div>
  );
}
