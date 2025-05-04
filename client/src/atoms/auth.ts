import { atom } from 'jotai';
import { atomWithStorage } from 'jotai/utils';
import { api } from '../hooks/useHttp';
import { InitiateLoginRequest, LoginVerificationRequest } from '../api/Api';

export enum AccessLevel {
    Anonymous = "anonymous",
    User = "user",
    Admin = "admin"
}

export const ACCESS_TOKEN_KEY = 'accessToken';
export const accessTokenAtom = atomWithStorage<string | null>(ACCESS_TOKEN_KEY, null);

export const userInfoAtom = atom(
    async (get) => {
        const token = get(accessTokenAtom);
        if (!token) return null;

        try {
            const response = await api.passwordlessauth.userInfo();
            return response.data;
        } catch (error) {
            return null;
        }
    }
);

export const authLoadingAtom = atom(
    (get) => {
        try {
            get(userInfoAtom);
            return false;
        } catch (error) {
            return true;
        }
    }
);

export const canAccessAtom = atom(
    (get) => (requiredLevel: AccessLevel) => {
        const user = get(userInfoAtom);

        if (requiredLevel === AccessLevel.Anonymous) {
            return true; 
        }

        if (!user) return false;

        if (requiredLevel === AccessLevel.User) {
            return true;
        }
        
        return false;
    }
);

export const initiateLoginAtom = atom(
    null,
    async (_get, _set, email: string) => {
        try {
            const request: InitiateLoginRequest = { email };
            await api.passwordlessauth.initiateLogin(request);
            return true;
        } catch (error) {
            console.error('Login initiation failed:', error);
            return false;
        }
    }
);

export const verifyCodeAtom = atom(
    null,
    async (_get, set, { email, code }: { email: string; code: number }) => {
        try {
            const request: LoginVerificationRequest = { email, code };
            const response = await api.passwordlessauth.verifyCode(request);
            
            const token = response.data.accessToken;
            if (token) {
                set(accessTokenAtom, token);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Code verification failed:', error);
            return false;
        }
    }
);

export const refreshTokenAtom = atom(
    null,
    async (_get, set) => {
        try {
            const response = await api.passwordlessauth.refreshToken();

            const token = response.data.accessToken;
            if (token) {
                set(accessTokenAtom, token);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Token refresh failed:', error);
            return false;
        }
    }
);

export const logoutAtom = atom(
    null,
    async (_get, set) => {
        try {
            await api.passwordlessauth.logout();
        } catch (error) {
            console.error('Logout failed:', error);
        } finally {
            set(accessTokenAtom, null);
        }
    }
);