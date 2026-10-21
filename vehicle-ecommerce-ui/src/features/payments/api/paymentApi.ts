import apiClient from "../../../api/axiosClient";
import type { PaymentRequest, PaymentResponse } from "../types/Payment";

export async function initiatePayment(
  request: PaymentRequest
): Promise<PaymentResponse> {
  const response = await apiClient.post<PaymentResponse>(
    "/api/payments",
    request,
    {
      headers: {
        "Idempotency-Key": request.idempotencyKey
      }
    }
  );
  return response.data;
}
