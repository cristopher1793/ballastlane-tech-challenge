import React, { useState, useEffect, useCallback } from 'react';
import { ShieldCheck, ShieldOff } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Spinner } from '@/components/ui/spinner';
import { authService } from '@/services/api';
import { useAuth } from '@/context/AuthContext';
import { cn } from '@/lib/utils';
import type { UserResponseDto } from '@/types';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface UsersPageProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function UsersPage({ showNotification }: UsersPageProps): React.ReactElement {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<UserResponseDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [acting, setActing] = useState<string | null>(null);

  const loadUsers = useCallback(async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await authService.getAllUsers();
      setUsers(data);
    } catch {
      showNotification('Failed to load users.', 'error');
    } finally {
      setLoading(false);
    }
  }, [showNotification]);

  useEffect(() => { loadUsers(); }, [loadUsers]);

  const handleLock = async (u: UserResponseDto): Promise<void> => {
    setActing(u.id);
    try {
      const updated = await authService.lockUser(u.id);
      setUsers((prev) => prev.map((x) => (x.id === updated.id ? updated : x)));
      showNotification(`${u.username} has been locked.`, 'success');
    } catch {
      showNotification('Failed to lock account.', 'error');
    } finally {
      setActing(null);
    }
  };

  const handleUnlock = async (u: UserResponseDto): Promise<void> => {
    setActing(u.id);
    try {
      const updated = await authService.unlockUser(u.id);
      setUsers((prev) => prev.map((x) => (x.id === updated.id ? updated : x)));
      showNotification(`${u.username} has been unlocked.`, 'success');
    } catch {
      showNotification('Failed to unlock account.', 'error');
    } finally {
      setActing(null);
    }
  };

  const lockedCount = users.filter((u) => u.isLocked).length;

  return (
    <div className="mx-auto max-w-5xl px-4 py-6">
      <div className="mb-5">
        <h1 className="text-2xl font-bold">Users</h1>
        {lockedCount > 0 && (
          <p className="text-sm text-destructive mt-0.5">
            {lockedCount} account{lockedCount > 1 ? 's' : ''} locked
          </p>
        )}
      </div>

      {loading ? (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      ) : (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Username</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-center w-24">Attempts</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {users.map((u) => {
                const isSelf = u.id === currentUser?.id;
                const isBusy = acting === u.id;
                return (
                  <TableRow key={u.id} className={u.isLocked ? 'bg-red-50/40' : ''}>
                    <TableCell className="font-medium">{u.fullName}</TableCell>
                    <TableCell className="text-muted-foreground">{u.username}</TableCell>
                    <TableCell className="text-muted-foreground">{u.email}</TableCell>
                    <TableCell>
                      <span className={cn(
                        'rounded-full px-2.5 py-0.5 text-xs font-semibold',
                        u.role === 'Admin'
                          ? 'bg-purple-100 text-purple-800'
                          : 'bg-sky-100 text-sky-800'
                      )}>
                        {u.role}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className={cn(
                        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold',
                        u.isLocked ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'
                      )}>
                        {u.isLocked
                          ? <><ShieldOff className="h-3 w-3" /> Locked</>
                          : <><ShieldCheck className="h-3 w-3" /> Active</>}
                      </span>
                    </TableCell>
                    <TableCell className="text-center text-sm">
                      <span className={u.failedLoginAttempts > 0 ? 'font-medium text-destructive' : 'text-muted-foreground'}>
                        {u.failedLoginAttempts}
                      </span>
                    </TableCell>
                    <TableCell className="text-right">
                      {isSelf ? (
                        <span className="text-xs text-muted-foreground">You</span>
                      ) : isBusy ? (
                        <Spinner size="sm" />
                      ) : u.isLocked ? (
                        <Button size="sm" variant="outline" onClick={() => handleUnlock(u)}>
                          Unlock
                        </Button>
                      ) : (
                        <Button
                          size="sm"
                          variant="outline"
                          className="text-destructive hover:text-destructive border-destructive/40 hover:bg-destructive/5"
                          onClick={() => handleLock(u)}
                        >
                          Lock
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
