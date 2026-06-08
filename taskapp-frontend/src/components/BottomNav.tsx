import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { ListTodo, LayoutDashboard } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/context/AuthContext';

const TABS = [
  { label: 'Tasks', path: '/tasks', Icon: ListTodo },
  { label: 'Dashboard', path: '/dashboard', Icon: LayoutDashboard },
] as const;

export function BottomNav(): React.ReactElement | null {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!isAuthenticated) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 md:hidden bg-primary border-t border-white/10">
      <div className="flex h-16">
        {TABS.map(({ label, path, Icon }) => {
          const active = location.pathname === path;
          return (
            <button
              key={path}
              onClick={() => navigate(path)}
              className={cn(
                'flex flex-1 flex-col items-center justify-center gap-1 text-xs font-medium transition-colors',
                active
                  ? 'bg-white/20 text-white'
                  : 'text-white/60 hover:text-white hover:bg-white/10'
              )}
            >
              <Icon className="h-5 w-5" />
              {label}
            </button>
          );
        })}
      </div>
    </div>
  );
}
