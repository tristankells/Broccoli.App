window.imageDropZone = {
    /**
     * Wires drag-and-drop onto a drop zone element so that dropped files are
     * forwarded to Blazor's hidden <InputFile> element and trigger its OnChange.
     *
     * @param {string} dropZoneId  - id of the visible drop zone container
     * @param {string} inputId     - id of the <input type="file"> rendered by <InputFile>
     */
    init: function (dropZoneId, inputId) {
        const zone  = document.getElementById(dropZoneId);
        const input = document.getElementById(inputId);
        if (!zone || !input) return;

        // Prevent the browser from opening the file when it is dropped on the page
        // outside the zone.
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(function (event) {
            document.body.addEventListener(event, function (e) { e.preventDefault(); }, false);
        });

        zone.addEventListener('dragenter', function (e) {
            e.preventDefault();
            zone.classList.add('drag-over');
        });

        zone.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
            zone.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', function (e) {
            // Only remove highlight when the cursor leaves the zone entirely,
            // not when it moves over a child element.
            if (!zone.contains(e.relatedTarget)) {
                zone.classList.remove('drag-over');
            }
        });

        zone.addEventListener('drop', function (e) {
            e.preventDefault();
            zone.classList.remove('drag-over');

            const files = e.dataTransfer && e.dataTransfer.files;
            if (!files || files.length === 0) return;

            // Reject if an upload is already in progress (input will be disabled).
            if (input.disabled) return;

            // Copy just the first file into the input's FileList.
            const dt = new DataTransfer();
            dt.items.add(files[0]);
            input.files = dt.files;

            // Dispatch a change event so Blazor's InputFile.OnChange fires.
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
    }
};

