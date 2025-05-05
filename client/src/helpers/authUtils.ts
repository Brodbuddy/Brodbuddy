import { UserInfoResponse } from '../api/Api';

export enum AccessLevel {
    Anonymous = 'anonymous',
    Protected = 'protected',
    User = 'user',
    Admin = 'admin'
}

export const canAccess = (accessLevel: AccessLevel, user: UserInfoResponse | null): boolean => {
    switch (accessLevel) {
        case AccessLevel.Anonymous:
            return true;
        case AccessLevel.Protected:
            return user !== null;
        case AccessLevel.Admin:
            return user !== null && user.isAdmin;
        case AccessLevel.User:
            return user !== null && !user.isAdmin;
        default:
            return false;
    }
};