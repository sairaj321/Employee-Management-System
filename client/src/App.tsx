import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/SecurityGates';
import { AppShell, AuthLayout } from './layouts/AppLayouts';

// Pages
import { Login, Unauthorized, NotFound } from './pages/AuthPages';
import { Dashboard } from './pages/Dashboard';
import { Employees } from './pages/Employees';
import { EmployeeDetails } from './pages/EmployeeDetails';
import { Departments, Positions } from './pages/OrganizationPages';
import { AttendancePage, LeavesPage } from './pages/TimeAndLeavePages';
import { SalaryPage, PayslipsPage } from './pages/SalaryPages';
import { ProjectsPage } from './pages/Projects';
import { AuditLogsPage, ReportsPage } from './pages/AuditAndReportPages';
import { UsersPage, RolesPage, PermissionsPage, NotificationsPage, ProfilePage } from './pages/AdminAndProfilePages';

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public Login Route */}
          <Route
            path="/login"
            element={
              <AuthLayout>
                <Login />
              </AuthLayout>
            }
          />

          {/* Protected Application Routes */}
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <AppShell />
              </ProtectedRoute>
            }
          >
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="employees" element={<Employees />} />
            <Route path="employees/:id" element={<EmployeeDetails />} />
            <Route path="departments" element={<Departments />} />
            <Route path="positions" element={<Positions />} />
            <Route path="attendance" element={<AttendancePage />} />
            <Route path="leaves" element={<LeavesPage />} />
            <Route path="salary" element={<SalaryPage />} />
            <Route path="payslips" element={<PayslipsPage />} />
            <Route path="projects" element={<ProjectsPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="roles" element={<RolesPage />} />
            <Route path="permissions" element={<PermissionsPage />} />
            <Route path="audit-logs" element={<AuditLogsPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="notifications" element={<NotificationsPage />} />
            <Route path="profile" element={<ProfilePage />} />
            <Route path="unauthorized" element={<Unauthorized />} />
          </Route>

          {/* Catch-all Not Found Route */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
