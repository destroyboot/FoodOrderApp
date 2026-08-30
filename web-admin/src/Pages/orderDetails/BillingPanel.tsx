import type { OrderDetailsDto, TFunction } from "./types";

function paymentMethodLabel(paymentMethod: number, t: TFunction) {
  switch (paymentMethod) {
    case 0:
      return t("orders.paymentMethod.inApp", "Pay in app");
    case 1:
      return t("orders.paymentMethod.counterOrDelivery", "Pay on delivery / counter");
    default:
      return String(paymentMethod);
  }
}

function paymentStatusLabel(paymentStatus: number, t: TFunction) {
  switch (paymentStatus) {
    case 0:
      return t("orders.paymentStatus.unpaid", "Unpaid");
    case 1:
      return t("orders.paymentStatus.pending", "Pending");
    case 2:
      return t("orders.paymentStatus.paid", "Paid");
    case 3:
      return t("orders.paymentStatus.failed", "Failed");
    case 4:
      return t("orders.paymentStatus.refunded", "Refunded");
    default:
      return String(paymentStatus);
  }
}

function invoiceStatusLabel(invoiceStatus: number | undefined, t: TFunction) {
  switch (invoiceStatus) {
    case 0:
      return t("orders.invoiceStatus.notRequested", "Not requested");
    case 1:
      return t("orders.invoiceStatus.requested", "Requested");
    case 2:
      return t("orders.invoiceStatus.issued", "Issued");
    case 3:
      return t("orders.invoiceStatus.sent", "Sent");
    default:
      return invoiceStatus === undefined ? "-" : String(invoiceStatus);
  }
}

function customerTypeLabel(customerType: number | undefined, t: TFunction) {
  switch (customerType) {
    case 0:
      return t("orders.billingType.person", "Person");
    case 1:
      return t("orders.billingType.company", "Company");
    default:
      return customerType === undefined ? "-" : String(customerType);
  }
}

function formatMaybeDate(value?: string | null) {
  if (!value) {
    return "-";
  }

  return new Date(value).toLocaleString();
}

type Props = {
  data: OrderDetailsDto;
  invoiceRequested: boolean;
  isHistoryView: boolean;
  onMarkPaid: () => void;
  onSendSummary: () => void;
  onDownloadSummaryPdf: () => void;
  onUpdateInvoiceStatus: (status: number) => void;
  onDownloadInvoice: () => void;
  t: TFunction;
};

export function BillingPanel({
  data,
  invoiceRequested,
  isHistoryView,
  onMarkPaid,
  onSendSummary,
  onDownloadSummaryPdf,
  onUpdateInvoiceStatus,
  onDownloadInvoice,
  t,
}: Props) {
  return (
    <div className="billing-panel">
      <h3>{t("orders.billing", "Billing")}</h3>
      <div><b>{t("orders.paymentMethod", "Payment method")}:</b> {paymentMethodLabel(data.paymentMethod, t)}</div>
      <div><b>{t("orders.paymentStatus", "Payment status")}:</b> {paymentStatusLabel(data.paymentStatus, t)}</div>
      {!isHistoryView && data.paymentMethod === 1 && data.paymentStatus !== 2 ? (
        <div className="spaced-top-md">
          <button onClick={onMarkPaid}>
            {t("orders.markAsPaid", "Mark as paid")}
          </button>
        </div>
      ) : null}
      <div><b>{t("orders.receiptDestination", "Receipt destination")}:</b> {data.receiptEmail ?? data.billingDetails?.receiptEmail ?? "-"}</div>
      <div><b>{t("orders.receiptSent", "Receipt sent")}:</b> {formatMaybeDate(data.receiptSentAt)}</div>

      <div className="spaced-top-md">
        <strong>{t("orders.receipt", "Receipt")}</strong>
        <div className="muted spaced-top-sm">
          {t("orders.receiptSummaryHint", "Receipt stays physical. Email sends the customer an order summary.")}
        </div>
        <div className="cluster-sm spaced-top-sm">
          <button onClick={onSendSummary}>{t("orders.sendOrderSummary", "Send order summary")}</button>
          <button onClick={onDownloadSummaryPdf}>{t("orders.downloadOrderSummaryPdf", "Download order summary PDF")}</button>
        </div>
      </div>

      <div className="spaced-top-lg">
        <strong>{t("orders.invoice", "Invoice")}</strong>
        {!invoiceRequested ? (
          <div className="muted spaced-top-sm">
            {t("orders.invoiceDisabledForReceipt", "Customer selected a regular receipt. Invoice actions are disabled for this order.")}
          </div>
        ) : (
          <>
            <div><b>{t("orders.billingType", "Billing type")}:</b> {customerTypeLabel(data.billingDetails?.customerType, t)}</div>
            <div><b>{t("orders.invoiceStatus", "Invoice status")}:</b> {invoiceStatusLabel(data.billingDetails?.invoiceStatus, t)}</div>
            <div><b>{t("orders.invoiceNumber", "Invoice number")}:</b> {data.invoiceNumber ?? "-"}</div>
            <div><b>{t("orders.issuedAt", "Issued at")}:</b> {formatMaybeDate(data.billingDetails?.invoiceIssuedAt)}</div>
            <div><b>{t("orders.sentAt", "Sent at")}:</b> {formatMaybeDate(data.billingDetails?.invoiceSentAt)}</div>
            {data.billingDetails?.personName ? <div><b>{t("common.name", "Name")}:</b> {data.billingDetails.personName}</div> : null}
            {data.billingDetails?.companyName ? <div><b>{t("orders.company", "Company")}:</b> {data.billingDetails.companyName}</div> : null}
            {data.billingDetails?.taxId ? <div><b>{t("orders.taxId", "Tax ID")}:</b> {data.billingDetails.taxId}</div> : null}
            {[data.billingDetails?.billingAddressLine1, data.billingDetails?.billingAddressLine2, data.billingDetails?.billingCity, data.billingDetails?.billingPostalCode, data.billingDetails?.billingCountry].some(Boolean) ? (
              <div>
                <b>{t("orders.billingAddress", "Billing address")}:</b>{" "}
                {[data.billingDetails?.billingAddressLine1, data.billingDetails?.billingAddressLine2, data.billingDetails?.billingCity, data.billingDetails?.billingPostalCode, data.billingDetails?.billingCountry]
                  .filter(Boolean)
                  .join(", ")}
              </div>
            ) : null}
            <div className="muted spaced-top-sm">
              {t("orders.invoiceHint", "Issuing an invoice stores a PDF in the backend that's included in summary emails.")}
            </div>
            <div className="cluster-sm spaced-top-sm">
              <button onClick={() => onUpdateInvoiceStatus(0)}>{t("orders.clearInvoiceRequest", "Clear invoice request")}</button>
              <button onClick={() => onUpdateInvoiceStatus(1)}>{t("orders.markInvoiceRequested", "Mark invoice requested")}</button>
              <button onClick={() => onUpdateInvoiceStatus(2)}>{t("orders.issueInvoice", "Issue invoice")}</button>
              <button onClick={() => onUpdateInvoiceStatus(3)}>{t("orders.markInvoiceSent", "Mark invoice sent")}</button>
              {data.hasInvoiceDocument ? (
                <button onClick={onDownloadInvoice}>{t("orders.downloadInvoicePdf", "Download invoice PDF")}</button>
              ) : null}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
