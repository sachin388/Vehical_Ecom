interface EmptyStateProps {
  title: string;
  message: string;
}

export default function EmptyState({
  title,
  message
}: EmptyStateProps) {
  return (
    <section aria-label={title}>
      <h2>{title}</h2>
      <p>{message}</p>
    </section>
  );
}
