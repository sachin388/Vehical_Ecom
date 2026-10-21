import { useQuery } from "@tanstack/react-query";
import { getCurrentPrice } from "../api/priceApi";

export function useCurrentPrice(itemId: number) {
  return useQuery({
    queryKey: ["current-price", itemId],
    queryFn: () => getCurrentPrice(itemId),
    enabled: itemId > 0
  });
}
