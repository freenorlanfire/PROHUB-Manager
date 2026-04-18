import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import { ToastContainer, type ToastItem, type ToastType } from '../components/Toast';

interface ToastContextValue {
  toast: (message: string, type?: ToastType, duration?: number) => void;
  success: (msg: string) => void;
  error: (msg: string) => void;
  info: (msg: string) => void;
  warn: (msg: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const toast = useCallback((message: string, type: ToastType = 'info', duration = 4000) => {
    const id = crypto.randomUUID();
    setToasts(prev => [...prev, { id, type, message, duration }]);
  }, []);

  const remove = useCallback((id: string) =>
    setToasts(prev => prev.filter(t => t.id !== id)), []);

  return (
    <ToastContext.Provider value={{
      toast,
      success: msg => toast(msg, 'success'),
      error: msg => toast(msg, 'error'),
      info: msg => toast(msg, 'info'),
      warn: msg => toast(msg, 'warning'),
    }}>
      {children}
      <ToastContainer toasts={toasts} onRemove={remove} />
    </ToastContext.Provider>
  );
}

export const useToast = () => {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be inside ToastProvider');
  return ctx;
};
