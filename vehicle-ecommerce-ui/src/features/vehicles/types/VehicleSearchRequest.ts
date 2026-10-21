export interface VehicleSearchRequest {
  search?: string;
  make?: string;
  model?: string;
  minPrice?: number;
  maxPrice?: number;
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}
