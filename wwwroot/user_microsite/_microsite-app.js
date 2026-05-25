(function () {
    var API_BASE = window.MICROSITE_API_BASE || window.API_BASE || window.domain || "http://microsite_backend.workarya.com";

    function getContext() {
        var ctx = window.MICROSITE_CONTEXT || {};
        if (!ctx.micrositeId) {
            var params = new URLSearchParams(window.location.search);
            ctx.micrositeId = params.get("microsite_id") || "";
            ctx.slug = params.get("slug") || ctx.slug || "";
            ctx.domain = params.get("domain") || ctx.domain || "";
        }
        return ctx;
    }

    function getCurrentPage() {
        if (document.body) {
            var fromBody = document.body.getAttribute("data-ms-page");
            if (fromBody) return fromBody;
        }
        var ctxPage = window.MICROSITE_CONTEXT && window.MICROSITE_CONTEXT.currentPage;
        if (ctxPage) return ctxPage;
        var path = (window.location.pathname || "").toLowerCase();
        if (path.indexOf("login") >= 0) return "login";
        if (path.indexOf("register") >= 0) return "register";
        if (path.indexOf("profile") >= 0) return "profile";
        if (path.indexOf("checkout") >= 0) return "checkout";
        if (path.indexOf("order-details") >= 0) return "order-details";
        if (path.indexOf("order") >= 0) return "order";
        if (path.indexOf("cart") >= 0) return "cart";
        if (path.indexOf("product") >= 0) return "product";
        if (path.indexOf("contact") >= 0) return "contact";
        return "";
    }

    function msNotify(type, options) {
        var opts = options || {};
        if (window.iziToast && typeof window.iziToast[type] === "function") {
            window.iziToast[type](opts);
            return;
        }
        var msg = opts.message || opts.title || "Notice";
        if (type === "error") console.error(msg);
        else console.log(msg);
        if (typeof window.alert === "function") window.alert(msg);
    }

    function getPageExt() {
        return ".html";
    }

    function pageUrl(name, extra) {
        if (typeof window.msPageUrl === "function") {
            return window.msPageUrl(name, extra);
        }
        return name + ".html" + getContextQuery(extra);
    }

    function getContextQuery(extra) {
        var ctx = getContext();
        var params = new URLSearchParams();
        if (ctx.micrositeId) params.set("microsite_id", ctx.micrositeId);
        if (ctx.slug) params.set("slug", ctx.slug);
        if (ctx.domain) params.set("domain", ctx.domain);
        if (extra) {
            Object.keys(extra).forEach(function (k) {
                if (extra[k] !== undefined && extra[k] !== null && extra[k] !== "") {
                    params.set(k, extra[k]);
                }
            });
        }
        var q = params.toString();
        return q ? "?" + q : "";
    }

    function getApiBase() {
        var base = window.MICROSITE_API_BASE || window.API_BASE || window.domain || "http://microsite_backend.workarya.com";
        return String(base).replace(/\/$/, "");
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

    function cartKey() {
        var id = getContext().micrositeId || "default";
        return "ms_cart_" + id;
    }

    function authKey() {
        var id = getContext().micrositeId || "default";
        return "ms_auth_" + id;
    }

    function getCart() {
        try {
            return JSON.parse(localStorage.getItem(cartKey()) || "[]");
        } catch (e) {
            return [];
        }
    }

    function saveCart(items) {
        localStorage.setItem(cartKey(), JSON.stringify(items || []));
        updateCartBadge();
    }

    function getCartCount() {
        var items = getCart();
        return items.length > 0 ? 1 : 0;
    }

    function canAddToCart(productId) {
        var items = getCart();
        if (!items.length) return { ok: true };
        var existing = items[0];
        if (String(existing.id) === String(productId)) {
            return { ok: false, message: "This product is already in your cart. Only one product is allowed." };
        }
        return { ok: false, message: "Only one product can be in the cart. Remove the current item first." };
    }

    function updateCartBadge() {
        var count = getCartCount();
        document.querySelectorAll(".ms-cart-badge").forEach(function (badge) {
            badge.textContent = String(count);
            if (count > 0) badge.classList.add("show");
            else badge.classList.remove("show");
        });
    }

    function closeMobileNav() {
        var el = document.getElementById("msNavOffcanvas");
        if (!el || !window.bootstrap || !window.bootstrap.Offcanvas) return;
        var instance = window.bootstrap.Offcanvas.getInstance(el);
        if (instance) instance.hide();
    }

    function getAuth() {
        try {
            return JSON.parse(localStorage.getItem(authKey()) || "null");
        } catch (e) {
            return null;
        }
    }

    function saveAuth(data) {
        localStorage.setItem(authKey(), JSON.stringify(data || {}));
        updateAuthNav();
    }

    function clearAuth() {
        localStorage.removeItem(authKey());
        updateAuthNav();
    }

    function getDomainForApi() {
        var ctx = getContext();
        if (ctx.domain) return ctx.domain;
        var domains = window.__MS_MICROSITE_DOMAINS;
        if (domains && domains.length) return domains[0];
        return "";
    }

    function lineItemImageHtml(imageUrl, alt) {
        var src = resolveAssetUrl(imageUrl || "");
        if (!src) {
            return '<span class="ms-line-thumb d-inline-block bg-light"></span>';
        }
        return (
            '<img src="' +
            src +
            '" alt="' +
            String(alt || "Product").replace(/"/g, "&quot;") +
            '" class="ms-line-thumb">'
        );
    }

    function requireAuth(redirectPage) {
        var auth = getAuth();
        if (auth && auth.token) return auth;
        window.location.href = pageUrl(redirectPage || "login");
        return null;
    }

    function handleLogout() {
        clearAuth();
        closeMobileNav();
        document.querySelectorAll(".dropdown-menu.show").forEach(function (menu) {
            menu.classList.remove("show");
        });
        document.querySelectorAll(".dropdown-toggle.show").forEach(function (toggle) {
            toggle.classList.remove("show");
            toggle.setAttribute("aria-expanded", "false");
        });
        msNotify("info", { title: "Logout", message: "Logged out.", position: "topRight" });
        updateAuthNav();
        preserveHeaderLinks();
    }

    function updateAuthNav() {
        var auth = getAuth();
        var isLoggedIn = !!(auth && auth.token);

        document.querySelectorAll(".ms-nav-login-guest").forEach(function (el) {
            el.style.display = isLoggedIn ? "none" : "";
            el.href = pageUrl("login");
        });

        document.querySelectorAll(".ms-account-dropdown").forEach(function (el) {
            el.style.display = isLoggedIn ? "" : "none";
        });

        document.querySelectorAll(".ms-offcanvas-guest").forEach(function (el) {
            el.style.display = isLoggedIn ? "none" : "";
        });

        document.querySelectorAll(".ms-offcanvas-account").forEach(function (el) {
            el.style.display = isLoggedIn ? "block" : "none";
        });
    }

    function initAccountNav() {
        document.querySelectorAll(".ms-nav-logout-action").forEach(function (el) {
            if (el.dataset.msLogoutBound === "1") return;
            el.dataset.msLogoutBound = "1";
            el.addEventListener("click", function (e) {
                e.preventDefault();
                handleLogout();
            });
        });
    }

    function initProfilePage() {
        if (!requireAuth("login")) return;
        var auth = getAuth();
        var nameEl = document.getElementById("msProfileName");
        var emailEl = document.getElementById("msProfileEmail");
        if (nameEl) nameEl.textContent = (auth.user && auth.user.name) || "-";
        if (emailEl) emailEl.textContent = (auth.user && auth.user.email) || "-";
    }

    function normalizeProduct(p) {
        return {
            id: p.productId || p.ProductId || p.id || p.Id,
            name: p.productName || p.ProductName || p.name || "Product",
            price: Number(p.discountPrice ?? p.DiscountPrice ?? p.price ?? p.Price ?? 0),
            originalPrice: Number(p.price ?? p.Price ?? 0),
            images: (p.images || p.Images || []).map(resolveAssetUrl).filter(Boolean),
            category: p.categoryName || p.CategoryName || p.brandName || p.BrandName || "",
            description: p.description || p.Description || "",
            stock: Number(p.stock ?? p.Stock ?? 0),
        };
    }

    function addToCart(product, qty) {
        var id = product.id || product.productId;
        if (!id) return;

        var check = canAddToCart(id);
        if (!check.ok) {
            if (window.iziToast) {
                iziToast.warning({ title: "Cart", message: check.message, position: "topRight" });
            }
            return;
        }

        var rawImg =
            (product.images && product.images[0]) ||
            product.image ||
            product.Image ||
            "";
        var entry = {
            id: id,
            name: product.name || product.productName || "Product",
            price: Number(product.price ?? product.discountPrice ?? product.Price ?? 0),
            image: resolveAssetUrl(rawImg),
            qty: 1,
        };
        saveCart([entry]);
        if (window.iziToast) {
            iziToast.success({
                title: "Cart",
                message: "Product cart me add ho gaya (max 1 product).",
                position: "topRight",
            });
        }
    }

    function removeFromCart(productId) {
        saveCart(
            getCart().filter(function (x) {
                return String(x.id) !== String(productId);
            })
        );
    }

    function getProductDetailUrl(productId) {
        return pageUrl("product", { id: productId });
    }

    function getAllowedEmailDomains() {
        var domains = window.__MS_MICROSITE_DOMAINS || [];
        return domains
            .map(function (d) {
                return String(d || "")
                    .trim()
                    .toLowerCase()
                    .replace(/^@/, "");
            })
            .filter(Boolean);
    }

    function isEmailEligibleForMicrosite(email) {
        var allowed = getAllowedEmailDomains();
        if (!allowed.length) return { ok: true };
        var parts = String(email || "")
            .trim()
            .toLowerCase()
            .split("@");
        if (parts.length < 2) return { ok: false, allowed: allowed };
        var emailDomain = parts.pop();
        var match = allowed.some(function (d) {
            return emailDomain === d || emailDomain.endsWith("." + d);
        });
        return match ? { ok: true, allowed: allowed } : { ok: false, allowed: allowed };
    }

    function updateAuthDomainHint() {
        var hint = document.getElementById("msAuthDomainHint");
        if (!hint) return;
        var allowed = getAllowedEmailDomains();
        if (!allowed.length) {
            hint.textContent = "";
            return;
        }
        hint.textContent = "Only @" + allowed.join(", @") + " email addresses can register or login.";
    }

    async function ensureMicrositeDomains() {
        if (window.__MS_MICROSITE_DOMAINS && window.__MS_MICROSITE_DOMAINS.length) return window.__MS_MICROSITE_DOMAINS;
        var ctx = getContext();
        if (!ctx.micrositeId) return [];
        try {
            var res = await fetch(
                API_BASE + "/api/microsite-public/by-id?microsite_id=" + encodeURIComponent(ctx.micrositeId)
            );
            if (!res.ok) return [];
            var payload = await res.json();
            var m = payload.data || payload;
            var domains = m.domains || m.Domains || [];
            if (domains.length) {
                window.__MS_MICROSITE_DOMAINS = domains;
                if (window.MICROSITE_CONTEXT && !window.MICROSITE_CONTEXT.domain) {
                    window.MICROSITE_CONTEXT.domain = domains[0];
                }
            }
            return domains;
        } catch (e) {
            return [];
        }
    }

    function showOtpStep() {
        var otpBlock = document.getElementById("msOtpBlock");
        var verifyBtn = document.getElementById("msVerifyOtpBtn");
        var otpEl = document.getElementById("msAuthOtp");
        if (otpBlock) otpBlock.style.display = "block";
        if (verifyBtn) verifyBtn.style.display = "";
        if (otpEl) {
            otpEl.value = "";
            otpEl.focus();
        }
    }

    async function sendOtp(email, name) {
        var ctx = getContext();
        await ensureMicrositeDomains();
        var body = {
            email: email,
            micrositeId: ctx.micrositeId || "",
            domain: ctx.domain || getDomainForApi() || "",
        };
        if (name) body.name = name;

        var res = await fetch(API_BASE + "/api/microsite-public/auth/send-otp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
        var data = null;
        try {
            data = await res.json();
        } catch (e) {
            return { status: false, message: "Invalid server response." };
        }
        if (!data) data = { status: false, message: "No response from server." };
        if (!res.ok && data.status !== true) {
            data.status = false;
            data.message = data.message || "Request failed (" + res.status + ").";
        }
        return data;
    }

    async function verifyOtp(email, otp, name) {
        var ctx = getContext();
        await ensureMicrositeDomains();
        var body = {
            email: email,
            otp: otp,
            micrositeId: ctx.micrositeId || "",
            domain: ctx.domain || getDomainForApi() || "",
        };
        if (name) body.name = name;

        var res = await fetch(API_BASE + "/api/microsite-public/auth/verify-otp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
        var data = null;
        try {
            data = await res.json();
        } catch (e) {
            return { status: false, message: "Invalid server response." };
        }
        if (!data) data = { status: false, message: "No response from server." };
        if (!res.ok && data.status !== true) {
            data.status = false;
            data.message = data.message || "Request failed (" + res.status + ").";
        }
        return data;
    }

    function initAuthForm(mode) {
        var emailEl = document.getElementById("msAuthEmail");
        var otpEl = document.getElementById("msAuthOtp");
        var nameEl = document.getElementById("msAuthName");
        var sendBtn = document.getElementById("msSendOtpBtn");
        var verifyBtn = document.getElementById("msVerifyOtpBtn");
        if (!emailEl || !sendBtn || !verifyBtn) return;

        ensureMicrositeDomains().then(function () {
            updateAuthDomainHint();
        });

        sendBtn.addEventListener("click", async function () {
            var email = (emailEl.value || "").trim();
            var name = nameEl ? (nameEl.value || "").trim() : "";
            if (!email) {
                msNotify("warning", { title: "Email", message: "Please enter your email.", position: "topRight" });
                return;
            }
            if (!getContext().micrositeId && !getContext().domain) {
                msNotify("warning", {
                    title: "Microsite",
                    message: "Add microsite_id to the URL.",
                    position: "topRight",
                });
                return;
            }
            if (mode === "register" && !name) {
                msNotify("warning", { title: "Name", message: "Name is required to register.", position: "topRight" });
                return;
            }

            await ensureMicrositeDomains();
            var eligible = isEmailEligibleForMicrosite(email);
            if (!eligible.ok) {
                msNotify("error", {
                    title: "Not eligible",
                    message: "You are not eligible for this microsite.",
                    position: "topRight",
                });
                return;
            }

            sendBtn.disabled = true;
            try {
                var data = await sendOtp(email, name || undefined);
                if (data.status) {
                    showOtpStep();
                    sendBtn.textContent = "Resend OTP";
                    msNotify("success", { title: "OTP", message: data.message || "OTP sent successfully.", position: "topRight" });
                } else {
                    msNotify("error", {
                        title: "OTP",
                        message: data.message || "OTP could not be sent.",
                        position: "topRight",
                    });
                }
            } catch (e) {
                msNotify("error", { title: "OTP", message: "Server error. Check API URL or CORS.", position: "topRight" });
            } finally {
                sendBtn.disabled = false;
            }
        });

        verifyBtn.addEventListener("click", async function () {
            var email = (emailEl.value || "").trim();
            var otp = (otpEl && otpEl.value ? otpEl.value : "").trim();
            var name = nameEl ? (nameEl.value || "").trim() : "";
            if (!email || !otp) {
                msNotify("warning", { title: "Verify", message: "Email and OTP are required.", position: "topRight" });
                return;
            }
            if (mode === "register" && !name) {
                msNotify("warning", { title: "Name", message: "Name is required.", position: "topRight" });
                return;
            }

            var eligible = isEmailEligibleForMicrosite(email);
            if (!eligible.ok) {
                msNotify("error", {
                    title: "Not eligible",
                    message: "You are not eligible for this microsite.",
                    position: "topRight",
                });
                return;
            }

            verifyBtn.disabled = true;
            try {
                var data = await verifyOtp(email, otp, name || undefined);
                if (data.status && data.data) {
                    saveAuth({ token: data.data.token, user: data.data.user });
                    msNotify("success", {
                        title: "Success",
                        message: data.message || "Login successful.",
                        position: "topRight",
                    });
                    window.location.href = pageUrl("index");
                } else {
                    msNotify("error", { title: "Verify", message: data.message || "OTP invalid.", position: "topRight" });
                }
            } catch (e) {
                msNotify("error", { title: "Verify", message: "Server error.", position: "topRight" });
            } finally {
                verifyBtn.disabled = false;
            }
        });
    }

    async function fetchProductById(productId) {
        var ctx = getContext();
        if (!ctx.micrositeId || !productId) return null;
        var res = await fetch(
            API_BASE +
                "/api/microsite-public/product-by-id?microsite_id=" +
                encodeURIComponent(ctx.micrositeId) +
                "&product_id=" +
                encodeURIComponent(productId)
        );
        if (!res.ok) return null;
        var data = await res.json();
        return data && data.data ? data.data : null;
    }

    function renderProductDetail(p) {
        var norm = normalizeProduct(p);
        var title = document.getElementById("msProductTitle");
        var price = document.getElementById("msProductPrice");
        var desc = document.getElementById("msProductDesc");
        var img = document.getElementById("msProductImage");
        var cat = document.getElementById("msProductCategory");
        var stock = document.getElementById("msProductStock");

        if (title) title.textContent = norm.name;
        if (cat) cat.textContent = norm.category || "Product";
        if (desc) desc.textContent = norm.description || "No description available.";
        if (stock) stock.textContent = norm.stock > 0 ? "In stock (" + norm.stock + ")" : "Out of stock";
        if (img && norm.images[0]) {
            img.src = norm.images[0];
            img.style.display = "";
        }

        var mrp = Number(p.price ?? p.Price ?? 0);
        var discount = Number(p.discountPrice ?? p.DiscountPrice ?? 0);
        var priceHtml = "";
        if (mrp > 0 && discount > 0) {
            priceHtml =
                '<span class="new-price">₹' +
                discount +
                '</span><span class="old-price">₹' +
                mrp +
                "</span>";
        } else if (discount > 0) {
            priceHtml = '<span class="new-price">₹' + discount + "</span>";
        } else {
            priceHtml = '<span class="new-price">₹' + mrp + "</span>";
        }
        if (price) price.innerHTML = priceHtml;

        var addBtn = document.getElementById("msAddToCartBtn");
        if (addBtn) {
            addBtn.onclick = function () {
                addToCart(norm, 1);
            };
        }
    }

    async function initProductPage() {
        var params = new URLSearchParams(window.location.search);
        var productId = params.get("id") || params.get("product_id");
        if (!productId) {
            document.getElementById("msProductEmpty").style.display = "block";
            return;
        }
        var p = await fetchProductById(productId);
        if (!p) {
            document.getElementById("msProductEmpty").style.display = "block";
            return;
        }
        renderProductDetail(p);
    }

    function renderCartTable() {
        var tbody = document.getElementById("msCartBody");
        var empty = document.getElementById("msCartEmpty");
        var totalEl = document.getElementById("msCartTotal");
        var checkoutBtn = document.getElementById("msCartCheckoutBtn");
        var continueBtn = document.getElementById("msCartContinueBtn");
        if (!tbody) return;

        var items = getCart();
        tbody.innerHTML = "";
        if (!items.length) {
            if (empty) empty.style.display = "block";
            if (totalEl) totalEl.textContent = "₹0";
            if (checkoutBtn) checkoutBtn.style.display = "none";
            if (continueBtn) continueBtn.style.display = "";
            return;
        }
        if (empty) empty.style.display = "none";
        if (checkoutBtn) checkoutBtn.style.display = "";
        if (continueBtn) continueBtn.style.display = "none";

        var item = items[0];
        item.qty = 1;
        var line = Number(item.price) || 0;
        var tr = document.createElement("tr");
        tr.innerHTML =
            "<td>" +
            lineItemImageHtml(item.image, item.name) +
            "</td><td>" +
            (item.name || "Product") +
            '</td><td><button type="button" class="btn btn-sm btn-outline-danger ms-remove-cart" data-id="' +
            item.id +
            '">Remove</button></td>';
        tbody.appendChild(tr);
        var total = line;
        if (totalEl) totalEl.textContent = "₹" + total.toFixed(2);

        tbody.querySelectorAll(".ms-remove-cart").forEach(function (btn) {
            btn.addEventListener("click", function () {
                removeFromCart(btn.getAttribute("data-id"));
                renderCartTable();
            });
        });
    }

    async function initCheckoutPage() {
        var form = document.getElementById("msCheckoutForm");
        var btn = document.getElementById("msPlaceOrderBtn");
        var hint = document.getElementById("msCheckoutHint");
        if (!form || !btn) return;

        var cart = getCart();
        if (!cart.length) {
            if (hint) hint.textContent = "Your cart is empty. Add a product first.";
            btn.disabled = true;
            return;
        }
        if (cart.length > 1) {
            saveCart([cart[0]]);
            cart = getCart();
        }
        var summary = document.getElementById("msCheckoutSummary");
        if (summary) {
            summary.className = "d-flex align-items-center gap-3 mb-3";
            summary.style.display = "flex";
            summary.innerHTML =
                lineItemImageHtml(cart[0].image, cart[0].name) +
                '<div><strong>' +
                (cart[0].name || "Product") +
                '</strong><div class="text-muted small">Qty: 1</div></div>';
        }
        if (hint) {
            hint.textContent = "Ordering: " + (cart[0].name || "Product") + " (qty 1 only)";
        }

        var auth = getAuth();
        if (!auth || !auth.token) {
            if (hint) hint.textContent = "Please sign in before placing an order.";
            btn.disabled = true;
            return;
        }

        btn.addEventListener("click", async function () {
            var ctx = getContext();
            var fd = new FormData(form);
            var body = {
                micrositeId: ctx.micrositeId || "",
                domain: ctx.domain || "",
                productId: Number(cart[0].id),
                quantity: 1,
                firstName: (fd.get("firstName") || "").toString().trim(),
                lastName: (fd.get("lastName") || "").toString().trim(),
                email: (fd.get("email") || auth.user?.email || "").toString().trim(),
                mobile: (fd.get("mobile") || "").toString().trim(),
                address: (fd.get("address") || "").toString().trim(),
                city: (fd.get("city") || "").toString().trim(),
                state: (fd.get("state") || "").toString().trim(),
                pincode: (fd.get("pincode") || "").toString().trim(),
                country: (fd.get("country") || "India").toString().trim(),
            };

            if (!body.firstName || !body.email || !body.address) {
                iziToast.warning({ title: "Checkout", message: "Please fill in all required fields.", position: "topRight" });
                return;
            }
            if (!body.micrositeId && !body.domain) {
                iziToast.warning({ title: "Checkout", message: "microsite_id is missing from the URL.", position: "topRight" });
                return;
            }

            btn.disabled = true;
            try {
                var res = await fetch(API_BASE + "/api/microsite-public/order", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: "Bearer " + auth.token,
                    },
                    body: JSON.stringify(body),
                });
                var data = await res.json();
                if (data.status) {
                    saveCart([]);
                    iziToast.success({ title: "Order", message: data.message || "Order placed.", position: "topRight" });
                    window.location.href = pageUrl("order");
                } else {
                    iziToast.error({ title: "Order", message: data.message || "Order failed.", position: "topRight" });
                }
            } catch (e) {
                iziToast.error({ title: "Order", message: "Server error.", position: "topRight" });
            } finally {
                btn.disabled = false;
            }
        });
    }

    function preserveHeaderLinks() {
        var query = getContextQuery().replace(/^\?/, "");
        if (!query) return;
        document.querySelectorAll(
            ".ms-header-nav a, .ms-offcanvas-nav a, .ms-offcanvas-account a, .ms-account-menu a, .ms-nav-home, .ms-cart-link, .ms-nav-login-guest"
        ).forEach(function (link) {
            if (link.classList.contains("ms-nav-logout-action")) return;
            if (link.classList.contains("dropdown-toggle")) return;
            var href = link.getAttribute("href") || "";
            if (!href || href === "#") return;
            var clean = href.split("?")[0];
            link.setAttribute("href", clean + "?" + query);
        });
    }

    function initMobileNav() {
        document.querySelectorAll(".ms-offcanvas-nav a[href]").forEach(function (link) {
            link.addEventListener("click", function () {
                if (!link.classList.contains("ms-nav-logout")) closeMobileNav();
            });
        });
    }

    async function ensureDomainForApi() {
        var domain = getDomainForApi();
        if (domain) return domain;
        var ctx = getContext();
        if (!ctx.micrositeId) return "";
        try {
            var res = await fetch(
                API_BASE +
                    "/api/microsite-public/by-id?microsite_id=" +
                    encodeURIComponent(ctx.micrositeId)
            );
            if (!res.ok) return "";
            var payload = await res.json();
            var m = payload.data || payload;
            var domains = m.domains || m.Domains || [];
            if (domains.length) {
                window.__MS_MICROSITE_DOMAINS = domains;
                if (window.MICROSITE_CONTEXT) window.MICROSITE_CONTEXT.domain = domains[0];
                return domains[0];
            }
        } catch (e) {
            return "";
        }
        return "";
    }

    async function fetchMyOrders() {
        var auth = getAuth();
        var domain = await ensureDomainForApi();
        if (!auth || !auth.token || !domain) return [];
        var res = await fetch(
            API_BASE + "/api/microsite-public/orders?domain=" + encodeURIComponent(domain),
            { headers: { Authorization: "Bearer " + auth.token } }
        );
        if (!res.ok) return [];
        var data = await res.json();
        return data && data.data ? data.data : [];
    }

    async function fetchOrderDetail(orderId) {
        var auth = getAuth();
        var domain = await ensureDomainForApi();
        if (!auth || !auth.token || !domain || !orderId) return null;
        var res = await fetch(
            API_BASE +
                "/api/microsite-public/orders/" +
                encodeURIComponent(orderId) +
                "?domain=" +
                encodeURIComponent(domain),
            { headers: { Authorization: "Bearer " + auth.token } }
        );
        if (!res.ok) return null;
        var data = await res.json();
        return data && data.data ? data.data : null;
    }

    async function initOrderPage() {
        if (!requireAuth("login")) return;
        var tbody = document.getElementById("msOrdersBody");
        var empty = document.getElementById("msOrdersEmpty");
        if (!tbody) return;

        tbody.innerHTML = '<tr><td colspan="5" class="text-muted">Loading...</td></tr>';
        var orders = await fetchMyOrders();
        tbody.innerHTML = "";
        if (!orders.length) {
            if (empty) empty.style.display = "block";
            return;
        }
        if (empty) empty.style.display = "none";

        for (var i = 0; i < orders.length; i++) {
            var o = orders[i];
            var product = await fetchProductById(o.productId);
            var img = "";
            if (product) {
                var norm = normalizeProduct(product);
                img = norm.images[0] || "";
            }
            var tr = document.createElement("tr");
            tr.innerHTML =
                "<td>" +
                lineItemImageHtml(img, o.productName) +
                "</td><td>#" +
                o.id +
                "</td><td>" +
                (o.productName || "Product") +
                "</td><td>" +
                (o.status || "-") +
                "</td><td>₹" +
                Number(o.totalPrice || 0).toFixed(2) +
                '</td><td><a class="btn btn-sm ms-btn" href="' +
                pageUrl("order-details", { order_id: o.id }) +
                '">View</a></td>';
            tbody.appendChild(tr);
        }
    }

    async function initOrderDetailsPage() {
        if (!requireAuth("login")) return;
        var params = new URLSearchParams(window.location.search);
        var orderId = params.get("order_id");
        if (!orderId) {
            var err = document.getElementById("msOrderDetailEmpty");
            if (err) err.style.display = "block";
            return;
        }

        var detail = await fetchOrderDetail(orderId);
        var empty = document.getElementById("msOrderDetailEmpty");
        if (!detail) {
            if (empty) empty.style.display = "block";
            return;
        }
        if (empty) empty.style.display = "none";

        var product = detail.product || {};
        var fullProduct = await fetchProductById(product.productId);
        var img = "";
        if (fullProduct) {
            img = normalizeProduct(fullProduct).images[0] || "";
        }

        var set = function (id, text) {
            var el = document.getElementById(id);
            if (el) el.textContent = text;
        };
        set("msOrderId", "#" + detail.id);
        set(
            "msOrderCustomer",
            [detail.customer && detail.customer.firstName, detail.customer && detail.customer.lastName]
                .filter(Boolean)
                .join(" ") || "-"
        );
        set("msOrderStatus", detail.status || "-");

        var tbody = document.getElementById("msOrderItemsBody");
        if (tbody) {
            tbody.innerHTML =
                "<tr><td>" +
                lineItemImageHtml(img, product.name) +
                "</td><td>" +
                (product.name || "Product") +
                "</td><td>" +
                (detail.quantity || 1) +
                "</td><td>₹" +
                Number(detail.totalPrice || 0).toFixed(2) +
                "</td></tr>";
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.body.classList.add("ms-no-hover");
        preserveHeaderLinks();
        initMobileNav();
        updateCartBadge();
        initAccountNav();
        updateAuthNav();

        var page = getCurrentPage();
        if (page === "login") initAuthForm("login");
        if (page === "register") initAuthForm("register");
        if (page === "profile") initProfilePage();
        if (page === "product") initProductPage();
        if (page === "cart") renderCartTable();
        if (page === "checkout") initCheckoutPage();
        if (page === "order") initOrderPage();
        if (page === "order-details") initOrderDetailsPage();

        document.body.addEventListener("click", function (e) {
            var btn = e.target.closest(".btn-cart[data-id]");
            if (!btn) return;
            e.preventDefault();
            var id = btn.getAttribute("data-id");
            var card = btn.closest(".product");
            var name = card ? card.querySelector(".grid-product-title") : null;
            var img = card ? card.querySelector(".grid-product-image") : null;
            addToCart(
                {
                    id: id,
                    name: name ? name.textContent : "Product",
                    price: 0,
                    image: img ? img.src : "",
                },
                1
            );
        });
    });

    window.MicrositeApp = {
        API_BASE: API_BASE,
        getContext: getContext,
        getContextQuery: getContextQuery,
        getPageExt: getPageExt,
        resolveAssetUrl: resolveAssetUrl,
        getCart: getCart,
        saveCart: saveCart,
        addToCart: addToCart,
        removeFromCart: removeFromCart,
        getCartCount: getCartCount,
        updateCartBadge: updateCartBadge,
        getProductDetailUrl: getProductDetailUrl,
        normalizeProduct: normalizeProduct,
        sendOtp: sendOtp,
        verifyOtp: verifyOtp,
        getAuth: getAuth,
        saveAuth: saveAuth,
        clearAuth: clearAuth,
        canAddToCart: canAddToCart,
        getApiBase: getApiBase,
    };
})();
