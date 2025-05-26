import { atomWithStorage } from 'jotai/utils';

export type AdminTab = 'analyzers' | 'features' | 'logging' | 'firmware';

export const adminTabAtom = atomWithStorage<AdminTab>('admin-tab', 'analyzers');