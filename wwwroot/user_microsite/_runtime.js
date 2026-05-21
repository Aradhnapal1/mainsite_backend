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

    function setImage(id, value) {
        var el = document.getElementById(id);
        if (!el || el.tagName !== "IMG" || !value) return;
        el.src = value;
        el.style.display = "";
    }

    function themeVal(theme, camel, snake) {
        if (!theme) return "";
        return theme[camel] || theme[snake] || "";
    }

    function applyTheme(theme) {
        if (!theme) return;
        var root = document.documentElement;
        var headerColor = themeVal(theme, "headerColor", "header_color");
        var textColor = themeVal(theme, "textColor", "text_color");
        var backgroundColor = themeVal(theme, "backgroundColor", "background_color");
        var buttonColor = themeVal(theme, "buttonColor", "button_color");
        var buttonTextColor = themeVal(theme, "buttonTextColor", "button_text_color");
        var footerColor = themeVal(theme, "footerColor", "footer_color");
        var footerTextColor = themeVal(theme, "footerTextColor", "footer_text_color");
        var fontFamily = themeVal(theme, "fontFamily", "font_family");

        if (headerColor) root.style.setProperty("--ms-header-color", headerColor);
        if (textColor) root.style.setProperty("--ms-text-color", textColor);
        if (backgroundColor) root.style.setProperty("--ms-bg-color", backgroundColor);
        if (buttonColor) root.style.setProperty("--ms-button-color", buttonColor);
        if (buttonTextColor) root.style.setProperty("--ms-button-text-color", buttonTextColor);
        if (footerColor) root.style.setProperty("--ms-footer-color", footerColor);
        if (footerTextColor) root.style.setProperty("--ms-footer-text-color", footerTextColor);
        if (fontFamily) root.style.setProperty("--ms-font-family", fontFamily);
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
        if (Array.isArray(payload.assignedProducts)) return payload.assignedProducts;
        if (payload.data && Array.isArray(payload.data.assignedProducts)) return payload.data.assignedProducts;
        if (payload.data && Array.isArray(payload.data.products)) return payload.data.products;
        if (Array.isArray(payload.data) && payload.data.length && payload.data[0].productId) return payload.data;
        return [];
    }

    function getApiBase() {
        var base = window.MICROSITE_API_BASE || window.location.origin || "";
        return String(base).replace(/\/$/, "");
    }

    function pickField(obj) {
        for (var i = 1; i < arguments.length; i++) {
            var key = arguments[i];
            if (obj && obj[key] !== undefined && obj[key] !== null && obj[key] !== "") {
                return obj[key];
            }
        }
        return "";
    }

    function resolveAssetUrl(path) {
        if (!path) return "";
        var cleanPath = String(path).trim().replace(/\\/g, "/");
        if (!cleanPath) return "";
        if (/^https?:\/\//i.test(cleanPath) || cleanPath.startsWith("data:")) return cleanPath;

        var base = getApiBase();
        if (cleanPath.startsWith(base)) return cleanPath;

        if (cleanPath.startsWith("/")) return base + cleanPath;
        return base + "/" + cleanPath.replace(/^\//, "");
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

        var logoUrl = resolveAssetUrl(
            pickField(m, "logoImage", "LogoImage", "logo_image")
        );
        var bannerUrl = resolveAssetUrl(
            pickField(m, "bannerImage", "BannerImage", "banner_image")
        );
        var faviconUrl = resolveAssetUrl(pickField(m, "favicon", "Favicon"));

        if (logoUrl) setImage("msLogo", logoUrl);
        if (bannerUrl) {
            setImage("msBanner", bannerUrl);
            var bannerEl = document.getElementById("msBanner");
            if (bannerEl) bannerEl.style.display = "";
        }

        applyTheme(m.theme || m.Theme || {});
        applySeo(m.seo || m.Seo || {}, name, faviconUrl);

        if (window.MICROSITE_CONTEXT) {
            var domains = m.domains || m.Domains || [];
            if (domains.length && !window.MICROSITE_CONTEXT.domain) {
                window.MICROSITE_CONTEXT.domain = domains[0];
            }
        }
    }

    function getProductName(p) {
        return p.productName || p.ProductName || p.name || p.Name || "Product";
    }

    function formatMoney(amount) {
        var n = Number(amount);
        if (isNaN(n)) return "0";
        return n % 1 === 0 ? String(n) : n.toFixed(2);
    }

    function getProductPrices(p) {
        var price = Number(p.price ?? p.Price ?? 0);
        var discountPrice = Number(p.discountPrice ?? p.DiscountPrice ?? 0);
        var salePrice = discountPrice > 0 ? discountPrice : price;
        return { price: price, discountPrice: discountPrice, salePrice: salePrice };
    }

    function getProductPriceHtml(p) {
        var prices = getProductPrices(p);
        if (prices.price > 0 && prices.discountPrice > 0) {
            return (
                '<span class="new-price">₹' +
                formatMoney(prices.discountPrice) +
                '</span><span class="old-price">₹' +
                formatMoney(prices.price) +
                "</span>"
            );
        }
        if (prices.discountPrice > 0) {
            return '<span class="new-price">₹' + formatMoney(prices.discountPrice) + "</span>";
        }
        return '<span class="new-price">₹' + formatMoney(prices.price) + "</span>";
    }

    function getProductImages(p) {
        var raw = p.images || p.Images || [];
        return raw.map(function (img) {
            return resolveAssetUrl(img);
        }).filter(Boolean);
    }

    function getIndexPage() {
        return (window.location.pathname || "").toLowerCase().indexOf(".php") >= 0
            ? "index.php"
            : "index.html";
    }

    function escapeHtml(text) {
        return String(text || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function getMainsiteProductCardHTML(p, wrapperClass) {
        var id = p.productId || p.ProductId || p.id || p.Id;
        var name = getProductName(p);
        var category = p.categoryName || p.CategoryName || p.brandName || p.BrandName || "Product";
        var images = getProductImages(p);
        var mainImg = images[0] || "https://via.placeholder.com/400x400?text=Product";
        var hoverImg = images[1] || "";
        var prices = getProductPrices(p);
        var priceHtml = getProductPriceHtml(p);
        var productUrl =
            window.MicrositeApp && window.MicrositeApp.getProductDetailUrl
                ? window.MicrositeApp.getProductDetailUrl(id)
                : "product" + (getIndexPage().indexOf(".php") >= 0 ? ".php" : ".html") +
                  (window.MICROSITE_CONTEXT && window.MICROSITE_CONTEXT.micrositeId
                      ? "?microsite_id=" + encodeURIComponent(window.MICROSITE_CONTEXT.micrositeId) + "&id=" + id
                      : "?id=" + id);
        var query = window.MICROSITE_CONTEXT && window.MICROSITE_CONTEXT.micrositeId
            ? "?microsite_id=" + encodeURIComponent(window.MICROSITE_CONTEXT.micrositeId)
            : "";
        var indexPage = getIndexPage();

        var hoverHtml = hoverImg
            ? '<img src="' + hoverImg + '" alt="' + escapeHtml(name) + '" class="product-image-hover grid-product-image-hover">'
            : "";

        var thumbHtml = "";
        images.slice(0, 4).forEach(function (img, i) {
            var nextHover = images[i + 1] || img;
            thumbHtml +=
                '<a href="#" class="grid-color-swatch ' +
                (i === 0 ? "active" : "") +
                '" data-img="' +
                img +
                '" data-hover-img="' +
                nextHover +
                '" data-price-html="' +
                escapeHtml(priceHtml) +
                '" data-name="' +
                escapeHtml(name) +
                '"><span style="background:#e5e5e5"></span></a>';
        });

        var innerHTML =
            '<div class="product product-7 text-center">' +
            '<figure class="product-media">' +
            '<a href="' +
            productUrl +
            '">' +
            '<img src="' +
            mainImg +
            '" alt="' +
            escapeHtml(name) +
            '" class="product-image grid-product-image">' +
            hoverHtml +
            "</a>" +
            '<div class="product-action-vertical">' +
            '<a href="#" class="btn-product-icon btn-wishlist btn-expandable"><span>add to wishlist</span></a>' +
            '<a href="#" class="btn-product-icon btn-quickview" title="Quick view"><span>Quick view</span></a>' +
            "</div>" +
            '<div class="product-action">' +
            '<a href="#" class="btn-product btn-cart" data-id="' +
            id +
            '"><span>add to cart</span></a>' +
            "</div>" +
            "</figure>" +
            '<div class="product-body">' +
            '<div class="product-cat"><a href="#">' +
            escapeHtml(category) +
            "</a></div>" +
            '<h3 class="product-title"><a href="' +
            productUrl +
            '" class="grid-product-title">' +
            escapeHtml(name) +
            "</a></h3>" +
            '<div class="product-price grid-product-price">' +
            priceHtml +
            "</div>" +
            (thumbHtml ? '<div class="product-nav product-nav-thumbs mt-1">' + thumbHtml + "</div>" : "") +
            "</div></div>";

        return '<div class="ms-product-slide">' + innerHTML + "</div>";
    }

    function bindProductCardEvents(root) {
        if (!root) return;
        root.querySelectorAll(".grid-color-swatch").forEach(function (swatch) {
            swatch.addEventListener("click", function (e) {
                e.preventDefault();
                var card = swatch.closest(".product");
                if (!card) return;
                var img = swatch.getAttribute("data-img");
                var hover = swatch.getAttribute("data-hover-img");
                var priceHtmlAttr = swatch.getAttribute("data-price-html");
                var name = swatch.getAttribute("data-name");
                card.querySelectorAll(".grid-color-swatch").forEach(function (s) {
                    s.classList.remove("active");
                });
                swatch.classList.add("active");
                var main = card.querySelector(".grid-product-image");
                var hoverEl = card.querySelector(".grid-product-image-hover");
                if (main && img) main.src = img;
                if (hoverEl && hover) hoverEl.src = hover;
                var priceEl = card.querySelector(".grid-product-price");
                if (priceEl && priceHtmlAttr) priceEl.innerHTML = priceHtmlAttr;
                var titleEl = card.querySelector(".grid-product-title");
                if (titleEl && name) titleEl.textContent = name;
            });
        });
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

        container.className = "ms-products-track";
        container.style.transform = "";

        products.forEach(function (p) {
            container.insertAdjacentHTML("beforeend", getMainsiteProductCardHTML(p));
        });

        bindProductCardEvents(container);
        initProductCarousel(products.length);
    }

    function getItemsPerPage() {
        var w = window.innerWidth || 1200;
        if (w < 576) return 1;
        if (w < 768) return 2;
        if (w < 992) return 3;
        if (w < 1200) return 4;
        return 5;
    }

    function initProductCarousel(totalProducts) {
        var track = document.getElementById("msProducts");
        var viewport = track ? track.parentElement : null;
        var prevBtn = document.getElementById("msCarouselPrev");
        var nextBtn = document.getElementById("msCarouselNext");
        var dotsWrap = document.getElementById("msCarouselDots");
        if (!track || !viewport) return;

        var state = { page: 0, perPage: getItemsPerPage(), total: totalProducts };

        function totalPages() {
            return Math.max(1, Math.ceil(state.total / state.perPage));
        }

        function updateLayout() {
            state.perPage = getItemsPerPage();
            var pages = totalPages();
            if (state.page >= pages) state.page = pages - 1;

            var slideBasis = 100 / state.perPage;
            track.querySelectorAll(".ms-product-slide").forEach(function (slide) {
                slide.style.flex = "0 0 " + slideBasis + "%";
                slide.style.maxWidth = slideBasis + "%";
            });

            var offset = state.page * 100;
            track.style.transform = "translate3d(-" + offset + "%, 0, 0)";

            if (prevBtn) prevBtn.disabled = state.page <= 0;
            if (nextBtn) nextBtn.disabled = state.page >= pages - 1;

            if (dotsWrap) {
                dotsWrap.innerHTML = "";
                if (pages > 1) {
                    for (var i = 0; i < pages; i++) {
                        var dot = document.createElement("button");
                        dot.type = "button";
                        dot.className = "ms-carousel-dot" + (i === state.page ? " active" : "");
                        dot.setAttribute("aria-label", "Page " + (i + 1));
                        dot.addEventListener(
                            "click",
                            (function (idx) {
                                return function () {
                                    state.page = idx;
                                    updateLayout();
                                };
                            })(i)
                        );
                        dotsWrap.appendChild(dot);
                    }
                    dotsWrap.style.display = "flex";
                } else {
                    dotsWrap.style.display = "none";
                }
            }

            var showNav = state.total > state.perPage;
            if (prevBtn) prevBtn.style.display = showNav ? "" : "none";
            if (nextBtn) nextBtn.style.display = showNav ? "" : "none";
        }

        if (prevBtn) {
            prevBtn.onclick = function () {
                if (state.page > 0) {
                    state.page -= 1;
                    updateLayout();
                }
            };
        }
        if (nextBtn) {
            nextBtn.onclick = function () {
                if (state.page < totalPages() - 1) {
                    state.page += 1;
                    updateLayout();
                }
            };
        }

        window.addEventListener("resize", function () {
            updateLayout();
        });

        updateLayout();
    }

    function preserveContextLinks(context) {
        var params = new URLSearchParams();
        if (context && context.micrositeId) params.set("microsite_id", context.micrositeId);
        if (context && context.slug) params.set("slug", context.slug);
        if (context && context.domain) params.set("domain", context.domain);
        var query = params.toString();
        if (!query) return;

        document.querySelectorAll(".ms-header-nav a, .ms-nav-home").forEach(function (link) {
            var href = link.getAttribute("href") || getIndexPage();
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
            if (document.getElementById("msProducts") && window.iziToast) {
                iziToast.warning({
                    title: "Microsite",
                    message: "Microsite data load nahi hui. URL me microsite_id check karein.",
                    position: "topRight",
                });
            }
            if (document.getElementById("msProducts")) renderProducts([]);
            return;
        }

        bindMicrosite(bundle.microsite);
        if (document.getElementById("msProducts")) {
            renderProducts(bundle.products);
        }

        if (window.MicrositeApp && window.MicrositeApp.updateCartBadge) {
            window.MicrositeApp.updateCartBadge();
        }
    });
})();
