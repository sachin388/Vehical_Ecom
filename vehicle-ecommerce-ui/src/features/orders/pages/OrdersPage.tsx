import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getOrders } from "../api/orderApi";
import Spinner from "../../../components/common/Spinner";
import EmptyState from "../../../components/common/EmptyState";
import ErrorState from "../../../components/common/ErrorState";
import { formatCurrency } from "../../../utils/currency";

export default function OrdersPage() {
  const query = useQuery({
    queryKey: ["orders"],
    queryFn: getOrders
  });

  if (query.isLoading) {
    return <Spinner label="Loading orders..." />;
  }

  if (query.isError) {
    return <ErrorState message="Unable to load orders." />;
  }

  if (!query.data?.length) {
    return (
      <EmptyState
        title="No orders"
        message="Your orders will appear here."
      />
    );
  }

  return (
    <section>
      <h1>My Orders</h1>
      {query.data.map(order => (
        <article key={order.id}>
          <h2>{order.orderNumber}</h2>
          <p>Status: {order.status}</p>
          <p>
            Total: {formatCurrency(order.finalAmount, order.currency)}
          </p>
          <Link to={`/orders/${order.id}`}>
            View Order
          </Link>
        </article>
      ))}
    </section>
  );
}
