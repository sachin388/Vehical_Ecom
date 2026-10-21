interface Props {
  make: string;
  model: string;
  onMakeChange: (value: string) => void;
  onModelChange: (value: string) => void;
}

export default function VehicleFilterPanel({
  make,
  model,
  onMakeChange,
  onModelChange
}: Props) {
  return (
    <aside>
      <label>
        Make
        <input
          value={make}
          onChange={(event) => onMakeChange(event.target.value)}
        />
      </label>
      <label>
        Model
        <input
          value={model}
          onChange={(event) => onModelChange(event.target.value)}
        />
      </label>
    </aside>
  );
}
