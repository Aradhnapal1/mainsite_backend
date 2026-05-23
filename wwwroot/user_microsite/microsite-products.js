(function () {
  var GRID_COLS = "col-sm-3 col-md-3 col-lg-3";

  function parseArray(val) {
    if (!val) return [];
    if (typeof val === "string") {
      try {
        return JSON.parse(val);
      } catch (e) {
        return [];
      }
    }
    return Array.isArray(val) ? val : [];
  }

  function buildProductLink(id, color) {
    var ctx = window.MICROSITE_CONTEXT || {};
    var q = "id=" + encodeURIComponent(id);
    if (color) q += "&color=" + encodeURIComponent(color);
    if (ctx.micrositeId) q += "&microsite_id=" + encodeURIComponent(ctx.micrositeId);
    if (ctx.slug) q += "&slug=" + encodeURIComponent(ctx.slug);
    if (ctx.domain) q += "&domain=" + encodeURIComponent(ctx.domain);
    return "../product.php?" + q;
  }

  function getProductCardHTML(p) {
    var pColors = parseArray(p.colorNames);
    var pVariants = parseArray(p.variants);
    var mainGallery = parseArray(p.imageGallery);
    var mainHoverImg = mainGallery.length > 0 ? mainGallery[0] : "";
    var gridColorMap = {};

    pColors.forEach(function (c) {
      gridColorMap[c] = {
        image: p.image,
        hoverImage: mainHoverImg,
        price: p.discountPrice > 0 ? p.discountPrice : p.price,
        oldPrice: p.price,
        name: p.productName,
      };
    });

    pVariants.forEach(function (v) {
      var vActive =
        v.isActive !== undefined
          ? v.isActive
          : v.IsActive !== undefined
            ? v.IsActive
            : v.isactive;
      if (
        vActive === true ||
        vActive === 1 ||
        String(vActive).toLowerCase() === "true"
      ) {
        parseArray(v.colorNames).forEach(function (c) {
          if (!gridColorMap[c]) {
            var vGallery = parseArray(v.imageGallery);
            gridColorMap[c] = {
              image: v.image || p.image,
              hoverImage: vGallery.length > 0 ? vGallery[0] : mainHoverImg,
              price:
                v.discountPrice > 0
                  ? v.discountPrice
                  : v.price > 0
                    ? v.price
                    : p.discountPrice > 0
                      ? p.discountPrice
                      : p.price,
              oldPrice: v.price > 0 ? v.price : p.price,
              name: v.variantName || p.productName,
            };
          }
        });
      }
    });

    var gridColorHtml = "";
    var firstGridColor = "";
    Object.keys(gridColorMap).forEach(function (c, i) {
      if (i === 0) firstGridColor = c;
      var vData = gridColorMap[c];
      gridColorHtml +=
        '<a href="#" class="grid-color-swatch ' +
        (i === 0 ? "active" : "") +
        '" title="' +
        c +
        '" style="display:inline-block;margin-right:5px;" data-img="' +
        (vData.image || "") +
        '" data-hover-img="' +
        (vData.hoverImage || "") +
        '" data-price="' +
        (vData.price || 0) +
        '" data-name="' +
        (vData.name || "") +
        '"><span style="display:block;width:20px;height:20px;border-radius:50%;background:' +
        c +
        ';border:1px solid #ccc"></span></a>';
    });

    var firstGridData =
      firstGridColor && gridColorMap[firstGridColor]
        ? gridColorMap[firstGridColor]
        : { image: p.image, hoverImage: mainHoverImg, price: p.discountPrice > 0 ? p.discountPrice : p.price, oldPrice: p.price };
    var hoverHtml = firstGridData.hoverImage
      ? '<img src="' + firstGridData.hoverImage + '" alt="' + p.productName + '" class="product-image-hover grid-product-image-hover">'
      : "";
    var salePrice = p.discountPrice > 0 ? p.discountPrice : p.price;
    var showOld = p.discountPrice > 0 && p.price > p.discountPrice;
    var priceHtml = showOld
      ? '<span class="new-price">₹' + salePrice + '</span><span class="old-price">₹' + p.price + "</span>"
      : "₹" + salePrice;
    var href = buildProductLink(p.id, firstGridColor);

    return (
      '<div class="' +
      GRID_COLS +
      '">' +
      '<div class="product product-7 text-center">' +
      '<figure class="product-media">' +
      '<a href="' +
      href +
      '">' +
      '<img src="' +
      firstGridData.image +
      '" alt="' +
      p.productName +
      '" class="product-image grid-product-image">' +
      hoverHtml +
      "</a>" +
      '<div class="product-action">' +
      '<a href="#" class="btn-product btn-cart" data-id="' +
      p.id +
      '"><span>add to cart</span></a>' +
      "</div>" +
      "</figure>" +
      '<div class="product-body">' +
      '<div class="product-cat"><a href="#">' +
      (p.categoryName || "Category") +
      "</a></div>" +
      '<h3 class="product-title"><a href="' +
      href +
      '" class="grid-product-title">' +
      p.productName +
      "</a></h3>" +
      '<div class="product-price grid-product-price">' +
      priceHtml +
      "</div>" +
      '<div class="product-nav product-nav-thumbs mt-1">' +
      gridColorHtml +
      "</div>" +
      "</div>" +
      "</div>" +
      "</div>"
    );
  }

  function normalizeProducts(data) {
    if (!data) return [];
    if (Array.isArray(data)) return data;
    if (Array.isArray(data.data)) return data.data;
    if (Array.isArray(data.products)) return data.products;
    if (Array.isArray(data.assignedProducts)) return data.assignedProducts.map(function (row) {
      return row.product || row;
    });
    return [];
  }

  async function fetchMicrositeProducts(microsite) {
    var base = window.MICROSITE_API_BASE || window.domain || "";
    var id = microsite && microsite.id ? String(microsite.id) : "";
    var endpoints = [];

    if (id) {
      endpoints.push(base + "/api/microsite-public/products?microsite_id=" + encodeURIComponent(id));
      endpoints.push(base + "/api/microsite-public/by-id?microsite_id=" + encodeURIComponent(id));
    }
    endpoints.push(base + "/api/product/getproduct");

    for (var i = 0; i < endpoints.length; i++) {
      try {
        var res = await fetch(endpoints[i]);
        if (!res.ok) continue;
        var json = await res.json();
        var list = normalizeProducts(json);
        if (endpoints[i].indexOf("by-id") !== -1 && list.length === 0 && json) {
          list = normalizeProducts(json.products || json.assignedProducts);
        }
        if (list.length > 0) return list;
      } catch (e) {
        /* try next */
      }
    }
    return [];
  }

  function renderFeaturedProducts(products) {
    var row = document.getElementById("featured-products-row");
    if (!row) return;

    if (window.jQuery && row.classList.contains("owl-loaded")) {
      window.jQuery(row)
        .trigger("destroy.owl.carousel")
        .removeClass("owl-carousel owl-loaded owl-hidden owl-simple");
      window.jQuery(row).find(".owl-stage-outer").children().unwrap();
    }

    row.classList.remove("owl-carousel", "owl-simple", "carousel-with-shadow");
    row.innerHTML = "";

    if (!products.length) {
      row.innerHTML =
        '<p class="col-12 text-center py-5">No products available for this microsite.</p>';
      return;
    }

    products.slice(0, 12).forEach(function (p) {
      if (p && (p.id || p.productId)) {
        if (!p.id && p.productId) p.id = p.productId;
        row.innerHTML += getProductCardHTML(p);
      }
    });
  }

  async function initProducts(microsite) {
    var products = await fetchMicrositeProducts(microsite);
    renderFeaturedProducts(products);
  }

  document.addEventListener("ms:ready", function (e) {
    initProducts(e.detail);
  });

  document.addEventListener("DOMContentLoaded", function () {
    if (window.MS_MICROSITE_DATA) {
      initProducts(window.MS_MICROSITE_DATA);
    }
  });
})();
