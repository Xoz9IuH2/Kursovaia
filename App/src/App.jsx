import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
import Layout from './components/Layout';
import Login from './pages/Login';
import AdminPanel from './pages/AdminPanel';
import Summons from './pages/Summons';
import Files from './pages/Files';
import Documents from './pages/Documents';
import Applications from './pages/Applications';
import Appointments from './pages/Appointments';

import Calendar from './pages/Calendar';
import Statistics from './pages/Statistics';
import Audit from './pages/Audit';
import Employees from './pages/Employees';
import RegisterCitizen from './pages/RegisterCitizen';
import Reports from './pages/Reports';

function ProtectedRoute({ children }) {
  const { user, loading } = useAuth();
  
  if (loading) return <div>Загрузка...</div>;
  if (!user) return <Navigate to="/login" />;
  
  return <Layout>{children}</Layout>;
}

function AppRoutes() {
  const { user, loading } = useAuth();
  
  if (loading) return <div>Загрузка...</div>;
  
  return (
    <Routes>
      <Route path="/login" element={user ? <Navigate to="/" /> : <Login />} />
      <Route path="/" element={<Navigate to="/files" replace />} />
      <Route path="/summons" element={<ProtectedRoute><Summons /></ProtectedRoute>} />
      <Route path="/files" element={<ProtectedRoute><Files /></ProtectedRoute>} />
      <Route path="/documents" element={<ProtectedRoute><Documents /></ProtectedRoute>} />
      <Route path="/applications" element={<ProtectedRoute><Applications /></ProtectedRoute>} />
      <Route path="/appointments" element={<ProtectedRoute><Appointments /></ProtectedRoute>} />
      <Route path="/calendar" element={<ProtectedRoute><Calendar /></ProtectedRoute>} />
      <Route path="/statistics" element={<ProtectedRoute><Statistics /></ProtectedRoute>} />
      <Route path="/audit" element={<ProtectedRoute><Audit /></ProtectedRoute>} />
      <Route path="/employees" element={<ProtectedRoute><Employees /></ProtectedRoute>} />
      <Route path="/register" element={<ProtectedRoute><RegisterCitizen /></ProtectedRoute>} />
      <Route path="/admin" element={<ProtectedRoute><AdminPanel /></ProtectedRoute>} />
      <Route path="/reports" element={<ProtectedRoute><Reports /></ProtectedRoute>} />
    </Routes>
  );
}

export default function App() {
  // Прокидываем JWT в все запросы автоматически
  useEffect(() => {
    const originalFetch = window.fetch;
    window.fetch = async (input, init = {}) => {
      const token = localStorage.getItem('token');
      const headers = init.headers ? { ...init.headers } : {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
      return originalFetch(input, { ...init, headers });
    };
  }, []);
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
