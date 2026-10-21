import { z } from "zod";

export const loginSchema = z.object({
  email: z
    .string()
    .trim()
    .email("Enter a valid email address."),
  password: z
    .string()
    .min(6, "Password must contain at least 6 characters.")
});

export type LoginFormValues = z.infer<typeof loginSchema>;
