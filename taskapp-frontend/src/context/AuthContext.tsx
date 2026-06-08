import React, { createContext, useContext, useState, useCallback, ReactNode } from 'react';
import type { UserResponseDto, LoginResponseDto } from '../types';
import { setAuthToken } from '../services/api';

const SESSION_TOKEN_KEY = 'taskapp_token';
const SESSION_USER_KEY = 'taskapp_user';

function restoreSession(): { token: string; user: UserResponseDto } | null {
  try {
    const token = sessionStorage.getItem(SESSION_TOKEN_KEY);
    const raw = sessionStorage.getItem(SESSION_USER_KEY);
    if (token && raw) {
      return { token, user: JSON.parse(raw) as UserResponseDto };
    }
  } catch {
    // corrupted storage — ignore
  }
  return null;
}

interface AuthContextValue {
  user: UserResponseDto | null;
  isAuthenticated: boolean;
  login: (response: LoginResponseDto) => void;
  logout: () => void;
  updateUser: (user: UserResponseDto) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps): React.ReactElement {
  const [user, setUser] = useState<UserResponseDto | null>(() => {
    const saved = restoreSession();
    if (saved) {
      setAuthToken(saved.token);
      return saved.user;
    }
    return null;
  });

  const login = useCallback((response: LoginResponseDto): void => {
    setAuthToken(response.token);
    sessionStorage.setItem(SESSION_TOKEN_KEY, response.token);
    sessionStorage.setItem(SESSION_USER_KEY, JSON.stringify(response.user));
    setUser(response.user);
  }, []);

  const logout = useCallback((): void => {
    setAuthToken(null);
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
    sessionStorage.removeItem(SESSION_USER_KEY);
    setUser(null);
  }, []);

  const updateUser = useCallback((updated: UserResponseDto): void => {
    sessionStorage.setItem(SESSION_USER_KEY, JSON.stringify(updated));
    setUser(updated);
  }, []);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    login,
    logout,
    updateUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
