import { Link } from "react-router-dom";
import type { Vehicle } from "../types/Vehicle";

interface Props {
  vehicle: Vehicle;
}

export default function VehicleCard({ vehicle }: Props) {
  return (
    <article>
      {vehicle.imageUrl && (
        <img
          src={vehicle.imageUrl}
          alt={`${vehicle.make} ${vehicle.model}`}
        />
      )}
      <h2>{vehicle.name}</h2>
      <p>{vehicle.make} {vehicle.model}</p>
      <p>Variant: {vehicle.variant}</p>
      <p>Status: {vehicle.status}</p>
      <Link to={`/vehicles/${vehicle.id}`}>
        View Details
      </Link>
    </article>
  );
}
