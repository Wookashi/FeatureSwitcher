import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Spin } from 'antd';
import { RequireAuth } from './auth';

const FeatureMatrixPage = lazy(() => import('./views/FeatureMatrix/FeatureMatrixPage'));
const LoginPage = lazy(() => import('./views/Login').then((m) => ({ default: m.LoginPage })));
const SetupPage = lazy(() => import('./views/Setup').then((m) => ({ default: m.SetupPage })));
const UserManagementPage = lazy(() =>
  import('./views/UserManagement').then((m) => ({ default: m.UserManagementPage }))
);
const AuditLogPage = lazy(() =>
  import('./views/AuditLog').then((m) => ({ default: m.AuditLogPage }))
);

function RouteFallback() {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
      <Spin size="large" />
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<RouteFallback />}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/setup" element={<SetupPage />} />
          <Route
            path="/users"
            element={
              <RequireAuth requiredRole="Admin">
                <UserManagementPage />
              </RequireAuth>
            }
          />
          <Route
            path="/audit-log"
            element={
              <RequireAuth requiredRole="Admin">
                <AuditLogPage />
              </RequireAuth>
            }
          />
          <Route
            path="/"
            element={
              <RequireAuth>
                <FeatureMatrixPage />
              </RequireAuth>
            }
          />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}
