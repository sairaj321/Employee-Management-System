import { axiosClient } from './axiosClient';

// Response Envelope
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: Record<string, string[]> | string[];
  traceId?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// User & Auth
export interface User {
  id: number;
  email: string;
  isLocked: boolean;
  lastLoginAt?: string;
  createdAt: string;
  employeeId?: number;
  fullName: string;
  roles: string[];
  permissions: string[];
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: User;
}

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  gender?: string;
  departmentId: number;
  departmentName?: string;
  positionId: number;
  positionTitle?: string;
  managerId?: number;
  managerName?: string;
  joiningDate: string;
  employmentType: number | string;
  status: number | string;
  userId: number;
  createdAt: string;
  updatedAt?: string;
}

export interface Department {
  id: number;
  name: string;
  location?: string;
  managerId?: number;
  managerName?: string;
  employeeCount: number;
}

export interface Position {
  id: number;
  title: string;
  description?: string;
  employeeCount: number;
}

export interface Attendance {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  departmentName: string;
  date: string;
  checkInTime?: string;
  checkOutTime?: string;
  status: number | string;
  totalHours?: number;
}

export interface Leave {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  daysCount: number;
  reason: string;
  status: number | string;
  approvedBy?: number;
  approverName?: string;
  approvedAt?: string;
}

export interface LeaveType {
  id: number;
  name: string;
  defaultDaysPerYear: number;
}

export interface SalaryStructure {
  id: number;
  employeeId: number;
  employeeName?: string;
  basicSalary: number;
  hra: number;
  otherAllowances: number;
  pfDeduction: number;
  grossSalary: number;
  netSalary: number;
  effectiveFrom: string;
}

export interface Payslip {
  id: number;
  employeeId: number;
  employeeName: string;
  employeeCode: string;
  departmentName: string;
  month: number;
  year: number;
  grossSalary: number;
  deductions: number;
  netSalary: number;
  generatedAt: string;
}

export interface Project {
  id: number;
  name: string;
  description?: string;
  startDate: string;
  endDate?: string;
  status: string;
  managerId?: number;
  managerName?: string;
  assignedEmployees: Array<{
    employeeId: number;
    employeeName: string;
    employeeCode: string;
    allocatedFrom: string;
    allocatedTo?: string;
  }>;
}

export interface Role {
  id: number;
  name: string;
  description?: string;
  permissions: Array<{ id: number; code: string; description?: string }>;
}

export interface Permission {
  id: number;
  code: string;
  description?: string;
}

export interface AuditLog {
  id: number;
  userId?: number;
  userEmail?: string;
  action: string;
  entityName: string;
  entityId: string;
  oldValue?: string;
  newValue?: string;
  ipAddress?: string;
  correlationId?: string;
  timestamp: string;
}

