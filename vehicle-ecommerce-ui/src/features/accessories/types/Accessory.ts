export interface Accessory {
  id: number;
  name: string;
  description?: string;
  price: number;
  currency: string;
  isActive: boolean;
}
