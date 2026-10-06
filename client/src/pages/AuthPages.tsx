import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Lock, Mail, AlertCircle, Sparkles, CheckCircle2 } from 'lucide-react';

export const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/dashboard';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await login(email, password);
      navigate(from, { replace: true });
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Invalid credentials or account locked.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleQuickLogin = (roleEmail: string, rolePass: string) => {
    setEmail(roleEmail);
    setPassword(rolePass);
  };

  return (
    <div className="flex min-h-screen w-full items-center justify-center p-4 bg-[#F8FAFC]">
      <div className="w-full max-w-md rounded-[10px] border border-[#E2E8F0] bg-white p-8 shadow-lg">
        {/* Header */}
        <div className="mb-6 text-center">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-tr from-blue-600 to-blue-500 font-black text-white shadow-sm text-xl">
            EMS
          </div>
          <h2 className="text-2xl font-bold tracking-tight text-slate-900">Employee Management</h2>
          <p className="mt-1 text-xs text-slate-500">
            Sign in to access your dashboard and workspace
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-5 flex items-center gap-2.5 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
            <AlertCircle className="h-4 w-4 shrink-0 text-red-500" />
            <span>{error}</span>
          </div>
        )}

        {/* Login Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1.5 block text-xs font-semibold text-slate-700">Email Address</label>
            <div className="relative">
              <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                <Mail className="h-4 w-4" />
              </div>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="admin@company.com"
                className="w-full rounded-lg border border-[#E2E8F0] bg-white py-2.5 pl-9 pr-3 text-xs text-slate-900 placeholder-slate-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="mb-1.5 block text-xs font-semibold text-slate-700">Password</label>
            <div className="relative">
              <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
                <Lock className="h-4 w-4" />
              </div>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full rounded-lg border border-[#E2E8F0] bg-white py-2.5 pl-9 pr-3 text-xs text-slate-900 placeholder-slate-400 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-lg bg-[#2563EB] py-2.5 text-xs font-bold text-white shadow-sm transition hover:bg-blue-700 focus:outline-none disabled:opacity-50"
          >
            {loading ? 'Authenticating...' : 'Sign In'}
          </button>
        </form>

        {/* Quick Demo Logins */}
        <div className="mt-6 border-t border-[#E2E8F0] pt-4">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-500 mb-2">
            <Sparkles className="h-3.5 w-3.5 text-amber-500" />
            <span>Preset Roles (Quick Fill)</span>
          </div>
          <div className="grid grid-cols-2 gap-2 text-[11px]">
            <button
              type="button"
              onClick={() => handleQuickLogin('admin@company.com', 'Admin@123')}
              className="rounded-lg border border-[#E2E8F0] bg-slate-50 px-2 py-1.5 text-left text-slate-700 hover:bg-slate-100 transition"
            >
              <span className="font-semibold text-blue-600 block">👑 Admin</span>
              <span className="text-[10px] text-slate-400">admin@company.com</span>
            </button>
            <button
              type="button"
              onClick={() => handleQuickLogin('hr@company.com', 'Hr@12345')}
              className="rounded-lg border border-[#E2E8F0] bg-slate-50 px-2 py-1.5 text-left text-slate-700 hover:bg-slate-100 transition"
            >
              <span className="font-semibold text-pink-600 block">💼 HR Officer</span>
              <span className="text-[10px] text-slate-400">hr@company.com</span>
            </button>
            <button
              type="button"
              onClick={() => handleQuickLogin('manager@company.com', 'Manager@123')}
              className="rounded-lg border border-[#E2E8F0] bg-slate-50 px-2 py-1.5 text-left text-slate-700 hover:bg-slate-100 transition"
            >
              <span className="font-semibold text-green-600 block">⚡ Manager</span>
              <span className="text-[10px] text-slate-400">manager@company.com</span>
            </button>
            <button
              type="button"
              onClick={() => handleQuickLogin('employee@company.com', 'Employee@123')}
              className="rounded-lg border border-[#E2E8F0] bg-slate-50 px-2 py-1.5 text-left text-slate-700 hover:bg-slate-100 transition"
            >
              <span className="font-semibold text-sky-600 block">👤 Employee</span>
              <span className="text-[10px] text-slate-400">employee@company.com</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export const Unauthorized: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="flex h-[75vh] flex-col items-center justify-center text-center">
      <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-red-50 text-red-600 border border-red-200 mb-4">
        <AlertCircle className="h-8 w-8" />
      </div>
      <h1 className="text-2xl font-bold text-slate-900">403 — Access Forbidden</h1>
      <p className="mt-2 max-w-md text-xs text-slate-500">
        You do not have the required role or permission claim to access this resource. Horizontal access control and backend RBAC policy enforcement prevented this action.
      </p>
      <button
        onClick={() => navigate('/dashboard')}
        className="mt-6 rounded-lg bg-[#2563EB] px-4 py-2 text-xs font-bold text-white hover:bg-blue-700 transition shadow-sm"
      >
        Return to Dashboard
      </button>
    </div>
  );
};

export const NotFound: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="flex h-[75vh] flex-col items-center justify-center text-center">
      <h1 className="text-5xl font-extrabold text-[#2563EB] tracking-tight">404</h1>
      <h2 className="mt-2 text-xl font-bold text-slate-900">Page Not Found</h2>
      <p className="mt-1 text-xs text-slate-500">
        The route you requested does not exist or has been moved.
      </p>
      <button
        onClick={() => navigate('/dashboard')}
        className="mt-5 rounded-lg bg-[#2563EB] px-4 py-2 text-xs font-bold text-white hover:bg-blue-700 transition shadow-sm"
      >
        Back to Dashboard
      </button>
    </div>
  );
};
