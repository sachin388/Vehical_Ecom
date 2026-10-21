interface Props {
  value: string;
  onChange: (value: string) => void;
}

export default function VehicleSearchBar({
  value,
  onChange
}: Props) {
  return (
    <label>
      Search vehicles
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder="Search make, model or SKU"
      />
    </label>
  );
}
