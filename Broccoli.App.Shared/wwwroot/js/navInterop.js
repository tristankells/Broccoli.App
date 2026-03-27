window.navInterop = {
    init: function () {
        const checkbox = document.getElementById('nav-toggle');
        const navScrollable = document.getElementById('nav-scrollable');

        if (!checkbox || !navScrollable) return;

        // Lock body scroll when nav drawer is open on mobile
        checkbox.addEventListener('change', function () {
            document.body.style.overflow = this.checked ? 'hidden' : '';
        });

        // Swipe left on the drawer to dismiss
        let startX = 0;
        let startY = 0;

        navScrollable.addEventListener('touchstart', function (e) {
            startX = e.changedTouches[0].clientX;
            startY = e.changedTouches[0].clientY;
        }, { passive: true });

        navScrollable.addEventListener('touchend', function (e) {
            const dx = e.changedTouches[0].clientX - startX;
            const dy = Math.abs(e.changedTouches[0].clientY - startY);
            // Left swipe: horizontal movement > 50px, mostly horizontal
            if (dx < -50 && dy < 80) {
                window.navInterop.closeNav();
            }
        }, { passive: true });
    },

    closeNav: function () {
        const checkbox = document.getElementById('nav-toggle');
        if (checkbox && checkbox.checked) {
            checkbox.checked = false;
            document.body.style.overflow = '';
        }
    }
};

