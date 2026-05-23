/**
 * Microsite API — local frontend + LIVE backend (default).
 * Local backend test: add ?localapi=1 to any page URL.
 */
(function (global) {
    var API_LIVE = "http://microsite_backend.workarya.com";
    var API_LOCAL = "https://localhost:7161";

    function resolveApiBase() {
        try {
            var params = new URLSearchParams(global.location.search);
            if (params.get("localapi") === "1") {
                global.localStorage.setItem("micrositeApiMode", "local");
                return API_LOCAL;
            }
            if (params.get("localapi") === "0") {
                global.localStorage.removeItem("micrositeApiMode");
                return API_LIVE;
            }
            if (global.localStorage.getItem("micrositeApiMode") === "local") {
                return API_LOCAL;
            }
        } catch (e) { /* ignore */ }
        return API_LIVE;
    }

    var base = resolveApiBase().replace(/\/$/, "");

    global.MICROSITE_API_LIVE = API_LIVE;
    global.MICROSITE_API_LOCAL = API_LOCAL;
    global.MICROSITE_API_BASE = base;
    global.domain = base;
    global.API_BASE = base;
})(window);
