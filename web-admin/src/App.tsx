import { Routes, Route, Navigate, Link, useNavigate } from "react-router-dom";
import Login from "./Pages/Login";
import ActiveOrders from "./Pages/ActiveOrders";
import OrderDetails from "./Pages/OrderDetails";
import MenuItems from "./Pages/MenuItems";
import MenuCategories from "./Pages/MenuCategories";
import { getToken, clearToken } from "./auth";
import { useEffect, useState } from "react";
import RequireAuth from "./RequireAuth";

export default function App() {
  const [authed, setAuthed] = useState<boolean>(!!getToken());
  const nav = useNavigate();

  useEffect(() => setAuthed(!!getToken()), []);

  if (!authed) return <Login onDone={() => { setAuthed(true); nav("/orders"); }} />;

  return (
    <div style={{ fontFamily: "Arial" }}>
      <div style={{ maxWidth: 1100, margin: "12px auto", display: "flex", gap: 12 }}>
        <Link to="/orders">Active Orders</Link>
        <Link to="/menu/categories">Categories</Link>
        <Link to="/menu/items">Items</Link>
        <button
          style={{ marginLeft: "auto" }}
          onClick={() => { clearToken(); setAuthed(false); nav("/login"); }}
        >
          Logout
        </button>
      </div>

      <Routes>
        <Route path="/" element={<Navigate to={getToken() ? "/orders" : "/login"} replace />} />
        <Route path="/login" element={<Login onDone={() => { setAuthed(true); nav("/orders"); }} />} />
        <Route path="/orders" element={
        <RequireAuth>
          <ActiveOrders />
        </RequireAuth>
      } />
      <Route path="/orders/:id" element={
        <RequireAuth>
          <OrderDetails />
        </RequireAuth>
      } />
      <Route path="/menu/categories" element={
        <RequireAuth>
          <MenuCategories />
        </RequireAuth>
      } />
      <Route path="/menu/items" element={
        <RequireAuth>
          <MenuItems />
        </RequireAuth>
      } />
      </Routes>
    </div>
  );
}