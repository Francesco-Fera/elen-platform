import React from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import { ProtectedRoute } from "./components/auth/ProtectedRoute";
import { AuthPage } from "./pages/auth/AuthPage";
import { DashboardPage } from "./pages/dashboard/DashboardPage";
import { ROUTES } from "./constants";
import "./App.css";

function App() {
  return (
    <Router>
      <AuthProvider>
        <div className='App'>
          <Routes>
            {/* Auth routes - both use the same AuthPage component */}
            <Route
              path={ROUTES.LOGIN}
              element={
                <ProtectedRoute requireAuth={false}>
                  <AuthPage />
                </ProtectedRoute>
              }
            />
            <Route
              path={ROUTES.REGISTER}
              element={
                <ProtectedRoute requireAuth={false}>
                  <AuthPage />
                </ProtectedRoute>
              }
            />

            {/* Protected routes */}
            <Route
              path={ROUTES.DASHBOARD}
              element={
                <ProtectedRoute requireAuth={true}>
                  <DashboardPage />
                </ProtectedRoute>
              }
            />

            {/* Redirect routes */}
            <Route
              path={ROUTES.HOME}
              element={<Navigate to={ROUTES.DASHBOARD} replace />}
            />
            <Route
              path='*'
              element={<Navigate to={ROUTES.DASHBOARD} replace />}
            />
          </Routes>
        </div>
      </AuthProvider>
    </Router>
  );
}

export default App;
