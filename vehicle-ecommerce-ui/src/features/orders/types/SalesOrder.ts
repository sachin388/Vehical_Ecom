export interface SalesOrderLine {
  id: number;
  itemId: number;
  sku: string;
  itemName: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  taxAmount: number;
  lineTotal: number;
}

export interface SalesOrder {
  id: number;
  orderNumber: string;
  status: string;
  currency: string;
  finalAmount: number;
  dealerId?: number;
  lines: SalesOrderLine[];
}
