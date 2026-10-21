import type { Accessory } from "../types/Accessory";
import { formatCurrency } from "../../../utils/currency";

interface Props {
  accessories: Accessory[];
  selectedIds: number[];
  onChange: (ids: number[]) => void;
}

export default function AccessorySelector({
  accessories,
  selectedIds,
  onChange
}: Props) {
  const toggle = (id: number) => {
    const next = selectedIds.includes(id)
      ? selectedIds.filter((item) => item !== id)
      : [...selectedIds, id];
    onChange(next);
  };

  return (
    <section>
      <h2>Accessories</h2>
      {accessories.map((accessory) => (
        <label key={accessory.id}>
          <input
            type="checkbox"
            checked={selectedIds.includes(accessory.id)}
            onChange={() => toggle(accessory.id)}
          />
          {accessory.name} -{" "}
          {formatCurrency(accessory.price, accessory.currency)}
        </label>
      ))}
    </section>
  );
}
