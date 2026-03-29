window.wakeLock = {
    _sentinel: null,
    async acquire() {
        if (!('wakeLock' in navigator)) return;
        try {
            this._sentinel = await navigator.wakeLock.request('screen');
            // Re-acquire if the browser releases it when the tab becomes visible again
            document.addEventListener('visibilitychange', async () => {
                if (document.visibilityState === 'visible' && this._sentinel?.released) {
                    try {
                        this._sentinel = await navigator.wakeLock.request('screen');
                    } catch { /* ignore */ }
                }
            }, { once: true });
        } catch { /* permission denied or not supported — silently ignore */ }
    },
    async release() {
        try {
            await this._sentinel?.release();
        } catch { /* ignore */ }
        this._sentinel = null;
    }
};

