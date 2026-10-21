import { Link } from "react-router-dom";
import { useAuth } from "../../features/auth/hooks/useAuth";
import { useCart } from "../../features/cart/context/CartContext";

export default function Header() {
  const { isAuthenticated, logout } = useAuth();
  const { totalItems } = useCart();

  return (
    <header className="app-header">
      <div className="header-container">
        <Link to="/" className="brand-logo">
          <span className="brand-icon">⚡</span>
          <span className="brand-text">Apex<span className="brand-accent">Auto</span></span>
        </Link>
        <nav className="header-nav">
          <Link to="/vehicles" className="nav-link">Catalog</Link>
          {isAuthenticated && <Link to="/orders" className="nav-link">My Orders</Link>}
          <Link to="/cart" className="nav-link cart-link">
            <span>Cart</span>
            {totalItems > 0 && <span className="cart-badge">{totalItems}</span>}
          </Link>
          {isAuthenticated ? (
            <button type="button" onClick={logout} className="nav-btn-logout">
              Logout
            </button>
          ) : (
            <Link to="/login" className="nav-btn-login">Login</Link>
          )}
        </nav>
      </div>
    </header>
  );
}
