export interface PriceBreakup {
  basePrice: number;
  discount: number;
  tax: number;
  registrationCharges: number;
  insurance: number;
  otherCharges: number;
  finalPrice: number;
  currency: string;
}
