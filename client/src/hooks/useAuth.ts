import { useAtomValue, useSetAtom } from 'jotai';
import { useNavigate } from 'react-router-dom';
import {
    userInfoAtom,
    authLoadingAtom,
    initiateLoginAtom,
    verifyCodeAtom,
    refreshTokenAtom,
    logoutAtom,
    canAccessAtom,
} from '../atoms/auth.ts';
import { AppRoutes } from '../helpers/appRoutes.ts';

export function useAuth() {
    const user = useAtomValue(userInfoAtom);
    const isLoading = useAtomValue(authLoadingAtom);
    const initiateLogin = useSetAtom(initiateLoginAtom);
    const verifyCode = useSetAtom(verifyCodeAtom);
    const refreshToken = useSetAtom(refreshTokenAtom);
    const logout = useSetAtom(logoutAtom);
    const canAccess = useAtomValue(canAccessAtom);
    
    const navigate = useNavigate();
    
    const startLogin = async (email: string) => {
        return await initiateLogin(email);
    };

    const completeLogin = async (email: string, code: number) => {
        const success = await verifyCode({ email, code });

        if (success) {
            const redirectPath = sessionStorage.getItem('redirectPath') || AppRoutes.home;
            sessionStorage.removeItem('redirectPath');
            navigate(redirectPath);
        }

        return success;
    };

    const handleLogout = async () => {
        await logout();
        navigate(AppRoutes.login);
    };

    return {
        user,
        isLoading,
        isAuthenticated: !!user,
        startLogin,
        completeLogin,
        refreshToken,
        logout: handleLogout,
        canAccess
    };
}