window.pfApp = {
    setTheme(theme) {
        if (theme === 'dark') {
            document.documentElement.dataset.theme = 'dark';
        } else {
            delete document.documentElement.dataset.theme;
        }
    },

    downloadText(fileName, content, mimeType, addBom) {
        // Excel only opens UTF-8 CSV (Cyrillic category names) correctly with a BOM.
        const blob = new Blob([(addBom ? '﻿' : '') + content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    },

    async storageUsageBytes() {
        if (!navigator.storage || !navigator.storage.estimate) {
            return -1;
        }
        const estimate = await navigator.storage.estimate();
        return estimate.usage ?? -1;
    },

    isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    }
};
