/**
 * Microsite context + nav links (.html) with microsite_id / domain / slug in query.
 */
(function (global) {
    function readContextFromUrl() {
        var params = new URLSearchParams(global.location.search);
        return {
            micrositeId: params.get("microsite_id") || "",
            slug: params.get("slug") || "",
            domain: params.get("domain") || "",
            currentPage: (document.body && document.body.getAttribute("data-ms-page")) || ""
        };
    }

    global.msBuildQuery = function (extra) {
        var c = global.MICROSITE_CONTEXT || {};
        var params = new URLSearchParams();
        if (c.micrositeId) params.set("microsite_id", c.micrositeId);
        if (c.slug) params.set("slug", c.slug);
        if (c.domain) params.set("domain", c.domain);
        if (extra && typeof extra === "object") {
            Object.keys(extra).forEach(function (k) {
                if (extra[k] != null && extra[k] !== "") params.set(k, extra[k]);
            });
        }
        var q = params.toString();
        return q ? "?" + q : "";
    };

    global.msPageUrl = function (page, extra) {
        var name = (page || "index").replace(/\.(php|html)$/i, "");
        return name + ".html" + global.msBuildQuery(extra);
    };

    function initContext() {
        var fromUrl = readContextFromUrl();
        var existing = global.MICROSITE_CONTEXT || {};
        global.MICROSITE_CONTEXT = {
            micrositeId: fromUrl.micrositeId || existing.micrositeId || "",
            slug: fromUrl.slug || existing.slug || "",
            domain: fromUrl.domain || existing.domain || "",
            currentPage: fromUrl.currentPage || existing.currentPage || ""
        };
    }

    function applyNavLinks() {
        document.querySelectorAll("[data-ms-nav]").forEach(function (el) {
            var page = el.getAttribute("data-ms-nav");
            if (page) el.setAttribute("href", global.msPageUrl(page));
        });
        document.querySelectorAll("[data-ms-href]").forEach(function (el) {
            var page = el.getAttribute("data-ms-href");
            if (page) el.setAttribute("href", global.msPageUrl(page));
        });
    }

    initContext();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", applyNavLinks);
    } else {
        applyNavLinks();
    }
})(window);
