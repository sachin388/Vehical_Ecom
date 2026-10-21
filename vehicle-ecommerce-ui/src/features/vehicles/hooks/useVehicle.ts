import { useQuery } from "@tanstack/react-query";
import { getVehicle } from "../api/vehicleApi";

export function useVehicle(id: number) {
  return useQuery({
    queryKey: ["vehicle", id],
    queryFn: () => getVehicle(id),
    enabled: Number.isFinite(id)
  });
}
