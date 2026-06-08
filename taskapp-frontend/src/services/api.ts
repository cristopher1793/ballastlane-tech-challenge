import axios from 'axios';
import type { AxiosInstance, AxiosResponse } from 'axios';
import type {
  LoginRequestDto,
  LoginResponseDto,
  RegisterRequestDto,
  UpdateProfileDto,
  UserResponseDto,
  TaskResponseDto,
  CreateTaskDto,
  UpdateTaskDto,
  DashboardStatsDto,
} from '../types';

let authToken: string | null = null;
let sessionExpiredDispatched = false;

export function setAuthToken(token: string | null): void {
  authToken = token;
}

export function getAuthToken(): string | null {
  return authToken;
}

const api: AxiosInstance = axios.create({
  baseURL: 'https://localhost:7020',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  if (authToken) {
    config.headers.Authorization = `Bearer ${authToken}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response) {
      const url = error.config?.url ?? '';

      if (error.response.status === 429) {
        window.dispatchEvent(
          new CustomEvent('app:rate-limited', { detail: 'Too many requests — please slow down' })
        );
      }

      if (error.response.status === 403 && url.includes('/login')) {
        window.dispatchEvent(
          new CustomEvent('app:account-locked', {
            detail: 'Your account is locked. Please contact an administrator.',
          })
        );
      }

      // 401 on any authenticated endpoint — session expired or token invalid
      const isPublicEndpoint = url.endsWith('/login') || url.endsWith('/register');
      if (error.response.status === 401 && !isPublicEndpoint && !sessionExpiredDispatched) {
        sessionExpiredDispatched = true;
        window.dispatchEvent(new CustomEvent('app:session-expired'));
        setTimeout(() => { sessionExpiredDispatched = false; }, 5000);
      }
    }
    return Promise.reject(error);
  }
);

export const authService = {
  register: async (dto: RegisterRequestDto): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.post('/api/auth/register', dto);
    return response.data;
  },

  login: async (dto: LoginRequestDto): Promise<LoginResponseDto> => {
    const response: AxiosResponse<LoginResponseDto> = await api.post('/api/auth/login', dto);
    return response.data;
  },

  me: async (): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.get('/api/auth/me');
    return response.data;
  },

  unlock: async (userId: string): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.post(`/api/auth/unlock/${userId}`);
    return response.data;
  },

  updateProfile: async (dto: UpdateProfileDto): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.put('/api/auth/profile', dto);
    return response.data;
  },

  getAllUsers: async (): Promise<UserResponseDto[]> => {
    const response: AxiosResponse<UserResponseDto[]> = await api.get('/api/auth/users');
    return response.data;
  },

  lockUser: async (userId: string): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.post(`/api/auth/lock/${userId}`);
    return response.data;
  },

  unlockUser: async (userId: string): Promise<UserResponseDto> => {
    const response: AxiosResponse<UserResponseDto> = await api.post(`/api/auth/unlock/${userId}`);
    return response.data;
  },
};

export const taskService = {
  getAll: async (): Promise<TaskResponseDto[]> => {
    const response: AxiosResponse<TaskResponseDto[]> = await api.get('/api/tasks');
    return response.data;
  },

  getLabels: async (): Promise<string[]> => {
    const response: AxiosResponse<string[]> = await api.get('/api/tasks/labels');
    return response.data;
  },

  getById: async (id: string): Promise<TaskResponseDto> => {
    const response: AxiosResponse<TaskResponseDto> = await api.get(`/api/tasks/${id}`);
    return response.data;
  },

  create: async (dto: CreateTaskDto): Promise<TaskResponseDto> => {
    const response: AxiosResponse<TaskResponseDto> = await api.post('/api/tasks', dto);
    return response.data;
  },

  update: async (id: string, dto: UpdateTaskDto): Promise<TaskResponseDto> => {
    const response: AxiosResponse<TaskResponseDto> = await api.put(`/api/tasks/${id}`, dto);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/tasks/${id}`);
  },

  getDashboardStats: async (): Promise<DashboardStatsDto> => {
    const response: AxiosResponse<DashboardStatsDto> = await api.get('/api/tasks/dashboard');
    return response.data;
  },
};

export const seedService = {
  seedMe: async (): Promise<{ message: string; tasksDeleted: number; tasksCreated: number }> => {
    const response = await api.post('/api/seed/me');
    return response.data;
  },
};
