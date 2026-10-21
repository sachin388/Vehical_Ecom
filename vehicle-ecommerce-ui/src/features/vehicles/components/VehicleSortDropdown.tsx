interface Props {
  value: string;
  onChange: (value: string) => void;
}

export default function VehicleSortDropdown({
  value,
  onChange
}: Props) {
  return (
    <label>
      Sort
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="name">Name</option>
        <option value="price">Price</option>
      </select>
    </label>
  );
}
