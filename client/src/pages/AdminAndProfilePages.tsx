import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, User, Role, Permission, Notification } from '../api';
import { ShieldCheck, ShieldAlert, Key, Bell, User as UserIcon, Lock, CheckCircle2, AlertCircle } from 'lucide-react';

export const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await api.auth.getUsers();
      if (res.data.success) setUsers(res.data.data);
    } catch {}
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <ShieldCheck className="h-6 w-6 text-[#2563EB]" />
          User Account Governance
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Authentication status, lockout controls, and linked accounts</p>
      </div>

      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <table className="w-full text-left text-xs text-slate-600">
          <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
            <tr>
              <th className="px-4 py-3">User Email</th>
              <th className="px-4 py-3">Linked Employee</th>
              <th className="px-4 py-3">Roles</th>
              <th className="px-4 py-3">Account Status</th>
              <th className="px-4 py-3">Last Login</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {loading ? (
              <tr><td colSpan={5} className="py-8 text-center text-slate-400">Loading users...</td></tr>
            ) : users.length === 0 ? (
              <tr><td colSpan={5} className="py-8 text-center text-slate-400">No users found.</td></tr>
            ) : (
              users.map((u) => (
                <tr key={u.id} className="hover:bg-slate-50 transition">
                  <td className="px-4 py-3 font-semibold text-slate-900">{u.email}</td>
                  <td className="px-4 py-3 text-slate-700">{u.fullName || 'System Account'}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-1">
                      {u.roles.map((r) => (
                        <span key={r} className="rounded-full bg-[#EFF6FF] px-2 py-0.5 text-[10px] font-semibold text-[#2563EB] border border-blue-200">
                          {r}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                      u.isLocked
                        ? 'bg-[#FEE2E2] text-[#DC2626] border border-red-200'
                        : 'bg-[#DCFCE7] text-[#15803D] border border-green-200'
                    }`}>
                      {u.isLocked ? 'Locked' : 'Active'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-500">
                    {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : 'Never'}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export const RolesPage: React.FC = () => {
  const [roles, setRoles] = useState<Role[]>([]);

  useEffect(() => {
    fetchRoles();
  }, []);

  const fetchRoles = async () => {
    try {
      const res = await api.auth.getRoles();
      if (res.data.success) setRoles(res.data.data);
    } catch {}
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <ShieldAlert className="h-6 w-6 text-[#2563EB]" />
          Roles & RBAC Matrix
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Role definitions and assigned permission policies</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {roles.map((r) => (
          <div key={r.id} className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-3 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
            <div className="flex justify-between items-start">
              <div>
                <h3 className="text-sm font-bold text-slate-900 flex items-center gap-2">
                  <span>{r.name}</span>
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">{r.description || 'System Role'}</p>
              </div>
              <span className="rounded-full bg-[#EFF6FF] px-2.5 py-0.5 text-[10px] font-mono font-semibold text-[#2563EB] border border-blue-200">
                {r.permissions.length} perms
              </span>
            </div>

            <div className="border-t border-[#E2E8F0] pt-3">
              <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 block mb-2">Assigned Permissions</span>
              <div className="flex flex-wrap gap-1 max-h-40 overflow-y-auto pr-1">
                {r.permissions.map((p) => (
                  <span key={p.id} className="rounded bg-slate-50 px-2 py-1 text-[10px] font-mono text-slate-700 border border-[#E2E8F0]">
                    {p.code}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export const PermissionsPage: React.FC = () => {
  const [permissions, setPermissions] = useState<Permission[]>([]);

  useEffect(() => {
    fetchPermissions();
  }, []);

  const fetchPermissions = async () => {
    try {
      const res = await api.auth.getPermissions();
      if (res.data.success) setPermissions(res.data.data);
    } catch {}
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <Key className="h-6 w-6 text-[#D97706]" />
          System Permission Catalog
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Granular resource operation permission strings</p>
      </div>

      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <table className="w-full text-left text-xs text-slate-600">
          <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
            <tr>
              <th className="px-4 py-3">Code</th>
              <th className="px-4 py-3">Description</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 font-mono">
            {permissions.map((p) => (
              <tr key={p.id} className="hover:bg-slate-50 transition">
                <td className="px-4 py-2.5 font-bold text-[#2563EB]">{p.code}</td>
                <td className="px-4 py-2.5 text-slate-700 font-sans">{p.description || 'System policy'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export const NotificationsPage: React.FC = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchNotifications();
  }, []);

  const fetchNotifications = async () => {
    setLoading(true);
    try {
      const res = await api.notifications.getAll(false);
      if (res.data.success) setNotifications(res.data.data);
    } catch {}
    finally { setLoading(false); }
  };

  const handleMarkAsRead = async (id: number) => {
    try {
      await api.notifications.markAsRead(id);
      fetchNotifications();
    } catch {}
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <Bell className="h-6 w-6 text-[#2563EB]" />
          My In-App Notifications
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Alerts, leave approval results, and payroll events</p>
      </div>

      <div className="space-y-3">
        {notifications.length === 0 ? (
          <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-8 text-center text-xs text-slate-400 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
            No notifications in your inbox.
          </div>
        ) : (
          notifications.map((n) => (
            <div
              key={n.id}
              onClick={() => handleMarkAsRead(n.id)}
              className={`cursor-pointer rounded-[10px] border p-4 transition shadow-sm ${
                n.isRead ? 'border-[#E2E8F0] bg-white opacity-75' : 'border-blue-200 bg-[#EFF6FF]'
              }`}
            >
              <div className="flex justify-between items-start">
                <div>
                  <h4 className="text-xs font-bold text-slate-900 flex items-center gap-2">
                    {!n.isRead && <span className="h-2 w-2 rounded-full bg-[#2563EB]"></span>}
                    {n.title}
                  </h4>
                  <p className="text-xs text-slate-600 mt-1">{n.message}</p>
                </div>
                <span className="text-[10px] text-slate-400 font-mono">
                  {new Date(n.createdAt).toLocaleString()}
                </span>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export const ProfilePage: React.FC = () => {
  const { user } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [msg, setMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setMsg({ type: 'error', text: 'New passwords do not match.' });
      return;
    }
    setMsg(null);
    try {
      await api.auth.changePassword({ currentPassword, newPassword });
      setMsg({ type: 'success', text: 'Password changed successfully! Refresh tokens rotated.' });
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      setMsg({ type: 'error', text: err.response?.data?.message || 'Failed to update password.' });
    }
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <UserIcon className="h-6 w-6 text-[#2563EB]" />
          Account Profile & Security
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Manage authentication credentials and user profile</p>
      </div>

      {msg && (
        <div className={`rounded-[10px] border p-3 text-xs flex items-center gap-2 ${
          msg.type === 'success' ? 'border-green-200 bg-[#DCFCE7] text-[#15803D]' : 'border-red-200 bg-[#FEE2E2] text-[#DC2626]'
        }`}>
          {msg.type === 'success' ? <CheckCircle2 className="h-4 w-4 shrink-0" /> : <AlertCircle className="h-4 w-4 shrink-0" />}
          <span>{msg.text}</span>
        </div>
      )}

      {/* Profile Overview */}
      <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-3 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Account Identity</h3>
        <div className="grid grid-cols-2 gap-4 text-xs">
          <div>
            <span className="text-slate-500 block">Name:</span>
            <span className="font-semibold text-slate-900">{user?.fullName}</span>
          </div>
          <div>
            <span className="text-slate-500 block">Email:</span>
            <span className="font-semibold text-slate-900">{user?.email}</span>
          </div>
          <div>
            <span className="text-slate-500 block">Roles:</span>
            <span className="font-semibold text-[#2563EB]">{user?.roles.join(', ')}</span>
          </div>
          <div>
            <span className="text-slate-500 block">User ID:</span>
            <span className="font-mono text-slate-700">#{user?.id}</span>
          </div>
        </div>
      </div>

      {/* Change Password */}
      <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Change Password (PBKDF2 100k Iterations)</h3>
        <form onSubmit={handleChangePassword} className="space-y-4 text-xs">
          <div>
            <label className="block mb-1 font-semibold text-slate-700">Current Password *</label>
            <input
              type="password"
              required
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
            />
          </div>
          <div>
            <label className="block mb-1 font-semibold text-slate-700">New Password (≥8 chars, uppercase, lowercase, digit) *</label>
            <input
              type="password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
            />
          </div>
          <div>
            <label className="block mb-1 font-semibold text-slate-700">Confirm New Password *</label>
            <input
              type="password"
              required
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
            />
          </div>
          <button
            type="submit"
            className="rounded-lg bg-[#2563EB] px-4 py-2 text-xs font-bold text-white hover:bg-blue-700 transition shadow-sm"
          >
            Update Password
          </button>
        </form>
      </div>
    </div>
  );
};
