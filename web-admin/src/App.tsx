import { NavLink, Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import { api } from "./api";
import { clearToken, getDefaultAuthorizedRoute, getDisplayName, getToken, getUserRoles } from "./auth";
import { useI18n } from "./i18n";
import "./App.css";
import RequireAuth from "./RequireAuth";
import InAppNotificationToast from "./Components/InAppNotificationToast";
import Login from "./Pages/Login";
import Register from "./Pages/Register";
import ActiveOrders from "./Pages/ActiveOrders";
import OrderHistory from "./Pages/OrderHistory";
import OrderDetails from "./Pages/OrderDetails";
import MenuItems from "./Pages/MenuItems";
import MenuCategories from "./Pages/MenuCategories";
import ForgotPassword from "./Pages/ForgotPassword";
import ResetPassword from "./Pages/ResetPassword";
import ChangePassword from "./Pages/ChangePassword";
import ConfirmRegistration from "./Pages/ConfirmRegistration";
import Users from "./Pages/Users";
import Cart from "./Pages/Cart";
import Restaurants from "./Pages/Restaurants";
import Ingredients from "./Pages/Ingredients";
import Reservations from "./Pages/Reservations";
import MyReservations from "./Pages/MyReservations";
import DataAdmin from "./Pages/DataAdmin";
import Tables from "./Pages/Tables";
import PlatformOptions from "./Pages/PlatformOptions";
import Reports from "./Pages/Reports";

type StaffNotificationDto = {
  id: number;
  type: number;
  title: string;
  body: string;
  payloadJson?: string | null;
  isRead: boolean;
  createdAt: string;
};

type ToastNotification = {
  title: string;
  body: string;
  targetUrl?: string | null;
};

export default function App() {
  const nav = useNavigate();
  const location = useLocation();
  const { languages, culture, setCulture, t } = useI18n();
  const hasToken = !!getToken();
  const roles = getUserRoles();
  const displayName = getDisplayName();
  const defaultRoute = getDefaultAuthorizedRoute();
  const [toast, setToast] = useState<ToastNotification | null>(null);
  const [workspaceRealtime, setWorkspaceRealtime] = useState("connecting");
  const toastQueueRef = useRef<ToastNotification[]>([]);
  const toastTimerRef = useRef<number | null>(null);
  const notificationsReadyRef = useRef(false);
  const lastSeenNotificationIdRef = useRef(0);

  const isMainAdmin = roles.includes("Admin");
  const isRestaurantAdmin = roles.includes("RestaurantAdmin");
  const isWaiter = roles.includes("Waiter");
  const isChef = roles.includes("Chef");
  const isDeliveryDriver = roles.includes("DeliveryDriver");
  const isStaff = isMainAdmin || isRestaurantAdmin || isWaiter || isChef || isDeliveryDriver;
  const hasAssignedRole = roles.length > 0;
  const isCustomerOnly = hasToken && !isStaff;

  const canSeeOrders = isRestaurantAdmin || isWaiter || isChef || isDeliveryDriver;
  const canManageMenu = isRestaurantAdmin;
  const canManageRestaurantSettings = isMainAdmin || isRestaurantAdmin;
  const canManageIngredients = isRestaurantAdmin;
  const canSeeReservations = isRestaurantAdmin || isWaiter;
  const canManageTables = isRestaurantAdmin || isWaiter;
  const canManageDataTables = isMainAdmin;
  const canManageUsers = isMainAdmin;
  const canSeeReports = isMainAdmin || isRestaurantAdmin;

  const navItems = [
    hasAssignedRole && canSeeOrders
      ? {
          to: "/orders",
          label: isDeliveryDriver && !isRestaurantAdmin && !isWaiter && !isChef
            ? t("nav.myDeliveries", "My Deliveries")
            : t("nav.activeOrders", "Active Orders"),
          tone: "orders",
        }
      : null,
    hasAssignedRole && canSeeReservations
      ? { to: "/reservations", label: t("nav.reservations", "Reservations"), tone: "reservations" }
      : null,
    hasAssignedRole && canSeeOrders
      ? { to: "/orders/history", label: t("nav.orderHistory", "Order History"), tone: "history" }
      : null,
    hasAssignedRole && canSeeReports
      ? { to: "/reports", label: t("nav.reports", "Reports"), tone: "history" }
      : null,
    hasAssignedRole && canManageMenu
      ? { to: "/menu/items", label: t("nav.items", "Items"), tone: "menu" }
      : null,
    hasAssignedRole && canManageMenu
      ? { to: "/menu/categories", label: t("nav.categories", "Categories"), tone: "menu" }
      : null,
    hasAssignedRole && canManageIngredients
      ? { to: "/ingredients", label: t("nav.ingredients", "Ingredients"), tone: "menu" }
      : null,
    hasAssignedRole && canManageTables
      ? { to: "/tables", label: t("nav.tables", "Tables"), tone: "settings" }
      : null,
    hasAssignedRole && canManageRestaurantSettings
      ? {
          to: "/restaurants",
          label: isMainAdmin ? t("nav.restaurants", "Restaurants") : t("nav.restaurantSettings", "Restaurant Settings"),
          tone: "settings",
        }
      : null,
    hasAssignedRole && canManageUsers
      ? { to: "/users", label: t("nav.users", "Users"), tone: "settings" }
      : null,
    hasAssignedRole && canManageDataTables
      ? { to: "/data-tables", label: t("nav.dataTables", "Data Tables"), tone: "settings" }
      : null,
    hasAssignedRole && isMainAdmin
      ? { to: "/platform", label: t("nav.platformSettings", "Platform Settings"), tone: "settings" }
      : null,
    hasAssignedRole && isCustomerOnly
      ? { to: "/my-reservations", label: t("nav.myReservations", "My Reservations"), tone: "customer" }
      : null,
    hasAssignedRole && isCustomerOnly
      ? { to: "/cart", label: t("nav.cart", "Cart"), tone: "customer" }
      : null,
    !isMainAdmin
      ? { to: "/change-password", label: t("nav.changePassword", "Change Password"), tone: "account" }
      : null,
  ].filter(Boolean) as Array<{ to: string; label: string; tone: string }>;

  const authOnlyPaths = ["/login", "/register", "/forgot-password", "/reset-password", "/confirm-registration"];
  const isAuthOnlyPage = authOnlyPaths.some((path) => location.pathname.startsWith(path));
  const showWorkspaceShell = hasToken && !isAuthOnlyPage;
  const currentNavItem = [...navItems]
    .sort((left, right) => right.to.length - left.to.length)
    .find((item) => location.pathname.startsWith(item.to));

  useEffect(() => {
    return () => {
      if (toastTimerRef.current) {
        window.clearTimeout(toastTimerRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (!hasToken || !isStaff) {
      setToast(null);
      setWorkspaceRealtime("connecting");
      toastQueueRef.current = [];
      notificationsReadyRef.current = false;
      lastSeenNotificationIdRef.current = 0;
      if (toastTimerRef.current) {
        window.clearTimeout(toastTimerRef.current);
        toastTimerRef.current = null;
      }
      return;
    }

    async function tick(announceNew: boolean) {
      try {
        const items = await api<StaffNotificationDto[]>("/api/notifications?take=20");
        setWorkspaceRealtime("connected");
        const newestId = items.reduce((max, item) => Math.max(max, item.id), 0);

        if (!notificationsReadyRef.current) {
          lastSeenNotificationIdRef.current = newestId;
          notificationsReadyRef.current = true;
          return;
        }

        const newItems = items
          .filter((item) => item.id > lastSeenNotificationIdRef.current && item.type === 4)
          .sort((a, b) => a.id - b.id);

        if (announceNew) {
          for (const item of newItems) {
            enqueueToast(item.title, item.body, parseNotificationTarget(item.payloadJson));
          }
        }

        lastSeenNotificationIdRef.current = Math.max(lastSeenNotificationIdRef.current, newestId);
      } catch {
        setWorkspaceRealtime("error");
      }
    }

    void tick(false);
    const handle = window.setInterval(() => {
      void tick(true);
    }, 15000);

    return () => {
      window.clearInterval(handle);
    };
  }, [hasToken, isStaff]);

  function showNextToast() {
    if (toastTimerRef.current) {
      window.clearTimeout(toastTimerRef.current);
      toastTimerRef.current = null;
    }

    const next = toastQueueRef.current.shift() ?? null;
    setToast(next);

    if (!next) {
      return;
    }

    toastTimerRef.current = window.setTimeout(() => {
      setToast(null);
      toastTimerRef.current = null;
      showNextToast();
    }, 3000);
  }

  function enqueueToast(title: string, body: string, targetUrl?: string | null) {
    toastQueueRef.current.push({ title, body, targetUrl });
    if (!toast) {
      showNextToast();
    }
  }

  function dismissToast() {
    if (toastTimerRef.current) {
      window.clearTimeout(toastTimerRef.current);
      toastTimerRef.current = null;
    }

    setToast(null);
    showNextToast();
  }

  function openToastTarget() {
    if (!toast?.targetUrl) {
      dismissToast();
      return;
    }

    const nextUrl = toast.targetUrl;
    dismissToast();
    nav(nextUrl);
  }

  function parseNotificationTarget(payloadJson?: string | null) {
    if (!payloadJson) {
      return null;
    }

    try {
      const payload = JSON.parse(payloadJson) as { url?: string };
      return payload.url ?? null;
    } catch {
      return null;
    }
  }

  const routeContent = (
    <Routes>
      <Route path="/" element={<Navigate to={hasToken ? defaultRoute : "/login"} replace />} />
      <Route
        path="/awaiting-role"
        element={
          hasToken ? (
            <div className="workspace-message-card">
              <h2>{t("page.awaitingRole.title", "Please await your role assignment")}</h2>
              <p>{t("page.awaitingRole.body", "Your account is active, but no restaurant role has been assigned yet.")}</p>
            </div>
          ) : <Navigate to="/login" replace />
        }
      />

      <Route
        path="/login"
        element={
          hasToken
            ? <Navigate to={defaultRoute} replace />
            : <Login onDone={(path) => nav(path)} />
        }
      />

      <Route
        path="/register"
        element={
          hasToken
            ? <Navigate to={defaultRoute} replace />
            : <Register />
        }
      />

      <Route
        path="/orders"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]}>
            <ActiveOrders />
          </RequireAuth>
        }
      />

      <Route
        path="/orders/:id"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]}>
            <OrderDetails />
          </RequireAuth>
        }
      />

      <Route
        path="/orders/history"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]}>
            <OrderHistory />
          </RequireAuth>
        }
      />

      <Route
        path="/menu/categories"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin"]}>
            <MenuCategories />
          </RequireAuth>
        }
      />

      <Route
        path="/menu/items"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin"]}>
            <MenuItems />
          </RequireAuth>
        }
      />

      <Route
        path="/forgot-password"
        element={
          hasToken
            ? <Navigate to={defaultRoute} replace />
            : <ForgotPassword />
        }
      />

      <Route
        path="/reset-password"
        element={
          hasToken
            ? <Navigate to={defaultRoute} replace />
            : <ResetPassword />
        }
      />

      <Route
        path="/change-password"
        element={
          <RequireAuth>
            <ChangePassword />
          </RequireAuth>
        }
      />

      <Route
        path="/confirm-registration"
        element={
          hasToken
            ? <Navigate to={defaultRoute} replace />
            : <ConfirmRegistration />
        }
      />

      <Route
        path="/restaurants"
        element={
          <RequireAuth allowedRoles={["Admin", "RestaurantAdmin"]}>
            <Restaurants />
          </RequireAuth>
        }
      />

      <Route
        path="/tables"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin", "Waiter"]}>
            <Tables />
          </RequireAuth>
        }
      />

      <Route
        path="/ingredients"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin"]}>
            <Ingredients />
          </RequireAuth>
        }
      />

      <Route
        path="/reservations"
        element={
          <RequireAuth allowedRoles={["RestaurantAdmin", "Waiter"]}>
            <Reservations />
          </RequireAuth>
        }
      />

      <Route
        path="/data-tables"
        element={
          <RequireAuth allowedRoles={["Admin"]}>
            <DataAdmin />
          </RequireAuth>
        }
      />

      <Route
        path="/platform"
        element={
          <RequireAuth allowedRoles={["Admin"]}>
            <PlatformOptions />
          </RequireAuth>
        }
      />

      <Route
        path="/reports"
        element={
          <RequireAuth allowedRoles={["Admin", "RestaurantAdmin"]}>
            <Reports />
          </RequireAuth>
        }
      />

      <Route
        path="/my-reservations"
        element={
          <RequireAuth blockedRoles={["Admin", "RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]}>
            <MyReservations />
          </RequireAuth>
        }
      />

      <Route
        path="/users"
        element={
          <RequireAuth allowedRoles={["Admin"]}>
            <Users />
          </RequireAuth>
        }
      />

      <Route
        path="/cart"
        element={
          <RequireAuth blockedRoles={["Admin", "RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"]}>
            <Cart />
          </RequireAuth>
        }
      />
    </Routes>
  );

  return (
    <div className="app-shell-root">
      <InAppNotificationToast
        visible={!!toast}
        title={toast?.title ?? ""}
        body={toast?.body ?? ""}
        onPress={toast?.targetUrl ? openToastTarget : undefined}
        onClose={dismissToast}
      />
      {showWorkspaceShell ? (
        <div className="workspace-shell">
          <aside className="workspace-sidebar">
            <div className="brand-block">
              <div className="brand-mark">FO</div>
              <div className="brand-copy">
                <strong>FoodOrderApp</strong>
                <span>
                  {isMainAdmin
                    ? t("common.appAdministration", "Application administration")
                    : t("common.restaurantOperations", "Restaurant operations")}
                </span>
              </div>
            </div>

            <nav className="workspace-nav">
              {navItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end
                  className={({ isActive }) => `workspace-nav-link${isActive ? " active" : ""}`}
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>

            <div className="workspace-sidebar-footer">
              {languages.length > 0 ? (
                <div className="workspace-sidebar-tools">
                  <label className="language-picker">
                    <span>{t("common.language", "Language")}</span>
                    <select value={culture} onChange={(e) => void setCulture(e.target.value)}>
                      {languages.map((language) => (
                        <option key={language.culture} value={language.culture}>
                          {language.nativeName}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
              ) : null}
              {displayName ? (
                <div className="profile-chip">
                  <span className="profile-kicker">{t("common.loggedInAs", "Logged in as")}</span>
                  <strong>{displayName}</strong>
                </div>
              ) : null}
              <button
                className="button-secondary"
                onClick={() => {
                  clearToken();
                  nav("/login");
                }}
              >
                {t("auth.logout", "Logout")}
              </button>
            </div>
          </aside>

          <main className="workspace-main">
            <header className="workspace-topbar">
              <div className="workspace-topbar-copy">
                <strong>{currentNavItem?.label ?? "FoodOrderApp"}</strong>
                <span>
                  {hasAssignedRole
                    ? t("common.connectedWorkspace", "Workspace for daily restaurant operations")
                    : t("common.welcomeBack", "Welcome back")}
                </span>
              </div>
              <div className="workspace-topbar-actions">
                {isStaff ? (
                  <div className="realtime-pill" data-state={workspaceRealtime}>
                    <span>{t("orders.realtime", "Realtime")}:</span>
                    <span>{workspaceRealtime}</span>
                  </div>
                ) : null}
              </div>
            </header>

            <section className="workspace-content">
              {routeContent}
            </section>
          </main>
        </div>
      ) : (
        <div className="public-shell">
          {routeContent}
        </div>
      )}
    </div>
  );
}
