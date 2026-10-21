import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getOrder, getOrderHistory } from "../api/orderApi";
import Spinner from "../../../components/common/Spinner";
import ErrorState from "../../../components/common/ErrorState";
import OrderTimeline from "../components/OrderTimeline";

export default function OrderDetailsPage() {
  const { id } = useParams();
  const orderId = Number(id);

  const orderQuery = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrder(orderId),
    enabled: orderId > 0
  });

  const historyQuery = useQuery({
    queryKey: ["order-history", orderId],
    queryFn: () => getOrderHistory(orderId),
    enabled: orderId > 0
  });

  if (orderQuery.isLoading || historyQuery.isLoading) {
    return <Spinner label="Loading order..." />;
  }

  if (orderQuery.isError || historyQuery.isError) {
    return <ErrorState message="Unable to load order." />;
  }

  if (!orderQuery.data) {
    return <ErrorState message="Order not found." />;
  }

  return (
    <section>
      <h1>{orderQuery.data.orderNumber}</h1>
      <p>Status: {orderQuery.data.status}</p>
      <h2>Items</h2>
      {orderQuery.data.lines.map(line => (
        <article key={line.id}>
          <strong>{line.itemName}</strong>
          <p>Quantity: {line.quantity}</p>
        </article>
      ))}
      <h2>Timeline</h2>
      <OrderTimeline history={historyQuery.data ?? []} />
    </section>
  );
}
