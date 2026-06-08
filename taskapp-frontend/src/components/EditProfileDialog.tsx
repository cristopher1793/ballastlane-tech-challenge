import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Spinner } from '@/components/ui/spinner';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { authService } from '@/services/api';
import { useAuth } from '@/context/AuthContext';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface EditProfileDialogProps {
  open: boolean;
  onClose: () => void;
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function EditProfileDialog({ open, onClose, showNotification }: EditProfileDialogProps): React.ReactElement {
  const { user, updateUser } = useAuth();

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (open && user) {
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setUsername(user.username);
      setEmail(user.email);
      setCurrentPassword('');
      setNewPassword('');
      setError('');
    }
  }, [open, user]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      const updated = await authService.updateProfile({
        firstName,
        lastName,
        username,
        email,
        currentPassword: currentPassword || undefined,
        newPassword: newPassword || undefined,
      });
      updateUser(updated);
      showNotification('Profile updated successfully.', 'success');
      onClose();
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response) {
        const data = err.response.data as { error?: string };
        setError(data?.error ?? 'Update failed.');
      } else {
        setError('An unexpected error occurred.');
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Profile</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="prof-firstName">First Name</Label>
              <Input
                id="prof-firstName"
                placeholder="John"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="prof-lastName">Last Name</Label>
              <Input
                id="prof-lastName"
                placeholder="Doe"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="prof-username">Username</Label>
            <Input
              id="prof-username"
              placeholder="johndoe"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="prof-email">Email</Label>
            <Input
              id="prof-email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="border-t pt-4 space-y-3">
            <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Change Password (optional)</p>
            <div className="space-y-1.5">
              <Label htmlFor="prof-currentPwd">Current Password</Label>
              <Input
                id="prof-currentPwd"
                type="password"
                placeholder="Enter current password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="prof-newPwd">New Password</Label>
              <Input
                id="prof-newPwd"
                type="password"
                placeholder="Min. 8 characters"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={onClose} disabled={saving}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving}>
              {saving ? <Spinner size="sm" /> : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
