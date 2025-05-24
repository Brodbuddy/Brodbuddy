import { atomWithStorage } from 'jotai/utils';

export type AdminTab = 'analyzers' | 'features' | 'logging';

export const adminTabAtom = atomWithStorage<AdminTab>('admin-tab', 'analyzers');