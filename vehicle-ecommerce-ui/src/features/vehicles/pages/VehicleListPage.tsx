import { useState } from "react";
import VehicleSearchBar from "../components/VehicleSearchBar";
import VehicleFilterPanel from "../components/VehicleFilterPanel";
import VehicleSortDropdown from "../components/VehicleSortDropdown";
import VehicleGrid from "../components/VehicleGrid";
import { useVehicleSearch } from "../hooks/useVehicleSearch";
import { useDebounce } from "../../../hooks/useDebounce";
import Spinner from "../../../components/common/Spinner";
import EmptyState from "../../../components/common/EmptyState";
import ErrorState from "../../../components/common/ErrorState";
import Pagination from "../../../components/common/Pagination";

export default function VehicleListPage() {
  const [search, setSearch] = useState("");
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [sortBy, setSortBy] = useState("name");
  const [pageNumber, setPageNumber] = useState(1);
  const debouncedSearch = useDebounce(search);

  const { data, isLoading, isError, refetch } = useVehicleSearch({
    search: debouncedSearch || undefined,
    make: make || undefined,
    model: model || undefined,
    pageNumber,
    pageSize: 12,
    sortBy,
    sortDirection: "asc"
  });

  if (isLoading && !data) {
    return <Spinner label="Loading vehicles..." />;
  }

  if (isError) {
    return (
      <ErrorState
        message="Unable to load vehicles."
        onRetry={() => void refetch()}
      />
    );
  }

  const vehicles = data?.items ?? [];

  return (
    <section>
      <h1>Vehicles</h1>
      <VehicleSearchBar
        value={search}
        onChange={(value) => {
          setSearch(value);
          setPageNumber(1);
        }}
      />
      <VehicleFilterPanel
        make={make}
        model={model}
        onMakeChange={(value) => {
          setMake(value);
          setPageNumber(1);
        }}
        onModelChange={(value) => {
          setModel(value);
          setPageNumber(1);
        }}
      />
      <VehicleSortDropdown
        value={sortBy}
        onChange={(value) => {
          setSortBy(value);
          setPageNumber(1);
        }}
      />
      {vehicles.length === 0 ? (
        <EmptyState
          title="No vehicles found"
          message="Try changing your search or filters."
        />
      ) : (
        <VehicleGrid vehicles={vehicles} />
      )}
      {data && (
        <Pagination
          currentPage={data.pageNumber}
          totalPages={data.totalPages}
          onPageChange={setPageNumber}
        />
      )}
    </section>
  );
}
