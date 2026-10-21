interface SpinnerProps {
  label?: string;
}

export default function Spinner({
  label = "Loading..."
}: SpinnerProps) {
  return <div role="status" aria-live="polite">{label}</div>;
}
