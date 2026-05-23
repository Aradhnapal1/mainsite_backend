/**
 * Microsite API base URL.
 * - HTML served from backend (localhost:7161 or workarya.com) → same-origin API
 * - HTML on XAMPP → live backend API
 * - ?localapi=1 → local dotnet API (https://localhost:7161)
 */
(function (global) {
    var API_LIVE = "http://microsite_backend.workarya.com";
    var API_LOCAL = "https://localhost:7161";

    function isBackendStaticHost() {
        var host = (global.location.hostname || "").toLowerCase();
        var port = global.location.port || "";
        if (host === "microsite_backend.workarya.com") return true;
        if ((host === "localhost" || host === "127.0.0.1") && port === "7161") return true;
        return false;
    }

    function resolveApiBase() {
        try {
            var params = new URLSearchParams(global.location.search);
            if (params.get("localapi") === "1") {
                global.localStorage.setItem("micrositeApiMode", "local");
                return API_LOCAL;
            }
            if (params.get("localapi") === "0") {
                global.localStorage.removeItem("micrositeApiMode");
            } else if (global.localStorage.getItem("micrositeApiMode") === "local") {
                return API_LOCAL;
            }
        } catch (e) { /* ignore */ }

        if (isBackendStaticHost()) {
            return String(global.location.origin).replace(/\/$/, "");
        }

        return API_LIVE;
    }

    var base = resolveApiBase().replace(/\/$/, "");

    global.MICROSITE_API_LIVE = API_LIVE;
    global.MICROSITE_API_LOCAL = API_LOCAL;
    global.MICROSITE_API_BASE = base;
    global.domain = base;
    global.API_BASE = base;
})(window);
