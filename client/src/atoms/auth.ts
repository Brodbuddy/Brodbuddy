import { atom } from 'jotai';
import { atomWithStorage, createJSONStorage } from 'jotai/utils';
import { UserInfoResponse } from '../api/Api';

export const TOKEN_KEY = "token";
export const tokenStorage = createJSONStorage<string | null>(
    () => localStorage
);

export const jwtAtom = atomWithStorage<string | null>(TOKEN_KEY, null, tokenStorage);

export const userInfoAtom = atom<UserInfoResponse | null>(null); 