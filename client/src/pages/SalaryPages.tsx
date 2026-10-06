import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, SalaryStructure, Payslip, Employee } from '../api';
import { DollarSign, FileSpreadsheet, Plus, Sparkles, X, Printer, AlertCircle } from 'lucide-react';

export const SalaryPage: React.FC = () => {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [selectedEmpId, setSelectedEmpId] = useState<number>(1);
  const [structure, setStructure] = useState<SalaryStructure | null>(null);
  const [basic, setBasic] = useState(5000);
  const [hra, setHra] = useState(1500);
  const [other, setOther] = useState(500);
  const [pf, setPf] = useState(350);
  const [genMonth, setGenMonth] = useState(new Date().getMonth() + 1);
  const [genYear, setGenYear] = useState(new Date().getFullYear());
  const [msg, setMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchEmployees();
  }, []);

  useEffect(() => {
    if (selectedEmpId) {
      fetchSalaryStructure(selectedEmpId);
    }
  }, [selectedEmpId]);

  const fetchEmployees = async () => {
    try {
      const res = await api.employees.getPaged({ page: 1, pageSize: 100 });
      if (res.data.success && res.data.data.items.length > 0) {
        setEmployees(res.data.data.items);
        setSelectedEmpId(res.data.data.items[0].id);
      }
    } catch {}
    finally { setLoading(false); }
  };

  const fetchSalaryStructure = async (id: number) => {
    try {
      const res = await api.salary.getStructure(id);
      if (res.data.success && res.data.data) {
        setStructure(res.data.data);
        setBasic(res.data.data.basicSalary);
        setHra(res.data.data.hra);
        setOther(res.data.data.otherAllowances);
        setPf(res.data.data.pfDeduction);
      }
    } catch {
      setStructure(null);
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);
    try {
      const res = await api.salary.updateStructure(selectedEmpId, {
        basicSalary: Number(basic),
        hra: Number(hra),
        otherAllowances: Number(other),
        pfDeduction: Number(pf)
      });
      if (res.data.success) {
        setStructure(res.data.data);
        setMsg('Salary structure updated successfully!');
      }
    } catch (err: any) {
      setMsg(err.response?.data?.message || 'Failed to update salary');
    }
  };

  const handleGeneratePayslip = async () => {
    setMsg(null);
    try {
      const res = await api.salary.generatePayslip(selectedEmpId, genMonth, genYear);
      if (res.data.success) {
        setMsg(`Payslip for ${genMonth}/${genYear} generated successfully!`);
      }
    } catch (err: any) {
      setMsg(err.response?.data?.message || 'Failed to generate payslip');
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <DollarSign className="h-6 w-6 text-[#16A34A]" />
          Compensation & Salary Administration
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">Configure employee pay structures and trigger monthly payroll generation</p>
      </div>

      {msg && (
        <div className="rounded-lg border border-blue-200 bg-[#EFF6FF] p-3 text-xs text-[#2563EB] font-medium">
          {msg}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Select Employee */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-3 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Select Employee</h3>
          <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
            {employees.map((emp) => (
              <button
                key={emp.id}
                onClick={() => setSelectedEmpId(emp.id)}
                className={`w-full text-left p-2.5 rounded-lg border text-xs transition flex items-center justify-between ${
                  selectedEmpId === emp.id
                    ? 'border-blue-300 bg-[#EFF6FF] text-slate-900'
                    : 'border-[#E2E8F0] bg-white text-slate-600 hover:bg-slate-50'
                }`}
              >
                <div>
                  <div className="font-semibold text-slate-900">{emp.firstName} {emp.lastName}</div>
                  <div className="text-[10px] text-slate-500">{emp.employeeCode} • {emp.departmentName}</div>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Edit Salary Structure Form */}
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)] lg:col-span-2">
          <div className="flex justify-between items-center border-b border-[#E2E8F0] pb-3">
            <h3 className="text-sm font-bold text-slate-900">Structure Configuration</h3>
            <span className="text-xs font-mono text-[#15803D] bg-[#DCFCE7] px-2.5 py-0.5 rounded-full border border-green-200 font-bold">
              Net: ${((Number(basic) || 0) + (Number(hra) || 0) + (Number(other) || 0) - (Number(pf) || 0)).toLocaleString()}
            </span>
          </div>

          <form onSubmit={handleUpdate} className="space-y-4 text-xs">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block mb-1 text-slate-700 font-semibold">Basic Salary ($)</label>
                <input
                  type="number"
                  min="0"
                  required
                  value={basic}
                  onChange={(e) => setBasic(Number(e.target.value))}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 text-slate-700 font-semibold">House Rent Allowance (HRA) ($)</label>
                <input
                  type="number"
                  min="0"
                  required
                  value={hra}
                  onChange={(e) => setHra(Number(e.target.value))}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 text-slate-700 font-semibold">Other Allowances ($)</label>
                <input
                  type="number"
                  min="0"
                  required
                  value={other}
                  onChange={(e) => setOther(Number(e.target.value))}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 text-slate-700 font-semibold">PF / Tax Deductions ($)</label>
                <input
                  type="number"
                  min="0"
                  required
                  value={pf}
                  onChange={(e) => setPf(Number(e.target.value))}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2.5 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
            </div>

            <button
              type="submit"
              className="rounded-lg bg-[#2563EB] px-4 py-2 text-xs font-bold text-white hover:bg-blue-700 transition shadow-sm"
            >
              Save Salary Structure
            </button>
          </form>

          {/* Payslip Generation Widget */}
          <div className="border-t border-[#E2E8F0] pt-4 mt-6">
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3">Generate Monthly Payslip</h4>
            <div className="flex flex-wrap items-center gap-3">
              <select
                value={genMonth}
                onChange={(e) => setGenMonth(Number(e.target.value))}
                className="rounded-lg border border-[#E2E8F0] bg-white p-2 text-xs text-slate-700 focus:border-[#2563EB] focus:outline-none"
              >
                {Array.from({ length: 12 }, (_, i) => (
                  <option key={i + 1} value={i + 1}>Month {i + 1}</option>
                ))}
              </select>
              <select
                value={genYear}
                onChange={(e) => setGenYear(Number(e.target.value))}
                className="rounded-lg border border-[#E2E8F0] bg-white p-2 text-xs text-slate-700 focus:border-[#2563EB] focus:outline-none"
              >
                <option value={2026}>2026</option>
                <option value={2025}>2025</option>
              </select>
              <button
                type="button"
                onClick={handleGeneratePayslip}
                className="flex items-center gap-1.5 rounded-lg bg-[#16A34A] px-3.5 py-2 text-xs font-bold text-white hover:bg-green-700 transition shadow-sm"
              >
                <Sparkles className="h-3.5 w-3.5" />
                <span>Generate Payslip</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export const PayslipsPage: React.FC = () => {
  const { user } = useAuth();
  const [payslips, setPayslips] = useState<Payslip[]>([]);
  const [loading, setLoading] = useState(true);
  const [viewingPayslip, setViewingPayslip] = useState<Payslip | null>(null);

  useEffect(() => {
    fetchPayslips();
  }, []);

  const fetchPayslips = async () => {
    setLoading(true);
    try {
      if (user?.employeeId) {
        const res = await api.salary.getPayslips(user.employeeId);
        if (res.data.success) {
          setPayslips(res.data.data);
        }
      }
    } catch {}
    finally { setLoading(false); }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
          <FileSpreadsheet className="h-6 w-6 text-[#D97706]" />
          My Payslips & Tax Statements
        </h1>
        <p className="text-xs text-slate-500 mt-0.5">View and download your monthly compensation slips</p>
      </div>

      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
              <tr>
                <th className="px-4 py-3">Period</th>
                <th className="px-4 py-3">Gross Salary</th>
                <th className="px-4 py-3">Deductions</th>
                <th className="px-4 py-3">Net Pay</th>
                <th className="px-4 py-3">Generated Date</th>
                <th className="px-4 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr><td colSpan={6} className="py-8 text-center text-slate-400">Loading payslips...</td></tr>
              ) : payslips.length === 0 ? (
                <tr><td colSpan={6} className="py-8 text-center text-slate-400">No payslips generated yet.</td></tr>
              ) : (
                payslips.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50 transition">
                    <td className="px-4 py-3 font-semibold text-slate-900">
                      Month {p.month}, {p.year}
                    </td>
                    <td className="px-4 py-3 text-slate-700 font-medium">${p.grossSalary.toLocaleString()}</td>
                    <td className="px-4 py-3 text-[#DC2626] font-medium">-${p.deductions.toLocaleString()}</td>
                    <td className="px-4 py-3 font-bold text-[#15803D] font-mono">${p.netSalary.toLocaleString()}</td>
                    <td className="px-4 py-3 text-slate-500">{new Date(p.generatedAt).toLocaleDateString()}</td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => setViewingPayslip(p)}
                        className="rounded-lg px-2.5 py-1 bg-[#EFF6FF] text-[#2563EB] hover:bg-[#2563EB] hover:text-white transition font-medium text-xs border border-blue-200"
                      >
                        View Statement
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Payslip Modal View */}
      {viewingPayslip && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="w-full max-w-lg rounded-[10px] border border-[#E2E8F0] bg-white p-6 shadow-xl text-slate-900">
            <div className="flex justify-between items-start border-b border-[#E2E8F0] pb-4 mb-4">
              <div>
                <h3 className="text-base font-bold text-slate-900">EMS Pay Advice</h3>
                <p className="text-xs text-slate-500">Month {viewingPayslip.month}, {viewingPayslip.year}</p>
              </div>
              <button onClick={() => setViewingPayslip(null)} className="text-slate-400 hover:text-slate-900"><X className="h-4 w-4" /></button>
            </div>

            <div className="space-y-4 text-xs">
              <div className="flex justify-between bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
                <div>
                  <span className="text-[10px] text-slate-400 uppercase block font-semibold">Employee</span>
                  <span className="font-semibold text-slate-900">{viewingPayslip.employeeName}</span>
                </div>
                <div className="text-right">
                  <span className="text-[10px] text-slate-400 uppercase block font-semibold">Code</span>
                  <span className="font-mono text-[#2563EB] font-semibold">{viewingPayslip.employeeCode}</span>
                </div>
              </div>

              <div className="space-y-2 border-t border-[#E2E8F0] pt-3">
                <div className="flex justify-between py-1">
                  <span className="text-slate-500">Gross Earnings:</span>
                  <span className="font-medium text-slate-900">${viewingPayslip.grossSalary.toLocaleString()}</span>
                </div>
                <div className="flex justify-between py-1">
                  <span className="text-slate-500">Total Deductions (PF / Tax):</span>
                  <span className="font-medium text-[#DC2626]">-${viewingPayslip.deductions.toLocaleString()}</span>
                </div>
                <div className="flex justify-between py-2 border-t border-[#E2E8F0] text-sm font-bold text-[#15803D]">
                  <span>Net Disbursed Salary:</span>
                  <span className="font-mono">${viewingPayslip.netSalary.toLocaleString()}</span>
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-4 border-t border-[#E2E8F0]">
                <button
                  onClick={() => window.print()}
                  className="flex items-center gap-1.5 px-3 py-1.5 border border-[#E2E8F0] rounded-lg text-slate-700 hover:bg-slate-50 transition"
                >
                  <Printer className="h-3.5 w-3.5" />
                  <span>Print Slip</span>
                </button>
                <button
                  onClick={() => setViewingPayslip(null)}
                  className="px-4 py-1.5 bg-[#2563EB] hover:bg-blue-700 rounded-lg text-white font-bold transition shadow-sm"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