export interface Notification {
  id: number;
  userId: number;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

// API Functions
export const api = {
  auth: {
    login: (data: { email: string; password: string }) =>
      axiosClient.post<ApiResponse<LoginResponse>>('/auth/login', data),
    refresh: (refreshToken: string) =>
      axiosClient.post<ApiResponse<LoginResponse>>('/auth/refresh', { refreshToken }),
    logout: (refreshToken: string) =>
      axiosClient.post('/auth/logout', { refreshToken }),
    changePassword: (data: { currentPassword: string; newPassword: string }) =>
      axiosClient.post('/auth/change-password', data),
    me: () => axiosClient.get<ApiResponse<User>>('/auth/me'),
    getUsers: () => axiosClient.get<ApiResponse<User[]>>('/auth/users'),
    getRoles: () => axiosClient.get<ApiResponse<Role[]>>('/auth/roles'),
    getPermissions: () => axiosClient.get<ApiResponse<Permission[]>>('/auth/permissions'),
  },

  employees: {
    getPaged: (params?: { page?: number; pageSize?: number; search?: string; departmentId?: number }) =>
      axiosClient.get<ApiResponse<PagedResult<Employee>>>('/employees', { params }),
    getById: (id: number) =>
      axiosClient.get<ApiResponse<Employee>>(`/employees/${id}`),
    getMyProfile: () =>
      axiosClient.get<ApiResponse<Employee>>('/employees/me'),
    getTeam: () =>
      axiosClient.get<ApiResponse<Employee[]>>('/employees/team'),
    create: (data: Partial<Employee> & { initialPassword?: string; roleIds?: number[] }) =>
      axiosClient.post<ApiResponse<Employee>>('/employees', data),
    update: (id: number, data: Partial<Employee>) =>
      axiosClient.put<ApiResponse<Employee>>(`/employees/${id}`, data),
    delete: (id: number) =>
      axiosClient.delete(`/employees/${id}`),
  },

  departments: {
    getAll: () => axiosClient.get<ApiResponse<Department[]>>('/departments'),
    getById: (id: number) => axiosClient.get<ApiResponse<Department>>(`/departments/${id}`),
    create: (data: { name: string; location?: string; managerId?: number }) =>
      axiosClient.post<ApiResponse<Department>>('/departments', data),
    update: (id: number, data: { name: string; location?: string; managerId?: number }) =>
      axiosClient.put<ApiResponse<Department>>(`/departments/${id}`, data),
    delete: (id: number) => axiosClient.delete(`/departments/${id}`),
  },

  positions: {
    getAll: () => axiosClient.get<ApiResponse<Position[]>>('/positions'),
    getById: (id: number) => axiosClient.get<ApiResponse<Position>>(`/positions/${id}`),
    create: (data: { title: string; description?: string }) =>
      axiosClient.post<ApiResponse<Position>>('/positions', data),
    update: (id: number, data: { title: string; description?: string }) =>
      axiosClient.put<ApiResponse<Position>>(`/positions/${id}`, data),
    delete: (id: number) => axiosClient.delete(`/positions/${id}`),
  },

  attendance: {
    checkIn: (employeeId?: number) =>
      axiosClient.post<ApiResponse<Attendance>>('/attendance/check-in', { employeeId }),
    checkOut: (employeeId?: number) =>
      axiosClient.post<ApiResponse<Attendance>>('/attendance/check-out', { employeeId }),
    getToday: (employeeId?: number) =>
      axiosClient.get<ApiResponse<Attendance>>('/attendance/today', { params: { employeeId } }),
    getHistory: (params?: { employeeId?: number; from?: string; to?: string; departmentId?: number }) =>
      axiosClient.get<ApiResponse<Attendance[]>>('/attendance', { params }),
  },

  leaves: {
    apply: (data: { employeeId?: number; leaveTypeId: number; startDate: string; endDate: string; reason: string }) =>
      axiosClient.post<ApiResponse<Leave>>('/leaves', data),
    getAll: (params?: { employeeId?: number; status?: number; from?: string; to?: string; managerId?: number }) =>
      axiosClient.get<ApiResponse<Leave[]>>('/leaves', { params }),
    approve: (id: number, comments?: string) =>
      axiosClient.put<ApiResponse<Leave>>(`/leaves/${id}/approve`, { approved: true, comments }),
    reject: (id: number, comments?: string) =>
      axiosClient.put<ApiResponse<Leave>>(`/leaves/${id}/reject`, { approved: false, comments }),
    cancel: (id: number) =>
      axiosClient.post(`/leaves/${id}/cancel`),
    getTypes: () =>
      axiosClient.get<ApiResponse<LeaveType[]>>('/leaves/types'),
  },

  salary: {
    getStructure: (employeeId: number) =>
      axiosClient.get<ApiResponse<SalaryStructure>>(`/employees/${employeeId}/salary`),
    updateStructure: (employeeId: number, data: Partial<SalaryStructure>) =>
      axiosClient.put<ApiResponse<SalaryStructure>>(`/employees/${employeeId}/salary`, data),
    generatePayslip: (employeeId: number, month: number, year: number) =>
      axiosClient.post<ApiResponse<Payslip>>(`/employees/${employeeId}/payslips/generate?month=${month}&year=${year}`),
    getPayslips: (employeeId: number, year?: number) =>
      axiosClient.get<ApiResponse<Payslip[]>>(`/employees/${employeeId}/payslips`, { params: { year } }),
    getPayslipById: (id: number) =>
      axiosClient.get<ApiResponse<Payslip>>(`/payslips/${id}`),
  },

  projects: {
    getAll: (managerId?: number) =>
      axiosClient.get<ApiResponse<Project[]>>('/projects', { params: { managerId } }),
    getById: (id: number) =>
      axiosClient.get<ApiResponse<Project>>(`/projects/${id}`),
    create: (data: Partial<Project>) =>
      axiosClient.post<ApiResponse<Project>>('/projects', data),
    update: (id: number, data: Partial<Project>) =>
      axiosClient.put<ApiResponse<Project>>(`/projects/${id}`, data),
    assignEmployee: (projectId: number, data: { employeeId: number; allocatedFrom: string; allocatedTo?: string }) =>
      axiosClient.post(`/projects/${projectId}/employees`, data),
    removeEmployee: (projectId: number, employeeId: number) =>
      axiosClient.delete(`/projects/${projectId}/employees/${employeeId}`),
  },

  auditLogs: {
    getPaged: (params?: { page?: number; pageSize?: number; entityName?: string; userId?: number; from?: string; to?: string }) =>
      axiosClient.get<ApiResponse<PagedResult<AuditLog>>>('/audit-logs', { params }),
  },

  notifications: {
    getAll: (unreadOnly = false) =>
      axiosClient.get<ApiResponse<Notification[]>>('/notifications', { params: { unreadOnly } }),
    getUnreadCount: () =>
      axiosClient.get<ApiResponse<{ unreadCount: number }>>('/notifications/unread-count'),
    markAsRead: (id: number) =>
      axiosClient.put(`/notifications/${id}/read`),
  },

  reports: {
    headcountByDepartment: () =>
      axiosClient.get<ApiResponse<Array<{ departmentId: number; departmentName: string; activeEmployees: number; totalEmployees: number }>>>('/reports/employees-by-department'),
    attendanceSummary: (params?: { from?: string; to?: string; departmentId?: number }) =>
      axiosClient.get<ApiResponse<Array<{ date: string; totalPresent: number; totalAbsent: number; totalLate: number; totalHalfDay: number }>>>('/reports/attendance-summary', { params }),
    leaveUtilization: (year?: number) =>
      axiosClient.get<ApiResponse<Array<{ employeeId: number; employeeName: string; departmentName: string; totalLeavesTaken: number; pendingRequests: number }>>>('/reports/leave-utilization', { params: { year } }),
    payrollSummary: (month: number, year: number) =>
      axiosClient.get<ApiResponse<{ month: number; year: number; totalEmployeesPaid: number; totalGrossSalary: number; totalDeductions: number; totalNetSalary: number }>>('/reports/payroll-summary', { params: { month, year } }),
    projectAllocation: (projectId?: number) =>
      axiosClient.get<ApiResponse<Array<{ projectId: number; projectName: string; managerName: string; status: string; assignedMembersCount: number }>>>('/reports/project-allocation', { params: { projectId } }),
  },
};
