import apiClient from "../../../api/axiosClient";
import type { CurrentPrice } from "../types/CurrentPrice";
import type { PriceBreakup } from "../types/PriceBreakup";

export async function getCurrentPrice(
  itemId: number
): Promise<CurrentPrice> {
  const response = await apiClient.get<CurrentPrice>(
    `/api/item-prices/current/${itemId}`
  );
  return response.data;
}

export async function getPriceBreakup(
  itemId: number
): Promise<PriceBreakup> {
  const response = await apiClient.get<PriceBreakup>(
    `/api/price-breakups/current/${itemId}`
  );
  return response.data;
}
