import { useEffect } from 'react';
import { toast } from 'sonner';

export type NotificationSeverity = 'success' | 'error' | 'warning' | 'info';

interface UseNotificationReturn {
  showNotification: (message: string, severity: NotificationSeverity) => void;
}

export function useNotification(): UseNotificationReturn {
  const showNotification = (message: string, severity: NotificationSeverity): void => {
    if (severity === 'success') toast.success(message);
    else if (severity === 'error') toast.error(message);
    else if (severity === 'warning') toast.warning(message);
    else toast.info(message);
  };

  useEffect(() => {
    const handleRateLimited = (e: Event): void => {
      toast.warning((e as CustomEvent<string>).detail);
    };
    const handleAccountLocked = (e: Event): void => {
      toast.error((e as CustomEvent<string>).detail);
    };

    window.addEventListener('app:rate-limited', handleRateLimited);
    window.addEventListener('app:account-locked', handleAccountLocked);

    return () => {
      window.removeEventListener('app:rate-limited', handleRateLimited);
      window.removeEventListener('app:account-locked', handleAccountLocked);
    };
  }, []);

  return { showNotification };
}
