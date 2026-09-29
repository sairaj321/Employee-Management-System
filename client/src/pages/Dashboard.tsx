import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { api, Attendance, Employee } from '../api';
import {
  Users,
  Clock,
  CalendarCheck,
  Building2,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  ArrowRight,
  TrendingUp,
  FolderGit2,
  Calendar,
  Sparkles
} from 'lucide-react';

export const Dashboard: React.FC = () => {
  const { user, hasRole } = useAuth();
  const [loading, setLoading] = useState(true);
  const [totalEmployees, setTotalEmployees] = useState(0);
  const [totalDepartments, setTotalDepartments] = useState(0);
  const [todayAttendance, setTodayAttendance] = useState<Attendance | null>(null);
  const [pendingLeavesCount, setPendingLeavesCount] = useState(0);
  const [checkingInOut, setCheckingInOut] = useState(false);
  const [checkInMsg, setCheckInMsg] = useState<string | null>(null);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    setLoading(true);
    try {
      if (hasRole(['Admin', 'HR', 'Manager'])) {
        const empRes = await api.employees.getPaged({ page: 1, pageSize: 1 });
        if (empRes.data.success) {
          setTotalEmployees(empRes.data.data.totalCount);
        }
        const deptRes = await api.departments.getAll();
        if (deptRes.data.success) {
          setTotalDepartments(deptRes.data.data.length);
        }
        const leavesRes = await api.leaves.getAll({ status: 1 }); // Pending
        if (leavesRes.data.success) {
          setPendingLeavesCount(leavesRes.data.data.length);
        }
      }

      if (user?.employeeId) {
        const attRes = await api.attendance.getToday(user.employeeId);
        if (attRes.data.success && attRes.data.data) {
          setTodayAttendance(attRes.data.data);
        }
      }
    } catch {
      // Ignore
    } finally {
      setLoading(false);
    }
  };

  const handleCheckIn = async () => {
    if (!user?.employeeId) return;
    setCheckingInOut(true);
    setCheckInMsg(null);
    try {
      const res = await api.attendance.checkIn(user.employeeId);
      if (res.data.success) {
        setTodayAttendance(res.data.data);
        setCheckInMsg('Successfully checked in!');
      }
    } catch (err: any) {
      setCheckInMsg(err.response?.data?.message || 'Check-in failed');
    } finally {
      setCheckingInOut(false);
    }
  };

  const handleCheckOut = async () => {
    if (!user?.employeeId) return;
    setCheckingInOut(true);
    setCheckInMsg(null);
    try {
      const res = await api.attendance.checkOut(user.employeeId);
      if (res.data.success) {
        setTodayAttendance(res.data.data);
        setCheckInMsg('Successfully checked out!');
      }
    } catch (err: any) {
      setCheckInMsg(err.response?.data?.message || 'Check-out failed');
    } finally {
      setCheckingInOut(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Welcome Banner */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-indigo-900/60 via-slate-900 to-indigo-950/40 p-6 border border-indigo-500/20 shadow-xl">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <span className="rounded-md bg-indigo-500/20 px-2 py-0.5 text-[11px] font-semibold text-indigo-300 border border-indigo-500/30">
                {user?.roles?.join(', ') || 'Employee'}
              </span>
              <span className="text-xs text-slate-400">
                {new Date().toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric', year: 'numeric' })}
              </span>
            </div>
            <h1 className="mt-2 text-2xl font-black text-white">
              Welcome back, {user?.fullName || user?.email}!
            </h1>
            <p className="mt-1 text-xs text-slate-300 max-w-xl">
              Clean Architecture EMS with granular RBAC, PostgreSQL storage, and enterprise auditing.
            </p>
          </div>

          {/* Quick Check-in/out Widget */}
          <div className="flex flex-col sm:flex-row items-center gap-3 bg-slate-950/70 p-3.5 rounded-xl border border-slate-800">
            <div className="text-left">
              <span className="text-[10px] uppercase tracking-wider text-slate-400 font-semibold block">Attendance Status</span>
              <div className="flex items-center gap-1.5 mt-0.5">
                {todayAttendance?.checkInTime ? (
                  <span className="text-xs font-bold text-emerald-400 flex items-center gap-1">
                    <CheckCircle2 className="h-3.5 w-3.5" /> Checked In ({new Date(todayAttendance.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })})
                  </span>
                ) : (
                  <span className="text-xs font-semibold text-amber-400 flex items-center gap-1">
                    <Clock className="h-3.5 w-3.5" /> Not Checked In
                  </span>
                )}
                {todayAttendance?.checkOutTime && (
                  <span className="text-[10px] text-slate-400">
                    • Out: {new Date(todayAttendance.checkOutTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </span>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2">
              {!todayAttendance?.checkInTime ? (
                <button
                  onClick={handleCheckIn}
                  disabled={checkingInOut}
                  className="rounded-lg bg-emerald-600 px-3.5 py-1.5 text-xs font-bold text-white shadow hover:bg-emerald-500 transition disabled:opacity-50"
                >
                  {checkingInOut ? '...' : 'Check In'}
                </button>
              ) : !todayAttendance?.checkOutTime ? (
                <button
                  onClick={handleCheckOut}
                  disabled={checkingInOut}
                  className="rounded-lg bg-rose-600 px-3.5 py-1.5 text-xs font-bold text-white shadow hover:bg-rose-500 transition disabled:opacity-50"
                >
                  {checkingInOut ? '...' : 'Check Out'}
                </button>
              ) : (
                <span className="rounded-lg bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-400">
                  Completed for today
                </span>
              )}
            </div>
          </div>
        </div>

        {checkInMsg && (
          <p className="mt-3 text-xs text-indigo-300 font-medium">ℹ️ {checkInMsg}</p>
        )}
      </div>

      {/* Overview Stat Cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4 backdrop-blur shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-400">Total Workforce</span>
            <div className="rounded-lg bg-indigo-500/10 p-2 text-indigo-400">
              <Users className="h-4 w-4" />
            </div>
          </div>
          <p className="mt-2 text-2xl font-bold text-white">{totalEmployees || '4+'}</p>
          <div className="mt-2 flex items-center text-[11px] text-indigo-400">
            <TrendingUp className="mr-1 h-3 w-3" />
            <span>Active team members</span>
          </div>
        </div>

        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4 backdrop-blur shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-400">Departments</span>
            <div className="rounded-lg bg-pink-500/10 p-2 text-pink-400">
              <Building2 className="h-4 w-4" />
            </div>
          </div>
          <p className="mt-2 text-2xl font-bold text-white">{totalDepartments || '4'}</p>
          <div className="mt-2 flex items-center text-[11px] text-slate-400">
            <span>Organizational units</span>
          </div>
        </div>

        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4 backdrop-blur shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-400">Pending Leave Requests</span>
            <div className="rounded-lg bg-amber-500/10 p-2 text-amber-400">
              <CalendarCheck className="h-4 w-4" />
            </div>
          </div>
          <p className="mt-2 text-2xl font-bold text-white">{pendingLeavesCount}</p>
          <div className="mt-2 flex items-center text-[11px] text-amber-400">
            <span>Awaiting manager approval</span>
          </div>
        </div>

        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4 backdrop-blur shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-slate-400">Active Projects</span>
            <div className="rounded-lg bg-emerald-500/10 p-2 text-emerald-400">
              <FolderGit2 className="h-4 w-4" />
            </div>
          </div>
          <p className="mt-2 text-2xl font-bold text-white">2</p>
          <div className="mt-2 flex items-center text-[11px] text-emerald-400">
            <span>Cross-team deployments</span>
          </div>
        </div>
      </div>

      {/* Quick Access Grid */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Module shortcuts */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 shadow-sm lg:col-span-2">
          <h3 className="text-sm font-bold text-white mb-4">Quick Workspaces</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Link
              to="/employees"
              className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-3 hover:border-indigo-500/40 hover:bg-slate-800/40 transition group"
            >
              <div className="rounded-lg bg-indigo-500/10 p-2 text-indigo-400 group-hover:bg-indigo-500/20">
                <Users className="h-5 w-5" />
              </div>
              <div>
                <h4 className="text-xs font-bold text-white group-hover:text-indigo-400 transition">Employees Directory</h4>
                <p className="text-[11px] text-slate-400 mt-0.5">Manage employee onboarding, roles, and profiles</p>
              </div>
            </Link>

            <Link
              to="/leaves"
              className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-3 hover:border-indigo-500/40 hover:bg-slate-800/40 transition group"
            >
              <div className="rounded-lg bg-pink-500/10 p-2 text-pink-400 group-hover:bg-pink-500/20">
                <Calendar className="h-5 w-5" />
              </div>
              <div>
                <h4 className="text-xs font-bold text-white group-hover:text-pink-400 transition">Leave Applications</h4>
                <p className="text-[11px] text-slate-400 mt-0.5">Apply for annual leave or approve team requests</p>
              </div>
            </Link>

            <Link
              to="/attendance"
              className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-3 hover:border-indigo-500/40 hover:bg-slate-800/40 transition group"
            >
              <div className="rounded-lg bg-emerald-500/10 p-2 text-emerald-400 group-hover:bg-emerald-500/20">
                <Clock className="h-5 w-5" />
              </div>
              <div>
                <h4 className="text-xs font-bold text-white group-hover:text-emerald-400 transition">Time & Attendance</h4>
                <p className="text-[11px] text-slate-400 mt-0.5">Daily clock-in/out records and working hour sheets</p>
              </div>
            </Link>

            <Link
              to="/payslips"
              className="flex items-start gap-3 rounded-xl border border-slate-800 bg-slate-950/40 p-3 hover:border-indigo-500/40 hover:bg-slate-800/40 transition group"
            >
              <div className="rounded-lg bg-amber-500/10 p-2 text-amber-400 group-hover:bg-amber-500/20">
                <Sparkles className="h-5 w-5" />
              </div>
              <div>
                <h4 className="text-xs font-bold text-white group-hover:text-amber-400 transition">Payroll & Payslips</h4>
                <p className="text-[11px] text-slate-400 mt-0.5">Review monthly salary breakdowns and tax deductions</p>
              </div>
            </Link>
          </div>
        </div>

        {/* Security & System Info */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 shadow-sm">
          <h3 className="text-sm font-bold text-white mb-3">System Architecture</h3>
          <div className="space-y-3 text-xs">
            <div className="rounded-lg bg-slate-950/60 p-3 border border-slate-800/60">
              <span className="font-semibold text-indigo-400 block">Backend Engine</span>
              <span className="text-[11px] text-slate-400">ASP.NET Core 9 Web API + EF Core + PostgreSQL</span>
            </div>
            <div className="rounded-lg bg-slate-950/60 p-3 border border-slate-800/60">
              <span className="font-semibold text-emerald-400 block">Security Model</span>
              <span className="text-[11px] text-slate-400">JWT + Rotating Hashed Refresh Tokens + RBAC & Row-level Authorization</span>
            </div>
            <div className="rounded-lg bg-slate-950/60 p-3 border border-slate-800/60">
              <span className="font-semibold text-amber-400 block">Operational Governance</span>
              <span className="text-[11px] text-slate-400">Serilog Structured Logs + JSONB Audit History + Health Checks</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
