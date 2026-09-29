import React, { createContext, useContext, useState, useEffect } from 'react';
import { api, User } from '../api';

interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
  hasRole: (role: string | string[]) => boolean;
  refreshUserData: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    const savedUser = localStorage.getItem('ems_user');
    return savedUser ? JSON.parse(savedUser) : null;
  });
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('ems_access_token'));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem('ems_access_token');
      if (storedToken) {
        try {
          const res = await api.auth.me();
          if (res.data.success && res.data.data) {
            setUser(res.data.data);
            localStorage.setItem('ems_user', JSON.stringify(res.data.data));
          }
        } catch {
          // Handled by axios refresh interceptor
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    const res = await api.auth.login({ email, password });
    if (res.data.success && res.data.data) {
      const { accessToken, refreshToken, user: userData } = res.data.data;
      localStorage.setItem('ems_access_token', accessToken);
      localStorage.setItem('ems_refresh_token', refreshToken);
      localStorage.setItem('ems_user', JSON.stringify(userData));
      setToken(accessToken);
      setUser(userData);
    } else {
      throw new Error(res.data.message || 'Login failed');
    }
  };

  const logout = async () => {
    const refreshToken = localStorage.getItem('ems_refresh_token');
    if (refreshToken) {
      try {
        await api.auth.logout(refreshToken);
      } catch {
        // Ignore logout error
      }
    }
    localStorage.removeItem('ems_access_token');
    localStorage.removeItem('ems_refresh_token');
    localStorage.removeItem('ems_user');
    setToken(null);
    setUser(null);
  };

  const refreshUserData = async () => {
    try {
      const res = await api.auth.me();
      if (res.data.success && res.data.data) {
        setUser(res.data.data);
        localStorage.setItem('ems_user', JSON.stringify(res.data.data));
      }
    } catch {
      // Ignore
    }
  };

  const hasPermission = (permission: string): boolean => {
    if (!user) return false;
    if (user.roles.includes('Admin') || user.permissions.includes('*')) return true;
    return user.permissions.includes(permission);
  };

  const hasRole = (role: string | string[]): boolean => {
    if (!user) return false;
    if (Array.isArray(role)) {
      return role.some((r) => user.roles.includes(r));
    }
    return user.roles.includes(role);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        loading,
        login,
        logout,
        hasPermission,
        hasRole,
        refreshUserData,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
