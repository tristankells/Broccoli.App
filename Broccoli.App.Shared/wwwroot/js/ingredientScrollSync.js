window.ingredientScrollSync = {
    /**
     * Binds the vertical scroll position of the ingredient textarea to the
     * right-panel div so both panels scroll in sync.
     * @param {string} textareaId - The id of the ingredient textarea element.
     * @param {string} panelId    - The id of the right-panel container div.
     */
    init(textareaId, panelId) {
        const ta = document.getElementById(textareaId);
        const panel = document.getElementById(panelId);
        if (!ta || !panel) return;

        ta.addEventListener('scroll', () => {
            panel.scrollTop = ta.scrollTop;
        });
    }
};
