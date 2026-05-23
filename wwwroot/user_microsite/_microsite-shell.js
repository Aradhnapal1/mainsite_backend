/**
 * Microsite context + nav links (.html) with microsite_id / domain / slug in query.
 */
(function (global) {
    var STORAGE_ID = "ms_microsite_id";
    var STORAGE_SLUG = "ms_microsite_slug";
    var STORAGE_DOMAIN = "ms_microsite_domain";

    function readStored(key) {
        try {
            return global.sessionStorage.getItem(key) || "";
        } catch (e) {
            return "";
        }
    }

    function writeStored(key, value) {
        if (!value) return;
        try {
            global.sessionStorage.setItem(key, value);
        } catch (e) { /* ignore */ }
    }

    function readContextFromUrl() {
        var params = new URLSearchParams(global.location.search);
        var micrositeId = params.get("microsite_id") || "";
        var slug = params.get("slug") || "";
        var domain = params.get("domain") || "";

        if (micrositeId) writeStored(STORAGE_ID, micrositeId);
        else micrositeId = readStored(STORAGE_ID);

        if (slug) writeStored(STORAGE_SLUG, slug);
        else slug = readStored(STORAGE_SLUG);

        if (domain) writeStored(STORAGE_DOMAIN, domain);
        else domain = readStored(STORAGE_DOMAIN);

        return {
            micrositeId: micrositeId,
            slug: slug,
            domain: domain,
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

    function syncUrlWithContext() {
        var ctx = global.MICROSITE_CONTEXT || {};
        if (!ctx.micrositeId) return;
        try {
            var url = new URL(global.location.href);
            if (url.searchParams.get("microsite_id") === ctx.micrositeId) return;
            url.searchParams.set("microsite_id", ctx.micrositeId);
            global.history.replaceState(null, "", url.toString());
        } catch (e) { /* ignore */ }
    }

    function ensureMicrositeIdInUrl() {
        var ctx = global.MICROSITE_CONTEXT || {};
        if (ctx.micrositeId || ctx.slug || ctx.domain) {
            syncUrlWithContext();
            return;
        }

        var page = (document.body && document.body.getAttribute("data-ms-page")) || "";
        if (page !== "home") return;

        var defaultId = "f29027c6192d4838823efcc96f837255";
        try {
            var url = new URL(global.location.href);
            url.searchParams.set("microsite_id", defaultId);
            global.location.replace(url.toString());
        } catch (e) { /* ignore */ }
    }

    initContext();
    ensureMicrositeIdInUrl();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", applyNavLinks);
    } else {
        applyNavLinks();
    }
})(window);
