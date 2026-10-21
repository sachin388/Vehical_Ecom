import { useParams } from "react-router-dom";
import PaymentButton from "../components/PaymentButton";

export default function PaymentPage() {
  const { orderId } = useParams();
  const id = Number(orderId);
  // In the real application, load the order from Orders API.
  const amount = 0;

  if (!id) {
    return <p>Invalid order.</p>;
  }

  return (
    <section>
      <h1>Payment</h1>
      <p>Order ID: {id}</p>
      <PaymentButton orderId={id} amount={amount} />
    </section>
  );
}
