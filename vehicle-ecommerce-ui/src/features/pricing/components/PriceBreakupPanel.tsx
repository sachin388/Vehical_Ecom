import { formatCurrency } from "../../../utils/currency";
import type { PriceBreakup } from "../types/PriceBreakup";

interface Props {
  breakup: PriceBreakup;
}

export default function PriceBreakupPanel({ breakup }: Props) {
  return (
    <section>
      <h2>Price Breakup</h2>
      <p>Base: {formatCurrency(breakup.basePrice, breakup.currency)}</p>
      <p>Discount: {formatCurrency(breakup.discount, breakup.currency)}</p>
      <p>Tax: {formatCurrency(breakup.tax, breakup.currency)}</p>
      <p>Registration: {formatCurrency(breakup.registrationCharges, breakup.currency)}</p>
      <p>Insurance: {formatCurrency(breakup.insurance, breakup.currency)}</p>
      <p>Other: {formatCurrency(breakup.otherCharges, breakup.currency)}</p>
      <strong>
        Final: {formatCurrency(breakup.finalPrice, breakup.currency)}
      </strong>
    </section>
  );
}
