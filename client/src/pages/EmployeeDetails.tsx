import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { api, Employee, SalaryStructure, Payslip, Attendance, Leave } from '../api';
import {
  Users,
  Building2,
  Briefcase,
  Mail,
  Phone,
  Calendar,
  DollarSign,
  Clock,
  CalendarDays,
  FileSpreadsheet,
  ArrowLeft,
  CheckCircle2,
  XCircle,
  AlertCircle
} from 'lucide-react';

export const EmployeeDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const empId = Number(id);
  const { hasRole } = useAuth();

  const [employee, setEmployee] = useState<Employee | null>(null);
  const [salaryStructure, setSalaryStructure] = useState<SalaryStructure | null>(null);
  const [payslips, setPayslips] = useState<Payslip[]>([]);
  const [attendances, setAttendances] = useState<Attendance[]>([]);
  const [leaves, setLeaves] = useState<Leave[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'profile' | 'salary' | 'attendance' | 'leaves'>('profile');

  useEffect(() => {
    if (empId) {
      fetchDetails();
    }
  }, [empId]);

  const fetchDetails = async () => {
    setLoading(true);
    try {
      const empRes = await api.employees.getById(empId);
      if (empRes.data.success) setEmployee(empRes.data.data);

      if (hasRole(['Admin', 'HR'])) {
        try {
          const salRes = await api.salary.getStructure(empId);
          if (salRes.data.success) setSalaryStructure(salRes.data.data);
        } catch {}
      }

      try {
        const payRes = await api.salary.getPayslips(empId);
        if (payRes.data.success) setPayslips(payRes.data.data);
      } catch {}

      try {
        const attRes = await api.attendance.getHistory({ employeeId: empId });
        if (attRes.data.success) setAttendances(attRes.data.data.slice(0, 10));
      } catch {}

      try {
        const leaveRes = await api.leaves.getAll({ employeeId: empId });
        if (leaveRes.data.success) setLeaves(leaveRes.data.data);
      } catch {}
    } catch {
      // Ignore
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-xs text-slate-500">Loading employee details...</div>;
  }

  if (!employee) {
    return (
      <div className="p-8 text-center text-xs text-rose-400">
        Employee record not found or access restricted.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back button & Title */}
      <div className="flex items-center gap-3">
        <Link
          to="/employees"
          className="rounded-lg border border-slate-800 p-2 text-slate-400 hover:bg-slate-800 hover:text-white transition"
        >
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <div>
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            {employee.firstName} {employee.lastName}
            <span className="text-xs font-mono font-normal text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded border border-indigo-500/20">
              {employee.employeeCode}
            </span>
          </h1>
          <p className="text-xs text-slate-400">{employee.positionTitle || 'Team Member'} • {employee.departmentName || 'General'}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-800 gap-4 text-xs font-semibold">
        <button
          onClick={() => setActiveTab('profile')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'profile' ? 'border-indigo-500 text-indigo-400' : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          Profile Details
        </button>
        {hasRole(['Admin', 'HR']) && (
          <button
            onClick={() => setActiveTab('salary')}
            className={`pb-3 border-b-2 transition ${
              activeTab === 'salary' ? 'border-indigo-500 text-indigo-400' : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            Compensation & Salary
          </button>
        )}
        <button
          onClick={() => setActiveTab('attendance')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'attendance' ? 'border-indigo-500 text-indigo-400' : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          Recent Attendance
        </button>
        <button
          onClick={() => setActiveTab('leaves')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'leaves' ? 'border-indigo-500 text-indigo-400' : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          Leave Applications
        </button>
      </div>

      {/* Tab Content */}
      {activeTab === 'profile' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Personal Information</h3>
            <div className="space-y-3 text-xs">
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Email:</span>
                <span className="font-medium text-white">{employee.email}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Phone:</span>
                <span className="font-medium text-white">{employee.phone}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Date of Birth:</span>
                <span className="font-medium text-white">{new Date(employee.dateOfBirth).toLocaleDateString()}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Gender:</span>
                <span className="font-medium text-white">{employee.gender || '—'}</span>
              </div>
            </div>
          </div>

          <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Employment Details</h3>
            <div className="space-y-3 text-xs">
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Department:</span>
                <span className="font-medium text-white">{employee.departmentName}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Position:</span>
                <span className="font-medium text-white">{employee.positionTitle}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Reporting Manager:</span>
                <span className="font-medium text-white">{employee.managerName || 'None'}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Joining Date:</span>
                <span className="font-medium text-white">{new Date(employee.joiningDate).toLocaleDateString()}</span>
              </div>
              <div className="flex justify-between border-b border-slate-800/60 pb-2">
                <span className="text-slate-400">Status:</span>
                <span className="font-semibold text-emerald-400">{employee.status === 1 ? 'Active' : 'Terminated'}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'salary' && salaryStructure && (
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-4">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Salary Breakdown</h3>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div className="bg-slate-950/60 p-3 rounded-lg border border-slate-800">
              <span className="text-[11px] text-slate-400 block">Basic Salary</span>
              <span className="text-base font-bold text-white">${salaryStructure.basicSalary.toLocaleString()}</span>
            </div>
            <div className="bg-slate-950/60 p-3 rounded-lg border border-slate-800">
              <span className="text-[11px] text-slate-400 block">HRA Allowance</span>
              <span className="text-base font-bold text-white">${salaryStructure.hra.toLocaleString()}</span>
            </div>
            <div className="bg-slate-950/60 p-3 rounded-lg border border-slate-800">
              <span className="text-[11px] text-slate-400 block">PF Deduction</span>
              <span className="text-base font-bold text-rose-400">-${salaryStructure.pfDeduction.toLocaleString()}</span>
            </div>
            <div className="bg-slate-950/60 p-3 rounded-lg border border-emerald-500/30">
              <span className="text-[11px] text-emerald-400 font-semibold block">Net Salary</span>
              <span className="text-base font-bold text-emerald-400">${salaryStructure.netSalary.toLocaleString()}</span>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'attendance' && (
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-4">Recent Attendance Logs</h3>
          <div className="space-y-2">
            {attendances.length === 0 ? (
              <p className="text-xs text-slate-500">No attendance history available.</p>
            ) : (
              attendances.map((a) => (
                <div key={a.id} className="flex items-center justify-between bg-slate-950/40 p-3 rounded-lg border border-slate-800/60 text-xs">
                  <span className="font-semibold text-white">{a.date}</span>
                  <div className="flex items-center gap-4 text-slate-400 text-[11px]">
                    <span>In: {a.checkInTime ? new Date(a.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}</span>
                    <span>Out: {a.checkOutTime ? new Date(a.checkOutTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}</span>
                    <span className="font-semibold text-emerald-400">{a.status === 1 ? 'Present' : 'Late'}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {activeTab === 'leaves' && (
        <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-5">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-4">Leave Records</h3>
          <div className="space-y-2">
            {leaves.length === 0 ? (
              <p className="text-xs text-slate-500">No leave requests found.</p>
            ) : (
              leaves.map((l) => (
                <div key={l.id} className="flex items-center justify-between bg-slate-950/40 p-3 rounded-lg border border-slate-800/60 text-xs">
                  <div>
                    <span className="font-semibold text-white block">{l.leaveTypeName}</span>
                    <span className="text-[11px] text-slate-400">{l.startDate} to {l.endDate} ({l.daysCount} days)</span>
                  </div>
                  <span className="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] font-semibold text-slate-300">
                    {l.status === 2 ? 'Approved' : l.status === 3 ? 'Rejected' : 'Pending'}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
