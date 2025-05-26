import { atomWithStorage } from 'jotai/utils';

export type AdminTab = 'analyzers' | 'diagnostics' | 'features' | 'logging';

export const adminTabAtom = atomWithStorage<AdminTab>('admin-tab', 'analyzers');