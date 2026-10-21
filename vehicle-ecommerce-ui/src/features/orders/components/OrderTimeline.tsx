import { formatDateTime } from "../../../utils/date";
import type { SalesOrderHistory } from "../types/SalesOrderHistory";

interface Props {
  history: SalesOrderHistory[];
}

export default function OrderTimeline({ history }: Props) {
  return (
    <ol>
      {history.map(item => (
        <li key={item.id}>
          <strong>{item.status}</strong>
          <p>{item.message}</p>
          <time>{formatDateTime(item.occurredAtUtc)}</time>
        </li>
      ))}
    </ol>
  );
}
