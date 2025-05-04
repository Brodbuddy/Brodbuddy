import { getDefaultStore} from 'jotai';
import { Api } from '../api/Api';
import { accessTokenAtom } from '../atoms/auth';

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
    const token = store.get(accessTokenAtom);
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
})