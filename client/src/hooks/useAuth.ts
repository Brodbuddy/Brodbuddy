import { useAtom } from 'jotai';
import { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api, REDIRECT_PATH_KEY } from "../hooks/useHttp";
import { jwtAtom, tokenStorage, TOKEN_KEY, userInfoAtom } from '../atoms/auth';
import { AppRoutes } from "../helpers/appRoutes.ts";
import { UserInfoResponse, InitiateLoginRequest } from '../api/Api';

type AuthHook = {
    user: UserInfoResponse | null;
    initiateLogin: (initiateLoginRequest: InitiateLoginRequest) => Promise<boolean>;
    completeLogin: (code: number) => Promise<false | undefined>;
    logout: () => void;
    isLoading: boolean;
};

export const useAuth = (): AuthHook => {
    const [_, setJwt] = useAtom(jwtAtom);
    const [user, setUser] = useAtom(userInfoAtom);
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        const checkAuth = async () => {
            try {
                const token = tokenStorage.getItem(TOKEN_KEY, null);
                if (token) {
                    const userResponse = await api.passwordlessAuth.userInfo({
                        headers: { Authorization: `Bearer ${token}` }
                    });
                    setUser(userResponse.data);
                }
            } catch (error) {
                if (error instanceof AxiosError) {
                    if (error.response?.status !== 401) {
                        setJwt(null);
                        setUser(null);
                    }
                } else {
                    setJwt(null);
                    setUser(null);
                }
            } finally {
                setIsLoading(false);
            }
        };

        checkAuth();
    }, []);
    
    const initiateLogin = async (initiateLoginRequest: InitiateLoginRequest) => {
        try {
            await api.passwordlessAuth.initiateLogin(initiateLoginRequest);
            sessionStorage.setItem('loginEmail', initiateLoginRequest.email);
            return true;
        } catch (error) {
            console.error('Login initiation failed:', error);
            return false;
        }
    }
    
    const completeLogin = async (code: number) => {
        try {
            const email = sessionStorage.getItem('loginEmail');
            if (!email) return false;
            
            const response = await api.passwordlessAuth.verifyCode({ email, code });
            const token = response.data.accessToken;

            sessionStorage.removeItem('loginEmail');
            tokenStorage.setItem(TOKEN_KEY, token);
            setJwt(token);

            const userResponse = await api.passwordlessAuth.userInfo({
                headers: { Authorization: `Bearer ${token}` }
            });
            setUser(userResponse.data);
            
            const redirectTo = localStorage.getItem(REDIRECT_PATH_KEY) || AppRoutes.home;
            localStorage.removeItem(REDIRECT_PATH_KEY);
            navigate(redirectTo);
        } catch (error) {
            tokenStorage.removeItem(TOKEN_KEY);
            setJwt(null);
            setUser(null);

            throw error; 
        }
    } 

    const logout = async () => {
        await api.passwordlessAuth.logout();
        setJwt(null);
        setUser(null);
        navigate(AppRoutes.login);
    };

    return { user, initiateLogin, completeLogin, logout, isLoading };
};