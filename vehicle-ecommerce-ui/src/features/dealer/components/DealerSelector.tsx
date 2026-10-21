import type { Dealer } from "../types/Dealer";

interface Props {
  dealers: Dealer[];
  selectedDealerId?: number;
  onChange: (id: number) => void;
}

export default function DealerSelector({
  dealers,
  selectedDealerId,
  onChange
}: Props) {
  return (
    <label>
      Dealer
      <select
        value={selectedDealerId ?? ""}
        onChange={(event) => onChange(Number(event.target.value))}
      >
        <option value="">Select dealer</option>
        {dealers.map(dealer => (
          <option key={dealer.id} value={dealer.id}>
            {dealer.name} - {dealer.city}
          </option>
        ))}
      </select>
    </label>
  );
}
