import { createContext, useContext } from 'react';
import { UserInfoResponse, InitiateLoginRequest } from './api/Api';

type AuthContextType = {
    user: UserInfoResponse | null;
    initiateLogin: (initiateLoginRequest: InitiateLoginRequest) => Promise<boolean>;
    completeLogin: (code: number) => Promise<false | undefined>;
    logout: () => void;
    isLoading: boolean;
};

export const AuthContext = createContext<AuthContextType | null>(null);

export function useAuthContext() {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuthContext must be used within an AuthProvider');
    }
    return context;
}