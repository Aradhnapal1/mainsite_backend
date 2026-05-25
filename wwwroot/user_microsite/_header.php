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
    <link rel="stylesheet" href="microsite-layout.css?v=20260524b">
    <link rel="stylesheet" href="microsite-products.css?v=20260525">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/izitoast/dist/css/iziToast.min.css">
    <script src="microsite-config.js"></script>
    <script src="_microsite-shell.js?v=20260523"></script>
</head>
<body data-ms-page="<?php echo htmlspecialchars($currentPage, ENT_QUOTES, 'UTF-8'); ?>">
<?php include __DIR__ . '/_ms-nav.html'; ?>
<main class="container pb-5">
