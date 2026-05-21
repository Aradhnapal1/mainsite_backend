(function () {
    var API_BASE = window.MICROSITE_API_BASE || "http://microsite_backend.workarya.com";

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

    function getPageExt() {
        return (window.location.pathname || "").toLowerCase().indexOf(".php") >= 0 ? ".php" : ".html";
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
        var base = window.MICROSITE_API_BASE || window.location.origin || "";
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
            return { ok: false, message: "Ye product pehle se cart me hai. Sirf 1 product allow hai." };
        }
        return { ok: false, message: "Microsite me sirf 1 product cart me ho sakta hai. Pehle remove karein." };
    }

    function updateCartBadge() {
        var badge = document.getElementById("msCartBadge");
        if (!badge) return;
        var count = getCartCount();
        badge.textContent = String(count);
        if (count > 0) badge.classList.add("show");
        else badge.classList.remove("show");
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

    function updateAuthNav() {
        var auth = getAuth();
        var login = document.getElementById("msNavLogin");
        var register = document.getElementById("msNavRegister");
        if (!login || !register) return;
        if (auth && auth.token) {
            login.textContent = auth.user && auth.user.email ? auth.user.email : "Account";
            login.href = "order.php" + getContextQuery();
            register.textContent = "Logout";
            register.href = "#";
            register.onclick = function (e) {
                e.preventDefault();
                clearAuth();
                if (window.iziToast) {
                    iziToast.info({ title: "Logout", message: "Logged out.", position: "topRight" });
                }
                updateAuthNav();
            };
        } else {
            login.textContent = "Login";
            login.href = "login" + getPageExt() + getContextQuery();
            register.textContent = "Register";
            register.href = "register" + getPageExt() + getContextQuery();
            register.onclick = null;
        }
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

        var entry = {
            id: id,
            name: product.name || product.productName || "Product",
            price: Number(product.price ?? product.discountPrice ?? product.Price ?? 0),
            image: (product.images && product.images[0]) || product.image || "",
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
        return "product" + getPageExt() + getContextQuery({ id: productId });
    }

    async function sendOtp(email, name) {
        var ctx = getContext();
        var body = { email: email };
        if (ctx.micrositeId) body.micrositeId = ctx.micrositeId;
        if (ctx.domain) body.domain = ctx.domain;
        if (name) body.name = name;

        var res = await fetch(API_BASE + "/api/microsite-public/auth/send-otp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
        return res.json();
    }

    async function verifyOtp(email, otp, name) {
        var ctx = getContext();
        var body = { email: email, otp: otp };
        if (ctx.micrositeId) body.micrositeId = ctx.micrositeId;
        if (ctx.domain) body.domain = ctx.domain;
        if (name) body.name = name;

        var res = await fetch(API_BASE + "/api/microsite-public/auth/verify-otp", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body),
        });
        return res.json();
    }

    function initAuthForm(mode) {
        var emailEl = document.getElementById("msAuthEmail");
        var otpEl = document.getElementById("msAuthOtp");
        var nameEl = document.getElementById("msAuthName");
        var sendBtn = document.getElementById("msSendOtpBtn");
        var verifyBtn = document.getElementById("msVerifyOtpBtn");
        if (!emailEl || !sendBtn || !verifyBtn) return;

        sendBtn.addEventListener("click", async function () {
            var email = (emailEl.value || "").trim();
            if (!email) {
                iziToast.warning({ title: "Email", message: "Email enter karein.", position: "topRight" });
                return;
            }
            if (!getContext().micrositeId && !getContext().domain) {
                iziToast.warning({
                    title: "Microsite",
                    message: "URL me microsite_id add karein.",
                    position: "topRight",
                });
                return;
            }
            sendBtn.disabled = true;
            try {
                var name = nameEl ? (nameEl.value || "").trim() : "";
                if (mode === "register" && !name) {
                    iziToast.warning({ title: "Name", message: "Register ke liye name required hai.", position: "topRight" });
                    return;
                }
                var data = await sendOtp(email, name || undefined);
                if (data.status) {
                    iziToast.success({ title: "OTP", message: data.message || "OTP bhej diya.", position: "topRight" });
                } else {
                    iziToast.error({ title: "OTP", message: data.message || "OTP send fail.", position: "topRight" });
                }
            } catch (e) {
                iziToast.error({ title: "OTP", message: "Server error.", position: "topRight" });
            } finally {
                sendBtn.disabled = false;
            }
        });

        verifyBtn.addEventListener("click", async function () {
            var email = (emailEl.value || "").trim();
            var otp = (otpEl && otpEl.value ? otpEl.value : "").trim();
            var name = nameEl ? (nameEl.value || "").trim() : "";
            if (!email || !otp) {
                iziToast.warning({ title: "Verify", message: "Email aur OTP required.", position: "topRight" });
                return;
            }
            if (mode === "register" && !name) {
                iziToast.warning({ title: "Name", message: "Name required hai.", position: "topRight" });
                return;
            }
            verifyBtn.disabled = true;
            try {
                var data = await verifyOtp(email, otp, name || undefined);
                if (data.status && data.data) {
                    saveAuth({ token: data.data.token, user: data.data.user });
                    iziToast.success({
                        title: "Success",
                        message: data.message || "Login successful.",
                        position: "topRight",
                    });
                    window.location.href = "index" + getPageExt() + getContextQuery();
                } else {
                    iziToast.error({ title: "Verify", message: data.message || "OTP invalid.", position: "topRight" });
                }
            } catch (e) {
                iziToast.error({ title: "Verify", message: "Server error.", position: "topRight" });
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
        if (!tbody) return;

        var items = getCart();
        tbody.innerHTML = "";
        if (!items.length) {
            if (empty) empty.style.display = "block";
            if (totalEl) totalEl.textContent = "₹0";
            return;
        }
        if (empty) empty.style.display = "none";

        var item = items[0];
        item.qty = 1;
        var line = Number(item.price) || 0;
        var tr = document.createElement("tr");
        tr.innerHTML =
            "<td>" +
            (item.name || "Product") +
            "</td><td>1</td><td>₹" +
            line.toFixed(2) +
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
            if (hint) hint.textContent = "Cart empty hai. Pehle 1 product add karein.";
            btn.disabled = true;
            return;
        }
        if (cart.length > 1) {
            saveCart([cart[0]]);
            cart = getCart();
        }
        if (hint) {
            hint.textContent = "Ordering: " + (cart[0].name || "Product") + " (qty 1 only)";
        }

        var auth = getAuth();
        if (!auth || !auth.token) {
            if (hint) hint.textContent = "Order ke liye pehle login karein.";
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
                iziToast.warning({ title: "Checkout", message: "Required fields fill karein.", position: "topRight" });
                return;
            }
            if (!body.micrositeId && !body.domain) {
                iziToast.warning({ title: "Checkout", message: "microsite_id URL me missing hai.", position: "topRight" });
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
                    window.location.href = "order" + getPageExt() + getContextQuery();
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
        document.querySelectorAll(".ms-header-nav a, .ms-nav-home").forEach(function (link) {
            if (link.id === "msNavRegister" && getAuth() && getAuth().token) return;
            var href = link.getAttribute("href") || "";
            if (!href || href === "#") return;
            var clean = href.split("?")[0];
            link.setAttribute("href", clean + "?" + query);
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        preserveHeaderLinks();
        updateCartBadge();
        updateAuthNav();

        var page = (window.MICROSITE_CONTEXT && window.MICROSITE_CONTEXT.currentPage) || "";
        if (page === "login") initAuthForm("login");
        if (page === "register") initAuthForm("register");
        if (page === "product") initProductPage();
        if (page === "cart") renderCartTable();
        if (page === "checkout") initCheckoutPage();

        document.body.addEventListener("click", function (e) {
            var btn = e.target.closest(".btn-cart[data-id]");
            if (!btn) return;
            e.preventDefault();
            var id = btn.getAttribute("data-id");
            var card = btn.closest(".product");
            var name = card ? card.querySelector(".grid-product-title") : null;
            var priceEl = card ? card.querySelector(".grid-product-price") : null;
            var img = card ? card.querySelector(".grid-product-image") : null;
            addToCart(
                {
                    id: id,
                    name: name ? name.textContent : "Product",
                    price: priceEl ? parseFloat((priceEl.textContent || "0").replace(/[^\d.]/g, "")) : 0,
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
