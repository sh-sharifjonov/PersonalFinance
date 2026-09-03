export function isOnline() {
    return navigator.onLine;
}

export function registerConnectivityCallback(dotNetRef) {
    window.addEventListener('online', () => dotNetRef.invokeMethodAsync('OnOnline'));
    window.addEventListener('offline', () => dotNetRef.invokeMethodAsync('OnOffline'));
}
