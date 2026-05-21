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

    function resolveAssetUrl(path) {
        if (!path) return "";
        var cleanPath = String(path).trim().replace(/\\/g, "/");
        if (!cleanPath) return "";
        if (/^https?:\/\//i.test(cleanPath) || cleanPath.startsWith("data:")) return cleanPath;
        if (cleanPath.startsWith("/")) return API_BASE + cleanPath;
        return API_BASE + "/" + cleanPath;
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
        return getCart().reduce(function (sum, item) {
            return sum + (Number(item.qty) || 1);
        }, 0);
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
        var items = getCart();
        var quantity = Math.max(1, Number(qty) || 1);
        var existing = items.find(function (x) {
            return String(x.id) === String(id);
        });
        var entry = {
            id: id,
            name: product.name || product.productName || "Product",
            price: Number(product.price ?? product.discountPrice ?? product.Price ?? 0),
            image: (product.images && product.images[0]) || product.image || "",
            qty: quantity,
        };
        if (existing) {
            existing.qty = (Number(existing.qty) || 0) + quantity;
        } else {
            items.push(entry);
        }
        saveCart(items);
        if (window.iziToast) {
            iziToast.success({ title: "Cart", message: "Product cart me add ho gaya.", position: "topRight" });
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
        if (img) img.src = norm.images[0] || "https://via.placeholder.com/600x600?text=Product";

        var priceHtml = "₹" + norm.price;
        if (norm.originalPrice > norm.price) {
            priceHtml =
                '<span class="new-price">₹' +
                norm.price +
                '</span> <span class="old-price text-muted"><del>₹' +
                norm.originalPrice +
                "</del></span>";
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

        var total = 0;
        items.forEach(function (item) {
            var line = (Number(item.price) || 0) * (Number(item.qty) || 1);
            total += line;
            var tr = document.createElement("tr");
            tr.innerHTML =
                "<td>" +
                (item.name || "Product") +
                "</td><td>" +
                (item.qty || 1) +
                "</td><td>₹" +
                line.toFixed(2) +
                '</td><td><button type="button" class="btn btn-sm btn-outline-danger ms-remove-cart" data-id="' +
                item.id +
                '">Remove</button></td>';
            tbody.appendChild(tr);
        });
        if (totalEl) totalEl.textContent = "₹" + total.toFixed(2);

        tbody.querySelectorAll(".ms-remove-cart").forEach(function (btn) {
            btn.addEventListener("click", function () {
                removeFromCart(btn.getAttribute("data-id"));
                renderCartTable();
            });
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
    };
})();
