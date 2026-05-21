(function () {
    var API_BASE = window.MICROSITE_API_BASE || "http://microsite_backend.workarya.com";

    function safeText(value, fallback) {
        if (value === null || value === undefined || value === "") {
            return fallback || "-";
        }
        return String(value);
    }

    function setText(id, value, fallback) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = safeText(value, fallback);
        }
    }

    function setImage(id, value, fallback) {
        var el = document.getElementById(id);
        if (el && el.tagName === "IMG") {
            el.src = value || fallback || el.src;
        }
    }

    function applyTheme(theme) {
        if (!theme) return;
        var root = document.documentElement;
        if (theme.headerColor) root.style.setProperty("--ms-header-color", theme.headerColor);
        if (theme.textColor) root.style.setProperty("--ms-text-color", theme.textColor);
        if (theme.backgroundColor) root.style.setProperty("--ms-bg-color", theme.backgroundColor);
        if (theme.buttonColor) root.style.setProperty("--ms-button-color", theme.buttonColor);
        if (theme.buttonTextColor) root.style.setProperty("--ms-button-text-color", theme.buttonTextColor);
        if (theme.footerColor) root.style.setProperty("--ms-footer-color", theme.footerColor);
        if (theme.footerTextColor) root.style.setProperty("--ms-footer-text-color", theme.footerTextColor);
        if (theme.fontFamily) root.style.setProperty("--ms-font-family", theme.fontFamily);
    }

    function applySeo(seo, siteName, favicon) {
        if (seo && seo.metaTitle) document.title = seo.metaTitle;

        var descMeta = document.querySelector('meta[name="description"]');
        if (descMeta && seo && seo.metaDescription) {
            descMeta.setAttribute("content", seo.metaDescription);
        }

        if (seo && seo.metaKeywords) {
            var keywords = document.querySelector('meta[name="keywords"]');
            if (!keywords) {
                keywords = document.createElement("meta");
                keywords.setAttribute("name", "keywords");
                document.head.appendChild(keywords);
            }
            keywords.setAttribute("content", seo.metaKeywords);
        }

        var finalFavicon = (seo && seo.ogImage) || favicon;
        if (finalFavicon) {
            var icon = document.querySelector("link[rel='icon']");
            if (!icon) {
                icon = document.createElement("link");
                icon.rel = "icon";
                document.head.appendChild(icon);
            }
            icon.href = resolveAssetUrl(finalFavicon);
        }

        if (!seo || !seo.metaTitle) {
            document.title = siteName + " | Microsite";
        }
    }

    function buildDateRange(startDate, endDate) {
        var from = safeText(startDate, "N/A");
        var to = safeText(endDate, "N/A");
        return "Validity: " + from + " to " + to;
    }

    function micrositeFromPayload(payload) {
        if (!payload) return null;
        if (payload.data && payload.data.id) return payload.data;
        if (payload.data && !Array.isArray(payload.data)) return payload.data;
        if (Array.isArray(payload) && payload.length > 0) return payload[0];
        if (payload.id) return payload;
        return null;
    }

    function extractProducts(payload) {
        if (!payload) return [];
        if (Array.isArray(payload.products)) return payload.products;
        if (payload.data && Array.isArray(payload.data.assignedProducts)) return payload.data.assignedProducts;
        if (Array.isArray(payload.data) && payload.data.length && payload.data[0].productId) return payload.data;
        return [];
    }

    function resolveAssetUrl(path) {
        if (!path) return "";
        var cleanPath = String(path).trim().replace(/\\/g, "/");
        if (!cleanPath) return "";
        if (/^https?:\/\//i.test(cleanPath) || cleanPath.startsWith("data:")) return cleanPath;
        if (cleanPath.startsWith("/")) return API_BASE + cleanPath;
        return API_BASE + "/" + cleanPath;
    }

    async function fetchMicrositeBundle(context) {
        var micrositeId = context && context.micrositeId ? context.micrositeId : "";
        var slug = context && context.slug ? context.slug : "";
        var domain = context && context.domain ? context.domain : "";

        var endpoints = [];
        if (micrositeId) {
            endpoints.push(
                API_BASE + "/api/microsite-public/by-id?microsite_id=" + encodeURIComponent(micrositeId)
            );
        }
        if (slug) {
            endpoints.push(API_BASE + "/api/microsite/slug/" + encodeURIComponent(slug));
        }
        if (domain) {
            endpoints.push(API_BASE + "/api/microsite-public/home?domain=" + encodeURIComponent(domain));
        }

        for (var i = 0; i < endpoints.length; i += 1) {
            try {
                var res = await fetch(endpoints[i]);
                if (!res.ok) continue;
                var data = await res.json();
                var microsite = micrositeFromPayload(data);
                if (!microsite || !microsite.id) continue;

                var products = extractProducts(data);
                if (!products.length && micrositeId) {
                    products = await fetchProductsFallback(micrositeId);
                }
                if (!products.length && domain) {
                    products = await fetchProductsByDomain(domain);
                }

                return { microsite: microsite, products: products };
            } catch (e) {
                // try next endpoint
            }
        }
        return { microsite: null, products: [] };
    }

    async function fetchProductsFallback(micrositeId) {
        try {
            var res = await fetch(
                API_BASE + "/api/microsite-public/products-by-id?microsite_id=" + encodeURIComponent(micrositeId)
            );
            if (!res.ok) return [];
            var data = await res.json();
            return extractProducts(data);
        } catch (e) {
            return [];
        }
    }

    async function fetchProductsByDomain(domain) {
        try {
            var res = await fetch(
                API_BASE + "/api/microsite-public/products?domain=" + encodeURIComponent(domain)
            );
            if (!res.ok) return [];
            var data = await res.json();
            return extractProducts(data);
        } catch (e) {
            return [];
        }
    }

    function bindMicrosite(m) {
        if (!m) return;

        var name = safeText(m.name, "Microsite");
        setText("msSiteName", name, "Microsite");
        setText("msFooterName", name, "Microsite");
        setText("msHeading", m.heading || m.name, "Microsite Heading");
        setText("msContent", m.content, "");
        setText("msFooterContent", m.content, "-");

        setText("msAddress", m.address, "-");
        setText("msEmail", m.email, "-");
        setText("msMobile", m.mobile, "-");
        setText("msContactAddress", m.address, "-");
        setText("msContactEmail", m.email, "-");
        setText("msContactMobile", m.mobile, "-");
        setText("msDateRange", buildDateRange(m.startDate, m.endDate), "Validity: -");

        var logoUrl = resolveAssetUrl(m.logoImage);
        var bannerUrl = resolveAssetUrl(m.bannerImage);
        setImage("msLogo", logoUrl, "https://via.placeholder.com/120x48?text=Logo");
        setImage("msBanner", bannerUrl, "https://via.placeholder.com/1200x350?text=Banner");

        applyTheme(m.theme || {});
        applySeo(m.seo || {}, name, m.favicon);
    }

    function getProductName(p) {
        return p.productName || p.ProductName || p.name || p.Name || "Product";
    }

    function getProductPrice(p) {
        var discount = p.discountPrice ?? p.DiscountPrice;
        if (discount !== null && discount !== undefined && Number(discount) > 0) {
            return discount;
        }
        return p.price ?? p.Price ?? 0;
    }

    function getProductImage(p) {
        if (Array.isArray(p.images) && p.images.length > 0) return resolveAssetUrl(p.images[0]);
        if (Array.isArray(p.Images) && p.Images.length > 0) return resolveAssetUrl(p.Images[0]);
        return "";
    }

    function renderProducts(products) {
        var container = document.getElementById("msProducts");
        var emptyEl = document.getElementById("msProductsEmpty");
        var sectionEl = document.getElementById("msProductsSection");
        if (!container) return;

        container.innerHTML = "";

        if (!products || products.length === 0) {
            if (sectionEl) sectionEl.style.display = "block";
            if (emptyEl) {
                emptyEl.textContent = "This microsite do not have products.";
                emptyEl.style.display = "block";
            }
            return;
        }

        if (emptyEl) emptyEl.style.display = "none";
        if (sectionEl) sectionEl.style.display = "block";

        products.forEach(function (p) {
            var name = getProductName(p);
            var price = getProductPrice(p);
            var img = getProductImage(p);
            var desc = p.description || p.Description || "";

            var card = document.createElement("div");
            card.className = "col-md-4 col-sm-6";
            card.innerHTML =
                '<div class="card h-100 shadow-sm">' +
                (img
                    ? '<img src="' + img + '" class="card-img-top" alt="' + name.replace(/"/g, "") + '" style="height:200px;object-fit:cover;">'
                    : "") +
                '<div class="card-body d-flex flex-column">' +
                '<h5 class="card-title">' + name + "</h5>" +
                (desc ? '<p class="card-text text-muted small flex-grow-1">' + desc.substring(0, 120) + "...</p>" : "") +
                '<p class="card-text fw-semibold mb-0">₹' + price + "</p>" +
                "</div></div>";
            container.appendChild(card);
        });
    }

    function preserveContextLinks(context) {
        var params = new URLSearchParams();
        if (context && context.micrositeId) params.set("microsite_id", context.micrositeId);
        if (context && context.slug) params.set("slug", context.slug);
        if (context && context.domain) params.set("domain", context.domain);
        var query = params.toString();
        if (!query) return;

        document.querySelectorAll(".ms-header-nav a, .ms-nav-home").forEach(function (link) {
            var href = link.getAttribute("href") || "index.html";
            var clean = href.split("?")[0];
            link.setAttribute("href", clean + "?" + query);
        });
    }

    document.addEventListener("DOMContentLoaded", async function () {
        var context = window.MICROSITE_CONTEXT || {};
        if (!context.micrositeId) {
            var params = new URLSearchParams(window.location.search);
            context.micrositeId = params.get("microsite_id") || "";
        }

        preserveContextLinks(context);
        var bundle = await fetchMicrositeBundle(context);

        if (!bundle.microsite) {
            if (window.iziToast) {
                iziToast.warning({
                    title: "Microsite",
                    message: "Microsite data load nahi hui. URL me microsite_id check karein.",
                    position: "topRight",
                });
            }
            renderProducts([]);
            return;
        }

        bindMicrosite(bundle.microsite);
        renderProducts(bundle.products);
    });
})();
