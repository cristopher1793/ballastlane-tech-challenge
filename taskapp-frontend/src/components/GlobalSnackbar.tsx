import React from 'react';
import { Toaster } from 'sonner';

export function GlobalSnackbar(): React.ReactElement {
  return <Toaster position="top-center" richColors />;
}
