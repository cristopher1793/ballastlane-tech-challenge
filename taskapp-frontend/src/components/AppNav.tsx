import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { ClipboardList } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { UserMenu } from '@/components/UserMenu';
import { useAuth } from '@/context/AuthContext';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface AppNavProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function AppNav({ showNotification }: AppNavProps): React.ReactElement {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const isLogin = location.pathname === '/login';
  const isRegister = location.pathname === '/register';

  return (
    <nav className="border-b bg-primary text-primary-foreground sticky top-0 z-40">
      <div className="px-4 flex h-14 items-center justify-between">

        {/* Logo */}
        <span
          className="flex items-center gap-2 text-lg font-semibold cursor-pointer select-none"
          onClick={() => navigate(isAuthenticated ? '/dashboard' : '/login')}
        >
          <ClipboardList className="h-5 w-5" />
          TaskApp
        </span>

        {/* Right: user menu or auth buttons */}
        <div className="flex items-center gap-2">
          {isAuthenticated ? (
            <UserMenu showNotification={showNotification} />
          ) : (
            <>
              {!isLogin && (
                <Button
                  size="sm"
                  className="bg-white text-primary hover:bg-white/90"
                  onClick={() => navigate('/login')}
                >
                  Login
                </Button>
              )}
              {!isRegister && (
                <Button
                  size="sm"
                  className="bg-white text-primary hover:bg-white/90"
                  onClick={() => navigate('/register')}
                >
                  Register
                </Button>
              )}
            </>
          )}
        </div>
      </div>
    </nav>
  );
}
