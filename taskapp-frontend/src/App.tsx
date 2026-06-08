import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'sonner';
import { AuthProvider } from '@/context/AuthContext';
import { useAuth } from '@/context/AuthContext';
import { AppNav } from '@/components/AppNav';
import { Sidebar } from '@/components/Sidebar';
import { BottomNav } from '@/components/BottomNav';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AdminRoute } from '@/components/AdminRoute';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { TasksPage } from '@/pages/TasksPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { UsersPage } from '@/pages/UsersPage';
import { useNotification } from '@/hooks/useNotification';
import { cn } from '@/lib/utils';

function AppRoutes(): React.ReactElement {
  const { showNotification } = useNotification();
  const { logout, isAuthenticated } = useAuth();

  useEffect(() => {
    const handleSessionExpired = (): void => {
      logout();
      showNotification('Session expired — please log in again.', 'warning');
    };
    window.addEventListener('app:session-expired', handleSessionExpired);
    return () => window.removeEventListener('app:session-expired', handleSessionExpired);
  }, [logout, showNotification]);

  return (
    <>
      <AppNav showNotification={showNotification} />
      <div className="flex min-h-[calc(100vh-3.5rem)]">
        <Sidebar />
        <main className={cn('flex-1 min-w-0', isAuthenticated && 'pb-16 md:pb-0')}>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/login" element={<LoginPage showNotification={showNotification} />} />
            <Route path="/register" element={<RegisterPage showNotification={showNotification} />} />
            <Route path="/tasks" element={<ProtectedRoute><TasksPage showNotification={showNotification} /></ProtectedRoute>} />
            <Route path="/dashboard" element={<ProtectedRoute><DashboardPage showNotification={showNotification} /></ProtectedRoute>} />
            <Route path="/users" element={<AdminRoute><UsersPage showNotification={showNotification} /></AdminRoute>} />
          </Routes>
        </main>
      </div>
      <BottomNav />
    </>
  );
}

function App(): React.ReactElement {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
        <Toaster position="top-center" richColors />
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
