import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserCircle2, UserPen, LogOut, Wand2 } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { EditProfileDialog } from '@/components/EditProfileDialog';
import { seedService } from '@/services/api';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface UserMenuProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function UserMenu({ showNotification }: UserMenuProps): React.ReactElement {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent): void => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = (): void => {
    setOpen(false);
    logout();
    navigate('/login');
  };

  const handleEditProfile = (): void => {
    setOpen(false);
    setProfileOpen(true);
  };

  const handleGenerateDemoData = async (): Promise<void> => {
    setOpen(false);
    if (!window.confirm('This will delete all your current tasks and replace them with 13 demo tasks. Continue?')) return;
    try {
      const result = await seedService.seedMe();
      showNotification(`Demo data ready — ${result.tasksCreated} tasks created. Refreshing…`, 'success');
      setTimeout(() => window.location.reload(), 1200);
    } catch {
      showNotification('Failed to generate demo data.', 'error');
    }
  };

  const displayName = user?.fullName || user?.username || '';

  return (
    <>
      <div ref={ref} className="relative">
        <button
          onClick={() => setOpen((o) => !o)}
          className="flex items-center gap-2 rounded-full px-2 py-1 text-primary-foreground hover:bg-white/10 transition-colors cursor-pointer"
          aria-label="User menu"
        >
          <UserCircle2 className="h-7 w-7" />
          <span className="text-sm font-medium hidden sm:inline">{displayName}</span>
        </button>

        {open && (
          <div className="absolute right-0 top-full mt-2 w-52 rounded-lg border bg-background shadow-lg z-50 py-1 text-foreground">
            <div className="px-3 py-2 border-b">
              <p className="text-sm font-semibold truncate">{displayName}</p>
              <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
            </div>

            <button
              onClick={handleEditProfile}
              className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-muted transition-colors cursor-pointer"
            >
              <UserPen className="h-4 w-4 text-muted-foreground" />
              Edit Profile
            </button>

            <button
              onClick={handleGenerateDemoData}
              className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-muted transition-colors cursor-pointer"
            >
              <Wand2 className="h-4 w-4 text-muted-foreground" />
              Generate Demo Data
            </button>

            <div className="border-t my-1" />

            <button
              onClick={handleLogout}
              className="flex w-full items-center gap-2 px-3 py-2 text-sm text-destructive hover:bg-muted transition-colors cursor-pointer"
            >
              <LogOut className="h-4 w-4" />
              Logout
            </button>
          </div>
        )}
      </div>

      <EditProfileDialog
        open={profileOpen}
        onClose={() => setProfileOpen(false)}
        showNotification={showNotification}
      />
    </>
  );
}
