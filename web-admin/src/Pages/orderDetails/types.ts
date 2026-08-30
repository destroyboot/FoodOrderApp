export type OrderItem = {
  menuItemId: number;
  menuItemName?: string | null;
  quantity: number;
  unitPrice: number;
  note: string | null;
};

export type OrderDetailsDto = {
  id: number;
  displayOrderNumber?: string;
  status: string | number;
  orderType: string | number;
  paymentMethod: number;
  paymentStatus: number;
  receiptEmail: string | null;
  receiptSentAt: string | null;
  invoiceNumber: string | null;
  hasInvoiceDocument: boolean;
  tableNumber: string | null;
  pickupContactName: string | null;
  pickupPhone: string | null;
  pickupNote: string | null;
  deliveryContactName: string | null;
  deliveryPhone: string | null;
  deliveryAddressLine1: string | null;
  deliveryAddressLine2: string | null;
  deliveryCity: string | null;
  deliveryPostalCode: string | null;
  deliveryCountry: string | null;
  deliveryNote: string | null;
  assignedDeliveryDriverUserId: string | null;
  assignedDeliveryDriverName: string | null;
  customerUserId: string | null;
  customerEmail: string | null;
  isAnonymousCustomer: boolean;
  scheduledFor: string | null;
  subtotal: number;
  deliveryFee: number;
  total: number;
  createdAt: string;
  billingDetails?: {
    customerType?: number;
    invoiceStatus?: number;
    receiptEmail?: string | null;
    personName?: string | null;
    companyName?: string | null;
    taxId?: string | null;
    billingAddressLine1?: string | null;
    billingAddressLine2?: string | null;
    billingCity?: string | null;
    billingPostalCode?: string | null;
    billingCountry?: string | null;
    invoiceIssuedAt?: string | null;
    invoiceSentAt?: string | null;
  } | null;
  items: OrderItem[];
};

export type DeliveryDriverOptionDto = {
  userId: string;
  displayName: string;
  email?: string | null;
};

export type TFunction = (key: string, fallback: string) => string;
