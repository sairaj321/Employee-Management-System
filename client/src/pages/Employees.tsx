import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { api, Employee, Department, Position } from '../api';
import {
  Users,
  Search,
  Plus,
  Edit2,
  Trash2,
  Eye,
  Building2,
  Briefcase,
  Mail,
  Phone,
  Filter,
  X,
  CheckCircle2,
  AlertCircle
} from 'lucide-react';

export const Employees: React.FC = () => {
  const { hasPermission, hasRole } = useAuth();
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [loading, setLoading] = useState(true);
  const [totalCount, setTotalCount] = useState(0);

  // Filters & Pagination
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [selectedDept, setSelectedDept] = useState<number | undefined>(undefined);

  // Modal state
  const [modalOpen, setModalOpen] = useState(false);
  const [editingEmp, setEditingEmp] = useState<Employee | null>(null);
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    dateOfBirth: '1995-01-01',
    gender: 'Male',
    departmentId: 0,
    positionId: 0,
    managerId: undefined as number | undefined,
    joiningDate: new Date().toISOString().split('T')[0],
    employmentType: 1,
    initialPassword: 'Employee@123',
    status: 1
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [formSubmitting, setFormSubmitting] = useState(false);

  useEffect(() => {
    fetchMetadata();
  }, []);

  useEffect(() => {
    fetchEmployees();
  }, [page, search, selectedDept]);

  const fetchMetadata = async () => {
    try {
      const [deptRes, posRes] = await Promise.all([
        api.departments.getAll(),
        api.positions.getAll(),
      ]);
      if (deptRes.data.success) setDepartments(deptRes.data.data);
      if (posRes.data.success) setPositions(posRes.data.data);
    } catch {
      // Ignore
    }
  };

  const fetchEmployees = async () => {
  setLoading(true);

  try {
   const res = await api.employees.getPaged({
  page,
  pageSize,
  search: search || undefined,
  departmentId: selectedDept,
});

    console.log("Employee API response:", res.data);

    if (res.data.success) {
      setEmployees(res.data.data.items);
      setTotalCount(res.data.data.totalCount);
    } else {
      console.error("Employee API failed:", res.data);
      setEmployees([]);
      setTotalCount(0);
    }
  } catch (err: any) {
    console.error("Employee API ERROR:", err);
    console.error("Status:", err.response?.status);
    console.error("Response:", err.response?.data);

    setEmployees([]);
    setTotalCount(0);
  } finally {
    setLoading(false);
  }
};

  const handleOpenCreateModal = () => {
    setEditingEmp(null);
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      dateOfBirth: '1995-01-01',
      gender: 'Male',
      departmentId: departments[0]?.id || 1,
      positionId: positions[0]?.id || 1,
      managerId: undefined,
      joiningDate: new Date().toISOString().split('T')[0],
      employmentType: 1,
      initialPassword: 'Employee@123',
      status: 1
    });
    setFormError(null);
    setModalOpen(true);
  };

  const handleOpenEditModal = (emp: Employee) => {
    setEditingEmp(emp);
    setFormData({
      firstName: emp.firstName,
      lastName: emp.lastName,
      email: emp.email,
      phone: emp.phone,
      dateOfBirth: emp.dateOfBirth.split('T')[0],
      gender: emp.gender || 'Male',
      departmentId: emp.departmentId,
      positionId: emp.positionId,
      managerId: emp.managerId,
      joiningDate: emp.joiningDate.split('T')[0],
      employmentType: typeof emp.employmentType === 'number' ? emp.employmentType : 1,
      initialPassword: '',
      status: typeof emp.status === 'number' ? emp.status : 1
    });
    setFormError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormSubmitting(true);
    setFormError(null);

    try {
      if (editingEmp) {
        await api.employees.update(editingEmp.id, {
          firstName: formData.firstName,
          lastName: formData.lastName,
          phone: formData.phone,
          dateOfBirth: formData.dateOfBirth,
          gender: formData.gender,
          departmentId: Number(formData.departmentId),
          positionId: Number(formData.positionId),
          managerId: formData.managerId ? Number(formData.managerId) : undefined,
          employmentType: Number(formData.employmentType),
          status: Number(formData.status)
        });
      } else {
        await api.employees.create({
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          phone: formData.phone,
          dateOfBirth: formData.dateOfBirth,
          gender: formData.gender,
          departmentId: Number(formData.departmentId),
          positionId: Number(formData.positionId),
          managerId: formData.managerId ? Number(formData.managerId) : undefined,
          joiningDate: formData.joiningDate,
          employmentType: Number(formData.employmentType),
          initialPassword: formData.initialPassword
        });
      }
      setModalOpen(false);
      fetchEmployees();
    } catch (err: any) {
      setFormError(err.response?.data?.message || 'Operation failed. Check input fields.');
    } finally {
      setFormSubmitting(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to deactivate this employee? (Soft Delete)')) return;
    try {
      await api.employees.delete(id);
      fetchEmployees();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to deactivate employee');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Users className="h-6 w-6 text-[#2563EB]" />
            Employees Directory
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Manage company employees, positions, departments, and credentials
          </p>
        </div>

        {hasPermission('Employee.Create') && (
          <button
            onClick={handleOpenCreateModal}
            className="flex items-center gap-2 rounded-lg bg-[#2563EB] px-3.5 py-2 text-xs font-bold text-white shadow-sm hover:bg-blue-700 transition"
          >
            <Plus className="h-4 w-4" />
            <span>Add New Employee</span>
          </button>
        )}
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row items-center gap-3 rounded-[10px] border border-[#E2E8F0] bg-white p-3 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="relative flex-1 w-full">
          <Search className="pointer-events-none absolute inset-y-0 left-3 h-4 w-4 my-auto text-slate-400" />
          <input
            type="text"
            placeholder="Search by name, email, or employee code..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="w-full rounded-lg border border-[#E2E8F0] bg-white py-2 pl-9 pr-3 text-xs text-slate-900 placeholder-slate-400 focus:border-blue-500 focus:outline-none"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <select
            value={selectedDept || ''}
            onChange={(e) => {
              setSelectedDept(e.target.value ? Number(e.target.value) : undefined);
              setPage(1);
            }}
            className="w-full sm:w-48 rounded-lg border border-[#E2E8F0] bg-white py-2 px-3 text-xs text-slate-700 focus:border-blue-500 focus:outline-none"
          >
            <option value="">All Departments</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Employees Table */}
      <div className="rounded-[10px] border border-[#E2E8F0] bg-white overflow-hidden shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-600">
            <thead className="border-b border-[#E2E8F0] bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
              <tr>
                <th className="px-4 py-3">Employee</th>
                <th className="px-4 py-3">Code</th>
                <th className="px-4 py-3">Department</th>
                <th className="px-4 py-3">Position</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-xs text-slate-400">
                    Loading employees...
                  </td>
                </tr>
              ) : employees.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-xs text-slate-400">
                    No employees found matching the filters
                  </td>
                </tr>
              ) : (
                employees.map((emp) => (
                  <tr key={emp.id} className="hover:bg-slate-50 transition">
                    <td className="px-4 py-3 font-medium text-slate-900 flex items-center gap-2.5">
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-blue-50 text-blue-600 font-bold text-xs border border-blue-200">
                        {emp.firstName.charAt(0)}
                      </div>
                      <div>
                        <span className="block font-semibold">{emp.firstName} {emp.lastName}</span>
                        <span className="text-[10px] text-slate-500">{emp.email}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 font-mono text-blue-600">{emp.employeeCode}</td>
                    <td className="px-4 py-3 text-slate-600">{emp.departmentName || '—'}</td>
                    <td className="px-4 py-3 text-slate-600">{emp.positionTitle || '—'}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold ${
                          emp.status === 1 || emp.status === 'Active'
                            ? 'bg-[#DCFCE7] text-[#15803D] border border-green-200'
                            : 'bg-[#FEE2E2] text-[#DC2626] border border-red-200'
                        }`}
                      >
                        {emp.status === 1 || emp.status === 'Active' ? 'Active' : 'Terminated'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <Link
                          to={`/employees/${emp.id}`}
                          className="rounded p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700 transition"
                          title="View Profile"
                        >
                          <Eye className="h-4 w-4" />
                        </Link>
                        {hasPermission('Employee.Update') && (
                          <button
                            onClick={() => handleOpenEditModal(emp)}
                            className="rounded p-1.5 text-slate-400 hover:bg-blue-50 hover:text-blue-600 transition"
                            title="Edit"
                          >
                            <Edit2 className="h-4 w-4" />
                          </button>
                        )}
                        {hasPermission('Employee.Delete') && (
                          <button
                            onClick={() => handleDelete(emp.id)}
                            className="rounded p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 transition"
                            title="Deactivate"
                          >
                            <Trash2 className="h-4 w-4" />
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

        {/* Pagination Footer */}
        <div className="flex items-center justify-between border-t border-[#E2E8F0] px-4 py-3 bg-slate-50 text-xs text-slate-500">
          <span>Total Employees: {totalCount}</span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="rounded-lg border border-[#E2E8F0] bg-white px-2.5 py-1 text-xs text-slate-700 hover:bg-slate-50 disabled:opacity-40"
            >
              Previous
            </button>
            <span>Page {page} of {Math.ceil(totalCount / pageSize) || 1}</span>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={page * pageSize >= totalCount}
              className="rounded-lg border border-[#E2E8F0] bg-white px-2.5 py-1 text-xs text-slate-700 hover:bg-slate-50 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      </div>

      {/* Create / Edit Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4 overflow-y-auto">
          <div className="w-full max-w-xl rounded-[10px] border border-[#E2E8F0] bg-white p-6 shadow-xl">
            <div className="flex items-center justify-between border-b border-[#E2E8F0] pb-3 mb-4">
              <h3 className="text-sm font-bold text-slate-900">
                {editingEmp ? `Edit Employee (${editingEmp.employeeCode})` : 'Add New Employee'}
              </h3>
              <button
                onClick={() => setModalOpen(false)}
                className="text-slate-400 hover:text-slate-900"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            {formError && (
              <div className="mb-4 flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 p-2.5 text-xs text-red-700">
                <AlertCircle className="h-4 w-4 shrink-0" />
                <span>{formError}</span>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4 text-xs">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">First Name *</label>
                  <input
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Last Name *</label>
                  <input
                    type="text"
                    required
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  />
                </div>
              </div>

              {!editingEmp && (
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Work Email *</label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  />
                </div>
              )}

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Phone Number *</label>
                  <input
                    type="tel"
                    required
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Date of Birth *</label>
                  <input
                    type="date"
                    required
                    value={formData.dateOfBirth}
                    onChange={(e) => setFormData({ ...formData, dateOfBirth: e.target.value })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Department *</label>
                  <select
                    value={formData.departmentId}
                    onChange={(e) => setFormData({ ...formData, departmentId: Number(e.target.value) })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  >
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>{d.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block mb-1 text-slate-700 font-semibold">Position *</label>
                  <select
                    value={formData.positionId}
                    onChange={(e) => setFormData({ ...formData, positionId: Number(e.target.value) })}
                    className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-blue-500 focus:outline-none"
                  >
                    {positions.map((p) => (
                      <option key={p.id} value={p.id}>{p.title}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="flex justify-end gap-2 border-t border-[#E2E8F0] pt-4 mt-6">
                <button
                  type="button"
                  onClick={() => setModalOpen(false)}
                  className="rounded-lg border border-[#E2E8F0] px-3.5 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-50 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={formSubmitting}
                  className="rounded-lg bg-[#2563EB] px-4 py-2 text-xs font-bold text-white hover:bg-blue-700 transition disabled:opacity-50"
                >
                  {formSubmitting ? 'Saving...' : (editingEmp ? 'Save Changes' : 'Create Employee')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
