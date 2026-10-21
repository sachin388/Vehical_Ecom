import { useParams } from "react-router-dom";
import { useState } from "react";
import { useVehicle } from "../hooks/useVehicle";
import { useCurrentPrice } from "../../pricing/hooks/useCurrentPrice";
import PriceCard from "../../pricing/components/PriceCard";
import Spinner from "../../../components/common/Spinner";
import ErrorState from "../../../components/common/ErrorState";
import AccessorySelector from "../../accessories/components/AccessorySelector";
import type { Accessory } from "../../accessories/types/Accessory";
import { useCart } from "../../cart/context/CartContext";

export default function VehicleDetailsPage() {
  const { id } = useParams();
  const itemId = Number(id);
  const [selectedAccessoryIds, setSelectedAccessoryIds] = useState<number[]>([]);
  const vehicleQuery = useVehicle(itemId);
  const priceQuery = useCurrentPrice(itemId);
  const { addItem } = useCart();

  if (vehicleQuery.isLoading || priceQuery.isLoading) {
    return <Spinner label="Loading vehicle details..." />;
  }

  if (vehicleQuery.isError || priceQuery.isError) {
    return <ErrorState message="Unable to load vehicle details." />;
  }

  if (!vehicleQuery.data || !priceQuery.data) {
    return <ErrorState message="Vehicle was not found." />;
  }

  const vehicle = vehicleQuery.data;
  const price = priceQuery.data;
  const accessories: Accessory[] = [];

  const handleAddToCart = () => {
    addItem({
      itemId: vehicle.id,
      sku: vehicle.sku,
      name: vehicle.name,
      quantity: 1,
      unitPrice: price.finalPrice,
      currency: price.currency,
      accessoryIds: selectedAccessoryIds
    });
  };

  return (
    <section>
      <h1>{vehicle.name}</h1>
      <p>{vehicle.make} {vehicle.model} - {vehicle.variant}</p>
      {vehicle.imageUrl && (
        <img src={vehicle.imageUrl} alt={vehicle.name} />
      )}
      <PriceCard price={price} />
      <AccessorySelector
        accessories={accessories}
        selectedIds={selectedAccessoryIds}
        onChange={setSelectedAccessoryIds}
      />
      <button type="button" onClick={handleAddToCart}>
        Add to Cart
      </button>
    </section>
  );
}
