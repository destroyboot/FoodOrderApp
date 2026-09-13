import { useEffect, useMemo, useState } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import { Table } from "@tiptap/extension-table";
import TableCell from "@tiptap/extension-table-cell";
import TableHeader from "@tiptap/extension-table-header";
import TableRow from "@tiptap/extension-table-row";
import TextAlign from "@tiptap/extension-text-align";
import Underline from "@tiptap/extension-underline";
import Color from "@tiptap/extension-color";
import { TextStyle } from "@tiptap/extension-text-style";
import { api } from "../api";
import { useI18n } from "../i18n";
import { PageShell } from "../Components/PageShell";

type PrintTemplateDto = {
  id: number;
  code: string;
  name: string;
  description: string;
  documentKind: string;
  htmlTemplate: string;
  isActive: boolean;
  isCustomized: boolean;
  updatedAtUtc: string;
  updatedByUserId?: string | null;
  availableTokens: string[];
};

type PrintTemplatePreviewDto = {
  html: string;
};

const doneCodes = new Set([
  "report.sales-summary",
  "report.customer-frequency",
  "report.customer-mix",
  "report.menu-popularity",
  "report.user-activity",
  "order.summary.pdf",
  "order.summary.email",
  "order.invoice.pdf",
  "account.registration-code.email",
  "account.activated.email",
  "account.password-reset.email",
  "account.password-changed.email",
  "account.email-change-code.email",
  "account.email-changed.email",
  "account.deletion-code.email",
]);

