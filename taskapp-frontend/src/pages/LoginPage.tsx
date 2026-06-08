import React, { useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { ClipboardList } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Spinner } from '@/components/ui/spinner';
import { authService } from '@/services/api';
import { useAuth } from '@/context/AuthContext';
import type { NotificationSeverity } from '@/hooks/useNotification';

interface LoginPageProps {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function LoginPage({ showNotification }: LoginPageProps): React.ReactElement {
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>): Promise<void> => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const response = await authService.login({ email, password });
      login(response);
      navigate('/dashboard');
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response) {
        if (err.response.status === 403) {
          const msg = 'Your account is locked. Please contact an administrator.';
          setError(msg);
          showNotification(msg, 'error');
        } else {
          const data = err.response.data as { error?: string };
          setError(data?.error ?? 'Login failed.');
        }
      } else {
        setError('An unexpected error occurred.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-[calc(100vh-56px)] items-center justify-center bg-muted/40 px-4">
      <div className="w-full max-w-sm">

        <div className="flex flex-col items-center mb-6">
          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary mb-3">
            <ClipboardList className="h-7 w-7 text-primary-foreground" />
          </div>
          <h1 className="text-2xl font-bold">Welcome back</h1>
          <p className="text-sm text-muted-foreground mt-1">Sign in to your TaskApp account</p>
        </div>

        <div className="rounded-xl border bg-background shadow-sm p-6 space-y-5">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? <Spinner size="sm" /> : 'Sign In'}
            </Button>
          </form>

          <p className="text-center text-sm text-muted-foreground">
            Don&apos;t have an account?{' '}
            <RouterLink to="/register" className="text-primary font-medium hover:underline cursor-pointer">
              Create one
            </RouterLink>
          </p>
        </div>

      </div>
    </div>
  );
}
