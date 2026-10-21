import { formatCurrency } from "../../../utils/currency";
import type { CurrentPrice } from "../types/CurrentPrice";

interface Props {
  price: CurrentPrice;
}

export default function PriceCard({ price }: Props) {
  return (
    <section>
      <p>Base Price: {formatCurrency(price.basePrice, price.currency)}</p>
      <p>Discount: {formatCurrency(price.totalDiscount, price.currency)}</p>
      <strong>
        Final Price: {formatCurrency(price.finalPrice, price.currency)}
      </strong>
      <small>Price Version: {price.version}</small>
    </section>
  );
}
