export interface PaymentRequest {
  orderId: number;
  amount: number;
  paymentMethod: string;
  idempotencyKey: string;
}

export interface PaymentResponse {
  paymentId: string;
  status: string;
  redirectUrl?: string;
}
