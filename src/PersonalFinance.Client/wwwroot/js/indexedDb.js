const DB_NAME = 'personal-finance';
const DB_VERSION = 2;
const STORES = ['accounts', 'categories', 'transactions', 'outbox'];

let dbPromise = null;

function openDb() {
    if (dbPromise) {
        return dbPromise;
    }

    dbPromise = new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = () => {
            const db = request.result;
            for (const store of STORES) {
                if (!db.objectStoreNames.contains(store)) {
                    db.createObjectStore(store, { keyPath: 'id' });
                }
            }
        };

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });

    return dbPromise;
}

function withStore(storeName, mode, callback) {
    return openDb().then(db => new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, mode);
        const store = tx.objectStore(storeName);
        const request = callback(store);

        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    }));
}

export function getAll(storeName) {
    return withStore(storeName, 'readonly', store => store.getAll());
}

export function get(storeName, id) {
    return withStore(storeName, 'readonly', store => store.get(id))
        .then(result => result === undefined ? null : result);
}

export function put(storeName, item) {
    return withStore(storeName, 'readwrite', store => store.put(item));
}

export function remove(storeName, id) {
    return withStore(storeName, 'readwrite', store => store.delete(id));
}

export function clear(storeName) {
    return withStore(storeName, 'readwrite', store => store.clear());
}
