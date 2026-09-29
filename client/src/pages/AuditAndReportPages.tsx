import React, { useState, useEffect } from 'react';
import { api, AuditLog } from '../api';
import { History, Search, Filter, Eye, X, ShieldAlert } from 'lucide-react';

export const AuditLogsPage: React.FC = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [entityFilter, setEntityFilter] = useState('');
  const [viewingLog, setViewingLog] = useState<AuditLog | null>(null);

  useEffect(() => {
    fetchLogs();
  }, [page, entityFilter]);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const res = await api.auditLogs.getPaged({
        page,
        pageSize: 15,
        entityName: entityFilter || undefined
      });
      if (res.data.success) {
        setLogs(res.data.data.items);
        setTotalCount(res.data.data.totalCount);
      }
    } catch {}
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            <History className="h-6 w-6 text-indigo-400" />
            Security & System Audit Logs
          </h1>
          <p className="text-xs text-slate-400 mt-0.5">Immutable business event tracking and compliance history stored in PostgreSQL JSONB</p>
        </div>

        <select
          value={entityFilter}
          onChange={(e) => {
            setEntityFilter(e.target.value);
            setPage(1);
          }}
          className="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-slate-200 focus:border-indigo-500 focus:outline-none"
        >
          <option value="">All Entities</option>
          <option value="User">User</option>
          <option value="Employee">Employee</option>
          <option value="Department">Department</option>
          <option value="Attendance">Attendance</option>
          <option value="Leave">Leave</option>
          <option value="SalaryStructure">SalaryStructure</option>
          <option value="Payslip">Payslip</option>
          <option value="Project">Project</option>
        </select>
      </div>

      <div className="rounded-xl border border-slate-800 bg-slate-900/60 overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-300">
            <thead className="border-b border-slate-800 bg-slate-950/50 text-[11px] uppercase tracking-wider text-slate-400">
              <tr>
                <th className="px-4 py-3">Timestamp</th>
                <th className="px-4 py-3">Action Event</th>
                <th className="px-4 py-3">Entity</th>
                <th className="px-4 py-3">Record ID</th>
                <th className="px-4 py-3">Actor</th>
                <th className="px-4 py-3">IP Address</th>
                <th className="px-4 py-3 text-right">Payload</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {loading ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-500">Loading audit history...</td></tr>
              ) : logs.length === 0 ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-500">No audit records found.</td></tr>
              ) : (
                logs.map((log) => (
                  <tr key={log.id} className="hover:bg-slate-800/40 transition font-mono">
                    <td className="px-4 py-3 text-slate-400 text-[11px]">{new Date(log.timestamp).toLocaleString()}</td>
                    <td className="px-4 py-3 font-semibold text-indigo-400">{log.action}</td>
                    <td className="px-4 py-3 text-white">{log.entityName}</td>
                    <td className="px-4 py-3 text-slate-400">#{log.entityId}</td>
                    <td className="px-4 py-3 text-slate-300 font-sans">{log.userEmail || (log.userId ? `User #${log.userId}` : 'System')}</td>
                    <td className="px-4 py-3 text-slate-500">{log.ipAddress || '127.0.0.1'}</td>
                    <td className="px-4 py-3 text-right font-sans">
                      <button
                        onClick={() => setViewingLog(log)}
                        className="rounded px-2.5 py-1 bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white transition text-xs"
                      >
                        Inspect
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between border-t border-slate-800 px-4 py-3 bg-slate-950/40 text-xs text-slate-400">
          <span>Total Logs: {totalCount}</span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded-lg border border-slate-700 bg-slate-900 px-2.5 py-1 text-xs text-slate-200 hover:bg-slate-800 disabled:opacity-40"
            >
              Previous
            </button>
            <span>Page {page} of {Math.ceil(totalCount / 15) || 1}</span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={page * 15 >= totalCount}
              className="rounded-lg border border-slate-700 bg-slate-900 px-2.5 py-1 text-xs text-slate-200 hover:bg-slate-800 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      </div>

      {/* JSON Payload Inspector Modal */}
      {viewingLog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-2xl rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
            <div className="flex justify-between items-center mb-4 border-b border-slate-800 pb-3">
              <div>
                <h3 className="text-sm font-bold text-white flex items-center gap-2 font-mono">
                  {viewingLog.action} • {viewingLog.entityName} #{viewingLog.entityId}
                </h3>
                <span className="text-[10px] text-slate-400 font-mono">Correlation ID: {viewingLog.correlationId || 'N/A'}</span>
              </div>
              <button onClick={() => setViewingLog(null)} className="text-slate-400 hover:text-white"><X className="h-4 w-4" /></button>
            </div>

            <div className="space-y-4 text-xs max-h-96 overflow-y-auto">
              {viewingLog.oldValue && (
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-rose-400 block mb-1">Previous Value</span>
                  <pre className="bg-slate-950 p-3 rounded-lg border border-slate-800 text-[11px] text-slate-300 font-mono overflow-x-auto">
                    {JSON.stringify(JSON.parse(viewingLog.oldValue), null, 2)}
                  </pre>
                </div>
              )}

              {viewingLog.newValue && (
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-emerald-400 block mb-1">New Value</span>
                  <pre className="bg-slate-950 p-3 rounded-lg border border-slate-800 text-[11px] text-slate-300 font-mono overflow-x-auto">
                    {JSON.stringify(JSON.parse(viewingLog.newValue), null, 2)}
                  </pre>
                </div>
              )}
            </div>

            <div className="flex justify-end pt-4 border-t border-slate-800 mt-4">
              <button onClick={() => setViewingLog(null)} className="px-4 py-1.5 bg-indigo-600 rounded text-white font-bold text-xs">
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export const ReportsPage: React.FC = () => {
  const [headcount, setHeadcount] = useState<Array<{ departmentId: number; departmentName: string; activeEmployees: number; totalEmployees: number }>>([]);
  const [leaveUtilization, setLeaveUtilization] = useState<Array<{ employeeId: number; employeeName: string; departmentName: string; totalLeavesTaken: number; pendingRequests: number }>>([]);
  const [payrollSummary, setPayrollSummary] = useState<{ month: number; year: number; totalEmployeesPaid: number; totalGrossSalary: number; totalDeductions: number; totalNetSalary: number } | null>(null);
  const [projectAllocation, setProjectAllocation] = useState<Array<{ projectId: number; projectName: string; managerName: string; status: string; assignedMembersCount: number }>>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchReports();
  }, []);

  const fetchReports = async () => {
    setLoading(true);
    try {
      const [hcRes, luRes, payRes, paRes] = await Promise.all([
        api.reports.headcountByDepartment(),
        api.reports.leaveUtilization(),
        api.reports.payrollSummary(new Date().getMonth() + 1, new Date().getFullYear()),
        api.reports.projectAllocation(),
      ]);
      if (hcRes.data.success) setHeadcount(hcRes.data.data);
      if (luRes.data.success) setLeaveUtilization(luRes.data.data);
      if (payRes.data.success) setPayrollSummary(payRes.data.data);
      if (paRes.data.success) setProjectAllocation(paRes.data.data);
    } catch {}
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold text-white flex items-center gap-2">
          <History className="h-6 w-6 text-pink-400" />
          Enterprise Analytics & Projections
        </h1>
        <p className="text-xs text-slate-400 mt-0.5">Read-only performance projections and headcount analytics</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Headcount by Department */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
          <h3 className="text-sm font-bold text-white">Headcount by Department</h3>
          <div className="space-y-3 text-xs">
            {headcount.map((h) => (
              <div key={h.departmentId} className="space-y-1">
                <div className="flex justify-between text-slate-300 font-medium">
                  <span>{h.departmentName}</span>
                  <span>{h.activeEmployees} active / {h.totalEmployees} total</span>
                </div>
                <div className="h-2 w-full bg-slate-800 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-indigo-500 rounded-full transition-all"
                    style={{ width: `${Math.min(100, (h.activeEmployees / 10) * 100)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Payroll Overview Summary */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
          <h3 className="text-sm font-bold text-white">Payroll Summary</h3>
          {payrollSummary ? (
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-[11px] text-slate-400 block">Total Disbursed Net</span>
                <span className="text-lg font-bold text-emerald-400 font-mono">${payrollSummary.totalNetSalary.toLocaleString()}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-[11px] text-slate-400 block">Tax / PF Deductions</span>
                <span className="text-lg font-bold text-rose-400 font-mono">${payrollSummary.totalDeductions.toLocaleString()}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-[11px] text-slate-400 block">Gross Payroll</span>
                <span className="text-lg font-bold text-white font-mono">${payrollSummary.totalGrossSalary.toLocaleString()}</span>
              </div>
              <div className="bg-slate-950 p-3 rounded-lg border border-slate-800">
                <span className="text-[11px] text-slate-400 block">Employees Processed</span>
                <span className="text-lg font-bold text-indigo-400">{payrollSummary.totalEmployeesPaid}</span>
              </div>
            </div>
          ) : (
            <p className="text-xs text-slate-500">No payroll data generated for current period.</p>
          )}
        </div>

        {/* Leave Utilization */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
          <h3 className="text-sm font-bold text-white">Leave Utilization</h3>
          <div className="space-y-2 text-xs max-h-60 overflow-y-auto">
            {leaveUtilization.map((lu) => (
              <div key={lu.employeeId} className="flex justify-between items-center bg-slate-950/40 p-2.5 rounded-lg border border-slate-800">
                <div>
                  <span className="font-semibold text-white block">{lu.employeeName}</span>
                  <span className="text-[10px] text-slate-400">{lu.departmentName}</span>
                </div>
                <div className="text-right">
                  <span className="text-indigo-400 font-bold">{lu.totalLeavesTaken} days used</span>
                  <span className="text-[10px] text-slate-500 block">{lu.pendingRequests} pending</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Project Allocation */}
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
          <h3 className="text-sm font-bold text-white">Project Allocations</h3>
          <div className="space-y-2 text-xs max-h-60 overflow-y-auto">
            {projectAllocation.map((pa) => (
              <div key={pa.projectId} className="flex justify-between items-center bg-slate-950/40 p-2.5 rounded-lg border border-slate-800">
                <div>
                  <span className="font-semibold text-white block">{pa.projectName}</span>
                  <span className="text-[10px] text-slate-400">Lead: {pa.managerName}</span>
                </div>
                <span className="rounded-full bg-emerald-500/10 px-2 py-0.5 text-[10px] font-semibold text-emerald-400">
                  {pa.assignedMembersCount} Allocated
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
