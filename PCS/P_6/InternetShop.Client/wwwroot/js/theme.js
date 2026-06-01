window.theme = {
    apply: function (mode) {
        document.documentElement.setAttribute('data-theme', mode);
    },
    store: function (mode) {
        try { localStorage.setItem('shop-theme', mode); } catch (e) { }
    },
    getStored: function () {
        try { return localStorage.getItem('shop-theme'); } catch (e) { return null; }
    }
};
