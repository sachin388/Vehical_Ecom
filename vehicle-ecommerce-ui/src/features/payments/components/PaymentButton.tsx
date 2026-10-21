import { useState } from "react";
import Button from "../../../components/common/Button";
import { initiatePayment } from "../api/paymentApi";

interface Props {
  orderId: number;
  amount: number;
}

export default function PaymentButton({ orderId, amount }: Props) {
  const [loading, setLoading] = useState(false);

  const pay = async () => {
    if (loading) return;
    setLoading(true);
    try {
      const result = await initiatePayment({
        orderId,
        amount,
        paymentMethod: "Online",
        idempotencyKey: crypto.randomUUID()
      });
      if (result.redirectUrl) {
        window.location.href = result.redirectUrl;
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button type="button" loading={loading} onClick={pay}>
      Pay Now
    </Button>
  );
}
