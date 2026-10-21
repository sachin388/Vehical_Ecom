import apiClient from "../../../api/axiosClient";
import type { CreateOrderRequest } from "../types/CreateOrderRequest";

export interface CheckoutResponse {
  orderId: number;
  orderNumber: string;
  status: string;
}

export async function createOrder(
  request: CreateOrderRequest
): Promise<CheckoutResponse> {
  const response = await apiClient.post<CheckoutResponse>(
    "/api/sales-orders",
    request
  );
  return response.data;
}
