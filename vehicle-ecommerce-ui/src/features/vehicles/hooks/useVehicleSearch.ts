import { useQuery } from "@tanstack/react-query";
import { searchVehicles } from "../api/vehicleApi";
import type { VehicleSearchRequest } from "../types/VehicleSearchRequest";

export function useVehicleSearch(
  request: VehicleSearchRequest
) {
  return useQuery({
    queryKey: ["vehicles", request],
    queryFn: () => searchVehicles(request),
    placeholderData: (previous) => previous
  });
}
