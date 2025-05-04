import { getDefaultStore} from 'jotai';
import { Api } from '../api/Api';
import { accessTokenAtom } from '../atoms/auth';
import {AxiosError, AxiosRequestConfig, InternalAxiosRequestConfig} from "axios";

export const baseUrl = import.meta.env.VITE_APP_BASE_API_URL
const store = getDefaultStore();

export const api = new Api({
    baseURL: baseUrl,
    headers: {
        "Prefer": "return=representation"
    },
    withCredentials: true
});

api.instance.interceptors.request.use((config) => {
    console.log("interceptors")
    const token = store.get(accessTokenAtom);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    console.log("token" + token)
    return config;
})

interface ExtendedAxiosRequestConfig extends InternalAxiosRequestConfig {
    _retry?: boolean;
}

let isRefreshing = false;

let refreshQueue: Array<{
    resolve: (value?: unknown) => void;
    reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: unknown = null) => {
    refreshQueue.forEach(({resolve, reject}) => {
        if (error) {
            reject(error);
        } else {
            resolve();
        }
    })
    refreshQueue = [];
}

api.instance.interceptors.response.use((response) => response, async (error) => {
    console.log("et eller amndet");
    const originalRequest = error.config as ExtendedAxiosRequestConfig;
    if (!originalRequest || error.response?.status !== 401) {
        return Promise.reject(error);
    }

    if (originalRequest._retry) {
        store.set(accessTokenAtom, null);
        return Promise.reject(error);
    }

    if (isRefreshing) {
        return new Promise((resolve, reject) => {
            refreshQueue.push({resolve, reject})
        }).then(() =>  {
            originalRequest._retry = true;
            return api.instance(originalRequest);
        }).catch((error) => Promise.reject(error));
    }

    isRefreshing = true;
    originalRequest._retry = true;

    try {
        const response = await api.passwordlessauth.refreshToken();
        const newToken = response.data.accessToken;
        store.set(accessTokenAtom, newToken);
        processQueue();
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        console.log("newtoken" + newToken);
        return api.instance(originalRequest);
    } catch (refreshError) {
        processQueue(refreshError);
        store.set(accessTokenAtom, null);
        console.log(refreshError + "hejdjjasdjsadasj");
        return Promise.reject(error);
    } finally {
        isRefreshing = false;
    }
})