export default function PrintTemplates() {
  const { t, languages, culture } = useI18n();
  const [templates, setTemplates] = useState<PrintTemplateDto[]>([]);
  const [selectedCode, setSelectedCode] = useState("");
  const [draft, setDraft] = useState<PrintTemplateDto | null>(null);
  const [previewHtml, setPreviewHtml] = useState("");
  const [kindFilter, setKindFilter] = useState("all");
  const [templateCulture, setTemplateCulture] = useState(culture || "pl-PL");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3] },
      }),
      Underline,
      TextStyle,
      Color,
      Link.configure({
        openOnClick: false,
      }),
      TextAlign.configure({
        types: ["heading", "paragraph"],
      }),
      Table.configure({
        resizable: true,
      }),
      TableRow,
      TableHeader,
      TableCell,
    ],
    content: "",
    immediatelyRender: false,
    onUpdate: ({ editor }) => {
      const html = editor.getHTML();
      setDraft((current) => current ? { ...current, htmlTemplate: html } : current);
    },
  });

  useEffect(() => {
    if (!editor || !draft) return;
    if (editor.getHTML() !== draft.htmlTemplate) {
      editor.commands.setContent(draft.htmlTemplate || "", { emitUpdate: false });
    }
  }, [editor, draft?.code, draft?.htmlTemplate]);

  async function load(nextCode = selectedCode) {
    setLoading(true);
    setErr(null);
    try {
      const result = await api<PrintTemplateDto[]>(`/api/admin/print-templates?culture=${encodeURIComponent(templateCulture)}`);
      const nextTemplates = result ?? [];
      setTemplates(nextTemplates);
      const code = nextCode && nextTemplates.some((item) => item.code === nextCode)
        ? nextCode
        : nextTemplates[0]?.code ?? "";
      setSelectedCode(code);
      const selected = nextTemplates.find((item) => item.code === code) ?? null;
      setDraft(selected ? { ...selected } : null);
      setPreviewHtml("");
    } catch (e: any) {
      setErr(e.message || t("printTemplates.loadFailed", "Failed to load print templates."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load("");
  }, []);

  useEffect(() => {
    if (!culture || culture === templateCulture) return;
    setTemplateCulture(culture);
  }, [culture]);

  useEffect(() => {
    void load(selectedCode);
  }, [templateCulture]);

  function selectTemplate(code: string) {
    setSelectedCode(code);
    const selected = templates.find((item) => item.code === code) ?? null;
    setDraft(selected ? { ...selected } : null);
    setPreviewHtml("");
    setInfo(null);
    setErr(null);
  }

  async function save() {
    if (!draft) return;

    setBusy(true);
    setErr(null);
    setInfo(null);
    try {
      await api(`/api/admin/print-templates/${encodeURIComponent(draft.code)}`, {
        method: "PUT",
        body: JSON.stringify({
          name: draft.name,
          description: draft.description,
          culture: templateCulture,
          htmlTemplate: draft.htmlTemplate,
          isActive: draft.isActive,
        }),
      });
      setInfo(t("printTemplates.saved", "Template saved."));
      await load(draft.code);
    } catch (e: any) {
      setErr(e.message || t("printTemplates.saveFailed", "Failed to save template."));
    } finally {
      setBusy(false);
    }
  }

  async function reset() {
    if (!draft) return;
    if (!confirm(t("printTemplates.resetConfirm", "Restore the default template?"))) return;

    setBusy(true);
    setErr(null);
    setInfo(null);
    try {
      await api(`/api/admin/print-templates/${encodeURIComponent(draft.code)}/reset?culture=${encodeURIComponent(templateCulture)}`, { method: "POST" });
      setInfo(t("printTemplates.resetDone", "Default template restored."));
      await load(draft.code);
    } catch (e: any) {
      setErr(e.message || t("printTemplates.resetFailed", "Failed to restore template."));
    } finally {
      setBusy(false);
    }
  }

  async function preview() {
    if (!draft) return;

    setBusy(true);
    setErr(null);
    try {
      const result = await api<PrintTemplatePreviewDto>(`/api/admin/print-templates/${encodeURIComponent(draft.code)}/preview`, {
        method: "POST",
        body: JSON.stringify({
          name: draft.name,
          description: draft.description,
          culture: templateCulture,
          htmlTemplate: draft.htmlTemplate,
          isActive: draft.isActive,
        }),
      });
      setPreviewHtml(result?.html ?? "");
    } catch (e: any) {
      setErr(e.message || t("printTemplates.previewFailed", "Failed to render preview."));
    } finally {
      setBusy(false);
    }
  }

  function insertToken(token: string) {
    const value = token.endsWith("Html") ? `{{{${token}}}}` : `{{${token}}}`;
    if (editor) {
      editor.chain().focus().insertContent(value).run();
      return;
    }

    setDraft((current) => current ? { ...current, htmlTemplate: `${current.htmlTemplate}${value}` } : current);
  }

  function setLink() {
    if (!editor) return;

    const previousUrl = editor.getAttributes("link").href as string | undefined;
    const url = window.prompt(t("printTemplates.linkUrl", "Link URL"), previousUrl ?? "");
    if (url === null) return;

    if (!url.trim()) {
      editor.chain().focus().extendMarkRange("link").unsetLink().run();
      return;
    }

    editor.chain().focus().extendMarkRange("link").setLink({ href: url.trim() }).run();
  }

  function applyColor(value: string) {
    if (!editor) return;
    editor.chain().focus().setColor(value).run();
  }

  const documentKinds = useMemo(
    () => Array.from(new Set(templates.map((item) => item.documentKind))).sort(),
    [templates]
  );

  const filteredTemplates = useMemo(() => {
    const normalized = search.trim().toLowerCase();
    return templates.filter((item) => {
      if (kindFilter !== "all" && item.documentKind !== kindFilter) return false;
      if (!normalized) return true;
      return `${item.name} ${item.code} ${item.description} ${item.documentKind}`.toLowerCase().includes(normalized);
    });
  }, [templates, kindFilter, search]);

  const checklist = useMemo(() => {
    const total = templates.length;
    const ready = templates.filter((item) => doneCodes.has(item.code)).length;
    const customized = templates.filter((item) => item.isCustomized).length;
    return { total, ready, customized };
  }, [templates]);

  return (
    <PageShell title={t("nav.printTemplates", "Print Templates")} error={err} maxWidth={1440}>
      <style>
        {`
          .print-template-editor .ProseMirror {
            min-height: 520px;
            outline: none;
          }

          .print-template-editor .ProseMirror h1,
          .print-template-editor .ProseMirror h2,
          .print-template-editor .ProseMirror h3,
          .print-template-editor .ProseMirror p {
            margin-top: 0;
          }

          .print-template-editor table {
            border-collapse: collapse;
            width: 100%;
          }

          .print-template-editor td,
          .print-template-editor th {
            border: 1px solid #d0d5dd;
            padding: 6px;
          }

          .print-template-editor th {
            background: #f2f4f7;
            text-align: left;
          }

          .print-template-editor button.active {
            background: #eef4ff;
            color: #175cd3;
            border-color: #84adff;
          }
        `}
      </style>
      {info ? <div className="alert-success" style={{ marginBottom: 12 }}>{info}</div> : null}
      {loading ? <div style={{ marginBottom: 12 }}>{t("common.loading", "Loading...")}</div> : null}

      <div style={{ marginBottom: 16, color: "#667085" }}>
        {t("printTemplates.checklist", "Configured document types")}: {checklist.ready}/{checklist.total}
        {" | "}
        {t("printTemplates.customized", "Customized")}: {checklist.customized}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "320px 1fr", gap: 20 }}>
        <aside>
          <div style={{ display: "grid", gap: 8, marginBottom: 12 }}>
            <select value={templateCulture} onChange={(event) => setTemplateCulture(event.target.value)}>
              {(languages.length > 0 ? languages : [{ culture: "pl-PL", nativeName: "Polski" }, { culture: "en-US", nativeName: "English" }]).map((language) => (
                <option key={language.culture} value={language.culture}>
                  {language.nativeName} ({language.culture})
                </option>
              ))}
            </select>
            <input
              placeholder={t("printTemplates.search", "Search templates")}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <select value={kindFilter} onChange={(event) => setKindFilter(event.target.value)}>
              <option value="all">{t("common.all", "All")}</option>
              {documentKinds.map((kind) => (
                <option key={kind} value={kind}>{kind}</option>
              ))}
            </select>
            <button onClick={() => void load(selectedCode)} disabled={loading || busy}>{t("common.reload", "Reload")}</button>
          </div>

          <div style={{ display: "grid", gap: 8 }}>
            {filteredTemplates.map((item) => (
              <button
                key={item.code}
                type="button"
                onClick={() => selectTemplate(item.code)}
                style={{
                  textAlign: "left",
                  padding: 10,
                  background: item.code === selectedCode ? "#eef4ff" : "#fff",
                  border: "1px solid #d0d5dd",
                  borderRadius: 6,
                }}
              >
                <div style={{ fontWeight: 700 }}>{item.name}</div>
                <div style={{ fontSize: 12, color: "#667085" }}>{item.documentKind} | {item.code}</div>
                <div style={{ fontSize: 12, color: item.isCustomized ? "#027a48" : "#667085" }}>
                  {item.isCustomized ? t("printTemplates.custom", "Custom") : t("printTemplates.default", "Default")}
                  {" | "}
                  {doneCodes.has(item.code) ? t("printTemplates.ready", "Ready") : t("printTemplates.pending", "Pending")}
                </div>
              </button>
            ))}
          </div>
        </aside>

        <main>
          {!draft ? (
            <div>{t("printTemplates.none", "No template selected.")}</div>
          ) : (
            <div style={{ display: "grid", gap: 14 }}>
              <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 12, alignItems: "start" }}>
                <div style={{ display: "grid", gap: 8 }}>
                  <input
                    value={draft.name}
                    onChange={(event) => setDraft({ ...draft, name: event.target.value })}
                  />
                  <textarea
                    value={draft.description}
                    onChange={(event) => setDraft({ ...draft, description: event.target.value })}
                    style={{ minHeight: 70 }}
                  />
                  <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <input
                      type="checkbox"
                      checked={draft.isActive}
                      onChange={(event) => setDraft({ ...draft, isActive: event.target.checked })}
                    />
                    {t("common.active", "Active")}
                  </label>
                </div>
                <div style={{ display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "flex-end" }}>
                  <button onClick={save} disabled={busy}>{busy ? t("common.saving", "Saving...") : t("common.save", "Save")}</button>
                  <button onClick={preview} disabled={busy}>{t("printTemplates.preview", "Preview")}</button>
                  <button onClick={reset} disabled={busy}>{t("printTemplates.restoreDefault", "Restore default")}</button>
                </div>
              </div>

              <div style={{ display: "grid", gridTemplateColumns: "1fr 260px", gap: 16 }}>
                <div>
                  <div className="print-template-editor" style={{ border: "1px solid #d0d5dd", borderRadius: 8, overflow: "hidden", background: "#fff" }}>
                    <div style={{ display: "flex", gap: 6, flexWrap: "wrap", padding: 8, borderBottom: "1px solid #e4e7ec", background: "#f9fafb" }}>
                      <button type="button" onClick={() => editor?.chain().focus().undo().run()} disabled={!editor?.can().undo()}>Undo</button>
                      <button type="button" onClick={() => editor?.chain().focus().redo().run()} disabled={!editor?.can().redo()}>Redo</button>
                      <button type="button" onClick={() => editor?.chain().focus().setParagraph().run()} className={editor?.isActive("paragraph") ? "active" : ""}>P</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleHeading({ level: 1 }).run()} className={editor?.isActive("heading", { level: 1 }) ? "active" : ""}>H1</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleHeading({ level: 2 }).run()} className={editor?.isActive("heading", { level: 2 }) ? "active" : ""}>H2</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleBold().run()} className={editor?.isActive("bold") ? "active" : ""}>B</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleItalic().run()} className={editor?.isActive("italic") ? "active" : ""}>I</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleUnderline().run()} className={editor?.isActive("underline") ? "active" : ""}>U</button>
                      <input
                        type="color"
                        aria-label="Text color"
                        onChange={(event) => applyColor(event.target.value)}
                        style={{ width: 40, minHeight: 34, padding: 2 }}
                      />
                      <button type="button" onClick={() => editor?.chain().focus().setTextAlign("left").run()}>Left</button>
                      <button type="button" onClick={() => editor?.chain().focus().setTextAlign("center").run()}>Center</button>
                      <button type="button" onClick={() => editor?.chain().focus().setTextAlign("right").run()}>Right</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleBulletList().run()} className={editor?.isActive("bulletList") ? "active" : ""}>Bullets</button>
                      <button type="button" onClick={() => editor?.chain().focus().toggleOrderedList().run()} className={editor?.isActive("orderedList") ? "active" : ""}>Numbers</button>
                      <button type="button" onClick={setLink} className={editor?.isActive("link") ? "active" : ""}>Link</button>
                      <button
                        type="button"
                        onClick={() => editor?.chain().focus().insertTable({ rows: 3, cols: 4, withHeaderRow: true }).run()}
                      >
                        Table
                      </button>
                      <button type="button" onClick={() => editor?.chain().focus().addRowAfter().run()} disabled={!editor?.can().addRowAfter()}>Row</button>
                      <button type="button" onClick={() => editor?.chain().focus().addColumnAfter().run()} disabled={!editor?.can().addColumnAfter()}>Column</button>
                    </div>
                    <EditorContent
                      editor={editor}
                      style={{
                        minHeight: 520,
                        padding: 16,
                        fontFamily: "Arial, sans-serif",
                        fontSize: 14,
                      }}
                    />
                  </div>
                </div>

                <aside style={{ border: "1px solid #e4e7ec", borderRadius: 8, padding: 12, background: "#fff" }}>
                  <h3 style={{ marginTop: 0 }}>{t("printTemplates.tokens", "Available parameters")}</h3>
                  <div style={{ display: "grid", gap: 6 }}>
                    {draft.availableTokens.map((token) => (
                      <button
                        key={token}
                        type="button"
                        onClick={() => insertToken(token)}
                        style={{ textAlign: "left", background: "#f9fafb", color: "#344054" }}
                      >
                        {token.endsWith("Html") ? `{{{${token}}}}` : `{{${token}}}`}
                      </button>
                    ))}
                  </div>
                </aside>
              </div>

              {previewHtml ? (
                <section style={{ border: "1px solid #d0d5dd", borderRadius: 8, padding: 16, background: "#fff" }}>
                  <h3 style={{ marginTop: 0 }}>{t("printTemplates.preview", "Preview")}</h3>
                  <div dangerouslySetInnerHTML={{ __html: previewHtml }} />
                </section>
              ) : null}
            </div>
          )}
        </main>
      </div>
    </PageShell>
  );
}
