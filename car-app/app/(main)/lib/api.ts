// lib/api.ts

import axios, { InternalAxiosRequestConfig } from "axios";
import { useLoadingStore } from "../stores/loading-store";
import { useAuthStore } from '../stores/auth-store';
import { refreshAccessToken } from "./authService";

declare module "axios" {
  export interface AxiosRequestConfig {
    showLoading?: boolean;
  }
}

interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
});

api.interceptors.request.use(async (config) => {
  if (config.showLoading !== false) {
    // useLoadingStore.getState().start();
  }
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => {
    if (response.config.showLoading !== false) {
      // useLoadingStore.getState().stop();
    }

    return response;
  },
  async (error) => {
    if (error.config?.showLoading !== false) {
      // useLoadingStore.getState().stop();
    }
    const originalRequest = error.config as CustomAxiosRequestConfig;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const newAccessToken = await refreshAccessToken();
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);


export default api;
