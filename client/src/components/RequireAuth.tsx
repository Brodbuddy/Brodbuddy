import { ReactElement } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AccessLevel } from '../atoms/auth';
import { AppRoutes } from '../helpers/appRoutes';

type RequireAuthProps = {
    element: ReactElement;
    accessLevel: AccessLevel;
};

export function RequireAuth({ element, accessLevel }: RequireAuthProps) {
    const { user, isLoading, canAccess } = useAuth();

    if (isLoading) {
        return <div>Loading...</div>;
    }

    if (accessLevel !== AccessLevel.Anonymous && !user) {
        sessionStorage.setItem('redirectPath', window.location.pathname);
        return <Navigate to={AppRoutes.login} replace />;
    }

    if (!canAccess(accessLevel)) {
        return <Navigate to={AppRoutes.home} replace />;
    }

    return element;
}