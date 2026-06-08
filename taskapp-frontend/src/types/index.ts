export type UserRole = 'User' | 'Admin';

export type TaskStatus = 'ToDo' | 'Pending' | 'InProgress' | 'Completed';

export interface UserResponseDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  username: string;
  email: string;
  role: UserRole;
  createdAt: string;
  isLocked: boolean;
  failedLoginAttempts: number;
}

export interface LoginResponseDto {
  token: string;
  user: UserResponseDto;
}

export interface RegisterRequestDto {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  password: string;
}

export interface UpdateProfileDto {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  currentPassword?: string;
  newPassword?: string;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface TaskResponseDto {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string;
  labels: string[];
  storyPoints: number | null;
  userId: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
  completedAt: string | null;
  ownerUsername: string | null;
}

export interface CompletionTimingEntry {
  title: string;
  dueDate: string;
  completedAt: string;
  daysVariance: number;
}

export interface WeeklyVelocityEntry {
  week: string;
  points: number;
  tasks: number;
}

export interface EstimationAccuracyEntry {
  storyPoints: number;
  avgDays: number;
  count: number;
}

export interface DashboardStatsDto {
  totalTasks: number;
  toDo: number;
  pending: number;
  inProgress: number;
  completed: number;
  onTimeRate: number;
  averageDaysVariance: number;
  timings: CompletionTimingEntry[];
  weeklyVelocity: WeeklyVelocityEntry[];
  estimationAccuracy: EstimationAccuracyEntry[];
}

export interface CreateTaskDto {
  title: string;
  description: string;
  dueDate: string;
  labels: string[];
  storyPoints: number | null;
}

export interface UpdateTaskDto {
  title: string;
  description: string;
  status: TaskStatus;
  dueDate: string;
  labels: string[];
  storyPoints: number | null;
}
