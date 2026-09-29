import React, { useState, useEffect } from 'react';
import { useAuth } from '../auth/AuthContext';
import { api, Department, Position } from '../api';
import { Building2, Briefcase, Plus, Edit2, Trash2, X, AlertCircle } from 'lucide-react';

export const Departments: React.FC = () => {
  const { hasPermission } = useAuth();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingDept, setEditingDept] = useState<Department | null>(null);
  const [name, setName] = useState('');
  const [location, setLocation] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDepartments();
  }, []);

  const fetchDepartments = async () => {
    setLoading(true);
    try {
      const res = await api.departments.getAll();
      if (res.data.success) setDepartments(res.data.data);
    } catch {}
    finally { setLoading(false); }
  };

  const handleOpenCreate = () => {
    setEditingDept(null);
    setName('');
    setLocation('');
    setError(null);
    setModalOpen(true);
  };

  const handleOpenEdit = (d: Department) => {
    setEditingDept(d);
    setName(d.name);
    setLocation(d.location || '');
    setError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      if (editingDept) {
        await api.departments.update(editingDept.id, { name, location });
      } else {
        await api.departments.create({ name, location });
      }
      setModalOpen(false);
      fetchDepartments();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Action failed.');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this department?')) return;
    try {
      await api.departments.delete(id);
      fetchDepartments();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete department');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            <Building2 className="h-6 w-6 text-pink-400" />
            Departments
          </h1>
          <p className="text-xs text-slate-400 mt-0.5">Manage organizational units and locations</p>
        </div>
        {hasPermission('Department.Create') && (
          <button
            onClick={handleOpenCreate}
            className="flex items-center gap-2 rounded-xl bg-pink-600 px-3.5 py-2 text-xs font-bold text-white shadow-lg shadow-pink-600/30 hover:bg-pink-500 transition"
          >
            <Plus className="h-4 w-4" />
            <span>Add Department</span>
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {departments.map((d) => (
          <div key={d.id} className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-3">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="text-sm font-bold text-white">{d.name}</h3>
                <span className="text-xs text-slate-400">{d.location || 'HQ'}</span>
              </div>
              <div className="flex items-center gap-1">
                {hasPermission('Department.Update') && (
                  <button onClick={() => handleOpenEdit(d)} className="rounded p-1 text-slate-400 hover:text-white">
                    <Edit2 className="h-3.5 w-3.5" />
                  </button>
                )}
                {hasPermission('Department.Delete') && (
                  <button onClick={() => handleDelete(d.id)} className="rounded p-1 text-slate-400 hover:text-rose-400">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            </div>
            <div className="border-t border-slate-800/60 pt-3 flex justify-between text-xs text-slate-400">
              <span>Department Head: <strong className="text-slate-200">{d.managerName || 'Unassigned'}</strong></span>
              <span>Employees: <strong className="text-indigo-400">{d.employeeCount}</strong></span>
            </div>
          </div>
        ))}
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
            <div className="flex justify-between items-center mb-4 border-b border-slate-800 pb-2">
              <h3 className="text-sm font-bold text-white">{editingDept ? 'Edit Department' : 'Create Department'}</h3>
              <button onClick={() => setModalOpen(false)} className="text-slate-400 hover:text-white"><X className="h-4 w-4" /></button>
            </div>
            {error && (
              <div className="mb-3 text-xs text-rose-400 flex items-center gap-1.5"><AlertCircle className="h-4 w-4" />{error}</div>
            )}
            <form onSubmit={handleSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 text-slate-300">Department Name</label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                />
              </div>
              <div>
                <label className="block mb-1 text-slate-300">Location / Floor</label>
                <input
                  type="text"
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                />
              </div>
              <div className="flex justify-end gap-2 pt-3">
                <button type="button" onClick={() => setModalOpen(false)} className="px-3 py-1.5 border border-slate-700 rounded text-slate-300">Cancel</button>
                <button type="submit" className="px-4 py-1.5 bg-pink-600 rounded text-white font-bold">Save</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export const Positions: React.FC = () => {
  const { hasPermission } = useAuth();
  const [positions, setPositions] = useState<Position[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingPos, setEditingPos] = useState<Position | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  useEffect(() => {
    fetchPositions();
  }, []);

  const fetchPositions = async () => {
    try {
      const res = await api.positions.getAll();
      if (res.data.success) setPositions(res.data.data);
    } catch {}
  };

  const handleOpenCreate = () => {
    setEditingPos(null);
    setTitle('');
    setDescription('');
    setModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingPos) {
        await api.positions.update(editingPos.id, { title, description });
      } else {
        await api.positions.create({ title, description });
      }
      setModalOpen(false);
      fetchPositions();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Delete position?')) return;
    try {
      await api.positions.delete(id);
      fetchPositions();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-white flex items-center gap-2">
            <Briefcase className="h-6 w-6 text-indigo-400" />
            Job Positions
          </h1>
          <p className="text-xs text-slate-400 mt-0.5">Define corporate hierarchy and position designations</p>
        </div>
        {hasPermission('Position.Create') && (
          <button
            onClick={handleOpenCreate}
            className="flex items-center gap-2 rounded-xl bg-indigo-600 px-3.5 py-2 text-xs font-bold text-white shadow-lg hover:bg-indigo-500 transition"
          >
            <Plus className="h-4 w-4" />
            <span>Add Position</span>
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {positions.map((p) => (
          <div key={p.id} className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 space-y-3">
            <div className="flex items-start justify-between">
              <div>
                <h3 className="text-sm font-bold text-white">{p.title}</h3>
                <p className="text-xs text-slate-400 line-clamp-2 mt-1">{p.description || 'General role'}</p>
              </div>
              <div className="flex items-center gap-1">
                {hasPermission('Position.Delete') && (
                  <button onClick={() => handleDelete(p.id)} className="rounded p-1 text-slate-400 hover:text-rose-400">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            </div>
            <div className="border-t border-slate-800/60 pt-3 text-xs text-slate-400">
              Active Employees: <strong className="text-indigo-400">{p.employeeCount}</strong>
            </div>
          </div>
        ))}
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-2xl">
            <div className="flex justify-between items-center mb-4 border-b border-slate-800 pb-2">
              <h3 className="text-sm font-bold text-white">Create Position</h3>
              <button onClick={() => setModalOpen(false)} className="text-slate-400 hover:text-white"><X className="h-4 w-4" /></button>
            </div>
            <form onSubmit={handleSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block mb-1 text-slate-300">Position Title</label>
                <input
                  type="text"
                  required
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                />
              </div>
              <div>
                <label className="block mb-1 text-slate-300">Description</label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 p-2 text-white"
                />
              </div>
              <div className="flex justify-end gap-2 pt-3">
                <button type="button" onClick={() => setModalOpen(false)} className="px-3 py-1.5 border border-slate-700 rounded text-slate-300">Cancel</button>
                <button type="submit" className="px-4 py-1.5 bg-indigo-600 rounded text-white font-bold">Save</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
