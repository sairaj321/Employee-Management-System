import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, Project, Employee } from '../api';
import { FolderGit2, Plus, Users, Calendar, X, AlertCircle, Trash2 } from 'lucide-react';

export const ProjectsPage: React.FC = () => {
  const { hasRole, hasPermission } = useAuth();
  const [projects, setProjects] = useState<Project[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);

  // Create Project Modal
  const [modalOpen, setModalOpen] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState(new Date().toISOString().split('T')[0]);
  const [managerId, setManagerId] = useState<number | undefined>(undefined);

  // Assign Employee Modal
  const [assignModalOpen, setAssignModalOpen] = useState(false);
  const [selectedProjectId, setSelectedProjectId] = useState<number | null>(null);
  const [assignEmpId, setAssignEmpId] = useState<number>(1);

  useEffect(() => {
    fetchProjects();
    fetchEmployees();
  }, []);

  const fetchProjects = async () => {
    setLoading(true);
    try {
      const res = await api.projects.getAll();
      if (res.data.success) setProjects(res.data.data);
    } catch {}
    finally { setLoading(false); }
  };

  const fetchEmployees = async () => {
    try {
      const res = await api.employees.getPaged({ page: 1, pageSize: 100 });
      if (res.data.success) {
        setEmployees(res.data.data.items);
        if (res.data.data.items.length > 0) {
          setAssignEmpId(res.data.data.items[0].id);
        }
      }
    } catch {}
  };

  const handleCreateProject = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.projects.create({
        name,
        description,
        startDate,
        managerId: managerId ? Number(managerId) : undefined
      });
      setModalOpen(false);
      setName('');
      setDescription('');
      fetchProjects();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to create project');
    }
  };

  const handleAssignEmployee = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProjectId) return;
    try {
      await api.projects.assignEmployee(selectedProjectId, {
        employeeId: Number(assignEmpId),
        allocatedFrom: new Date().toISOString()
      });
      setAssignModalOpen(false);
      fetchProjects();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to assign employee');
    }
  };

  const handleRemoveEmployee = async (projectId: number, empId: number) => {
    if (!window.confirm('Remove employee from project?')) return;
    try {
      await api.projects.removeEmployee(projectId, empId);
      fetchProjects();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 flex items-center gap-2">
            <FolderGit2 className="h-6 w-6 text-[#16A34A]" />
            Projects & Workflows
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">Track deliverables and allocate employee engineering teams</p>
        </div>

        {hasPermission('Project.Create') && (
          <button
            onClick={() => {
              setManagerId(employees[0]?.id);
              setModalOpen(true);
            }}
            className="flex items-center gap-2 rounded-lg bg-[#2563EB] px-3.5 py-2 text-xs font-bold text-white shadow-sm hover:bg-blue-700 transition"
          >
            <Plus className="h-4 w-4" />
            <span>New Project</span>
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {loading ? (
          <div className="col-span-full py-8 text-center text-xs text-slate-400">Loading projects...</div>
        ) : projects.length === 0 ? (
          <div className="col-span-full py-8 text-center text-xs text-slate-400">No active projects found.</div>
        ) : (
          projects.map((proj) => (
            <div key={proj.id} className="rounded-[10px] border border-[#E2E8F0] bg-white p-5 space-y-4 shadow-[0_1px_2px_rgba(15,23,42,0.06)]">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="text-sm font-bold text-slate-900">{proj.name}</h3>
                  <p className="text-xs text-slate-500 mt-1">{proj.description || 'No description'}</p>
                </div>
                <span className="rounded-full bg-[#DCFCE7] px-2.5 py-0.5 text-[10px] font-bold text-[#15803D] border border-green-200">
                  {proj.status}
                </span>
              </div>

              <div className="border-t border-[#E2E8F0] pt-3 flex justify-between text-xs text-slate-500">
                <span>Manager: <strong className="text-slate-800 font-semibold">{proj.managerName || 'None'}</strong></span>
                <span>Started: <strong className="text-slate-800 font-semibold">{new Date(proj.startDate).toLocaleDateString()}</strong></span>
              </div>

              {/* Assigned members */}
              <div className="space-y-2 border-t border-[#E2E8F0] pt-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-700">Allocated Members ({proj.assignedEmployees.length})</span>
                  {hasRole(['Admin', 'HR', 'Manager']) && (
                    <button
                      onClick={() => {
                        setSelectedProjectId(proj.id);
                        setAssignModalOpen(true);
                      }}
                      className="text-[11px] text-[#2563EB] hover:underline flex items-center gap-1 font-semibold"
                    >
                      <Plus className="h-3 w-3" /> Assign Member
                    </button>
                  )}
                </div>

                <div className="flex flex-wrap gap-2">
                  {proj.assignedEmployees.length === 0 ? (
                    <span className="text-xs text-slate-400">No members assigned yet.</span>
                  ) : (
                    proj.assignedEmployees.map((m) => (
                      <span
                        key={m.employeeId}
                        className="inline-flex items-center gap-1.5 rounded-lg bg-slate-50 px-2.5 py-1 text-xs text-slate-700 border border-[#E2E8F0]"
                      >
                        <span className="font-medium">{m.employeeName}</span>
                        {hasRole(['Admin', 'HR', 'Manager']) && (
                          <button
                            onClick={() => handleRemoveEmployee(proj.id, m.employeeId)}
                            className="text-slate-400 hover:text-[#DC2626] transition"
                          >
                            <X className="h-3 w-3" />
                          </button>
                        )}
                      </span>
                    ))
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Create Project Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-[10px] border border-[#E2E8F0] bg-white p-6 shadow-xl">
            <div className="flex justify-between items-center mb-4 border-b border-[#E2E8F0] pb-2">
              <h3 className="text-sm font-bold text-slate-900">Create Project</h3>
              <button onClick={() => setModalOpen(false)} className="text-slate-400 hover:text-slate-900"><X className="h-4 w-4" /></button>
            </div>
            <form onSubmit={handleCreateProject} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Project Name *</label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Description</label>
                <textarea
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Start Date *</label>
                <input
                  type="date"
                  required
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                />
              </div>
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Lead Manager</label>
                <select
                  value={managerId || ''}
                  onChange={(e) => setManagerId(e.target.value ? Number(e.target.value) : undefined)}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                >
                  <option value="">Unassigned</option>
                  {employees.map((e) => (
                    <option key={e.id} value={e.id}>{e.firstName} {e.lastName}</option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end gap-2 pt-3 border-t border-[#E2E8F0]">
                <button type="button" onClick={() => setModalOpen(false)} className="px-3.5 py-1.5 border border-[#E2E8F0] rounded-lg text-slate-600 hover:bg-slate-50 transition">Cancel</button>
                <button type="submit" className="px-4 py-1.5 bg-[#2563EB] hover:bg-blue-700 rounded-lg text-white font-bold transition shadow-sm">Create</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Assign Employee Modal */}
      {assignModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4">
          <div className="w-full max-w-sm rounded-[10px] border border-[#E2E8F0] bg-white p-5 shadow-xl">
            <div className="flex justify-between items-center mb-3 border-b border-[#E2E8F0] pb-2">
              <h3 className="text-sm font-bold text-slate-900">Assign Member</h3>
              <button onClick={() => setAssignModalOpen(false)} className="text-slate-400 hover:text-slate-900"><X className="h-4 w-4" /></button>
            </div>
            <form onSubmit={handleAssignEmployee} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 font-semibold text-slate-700">Select Employee *</label>
                <select
                  value={assignEmpId}
                  onChange={(e) => setAssignEmpId(Number(e.target.value))}
                  className="w-full rounded-lg border border-[#E2E8F0] bg-white p-2 text-slate-900 focus:border-[#2563EB] focus:outline-none"
                >
                  {employees.map((e) => (
                    <option key={e.id} value={e.id}>{e.firstName} {e.lastName} ({e.employeeCode})</option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end gap-2 pt-2 border-t border-[#E2E8F0]">
                <button type="button" onClick={() => setAssignModalOpen(false)} className="px-3.5 py-1.5 border border-[#E2E8F0] rounded-lg text-slate-600 hover:bg-slate-50 transition">Cancel</button>
                <button type="submit" className="px-4 py-1.5 bg-[#2563EB] hover:bg-blue-700 rounded-lg text-white font-bold transition shadow-sm">Assign</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
