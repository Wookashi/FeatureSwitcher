import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { RequireAuth } from './auth';
import FeatureMatrixPage from './views/FeatureMatrix/FeatureMatrixPage';
import { LoginPage } from './views/Login';
import { SetupPage } from './views/Setup';
import { UserManagementPage } from './views/UserManagement';

export default function App() {
  return (
    <BrowserRouter>
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
          path="/"
          element={
            <RequireAuth>
              <FeatureMatrixPage />
            </RequireAuth>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
