import { z } from "zod";

export const checkoutSchema = z.object({
  dealerId: z.number().positive("Select a dealer.")
});

export type CheckoutFormValues =
  z.infer<typeof checkoutSchema>;
