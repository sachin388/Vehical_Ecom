import apiClient from "../../../api/axiosClient";
import type { SalesOrder } from "../types/SalesOrder";
import type { SalesOrderHistory } from "../types/SalesOrderHistory";

export async function getOrders(): Promise<SalesOrder[]> {
  const response = await apiClient.get<SalesOrder[]>("/api/sales-orders");
  return response.data;
}

export async function getOrder(
  id: number
): Promise<SalesOrder> {
  const response = await apiClient.get<SalesOrder>(
    `/api/sales-orders/${id}`
  );
  return response.data;
}

export async function getOrderHistory(
  id: number
): Promise<SalesOrderHistory[]> {
  const response = await apiClient.get<SalesOrderHistory[]>(
    `/api/sales-orders/${id}/history`
  );
  return response.data;
}
