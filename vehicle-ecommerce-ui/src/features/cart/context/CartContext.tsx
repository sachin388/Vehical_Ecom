import {
  createContext,
  useContext,
  useMemo,
  useState,
  type ReactNode
} from "react";
import type { CartItem } from "../types/Cart";

interface CartContextValue {
  items: CartItem[];
  totalItems: number;
  addItem: (item: CartItem) => void;
  removeItem: (itemId: number) => void;
  clear: () => void;
}

const CartContext = createContext<CartContextValue | undefined>(
  undefined
);

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>([]);

  const addItem = (item: CartItem) => {
    setItems((current) => {
      const existing = current.find(x => x.itemId === item.itemId);
      if (!existing) {
        return [...current, item];
      }
      return current.map(x =>
        x.itemId === item.itemId
          ? { ...x, quantity: x.quantity + item.quantity }
          : x
      );
    });
  };

  const removeItem = (itemId: number) => {
    setItems(current => current.filter(x => x.itemId !== itemId));
  };

  const clear = () => setItems([]);

  const totalItems = useMemo(
    () => items.reduce((sum, item) => sum + item.quantity, 0),
    [items]
  );

  return (
    <CartContext.Provider
      value={{ items, totalItems, addItem, removeItem, clear }}
    >
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error("useCart must be used inside CartProvider.");
  }
  return context;
}
