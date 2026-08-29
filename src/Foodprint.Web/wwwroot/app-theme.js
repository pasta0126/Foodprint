// Native <dialog> control for interactive components.
window.fpDialog = {
    open: function (el) { if (el && !el.open) el.showModal(); },
    close: function (el) { if (el && el.open) el.close(); }
};

// Runtime theme helpers used by the interactive ThemeToggle component.
window.fpTheme = {
    get: function () {
        try { return localStorage.getItem("fp-theme") || "system"; }
        catch (e) { return "system"; }
    },
    set: function (value) {
        try {
            if (value === "light" || value === "dark") {
                localStorage.setItem("fp-theme", value);
                document.documentElement.setAttribute("data-theme", value);
            } else {
                localStorage.removeItem("fp-theme");
                document.documentElement.removeAttribute("data-theme");
            }
        } catch (e) { /* storage blocked */ }
    }
};
