import type { Vehicle } from "../types/Vehicle";
import VehicleCard from "./VehicleCard";

interface Props {
  vehicles: Vehicle[];
}

export default function VehicleGrid({ vehicles }: Props) {
  return (
    <section aria-label="Vehicles">
      {vehicles.map((vehicle) => (
        <VehicleCard key={vehicle.id} vehicle={vehicle} />
      ))}
    </section>
  );
}
