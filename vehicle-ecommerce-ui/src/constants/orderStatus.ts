export const ORDER_STATUS = {
  DRAFT: "Draft",
  PENDING_PAYMENT: "PendingPayment",
  CONFIRMED: "Confirmed",
  ALLOCATED: "Allocated",
  PROCESSING: "Processing",
  COMPLETED: "Completed",
  CANCELLED: "Cancelled"
} as const;

export type OrderStatus =
  typeof ORDER_STATUS[keyof typeof ORDER_STATUS];
