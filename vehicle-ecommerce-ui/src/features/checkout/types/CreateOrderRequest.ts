export interface CreateOrderRequest {
  customerId: string;
  dealerId?: number;
  lines: {
    itemId: number;
    quantity: number;
    accessoryIds: number[];
  }[];
}
