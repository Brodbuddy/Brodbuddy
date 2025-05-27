import { atomWithStorage } from 'jotai/utils';

export type AdminTab = 'analyzers' | 'diagnostics' | 'features' | 'logging' | 'firmware';

export const adminTabAtom = atomWithStorage<AdminTab>('admin-tab', 'analyzers');