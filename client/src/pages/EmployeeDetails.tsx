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
    return <div className="p-8 text-center text-xs text-slate-400">Loading employee details...</div>;
  }

  if (!employee) {
    return (
      <div className="p-8 text-center text-xs text-red-600">
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
          className="rounded-lg border border-[#E2E8F0] p-2 text-slate-500 hover:bg-slate-50 hover:text-slate-900 transition"
        >
          <ArrowLeft className="h-4 w-4" />
        </Link>
        <div>
          <h1 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            {employee.firstName} {employee.lastName}
            <span className="text-xs font-mono font-normal text-blue-600 bg-blue-50 px-2 py-0.5 rounded border border-blue-200">
              {employee.employeeCode}
            </span>
          </h1>
          <p className="text-xs text-slate-500">{employee.positionTitle || 'Team Member'} • {employee.departmentName || 'General'}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-[#E2E8F0] gap-4 text-xs font-semibold">
        <button
          onClick={() => setActiveTab('profile')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'profile' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          Profile Details
        </button>
        {hasRole(['Admin', 'HR']) && (
          <button
            onClick={() => setActiveTab('salary')}
            className={`pb-3 border-b-2 transition ${
              activeTab === 'salary' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            Compensation & Salary
          </button>
        )}
        <button
          onClick={() => setActiveTab('attendance')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'attendance' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          Recent Attendance
        </button>
        <button
          onClick={() => setActiveTab('leaves')}
          className={`pb-3 border-b-2 transition ${
            activeTab === 'leaves' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-slate-500 hover:text-slate-700'
          }`}
        >
          Leave Applications
        </button>
      </div>

      {/* Tab Content */}
      {activeTab === 'profile' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Personal Information</h3>
            <div className="space-y-3 text-xs">
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Email:</span>
                <span className="font-medium text-slate-900">{employee.email}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Phone:</span>
                <span className="font-medium text-slate-900">{employee.phone}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Date of Birth:</span>
                <span className="font-medium text-slate-900">{new Date(employee.dateOfBirth).toLocaleDateString()}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Gender:</span>
                <span className="font-medium text-slate-900">{employee.gender || '—'}</span>
              </div>
            </div>
          </div>

          <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
            <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Employment Details</h3>
            <div className="space-y-3 text-xs">
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Department:</span>
                <span className="font-medium text-slate-900">{employee.departmentName}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Position:</span>
                <span className="font-medium text-slate-900">{employee.positionTitle}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Reporting Manager:</span>
                <span className="font-medium text-slate-900">{employee.managerName || 'None'}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Joining Date:</span>
                <span className="font-medium text-slate-900">{new Date(employee.joiningDate).toLocaleDateString()}</span>
              </div>
              <div className="flex justify-between border-b border-slate-100 pb-2">
                <span className="text-slate-500">Status:</span>
                <span className="font-semibold text-green-700">{employee.status === 1 ? 'Active' : 'Terminated'}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'salary' && salaryStructure && (
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400">Salary Breakdown</h3>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
              <span className="text-[11px] text-slate-500 block">Basic Salary</span>
              <span className="text-base font-bold text-slate-900">${salaryStructure.basicSalary.toLocaleString()}</span>
            </div>
            <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
              <span className="text-[11px] text-slate-500 block">HRA Allowance</span>
              <span className="text-base font-bold text-slate-900">${salaryStructure.hra.toLocaleString()}</span>
            </div>
            <div className="bg-slate-50 p-3 rounded-lg border border-[#E2E8F0]">
              <span className="text-[11px] text-slate-500 block">PF Deduction</span>
              <span className="text-base font-bold text-red-600">-${salaryStructure.pfDeduction.toLocaleString()}</span>
            </div>
            <div className="bg-green-50 p-3 rounded-lg border border-green-200">
              <span className="text-[11px] text-green-700 font-semibold block">Net Salary</span>
              <span className="text-base font-bold text-green-700">${salaryStructure.netSalary.toLocaleString()}</span>
            </div>
          </div>
        </div>
      )}

      {activeTab === 'attendance' && (
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-4">Recent Attendance Logs</h3>
          <div className="space-y-2">
            {attendances.length === 0 ? (
              <p className="text-xs text-slate-400">No attendance history available.</p>
            ) : (
              attendances.map((a) => (
                <div key={a.id} className="flex items-center justify-between bg-slate-50 p-3 rounded-lg border border-[#E2E8F0] text-xs">
                  <span className="font-semibold text-slate-900">{a.date}</span>
                  <div className="flex items-center gap-4 text-slate-500 text-[11px]">
                    <span>In: {a.checkInTime ? new Date(a.checkInTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}</span>
                    <span>Out: {a.checkOutTime ? new Date(a.checkOutTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '—'}</span>
                    <span className="font-semibold text-green-700">{a.status === 1 ? 'Present' : 'Late'}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {activeTab === 'leaves' && (
        <div className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
          <h3 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-4">Leave Records</h3>
          <div className="space-y-2">
            {leaves.length === 0 ? (
              <p className="text-xs text-slate-400">No leave requests found.</p>
            ) : (
              leaves.map((l) => (
                <div key={l.id} className="flex items-center justify-between bg-slate-50 p-3 rounded-lg border border-[#E2E8F0] text-xs">
                  <div>
                    <span className="font-semibold text-slate-900 block">{l.leaveTypeName}</span>
                    <span className="text-[11px] text-slate-500">{l.startDate} to {l.endDate} ({l.daysCount} days)</span>
                  </div>
                  <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${
                    l.status === 2 ? 'bg-[#DCFCE7] text-[#15803D] border border-green-200'
                    : l.status === 3 ? 'bg-[#FEE2E2] text-[#DC2626] border border-red-200'
                    : 'bg-[#FEF3C7] text-[#D97706] border border-amber-200'
                  }`}>
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
