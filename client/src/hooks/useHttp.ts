import { Api } from '../api/Api';

export const baseUrl = import.meta.env.VITE_APP_BASE_API_URL

export const api = new Api({
    baseURL: baseUrl,
    headers: {
        "Prefer": "return=representation"
    },
    withCredentials: true
});