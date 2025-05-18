import { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { Api } from '../api/Api';
import { tokenStorage, TOKEN_KEY, jwtAtom, userInfoAtom } from "../atoms/auth";
import { getDefaultStore } from "jotai";
import { AppRoutes } from '../helpers/appRoutes';
import config from '../config';

export const baseUrl = config.httpUrl; 
export const REDIRECT_PATH_KEY = 'redirectPath';

const store = getDefaultStore();

const handleAuthFailure = (error: any) => {
    store.set(jwtAtom, null);
    store.set(userInfoAtom, null);
    localStorage.setItem(REDIRECT_PATH_KEY, window.location.pathname);
    window.location.href = AppRoutes.login;
    return Promise.reject(error);
};

export const api = new Api({
    baseURL: baseUrl,
    headers: {
        "Prefer": "return=representation"
    },
    withCredentials: true
});

let isRefreshing = false;
let failedQueue: any[] = [];

const processQueue = (error: any = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve();
        }
    });

    failedQueue = [];
};

export const refreshToken = async (): Promise<string> => {
    if (isRefreshing) {
        return new Promise((resolve, reject) => {
            failedQueue.push({
                resolve: (): void => {
                    const token = tokenStorage.getItem(TOKEN_KEY, null);
                    if (token !== null) {
                        resolve(token);
                    } else {
                        reject(new Error("No token available"));
                    }
                },
                reject
            });
        });
    }

    isRefreshing = true;

    try {
        const response = await api.passwordlessAuth.refreshToken();
        const newToken = response.data.accessToken;

        store.set(jwtAtom, newToken);
        tokenStorage.setItem(TOKEN_KEY, newToken);

        processQueue();

        return newToken;
    } catch (refreshError) {
        processQueue(refreshError);
        return handleAuthFailure(refreshError);
    } finally {
        isRefreshing = false;
    }
};

api.instance.interceptors.request.use((config) => {
    if (config.url?.endsWith('api/passwordless-auth/user-info')) {
        return config;
    }

    const jwt = tokenStorage.getItem(TOKEN_KEY, null);
    if (jwt) {
        config.headers.Authorization = `Bearer ${jwt}`;
    }
    return config;
});

interface ExtendedAxiosRequestConfig extends InternalAxiosRequestConfig {
    _retry?: boolean;
}


api.instance.interceptors.response.use((response) => response, async (error: AxiosError) => {
    const originalRequest = error.config as ExtendedAxiosRequestConfig | undefined;

    if (!originalRequest || error.response?.status !== 401) {
        return Promise.reject(error);
    }

    if (originalRequest.url?.endsWith('api/passwordless-auth/refresh')) {
        return handleAuthFailure(error);
    }

    try {
        const newToken = await refreshToken();

        originalRequest.headers.Authorization = `Bearer ${newToken}`;

        return await api.instance(originalRequest);
    } catch (refreshError) {
        return handleAuthFailure(refreshError);
    }
});