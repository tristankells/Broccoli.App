window.foodFile = {
    exportFoods: function (filename, content) {
        const blob = new Blob([content], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    importFoods: function () {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = '.json,application/json';
            input.style.display = 'none';
            document.body.appendChild(input);

            input.onchange = function () {
                const file = input.files && input.files[0];
                document.body.removeChild(input);
                if (!file) { resolve(null); return; }
                const reader = new FileReader();
                reader.onload = function (e) { resolve(e.target.result); };
                reader.onerror = function () { resolve(null); };
                reader.readAsText(file);
            };

            // If the user closes the dialog without picking a file
            window.addEventListener('focus', function onFocus() {
                window.removeEventListener('focus', onFocus);
                setTimeout(function () {
                    if (!input.files || input.files.length === 0) {
                        if (document.body.contains(input)) document.body.removeChild(input);
                        resolve(null);
                    }
                }, 500);
            }, { once: true });

            input.click();
        });
    }
};
