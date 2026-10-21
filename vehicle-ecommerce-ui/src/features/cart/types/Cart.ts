export interface CartItem {
  itemId: number;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  accessoryIds: number[];
}

export interface Cart {
  items: CartItem[];
}
