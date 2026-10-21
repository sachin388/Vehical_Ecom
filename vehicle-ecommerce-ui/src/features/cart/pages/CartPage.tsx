import { Link } from "react-router-dom";
import { useCart } from "../context/CartContext";
import CartItem from "../components/CartItem";
import CartSummary from "../components/CartSummary";
import EmptyState from "../../../components/common/EmptyState";

export default function CartPage() {
  const { items, removeItem } = useCart();

  if (items.length === 0) {
    return (
      <EmptyState
        title="Your cart is empty"
        message="Add a vehicle to continue."
      />
    );
  }

  return (
    <section>
      <h1>Cart</h1>
      {items.map(item => (
        <CartItem
          key={item.itemId}
          item={item}
          onRemove={removeItem}
        />
      ))}
      <CartSummary items={items} />
      <Link to="/checkout">Checkout</Link>
    </section>
  );
}
