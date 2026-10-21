import apiClient from "../../../api/axiosClient";
import type { Accessory } from "../types/Accessory";

export async function getAccessories(
  itemId: number
): Promise<Accessory[]> {
  const response = await apiClient.get<Accessory[]>(
    `/api/accessories/item/${itemId}`
  );
  return response.data;
}
