<?php
if (!isset($pageTitle)) {
    $pageTitle = 'Microsite';
}
if (!isset($currentPage)) {
    $currentPage = 'home';
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
    <title><?php echo htmlspecialchars($pageTitle, ENT_QUOTES, 'UTF-8'); ?></title>
    <meta name="description" content="Microsite page">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css">
    <link rel="stylesheet" href="../assets/css/style.css">
    <link rel="stylesheet" href="microsite-layout.css">
    <link rel="stylesheet" href="microsite-products.css">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/izitoast/dist/css/iziToast.min.css">
    <script src="microsite-config.js"></script>
    <script src="_microsite-shell.js"></script>
</head>
<body data-ms-page="<?php echo htmlspecialchars($currentPage, ENT_QUOTES, 'UTF-8'); ?>">
<header class="ms-header py-3 mb-4">
    <div class="container d-flex justify-content-between align-items-center flex-wrap gap-2">
        <a href="#" data-ms-nav="index" class="text-decoration-none d-flex align-items-center text-reset ms-nav-home">
            <img id="msLogo" class="ms-logo me-2" src="" alt="Logo" style="display:none;">
            <strong id="msSiteName">Microsite</strong>
        </a>
        <nav class="d-flex flex-wrap gap-2 gap-md-3 ms-header-nav">
            <a class="text-reset" href="#" data-ms-nav="index">Home</a>
            <a class="text-reset" href="#" data-ms-nav="contact">Contact</a>
            <a class="text-reset" href="#" data-ms-nav="login" id="msNavLogin">Login</a>
            <a class="text-reset" href="#" data-ms-nav="register" id="msNavRegister">Register</a>
            <a class="text-reset ms-cart-link" href="#" data-ms-nav="cart" title="Cart" aria-label="Cart">
                <i class="icon-shopping-cart"></i>
                <span class="ms-cart-badge" id="msCartBadge">0</span>
            </a>
            <a class="text-reset" href="#" data-ms-nav="checkout">Checkout</a>
            <a class="text-reset" href="#" data-ms-nav="order">Order</a>
        </nav>
    </div>
</header>
<main class="container pb-5">
