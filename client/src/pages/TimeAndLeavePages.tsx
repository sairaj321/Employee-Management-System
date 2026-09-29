import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, Attendance, Leave, LeaveType } from '../api';
import { Clock, CalendarDays, Plus, CheckCircle, XCircle, AlertCircle, X, Filter } from 'lucide-react';

export const AttendancePage: React.FC = () => {
  const { user } = useAuth();
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
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            <Clock className="h-6 w-6 text-emerald-400" />
            Attendance & Work Logs
          </h1>
          <p className="text-xs text-slate-400 mt-0.5">Track daily check-ins, check-outs, and working hours</p>
        </div>

        {/* Quick Check in/out */}
        <div className="flex items-center gap-2">
          {!todayAttendance?.checkInTime ? (
            <button
              onClick={handleCheckIn}
              disabled={checking}
              className="rounded-xl bg-emerald-600 px-4 py-2 text-xs font-bold text-white shadow-lg hover:bg-emerald-500 transition disabled:opacity-50"
            >
              {checking ? 'Processing...' : 'Clock In Now'}
            </button>
          ) : !todayAttendance?.checkOutTime ? (
            <button
              onClick={handleCheckOut}
              disabled={checking}
              className="rounded-xl bg-rose-600 px-4 py-2 text-xs font-bold text-white shadow-lg hover:bg-rose-500 transition disabled:opacity-50"
            >
              {checking ? 'Processing...' : 'Clock Out Now'}
            </button>
          ) : (
            <span className="rounded-xl bg-slate-800 px-4 py-2 text-xs font-semibold text-emerald-400 border border-emerald-500/20">
              ✓ Day Completed
            </span>
          )}
        </div>
      </div>

      {errorMsg && (
        <div className="rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-xs text-rose-300 flex items-center gap-2">
          <AlertCircle className="h-4 w-4 shrink-0 text-rose-400" />
          <span>{errorMsg}</span>
        </div>
      )}

      {/* Attendance Table */}
      <div className="rounded-xl border border-slate-800 bg-slate-900/60 overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-300">
            <thead className="border-b border-slate-800 bg-slate-950/50 text-[11px] uppercase tracking-wider text-slate-400">
              <tr>
                <th className="px-4 py-3">Employee</th>
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3">Clock In</th>
                <th className="px-4 py-3">Clock Out</th>
                <th className="px-4 py-3">Total Hours</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {loading ? (
                <tr><td colSpan={6} className="py-8 text-center text-slate-500">Loading attendance...</td></tr>
              ) : attendances.length === 0 ? (
                <tr><td colSpan={6} className="py-8 text-center text-slate-500">No attendance entries recorded.</td></tr>
              ) : (
                attendances.map((a) => (
                  <tr key={a.id} className="hover:bg-slate-800/40 transition">
                    <td className="px-4 py-3 font-medium text-white">
                      <div>{a.employeeName}</div>
                      <span className="text-[10px] text-slate-500">{a.employeeCode}</span>
                    </td>
                    <td className="px-4 py-3 font-semibold text-slate-200">{a.date}</td>
                    <td className="px-4 py-3 text-emerald-400 font-mono">
                      {a.checkInTime ? new Date(a.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
                    </td>
                    <td className="px-4 py-3 text-rose-400 font-mono">
                      {a.checkOutTime ? new Date(a.checkOutTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}
                    </td>
                    <td className="px-4 py-3 text-slate-300">
                      {a.totalHours ? `${a.totalHours.toFixed(1)} hrs` : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                        a.status === 1 || a.status === 'Present'
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                          : 'bg-amber-500/10 text-amber-400 border border-amber-500/30'
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
  const [leaves, setLeaves] = useState<Leave[]>([]);
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    leaveTypeId: 1,
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date(Date.now() + 86400000).toISOString().split('T')[0],
    reason: ''
  });
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchLeaves();
  }, []);

  const fetchLeaves = async () => {
    setLoading(true);
    try {
      const [leavesRes, typesRes] = await Promise.all([
        api.leaves.getAll({}),
        api.leaves.getTypes(),
      ]);
      if (leavesRes.data.success) setLeaves(leavesRes.data.data);
      if (typesRes.data.success) setLeaveTypes(typesRes.data.data);
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
      fetchLeaves();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to submit leave application');
    }
  };

  const handleApprove = async (id: number) => {
    try {
      await api.leaves.approve(id, 'Approved');
      fetchLeaves();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  const handleReject = async (id: number) => {
    try {
      await api.leaves.reject(id, 'Rejected');
      fetchLeaves();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  const handleCancel = async (id: number) => {
    if (!window.confirm('Cancel this leave request?')) return;
    try {
      await api.leaves.cancel(id);
      fetchLeaves();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            <CalendarDays className="h-6 w-6 text-pink-400" />
            Leave Management
          </h1>
          <p className="text-xs text-slate-400 mt-0.5">Apply for time off and review approvals</p>
        </div>

        <button
          onClick={() => {
            setFormData({
              leaveTypeId: leaveTypes[0]?.id || 1,
              startDate: new Date().toISOString().split('T')[0],
              endDate: new Date(Date.now() + 86400000).toISOString().split('T')[0],
              reason: ''
            });
            setError(null);
            setModalOpen(true);
          }}
          className="flex items-center gap-2 rounded-xl bg-pink-600 px-3.5 py-2 text-xs font-bold text-white shadow-lg hover:bg-pink-500 transition"
        >
          <Plus className="h-4 w-4" />
          <span>Apply for Leave</span>
        </button>
      </div>

      {/* Leaves List */}
      <div className="rounded-xl border border-slate-800 bg-slate-900/60 overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-300">
            <thead className="border-b border-slate-800 bg-slate-950/50 text-[11px] uppercase tracking-wider text-slate-400">
              <tr>
                <th className="px-4 py-3">Employee</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Date Range</th>
                <th className="px-4 py-3">Days</th>
                <th className="px-4 py-3">Reason</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {loading ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-500">Loading leave requests...</td></tr>
              ) : leaves.length === 0 ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-500">No leave applications found.</td></tr>
              ) : (
                leaves.map((l) => (
                  <tr key={l.id} className="hover:bg-slate-800/40 transition">
                    <td className="px-4 py-3 font-medium text-white">{l.employeeName}</td>
                    <td className="px-4 py-3 text-indigo-300">{l.leaveTypeName}</td>
                    <td className="px-4 py-3 text-slate-300">{l.startDate} → {l.endDate}</td>
                    <td className="px-4 py-3 font-semibold text-white">{l.daysCount}</td>
                    <td className="px-4 py-3 text-slate-400 max-w-xs truncate">{l.reason}</td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                        l.status === 2 || l.status === 'Approved'
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/30'
                          : l.status === 3 || l.status === 'Rejected'
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/30'
                          : 'bg-amber-500/10 text-amber-400 border border-amber-500/30'
                      }`}>
                        {l.status === 2 || l.status === 'Approved' ? 'Approved' : l.status === 3 || l.status === 'Rejected' ? 'Rejected' : l.status === 4 || l.status === 'Cancelled' ? 'Cancelled' : 'Pending'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {hasRole(['Admin', 'HR', 'Manager']) && (l.status === 1 || l.status === 'Pending') && (
                          <>
                            <button
                              onClick={() => handleApprove(l.id)}
                              className="rounded px-2 py-1 bg-emerald-600/20 text-emerald-400 hover:bg-emerald-600 hover:text-white transition font-medium"
                            >
                              Approve
                            </button>
                            <button
                              onClick={() => handleReject(l.id)}
                              className="rounded px-2 py-1 bg-rose-600/20 text-rose-400 hover:bg-rose-600 hover:text-white transition font-medium"
                            >
                              Reject
                            </button>
                          </>
                        )}
                        {l.employeeId === user?.employeeId && (l.status === 1 || l.status === 'Pending') && (
                          <button
                            onClick={() => handleCancel(l.id)}
                            className="rounded px-2 py-1 border border-slate-700 text-slate-400 hover:text-rose-400"
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
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
            <div className="flex justify-between items-center mb-4 border-b border-slate-800 pb-2">
              <h3 className="text-sm font-bold text-white">Apply for Leave</h3>
              <button onClick={() => setModalOpen(false)} className="text-slate-400 hover:text-white"><X className="h-4 w-4" /></button>
            </div>
            {error && (
              <div className="mb-3 text-xs text-rose-400 flex items-center gap-1.5"><AlertCircle className="h-4 w-4" />{error}</div>
            )}
            <form onSubmit={handleApply} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 text-slate-300">Leave Type</label>
                <select
                  value={formData.leaveTypeId}
                  onChange={(e) => setFormData({ ...formData, leaveTypeId: Number(e.target.value) })}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                >
                  {leaveTypes.map((t) => (
                    <option key={t.id} value={t.id}>{t.name} ({t.defaultDaysPerYear} days/yr)</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block mb-1 text-slate-300">Start Date</label>
                  <input
                    type="date"
                    required
                    value={formData.startDate}
                    onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                    className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                  />
                </div>
                <div>
                  <label className="block mb-1 text-slate-300">End Date</label>
                  <input
                    type="date"
                    required
                    value={formData.endDate}
                    onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                    className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                  />
                </div>
              </div>
              <div>
                <label className="block mb-1 text-slate-300">Reason</label>
                <textarea
                  required
                  rows={3}
                  value={formData.reason}
                  onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                  placeholder="State reason for absence..."
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                />
              </div>
              <div className="flex justify-end gap-2 pt-3">
                <button type="button" onClick={() => setModalOpen(false)} className="px-3 py-1.5 border border-slate-700 rounded text-slate-300">Cancel</button>
                <button type="submit" className="px-4 py-1.5 bg-pink-600 rounded text-white font-bold">Submit Request</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
