import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { LayoutDashboard, ListTodo, Users } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/context/AuthContext';

export function Sidebar(): React.ReactElement | null {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!isAuthenticated) return null;

  const isAdmin = user?.role === 'Admin';

  const links = [
    { label: 'Dashboard', path: '/dashboard', Icon: LayoutDashboard },
    { label: 'Tasks', path: '/tasks', Icon: ListTodo },
    ...(isAdmin ? [{ label: 'Users', path: '/users', Icon: Users }] : []),
  ];

  return (
    <aside className="hidden md:flex flex-col w-56 shrink-0 border-r bg-background">
      <nav className="flex flex-col gap-0.5 p-3 pt-4">
        {links.map(({ label, path, Icon }) => {
          const active = location.pathname === path;
          return (
            <button
              key={path}
              onClick={() => navigate(path)}
              className={cn(
                'flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors text-left',
                active
                  ? 'bg-muted text-foreground'
                  : 'text-muted-foreground hover:bg-muted hover:text-foreground'
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              {label}
            </button>
          );
        })}
      </nav>
    </aside>
  );
}
