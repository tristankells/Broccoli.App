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
    },

    /**
     * Restores focus and caret position to the textarea after a Blazor re-render.
     * Call this after every StateHasChanged that runs while the textarea may be active.
     * @param {string} textareaId - The id of the ingredient textarea element.
     */
    restoreFocus(textareaId) {
        const ta = document.getElementById(textareaId);
        if (!ta) return;
        const sel = { start: ta.selectionStart, end: ta.selectionEnd };
        ta.focus({ preventScroll: true });
        ta.setSelectionRange(sel.start, sel.end);
    }
};
