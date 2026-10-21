import { createBrowserRouter } from "react-router-dom";
import MainLayout from "../components/layout/MainLayout";
import LoginPage from "../features/auth/pages/LoginPage";
import ProtectedRoute from "../features/auth/components/ProtectedRoute";
import VehicleListPage from "../features/vehicles/pages/VehicleListPage";
import VehicleDetailsPage from "../features/vehicles/pages/VehicleDetailsPage";
import CartPage from "../features/cart/pages/CartPage";
import CheckoutPage from "../features/checkout/pages/CheckoutPage";
import PaymentPage from "../features/payments/pages/PaymentPage";
import OrdersPage from "../features/orders/pages/OrdersPage";
import OrderDetailsPage from "../features/orders/pages/OrderDetailsPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <MainLayout />,
    children: [
      { index: true, element: <VehicleListPage /> },
      { path: "login", element: <LoginPage /> },
      { path: "vehicles", element: <VehicleListPage /> },
      { path: "vehicles/:id", element: <VehicleDetailsPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: "cart", element: <CartPage /> },
          { path: "checkout", element: <CheckoutPage /> },
          { path: "payment/:orderId", element: <PaymentPage /> },
          { path: "orders", element: <OrdersPage /> },
          { path: "orders/:id", element: <OrderDetailsPage /> }
        ]
      }
    ]
  }
]);
