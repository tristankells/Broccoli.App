window.setAppTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
};

window.getOsPrefersDark = function () {
    return window.matchMedia &&
           window.matchMedia('(prefers-color-scheme: dark)').matches;
};

