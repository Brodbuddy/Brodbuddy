import { atomWithStorage, createJSONStorage } from 'jotai/utils';
import { v4 as uuidv4 } from 'uuid';

interface SimpleStorage {
    getItem: (key: string) => string | null;
    setItem: (key: string, newValue: string) => void;
    removeItem: (key: string) => void;
}

const memoryStorage = new Map<string, string>();
const simpleSyncStorage: SimpleStorage = {
    getItem: (key: string) => memoryStorage.get(key) ?? null,
    setItem: (key: string, value: string) => { memoryStorage.set(key, value); }, 
    removeItem: (key: string) => { memoryStorage.delete(key); },                 
}

const getLocalStorage = (): SimpleStorage => {
    try {
        const testKey = 'jotai_storage_test';
        window.localStorage.setItem(testKey, testKey);
        window.localStorage.removeItem(testKey);
        return window.localStorage;
    } catch {
        console.warn("localStorage is not available, using in-memory fallback for Jotai atoms.");
        return simpleSyncStorage;
    }
};

const localStorageJotai = createJSONStorage<string>(() => getLocalStorage());

const getOrCreateClientId = (): string => {
    const storage = getLocalStorage();
    const existingId = storage.getItem('websocketDeviceId');
    
    if (existingId) {
        try {
            const parsed = JSON.parse(existingId);
            if (typeof parsed === 'string' && parsed.length > 0) {
                return parsed;
            }
        } catch {
            // Ugyldig JSON, laver nyt ID 
        }
    }
    
    const newId = uuidv4();
    storage.setItem('websocketDeviceId', JSON.stringify(newId));
    return newId;
};

export const clientIdAtom = atomWithStorage<string>(
    'websocketDeviceId',
    getOrCreateClientId(),    
    localStorageJotai,      
    { getOnInit: true }         
);