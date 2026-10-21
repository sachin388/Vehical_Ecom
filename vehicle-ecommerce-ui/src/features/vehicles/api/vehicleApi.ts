import apiClient from "../../../api/axiosClient";
import type { Vehicle } from "../types/Vehicle";
import type { VehicleSearchRequest } from "../types/VehicleSearchRequest";
import type { PagedResponse } from "../types/PagedResponse";

export async function getVehicle(id: number): Promise<Vehicle> {
  const response = await apiClient.get<Vehicle>(`/api/items/${id}`);
  return response.data;
}

export async function searchVehicles(
  request: VehicleSearchRequest
): Promise<PagedResponse<Vehicle>> {
  const response = await apiClient.get<PagedResponse<Vehicle>>(
    "/api/items",
    { params: request }
  );
  return response.data;
}
