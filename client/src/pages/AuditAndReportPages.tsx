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
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <History className="h-6 w-6 text-[#2563EB]" />
            Security & System Audit Logs
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">Immutable business event tracking and compliance history stored in PostgreSQL JSONB</p>
        </div>

        <select
          value={entityFilter}
          onChange={(e) => {
            setEntityFilter(e.target.value);
            setPage(1);
          }}
          className="rounded-lg border border-[#E2E8F0] bg-white px-3 py-2 text-xs text-slate-700 focus:border-[#2563EB] focus:outline-none shadow-sm"
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

      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
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
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-400">Loading audit history...</td></tr>
              ) : logs.length === 0 ? (
                <tr><td colSpan={7} className="py-8 text-center text-slate-400">No audit records found.</td></tr>
              ) : (
                logs.map((log) => (
                  <tr key={log.id} className="hover:bg-slate-50 transition font-mono">
                    <td className="px-4 py-3 text-slate-500 text-[11px]">{new Date(log.timestamp).toLocaleString()}</td>
                    <td className="px-4 py-3 font-semibold text-[#2563EB]">{log.action}</td>
                    <td className="px-4 py-3 text-slate-900 font-semibold">{log.entityName}</td>
                    <td className="px-4 py-3 text-slate-500">#{log.entityId}</td>
                    <td className="px-4 py-3 text-slate-700 font-sans">{log.userEmail || (log.userId ? `User #${log.userId}` : 'System')}</td>
                    <td className="px-4 py-3 text-slate-400">{log.ipAddress || '127.0.0.1'}</td>
                    <td className="px-4 py-3 text-right font-sans">
                      <button
                        onClick={() => setViewingLog(log)}
                        className="rounded-lg px-2.5 py-1 bg-slate-100 text-slate-700 hover:bg-slate-200 transition text-xs font-semibold"
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

        <div className="flex items-center justify-between border-t border-[#E2E8F0] px-4 py-3 bg-slate-50 text-xs text-slate-500">
          <span>Total Logs: {totalCount}</span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded-lg border border-[#E2E8F0] bg-white px-2.5 py-1 text-xs text-slate-700 hover:bg-slate-50 disabled:opacity-40 shadow-sm"
            >
              Previous
            </button>
            <span>Page {page} of {Math.ceil(totalCount / 15) || 1}</span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={page * 15 >= totalCount}
              className="rounded-lg border border-[#E2E8F0] bg-white px-2.5 py-1 text-xs text-slate-700 hover:bg-slate-50 disabled:opacity-40 shadow-sm"
            >
              Next
            </button>
          </div>
        </div>
      </div>

      {/* JSON Payload Inspector Modal */}
      {viewingLog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="w-full max-w-2xl rounded-[10px] border border-[#E2E8F0] bg-white p-6 shadow-xl">
            <div className="flex justify-between items-center mb-4 border-b border-[#E2E8F0] pb-3">
              <div>
                <h3 className="text-sm font-bold text-slate-900 flex items-center gap-2 font-mono">
                  {viewingLog.action} • {viewingLog.entityName} #{viewingLog.entityId}
                </h3>
                <span className="text-[10px] text-slate-400 font-mono">Correlation ID: {viewingLog.correlationId || 'N/A'}</span>
              </div>
              <button onClick={() => setViewingLog(null)} className="text-slate-400 hover:text-slate-900"><X className="h-4 w-4" /></button>
            </div>

            <div className="space-y-4 text-xs max-h-96 overflow-y-auto">
              {viewingLog.oldValue && (
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-[#DC2626] block mb-1">Previous Value</span>
                  <pre className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0] text-[11px] text-slate-700 font-mono overflow-x-auto">
                    {JSON.stringify(JSON.parse(viewingLog.oldValue), null, 2)}
                  </pre>
                </div>
              )}

              {viewingLog.newValue && (
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-[#16A34A] block mb-1">New Value</span>
                  <pre className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0] text-[11px] text-slate-700 font-mono overflow-x-auto">
                    {JSON.stringify(JSON.parse(viewingLog.newValue), null, 2)}
                  </pre>
                </div>
              )}
            </div>

            <div className="flex justify-end pt-4 border-t border-[#E2E8F0] mt-4">
              <button onClick={() => setViewingLog(null)} className="px-4 py-1.5 bg-[#2563EB] hover:bg-blue-700 rounded-lg text-white font-bold text-xs shadow-sm transition">
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
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <History className="h-6 w-6 text-[#2563EB]" />
          Enterprise Analytics & Projections
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Read-only performance projections and headcount analytics</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Headcount by Department */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-sm font-bold text-slate-900">Headcount by Department</h3>
          <div className="space-y-3 text-xs">
            {headcount.map((h) => (
              <div key={h.departmentId} className="space-y-1">
                <div className="flex justify-between text-slate-700 font-medium">
                  <span>{h.departmentName}</span>
                  <span className="text-slate-500">{h.activeEmployees} active / {h.totalEmployees} total</span>
                </div>
                <div className="h-2 w-full bg-slate-100 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-[#2563EB] rounded-full transition-all"
                    style={{ width: `${Math.min(100, (h.activeEmployees / 10) * 100)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Payroll Overview Summary */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-sm font-bold text-slate-900">Payroll Summary</h3>
          {payrollSummary ? (
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
                <span className="text-[11px] text-slate-500 block">Total Disbursed Net</span>
                <span className="text-lg font-bold text-[#15803D] font-mono">${payrollSummary.totalNetSalary.toLocaleString()}</span>
              </div>
              <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
                <span className="text-[11px] text-slate-500 block">Tax / PF Deductions</span>
                <span className="text-lg font-bold text-[#DC2626] font-mono">${payrollSummary.totalDeductions.toLocaleString()}</span>
              </div>
              <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
                <span className="text-[11px] text-slate-500 block">Gross Payroll</span>
                <span className="text-lg font-bold text-slate-900 font-mono">${payrollSummary.totalGrossSalary.toLocaleString()}</span>
              </div>
              <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
                <span className="text-[11px] text-slate-500 block">Employees Processed</span>
                <span className="text-lg font-bold text-[#2563EB]">{payrollSummary.totalEmployeesPaid}</span>
              </div>
            </div>
          ) : (
            <p className="text-xs text-slate-400">No payroll data generated for current period.</p>
          )}
        </div>

        {/* Leave Utilization */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-sm font-bold text-slate-900">Leave Utilization</h3>
          <div className="space-y-2 text-xs max-h-60 overflow-y-auto">
            {leaveUtilization.map((lu) => (
              <div key={lu.employeeId} className="flex justify-between items-center bg-slate-50 p-2.5 rounded-lg border border-[#E2E8F0]">
                <div>
                  <span className="font-semibold text-slate-900 block">{lu.employeeName}</span>
                  <span className="text-[10px] text-slate-500">{lu.departmentName}</span>
                </div>
                <div className="text-right">
                  <span className="text-[#2563EB] font-bold">{lu.totalLeavesTaken} days used</span>
                  <span className="text-[10px] text-slate-400 block">{lu.pendingRequests} pending</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Project Allocation */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-sm font-bold text-slate-900">Project Allocations</h3>
          <div className="space-y-2 text-xs max-h-60 overflow-y-auto">
            {projectAllocation.map((pa) => (
              <div key={pa.projectId} className="flex justify-between items-center bg-slate-50 p-2.5 rounded-lg border border-[#E2E8F0]">
                <div>
                  <span className="font-semibold text-slate-900 block">{pa.projectName}</span>
                  <span className="text-[10px] text-slate-500">Lead: {pa.managerName}</span>
                </div>
                <span className="rounded-full bg-[#DCFCE7] px-2 py-0.5 text-[10px] font-semibold text-[#15803D] border border-green-200">
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
