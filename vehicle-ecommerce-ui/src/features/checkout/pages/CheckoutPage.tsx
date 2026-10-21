import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate } from "react-router-dom";
import { checkoutSchema, type CheckoutFormValues } from "../schemas/checkoutSchema";
import { createOrder } from "../api/checkoutApi";
import { useCart } from "../../cart/context/CartContext";
import DealerSelector from "../../dealer/components/DealerSelector";
import type { Dealer } from "../../dealer/types/Dealer";
import Button from "../../../components/common/Button";

export default function CheckoutPage() {
  const navigate = useNavigate();
  const { items, clear } = useCart();
  const dealers: Dealer[] = [];
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting }
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema)
  });

  const selectedDealerId = watch("dealerId");

  const submit = async (values: CheckoutFormValues) => {
    const response = await createOrder({
      customerId: "current-user",
      dealerId: values.dealerId,
      lines: items.map(item => ({
        itemId: item.itemId,
        quantity: item.quantity,
        accessoryIds: item.accessoryIds
      }))
    });
    clear();
    navigate(`/payment/${response.orderId}`);
  };

  return (
    <form onSubmit={handleSubmit(submit)}>
      <h1>Checkout</h1>
      <DealerSelector
        dealers={dealers}
        selectedDealerId={selectedDealerId}
        onChange={(id) => setValue("dealerId", id, { shouldValidate: true })}
      />
      <input
        type="hidden"
        {...register("dealerId", { valueAsNumber: true })}
      />
      {errors.dealerId && <p>{errors.dealerId.message}</p>}
      <Button type="submit" loading={isSubmitting}>
        Place Order
      </Button>
    </form>
  );
}
