import apiClient from "../../../api/axiosClient";
import type { Dealer } from "../types/Dealer";

export async function getAvailableDealers(
  itemId: number
): Promise<Dealer[]> {
  const response = await apiClient.get<Dealer[]>(
    `/api/dealers/available`,
    { params: { itemId } }
  );
  return response.data;
}
