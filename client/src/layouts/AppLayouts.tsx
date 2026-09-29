import React, { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { api, Notification } from '../api';
import {
  LayoutDashboard,
  Users,
  Building2,
  Briefcase,
  Clock,
  CalendarDays,
  DollarSign,
  FileSpreadsheet,
  FolderGit2,
  ShieldCheck,
  ShieldAlert,
  History,
  BarChart3,
  Bell,
  LogOut,
  User as UserIcon,
  Menu,
  X,
  ChevronDown
} from 'lucide-react';

export const AppShell: React.FC = () => {
  const { user, logout, hasPermission, hasRole } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifDropdownOpen, setNotifDropdownOpen] = useState(false);
  const [userDropdownOpen, setUserDropdownOpen] = useState(false);

  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 30000);
    return () => clearInterval(interval);
  }, []);

  const fetchNotifications = async () => {
    try {
      const countRes = await api.notifications.getUnreadCount();
      if (countRes.data.success) {
        setUnreadCount(countRes.data.data.unreadCount);
      }
      const notifRes = await api.notifications.getAll(false);
      if (notifRes.data.success) {
        setNotifications(notifRes.data.data.slice(0, 5));
      }
    } catch {
      // Ignore
    }
  };

  const handleMarkAsRead = async (id: number) => {
    try {
      await api.notifications.markAsRead(id);
      fetchNotifications();
    } catch {
      // Ignore
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const navSections = [
    {
      title: 'Main',
      items: [
        { label: 'Dashboard', path: '/dashboard', icon: LayoutDashboard, show: true },
        { label: 'Employees', path: '/employees', icon: Users, show: hasPermission('Employee.Read') || hasPermission('Employee.Read.Team') || hasPermission('Employee.Read.Own') },
        { label: 'Departments', path: '/departments', icon: Building2, show: hasPermission('Department.Read') },
        { label: 'Positions', path: '/positions', icon: Briefcase, show: hasPermission('Position.Read') },
      ],
    },
    {
      title: 'Work & Time',
      items: [
        { label: 'Attendance', path: '/attendance', icon: Clock, show: true },
        { label: 'Leaves', path: '/leaves', icon: CalendarDays, show: true },
        { label: 'Projects', path: '/projects', icon: FolderGit2, show: true },
      ],
    },
    {
      title: 'Compensation',
      items: [
        { label: 'Salary Structures', path: '/salary', icon: DollarSign, show: hasRole(['Admin', 'HR']) },
        { label: 'My Payslips', path: '/payslips', icon: FileSpreadsheet, show: true },
      ],
    },
    {
      title: 'Administration',
      items: [
        { label: 'Users', path: '/users', icon: ShieldCheck, show: hasPermission('User.Manage') },
        { label: 'Roles & RBAC', path: '/roles', icon: ShieldAlert, show: hasRole('Admin') },
        { label: 'Audit Logs', path: '/audit-logs', icon: History, show: hasPermission('AuditLog.Read') },
        { label: 'Reports & Analytics', path: '/reports', icon: BarChart3, show: hasRole(['Admin', 'HR', 'Manager']) },
      ],
    },
  ];

  return (
    <div className="flex min-h-screen bg-slate-950 text-slate-100 font-sans">
      {/* Mobile Sidebar Backdrop */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Sidebar Navigation */}
      <aside
        className={`fixed inset-y-0 left-0 z-50 w-72 flex-col justify-between border-r border-slate-800 bg-slate-900/95 backdrop-blur-md p-4 transition-transform duration-300 ease-in-out lg:static lg:flex lg:translate-x-0 ${
          sidebarOpen ? 'translate-x-0 flex' : '-translate-x-full hidden'
        }`}
      >
        <div className="flex flex-col gap-6">
          {/* Brand Logo */}
          <div className="flex items-center justify-between px-2">
            <Link to="/dashboard" className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-tr from-indigo-600 to-indigo-400 font-bold text-white shadow-lg shadow-indigo-500/20 text-lg">
                EMS
              </div>
              <div>
                <h1 className="text-base font-bold text-white leading-tight">Enterprise EMS</h1>
                <p className="text-xs text-slate-400">Enterprise Edition</p>
              </div>
            </Link>
            <button
              onClick={() => setSidebarOpen(false)}
              className="text-slate-400 hover:text-white lg:hidden"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Navigation Links */}
          <nav className="flex flex-col gap-5 overflow-y-auto max-h-[calc(100vh-160px)] pr-1">
            {navSections.map((section, idx) => {
              const visibleItems = section.items.filter((item) => item.show);
              if (visibleItems.length === 0) return null;

              return (
                <div key={idx} className="flex flex-col gap-1">
                  <p className="px-3 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                    {section.title}
                  </p>
                  {visibleItems.map((item) => {
                    const Icon = item.icon;
                    const isActive = location.pathname === item.path || (item.path !== '/dashboard' && location.pathname.startsWith(item.path));
                    return (
                      <Link
                        key={item.path}
                        to={item.path}
                        onClick={() => setSidebarOpen(false)}
                        className={`group flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-all ${
                          isActive
                            ? 'bg-indigo-600/15 text-indigo-400 border border-indigo-500/30'
                            : 'text-slate-400 hover:bg-slate-800 hover:text-slate-100'
                        }`}
                      >
                        <Icon className={`h-4 w-4 transition-colors ${isActive ? 'text-indigo-400' : 'text-slate-400 group-hover:text-slate-200'}`} />
                        <span>{item.label}</span>
                      </Link>
                    );
                  })}
                </div>
              );
            })}
          </nav>
        </div>

        {/* User Card in Sidebar Footer */}
        <div className="border-t border-slate-800 pt-3">
          <div className="flex items-center justify-between rounded-lg bg-slate-800/40 p-2 border border-slate-800/60">
            <div className="flex items-center gap-2.5 overflow-hidden">
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-indigo-500/20 text-indigo-300 font-semibold text-sm border border-indigo-500/30">
                {user?.fullName?.charAt(0) || 'U'}
              </div>
              <div className="flex flex-col overflow-hidden">
                <span className="truncate text-xs font-semibold text-white">{user?.fullName || user?.email}</span>
                <span className="truncate text-[10px] text-slate-400 font-medium">
                  {user?.roles?.join(', ') || 'Employee'}
                </span>
              </div>
            </div>
            <button
              onClick={handleLogout}
              title="Sign Out"
              className="rounded p-1.5 text-slate-400 hover:bg-rose-500/20 hover:text-rose-300 transition"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Top Navbar */}
        <header className="sticky top-0 z-30 flex h-16 w-full items-center justify-between border-b border-slate-800 bg-slate-900/80 backdrop-blur-md px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setSidebarOpen(true)}
              className="rounded-lg p-2 text-slate-400 hover:bg-slate-800 hover:text-white lg:hidden"
            >
              <Menu className="h-5 w-5" />
            </button>
            <div className="hidden sm:flex items-center gap-2 text-xs text-slate-400">
              <span className="rounded bg-emerald-500/10 px-2 py-0.5 text-emerald-400 border border-emerald-500/20 font-medium">
                Connected
              </span>
              <span>•</span>
              <span>PostgreSQL & ASP.NET Core API</span>
            </div>
          </div>

          <div className="flex items-center gap-4">
            {/* Notification Bell */}
            <div className="relative">
              <button
                onClick={() => {
                  setNotifDropdownOpen(!notifDropdownOpen);
                  setUserDropdownOpen(false);
                }}
                className="relative rounded-lg p-2 text-slate-300 hover:bg-slate-800 hover:text-white transition"
              >
                <Bell className="h-5 w-5" />
                {unreadCount > 0 && (
                  <span className="absolute top-1 right-1 flex h-4 w-4 items-center justify-center rounded-full bg-rose-500 text-[10px] font-bold text-white animate-pulse">
                    {unreadCount}
                  </span>
                )}
              </button>

              {/* Notification Dropdown Menu */}
              {notifDropdownOpen && (
                <div className="absolute right-0 mt-2 w-80 rounded-xl border border-slate-800 bg-slate-900 shadow-2xl p-3 z-50">
                  <div className="flex items-center justify-between border-b border-slate-800 pb-2 mb-2">
                    <span className="text-xs font-bold uppercase tracking-wider text-slate-300">Notifications</span>
                    <Link
                      to="/notifications"
                      onClick={() => setNotifDropdownOpen(false)}
                      className="text-xs text-indigo-400 hover:underline"
                    >
                      View All
                    </Link>
                  </div>
                  <div className="flex flex-col gap-2 max-h-64 overflow-y-auto">
                    {notifications.length === 0 ? (
                      <p className="py-4 text-center text-xs text-slate-500">No new notifications</p>
                    ) : (
                      notifications.map((notif) => (
                        <div
                          key={notif.id}
                          onClick={() => handleMarkAsRead(notif.id)}
                          className={`cursor-pointer rounded-lg p-2.5 transition border ${
                            notif.isRead
                              ? 'bg-slate-950/40 border-slate-800/40 opacity-70'
                              : 'bg-indigo-950/30 border-indigo-500/30'
                          }`}
                        >
                          <p className="text-xs font-semibold text-white">{notif.title}</p>
                          <p className="text-[11px] text-slate-300 line-clamp-2 mt-0.5">{notif.message}</p>
                          <span className="text-[9px] text-slate-500 mt-1 block">
                            {new Date(notif.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </span>
                        </div>
                      ))
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Profile Dropdown */}
            <div className="relative">
              <button
                onClick={() => {
                  setUserDropdownOpen(!userDropdownOpen);
                  setNotifDropdownOpen(false);
                }}
                className="flex items-center gap-2 rounded-lg p-1.5 hover:bg-slate-800 transition"
              >
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-tr from-indigo-600 to-indigo-400 text-xs font-bold text-white shadow">
                  {user?.fullName?.charAt(0) || 'U'}
                </div>
                <span className="hidden md:inline text-xs font-semibold text-slate-200">{user?.fullName}</span>
                <ChevronDown className="h-3.5 w-3.5 text-slate-400" />
              </button>

              {userDropdownOpen && (
                <div className="absolute right-0 mt-2 w-48 rounded-xl border border-slate-800 bg-slate-900 shadow-2xl p-1 z-50">
                  <div className="px-3 py-2 border-b border-slate-800 mb-1">
                    <p className="text-xs font-semibold text-white truncate">{user?.fullName}</p>
                    <p className="text-[10px] text-slate-400 truncate">{user?.email}</p>
                  </div>
                  <Link
                    to="/profile"
                    onClick={() => setUserDropdownOpen(false)}
                    className="flex items-center gap-2 rounded-lg px-3 py-2 text-xs text-slate-300 hover:bg-slate-800 hover:text-white transition"
                  >
                    <UserIcon className="h-3.5 w-3.5" />
                    <span>My Profile</span>
                  </Link>
                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-xs text-rose-400 hover:bg-rose-500/10 hover:text-rose-300 transition"
                  >
                    <LogOut className="h-3.5 w-3.5" />
                    <span>Sign Out</span>
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        {/* Dynamic Route Content */}
        <main className="flex-1 overflow-y-auto bg-slate-950 p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export const AuthLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/40 flex items-center justify-center p-4">
      {children}
    </div>
  );
};
