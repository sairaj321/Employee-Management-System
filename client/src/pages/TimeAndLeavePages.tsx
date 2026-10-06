import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, Attendance, Leave, LeaveType, LeaveBalance } from '../api';
import { Clock, CalendarDays, Plus, CheckCircle, XCircle, AlertCircle, X, Filter, PieChart, CheckCircle2, UserCheck, History } from 'lucide-react';

export const AttendancePage: React.FC = () => {
  const { user, hasRole } = useAuth();
  const isPrivileged = hasRole(['Admin', 'HR']);
  const [attendances, setAttendances] = useState<Attendance[]>([]);
  const [todayAttendance, setTodayAttendance] = useState<Attendance | null>(null);
  const [loading, setLoading] = useState(true);
  const [checking, setChecking] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    fetchAttendance();
  }, []);

  const fetchAttendance = async () => {
    setLoading(true);
    try {
      if (user?.employeeId) {
        const todayRes = await api.attendance.getToday(user.employeeId);
        if (todayRes.data.success && todayRes.data.data) {
          setTodayAttendance(todayRes.data.data);
        }
      }
      const historyRes = await api.attendance.getHistory({});
      if (historyRes.data.success) {
        setAttendances(historyRes.data.data);
      }
    } catch {}
    finally { setLoading(false); }
  };

  const handleCheckIn = async () => {
    if (!user?.employeeId) return;
    setChecking(true);
    setErrorMsg(null);
    try {
      const res = await api.attendance.checkIn(user.employeeId);
      if (res.data.success) {
        setTodayAttendance(res.data.data);
        fetchAttendance();
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Check-in failed');
    } finally {
      setChecking(false);
    }
  };

  const handleCheckOut = async () => {
    if (!user?.employeeId) return;
    setChecking(true);
    setErrorMsg(null);
    try {
      const res = await api.attendance.checkOut(user.employeeId);
      if (res.data.success) {
        setTodayAttendance(res.data.data);
        fetchAttendance();
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Check-out failed');
    } finally {
      setChecking(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <Clock className="h-6 w-6 text-[#16A34A]" />
            {isPrivileged ? 'Attendance & Work Logs' : 'My Attendance & Work Logs'}
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">Track daily check-ins, check-outs, and working hours</p>
        </div>

        {/* Quick Check in/out */}
        <div className="flex items-center gap-2">
          {!todayAttendance?.checkInTime ? (
            <button
              onClick={handleCheckIn}
              disabled={checking}
              className="rounded-lg bg-[#16A34A] px-4 py-2 text-xs font-bold text-white shadow-sm hover:bg-green-700 transition disabled:opacity-50"
            >
              {checking ? 'Processing...' : 'Clock In Now'}
            </button>
          ) : !todayAttendance?.checkOutTime ? (
            <button
              onClick={handleCheckOut}
              disabled={checking}
              className="rounded-lg bg-[#DC2626] px-4 py-2 text-xs font-bold text-white shadow-sm hover:bg-red-700 transition disabled:opacity-50"
            >
              {checking ? 'Processing...' : 'Clock Out Now'}
            </button>
          ) : (
            <span className="rounded-lg bg-[#DCFCE7] px-4 py-2 text-xs font-semibold text-[#15803D] border border-green-200">
              ✓ Day Completed
            </span>
          )}
        </div>
      </div>

      {errorMsg && (
        <div className="rounded-lg border border-red-200 bg-[#FEE2E2] p-3 text-xs text-[#DC2626] flex items-center gap-2">
          <AlertCircle className="h-4 w-4 shrink-0 text-[#DC2626]" />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Attendance Table */}
      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
              <tr>
                {isPrivileged && <th className="px-4 py-3">Employee</th>}
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3">Clock In</th>
                <th className="px-4 py-3">Clock Out</th>
                <th className="px-4 py-3">Total Hours</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr><td colSpan={isPrivileged ? 6 : 5} className="py-8 text-center text-slate-400">Loading attendance...</td></tr>
              ) : attendances.length === 0 ? (
                <tr><td colSpan={isPrivileged ? 6 : 5} className="py-8 text-center text-slate-400">No attendance entries recorded.</td></tr>
              ) : (
                attendances.map((a) => (
                  <tr key={a.id} className="hover:bg-slate-50 transition">
                    {isPrivileged && (
                      <td className="px-4 py-3 font-medium text-slate-900">
                        <div>{a.employeeName}</div>
                        <span className="text-[10px] text-slate-400">{a.employeeCode}</span>
                      </td>
                    )}
                    <td className="px-4 py-3 font-semibold text-slate-700">{a.date}</td>
                    <td className="px-4 py-3 text-[#16A34A] font-mono font-medium">
                      {a.checkInTime ? new Date(a.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
                    </td>
                    <td className="px-4 py-3 text-[#DC2626] font-mono font-medium">
                      {a.checkOutTime ? new Date(a.checkOutTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
                    </td>
                    <td className="px-4 py-3 text-slate-600 font-medium">
                      {a.totalHours ? `${a.totalHours.toFixed(1)} hrs` : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                        a.status === 1 || a.status === 'Present'
                          ? 'bg-[#DCFCE7] text-[#15803D] border border-green-200'
                          : 'bg-[#FEF3C7] text-[#D97706] border border-amber-200'
                      }`}>
                        {a.status === 1 || a.status === 'Present' ? 'Present' : 'Late'}
                      </span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export const LeavesPage: React.FC = () => {
  const { user, hasRole } = useAuth();
  const canManageLeaves = hasRole(['Admin', 'HR', 'Manager']);
  const [activeTab, setActiveTab] = useState<'my-leaves' | 'pending-approvals' | 'all-history'>('my-leaves');
  const [leaves, setLeaves] = useState<Leave[]>([]);
  const [pendingCount, setPendingCount] = useState(0);
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([]);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    leaveTypeId: 1,
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    reason: ''
  });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, [activeTab]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [typesRes, balancesRes] = await Promise.all([
        api.leaves.getTypes(),
        api.leaves.getBalances({ employeeId: user?.employeeId })
      ]);

      if (typesRes.data.success) setLeaveTypes(typesRes.data.data);
      if (balancesRes.data.success) setBalances(balancesRes.data.data);

      if (canManageLeaves) {
        // Also fetch pending approvals count
        const pendingRes = await api.leaves.getPendingApprovals();
        if (pendingRes.data.success) {
          setPendingCount(pendingRes.data.data.length);
        }
      }

      // Fetch leaves based on active tab
      if (activeTab === 'pending-approvals') {
        const res = await api.leaves.getPendingApprovals();
        if (res.data.success) setLeaves(res.data.data);
      } else if (activeTab === 'all-history') {
        const res = await api.leaves.getAll({ teamOnly: true });
        if (res.data.success) setLeaves(res.data.data);
      } else {
        const res = await api.leaves.getAll({ employeeId: user?.employeeId });
        if (res.data.success) setLeaves(res.data.data);
      }
    } catch {}
    finally { setLoading(false); }
  };

  const handleApply = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await api.leaves.apply({
        employeeId: user?.employeeId,
        leaveTypeId: Number(formData.leaveTypeId),
        startDate: formData.startDate,
        endDate: formData.endDate,
        reason: formData.reason
      });
      setModalOpen(false);
      loadData();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to submit leave application');
    }
  };

  const handleApprove = async (id: number) => {
    try {
      await api.leaves.approve(id, 'Approved');
      loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to approve');
    }
  };

  const handleReject = async (id: number) => {
    try {
      await api.leaves.reject(id, 'Rejected');
      loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to reject');
    }
  };

  const handleCancel = async (id: number) => {
    if (!window.confirm('Cancel this leave request?')) return;
    try {
      await api.leaves.cancel(id);
      loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to cancel');
    }
  };

  // Calculate requested days for modal
  const requestedDays = formData.startDate && formData.endDate
    ? Math.max(1, Math.round((new Date(formData.endDate).getTime() - new Date(formData.startDate).getTime()) / (1000 * 60 * 60 * 24)) + 1)
    : 1;

  const selectedTypeBalance = balances.find((b) => b.leaveTypeId === Number(formData.leaveTypeId));

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <CalendarDays className="h-6 w-6 text-[#2563EB]" />
            Leave Management & Balances
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Track available leave quota, apply for time off, and manage approvals
          </p>
        </div>

        <button
          onClick={() => {
            const firstType = leaveTypes[0]?.id || 1;
            setFormData({
              leaveTypeId: firstType,
              startDate: new Date().toISOString().split('T')[0],
              endDate: new Date().toISOString().split('T')[0],
              reason: ''
            });
            setError(null);
            setModalOpen(true);
          }}
          className="flex items-center gap-2 rounded-lg bg-[#2563EB] px-3.5 py-2 text-xs font-bold text-white shadow-sm hover:bg-blue-700 transition"
        >
          <Plus className="h-4 w-4" />
          <span>Apply for Leave</span>
        </button>
      </div>

      {/* Available Leave Balance Cards */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-xs font-bold text-slate-700 uppercase tracking-wider flex items-center gap-1.5">
            <PieChart className="h-4 w-4 text-[#2563EB]" />
            Available Leave Quota ({new Date().getFullYear()})
          </h2>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {balances.length === 0 ? (
            <div className="col-span-4 rounded-lg border border-[#E2E8F0] bg-white p-4 text-center text-xs text-slate-400">
              Loading leave balances...
            </div>
          ) : (
            balances.map((b) => {
              const usedPercentage = Math.min(100, Math.round(((b.usedDays + b.pendingDays) / (b.totalAllocatedDays || 1)) * 100));
              return (
                <div
                  key={b.leaveTypeId}
                  className="rounded-[10px] border border-[#E2E8F0] bg-white p-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)] hover:border-blue-200 transition"
                >
                  <div className="flex justify-between items-start">
                    <span className="text-xs font-bold text-slate-800">{b.leaveTypeName}</span>
                    <span className="inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-bold text-[#2563EB] border border-blue-100">
                      {b.totalAllocatedDays}d Total
                    </span>
                  </div>
                  <div className="mt-3 flex items-baseline gap-1">
                    <span className="text-2xl font-black text-slate-900">{b.availableDays}</span>
                    <span className="text-xs font-medium text-slate-500">days available</span>
                  </div>
                  <div className="mt-3 space-y-1.5">
                    <div className="flex justify-between text-[11px] text-slate-500">
                      <span>Used: <strong className="text-slate-800">{b.usedDays}d</strong></span>
                      <span>Pending: <strong className="text-amber-600">{b.pendingDays}d</strong></span>
                    </div>
                    <div className="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all ${
                          b.availableDays === 0 ? 'bg-red-500' : b.availableDays <= 3 ? 'bg-amber-500' : 'bg-[#2563EB]'
                        }`}
                        style={{ width: `${usedPercentage}%` }}
                      />
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* Tabs for Managers / HR */}
      {canManageLeaves && (
        <div className="flex items-center gap-2 border-b border-[#E2E8F0] pb-2">
          <button
            onClick={() => setActiveTab('my-leaves')}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg transition ${
              activeTab === 'my-leaves'
                ? 'bg-[#2563EB] text-white shadow-sm'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            <CalendarDays className="h-3.5 w-3.5" />
            <span>My Leaves</span>
          </button>
          <button
            onClick={() => setActiveTab('pending-approvals')}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg transition ${
              activeTab === 'pending-approvals'
                ? 'bg-[#2563EB] text-white shadow-sm'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            <UserCheck className="h-3.5 w-3.5" />
            <span>Pending Team Approvals</span>
            {pendingCount > 0 && (
              <span className={`ml-1 rounded-full px-1.5 py-0.2 text-[10px] font-bold ${
                activeTab === 'pending-approvals' ? 'bg-white text-[#2563EB]' : 'bg-amber-100 text-amber-800 border border-amber-200'
              }`}>
                {pendingCount}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab('all-history')}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg transition ${
              activeTab === 'all-history'
                ? 'bg-[#2563EB] text-white shadow-sm'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            <History className="h-3.5 w-3.5" />
            <span>Team History</span>
          </button>
        </div>
      )}

      {/* Leaves List */}
      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
              <tr>
                {(canManageLeaves && activeTab !== 'my-leaves') && <th className="px-4 py-3">Employee</th>}
                <th className="px-4 py-3">Leave Type</th>
                <th className="px-4 py-3">From</th>
                <th className="px-4 py-3">To</th>
                <th className="px-4 py-3">Days</th>
                <th className="px-4 py-3">Reason</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr><td colSpan={8} className="py-8 text-center text-slate-400">Loading leave requests...</td></tr>
              ) : leaves.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-8 text-center text-slate-400">
                    {activeTab === 'pending-approvals'
                      ? 'No pending leave requests to approve. All requests have been reviewed.'
                      : 'No leave applications found.'}
                  </td>
                </tr>
              ) : (
                leaves.map((l) => (
                  <tr key={l.id} className="hover:bg-slate-50 transition">
                    {(canManageLeaves && activeTab !== 'my-leaves') && (
                      <td className="px-4 py-3 font-medium text-slate-900">
                        <div>{l.employeeName}</div>
                        <span className="text-[10px] text-slate-400">{l.employeeCode}</span>
                      </td>
                    )}
                    <td className="px-4 py-3 text-[#2563EB] font-medium">{l.leaveTypeName}</td>
                    <td className="px-4 py-3 text-slate-600">{l.startDate}</td>
                    <td className="px-4 py-3 text-slate-600">{l.endDate}</td>
                    <td className="px-4 py-3 font-semibold text-slate-900">{l.daysCount}</td>
                    <td className="px-4 py-3 text-slate-500 max-w-xs truncate">{l.reason}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                        l.status === 2 || l.status === 'Approved'
                          ? 'bg-[#DCFCE7] text-[#15803D] border border-green-200'
                          : l.status === 3 || l.status === 'Rejected'
                          ? 'bg-[#FEE2E2] text-[#DC2626] border border-red-200'
                          : l.status === 4 || l.status === 'Cancelled'
                          ? 'bg-slate-100 text-slate-600 border border-slate-200'
                          : 'bg-[#FEF3C7] text-[#D97706] border border-amber-200'
                      }`}>
                        {l.status === 2 || l.status === 'Approved' ? 'Approved' : l.status === 3 || l.status === 'Rejected' ? 'Rejected' : l.status === 4 || l.status === 'Cancelled' ? 'Cancelled' : 'Pending'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {canManageLeaves && (l.status === 1 || l.status === 'Pending') && l.employeeId !== user?.employeeId && (
                          <>
                            <button
                              onClick={() => handleApprove(l.id)}
                              className="rounded-lg px-2.5 py-1 bg-[#DCFCE7] text-[#15803D] hover:bg-[#16A34A] hover:text-white transition font-medium text-[11px] border border-green-200"
                            >
                              Approve
                            </button>
                            <button
                              onClick={() => handleReject(l.id)}
                              className="rounded-lg px-2.5 py-1 bg-[#FEE2E2] text-[#DC2626] hover:bg-[#DC2626] hover:text-white transition font-medium text-[11px] border border-red-200"
                            >
                              Reject
                            </button>
                          </>
                        )}
                        {l.employeeId === user?.employeeId && (l.status === 1 || l.status === 'Pending') && (
                          <button
                            onClick={() => handleCancel(l.id)}
                            className="rounded-lg px-2.5 py-1 border border-[#E2E8F0] text-slate-500 hover:bg-red-50 hover:text-[#DC2626] hover:border-red-200 transition text-[11px]"
                          >
                            Cancel
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Apply Leave Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-[10px] border border-[#E2E8F0] bg-white p-6 shadow-xl">
            <div className="flex justify-between items-center mb-4 border-b border-[#E2E8F0] pb-2">
              <h3 className="text-sm font-bold text-slate-900">Apply for Leave</h3>
              <button onClick={() => setModalOpen(false)} className="text-slate-400 hover:text-slate-900"><X className="h-4 w-4" /></button>
            </div>
            {error && (
              <div className="mb-3 text-xs text-[#DC2626] bg-[#FEE2E2] border border-red-200 rounded-lg p-2.5 flex items-center gap-1.5">
                <AlertCircle className="h-4 w-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}
            <form onSubmit={handleApply} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Leave Type *</label>
                <select
                  value={formData.leaveTypeId}
                  onChange={(e) => setFormData({ ...formData, leaveTypeId: Number(e.target.value) })}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                >
                  {leaveTypes.map((t) => {
                    const balance = balances.find((b) => b.leaveTypeId === t.id);
                    return (
                      <option key={t.id} value={t.id}>
                        {t.name} ({balance ? `${balance.availableDays} days available` : `${t.defaultDaysPerYear} days/yr`})
                      </option>
                    );
                  })}
                </select>
                {selectedTypeBalance && (
                  <div className="mt-1.5 flex items-center gap-2 text-[11px] text-slate-500">
                    <span>Available: <strong className="text-[#16A34A]">{selectedTypeBalance.availableDays} days</strong></span>
                    <span>•</span>
                    <span>Total Quota: {selectedTypeBalance.totalAllocatedDays} days</span>
                  </div>
                )}
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block mb-1 font-semibold text-slate-700">Start Date *</label>
                  <input
                    type="date"
                    required
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block mb-1 font-semibold text-slate-700">End Date *</label>
                  <input
                    type="date"
                    required
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                  />
                </div>
              </div>

              {/* Days duration summary */}
              <div className="rounded-lg bg-slate-50 border border-slate-200 p-2.5 flex items-center justify-between text-xs">
                <span className="text-slate-600">Total Requested Duration:</span>
                <span className="font-bold text-slate-900">{requestedDays} {requestedDays === 1 ? 'day' : 'days'}</span>
              </div>

              {selectedTypeBalance && requestedDays > selectedTypeBalance.availableDays && (
                <div className="text-[11px] text-red-600 bg-red-50 border border-red-200 rounded p-2 flex items-center gap-1.5">
                  <AlertCircle className="h-3.5 w-3.5 shrink-0 text-red-500" />
                  <span>Requested days ({requestedDays}) exceed available balance ({selectedTypeBalance.availableDays} days).</span>
                </div>
              )}

              <div>
                <label className="block mb-1 font-semibold text-slate-700">Reason *</label>
                <textarea
                  required
                  rows={3}
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  placeholder="State reason for absence..."
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none placeholder-slate-400"
                />
              </div>
              <div className="flex justify-end gap-2 pt-3 border-t border-[#E2E8F0]">
                <button type="button" onClick={() => setModalOpen(false)} className="px-3.5 py-1.5 border border-[#E2E8F0] rounded-lg text-slate-600 hover:bg-slate-50 transition">Cancel</button>
                <button
                  type="submit"
                  disabled={Boolean(selectedTypeBalance && requestedDays > selectedTypeBalance.availableDays)}
                  className="px-4 py-1.5 bg-[#2563EB] hover:bg-blue-700 rounded-lg text-white font-bold transition shadow-sm disabled:opacity-50"
                >
                  Submit Request
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
