using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace firstproject.Models.DatabaseLayer
{
    // =========================================
    // INTERFACE
    // =========================================

    public partial interface IDatabaseLayer
    {
        Task<List<CartItemModel>> GetCart(
            int? userId,
            string? ipAddress
        );

        Task<string> AddToCart(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null
        );

        Task<string> AddMultipleToCart(
            int? userId,
            string? ipAddress,
            List<int> productIds
        );

        Task<string> UpdateCartQuantity(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null,
            int change = 1
        );

        Task MergeGuestCart(
            int userId,
            string ipAddress
        );

        Task<IActionResult> DeleteCartItem(
            int id
        );

        Task<IActionResult> ClearCart(
            int? userId,
            string? ipAddress
        );
    }

    // =========================================
    // DATABASE LAYER
    // =========================================

    public partial class DatabaseLayer : IDatabaseLayer
    {
        // =========================================
        // GET CART
        // =========================================

        public async Task<List<CartItemModel>> GetCart(
            int? userId,
            string? ipAddress
        )
        {
            var list = new List<CartItemModel>();

            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            string where = userId.HasValue
                ? "c.userid=@uid"
                : "c.ipaddress=@ip";

            string query = $@"
SELECT
    c.id,
    c.userid,
    c.ipaddress,
    c.productid,
    c.variantids,
    c.quantity,
    c.createdat,

    p.productname,
    p.image,

    COALESCE(
        p.discountprice,
        p.price,
        0
    ) AS productprice

FROM addtocart c

LEFT JOIN product p
ON p.id = c.productid

WHERE {where}

ORDER BY c.createdat DESC
";

            using var cmd =
                new NpgsqlCommand(query, con);

            if (userId.HasValue)
            {
                cmd.Parameters.AddWithValue(
                    "@uid",
                    userId.Value
                );
            }
            else
            {
                cmd.Parameters.AddWithValue(
                    "@ip",
                    ipAddress ?? ""
                );
            }

            using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var item = new CartItemModel
                {
                    id = reader.GetInt32(0),

                    userid = reader.IsDBNull(1)
                        ? null
                        : reader.GetInt32(1),

                    ipaddress = reader.IsDBNull(2)
                        ? null
                        : reader.GetString(2),

                    productid = reader.IsDBNull(3)
                        ? null
                        : reader.GetInt32(3),

                    variantids = reader.IsDBNull(4)
                        ? null
                        : (int[])reader.GetValue(4),

                    quantity = reader.GetInt32(5),

                    createdat = reader.IsDBNull(6)
                        ? null
                        : reader.GetDateTime(6),

                    Name = reader.IsDBNull(7)
                        ? null
                        : reader.GetString(7),

                    Image = reader.IsDBNull(8)
                        ? null
                        : reader.GetString(8),

                    Price = reader.IsDBNull(9)
                        ? 0
                        : reader.GetDecimal(9),

                    Variants =
                        new List<VariantCartModel>()
                };

                list.Add(item);
            }

            reader.Close();

            // =========================================
            // LOAD VARIANTS
            // =========================================

            foreach (var item in list)
            {
                decimal finalPrice = 0;

                // =====================================
                // ONLY PRODUCT
                // =====================================

                if (
                    item.variantids == null
                    || !item.variantids.Any()
                )
                {
                    finalPrice = item.Price;

                    item.totalprice =
                        finalPrice * item.quantity;

                    continue;
                }

                // =====================================
                // VARIANT EXISTS
                // =====================================

                foreach (var variantId in item.variantids)
                {
                    var variantCmd =
                        new NpgsqlCommand(@"
SELECT
    id,
    variantname,
    sizeid,
    colorid,

    COALESCE(
        discountprice,
        price,
        0
    ) AS finalprice,

    image,
    imagegallery

FROM variant

WHERE id=@id
", con);

                    variantCmd.Parameters.AddWithValue(
                        "@id",
                        variantId
                    );

                    using var variantReader =
                        await variantCmd.ExecuteReaderAsync();

                    while (
                        await variantReader.ReadAsync()
                    )
                    {
                        var variant =
                            new VariantCartModel
                            {
                                id =
                                    variantReader.GetInt32(0),

                                variantname =
                                    variantReader.IsDBNull(1)
                                    ? null
                                    : variantReader.GetString(1),

                                sizeid =
                                    variantReader.IsDBNull(2)
                                    ? null
                                    : (int[])variantReader.GetValue(2),

                                colorid =
                                    variantReader.IsDBNull(3)
                                    ? null
                                    : (int[])variantReader.GetValue(3),

                                price =
                                    variantReader.IsDBNull(4)
                                    ? 0
                                    : variantReader.GetDecimal(4),

                                image =
                                    variantReader.IsDBNull(5)
                                    ? null
                                    : variantReader.GetString(5),

                                imagegallery =
                                    variantReader.IsDBNull(6)
                                    ? null
                                    : (string[])variantReader.GetValue(6)
                            };

                        item.Variants!.Add(
                            variant
                        );

                        // ✅ ONLY VARIANT PRICE

                        finalPrice =
                            variant.price;
                    }

                    variantReader.Close();
                }

                item.Price = finalPrice;

                item.totalprice =
                    finalPrice * item.quantity;
            }

            return list;
        }

        // =========================================
        // ADD TO CART
        // =========================================

        public async Task<string> AddToCart(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            // =====================================
            // PRODUCT ADD
            // =====================================

            if (productId.HasValue)
            {
                var check =
                    new NpgsqlCommand(@"
SELECT id
FROM addtocart
WHERE productid=@pid
AND variantids IS NULL
AND (userid=@uid OR ipaddress=@ip)
", con);

                check.Parameters.AddWithValue(
                    "@pid",
                    productId.Value
                );

                check.Parameters.AddWithValue(
                    "@uid",
                    (object?)userId
                    ?? DBNull.Value
                );

                check.Parameters.AddWithValue(
                    "@ip",
                    (object?)ipAddress
                    ?? DBNull.Value
                );

                var exists =
                    await check.ExecuteScalarAsync();

                // ✅ UPDATE PRODUCT QTY

                if (exists != null)
                {
                    var update =
                        new NpgsqlCommand(@"
UPDATE addtocart

SET quantity = quantity + 1

WHERE id=@id
", con);

                    update.Parameters.AddWithValue(
                        "@id",
                        Convert.ToInt32(exists)
                    );

                    await update.ExecuteNonQueryAsync();

                    return "QuantityUpdated";
                }

                // ✅ INSERT PRODUCT

                var insert =
                    new NpgsqlCommand(@"
INSERT INTO addtocart
(
    userid,
    ipaddress,
    productid,
    variantids,
    quantity,
    createdat
)
VALUES
(
    @uid,
    @ip,
    @pid,
    NULL,
    1,
    NOW()
)
", con);

                insert.Parameters.AddWithValue(
                    "@uid",
                    (object?)userId
                    ?? DBNull.Value
                );

                insert.Parameters.AddWithValue(
                    "@ip",
                    (object?)ipAddress
                    ?? DBNull.Value
                );

                insert.Parameters.AddWithValue(
                    "@pid",
                    productId.Value
                );

                await insert.ExecuteNonQueryAsync();

                return "Success";
            }

            // =====================================
            // VARIANT ADD
            // =====================================

            if (variantId.HasValue)
            {
                // ✅ GET PRODUCT ID

                var productCmd =
                    new NpgsqlCommand(@"
SELECT productid
FROM variant
WHERE id=@vid
", con);

                productCmd.Parameters.AddWithValue(
                    "@vid",
                    variantId.Value
                );

                var productResult =
                    await productCmd.ExecuteScalarAsync();

                if (productResult == null)
                    return "VariantNotFound";

                int productIdFromVariant =
                    Convert.ToInt32(productResult);

                // =====================================
                // CHECK PRODUCT ROW
                // =====================================

                int? cartId = null;

                int[] existingVariants =
                    Array.Empty<int>();

                var check =
                    new NpgsqlCommand(@"
SELECT
    id,
    variantids

FROM addtocart

WHERE productid=@pid
AND (userid=@uid OR ipaddress=@ip)

LIMIT 1
", con);

                check.Parameters.AddWithValue(
                    "@pid",
                    productIdFromVariant
                );

                check.Parameters.AddWithValue(
                    "@uid",
                    (object?)userId
                    ?? DBNull.Value
                );

                check.Parameters.AddWithValue(
                    "@ip",
                    (object?)ipAddress
                    ?? DBNull.Value
                );

                using (
                    var reader =
                        await check.ExecuteReaderAsync()
                )
                {
                    if (await reader.ReadAsync())
                    {
                        cartId =
                            reader.GetInt32(0);

                        existingVariants =
                            reader.IsDBNull(1)
                            ? Array.Empty<int>()
                            : (int[])reader.GetValue(1);
                    }
                }

                // =====================================
                // UPDATE EXISTING
                // =====================================

                if (cartId.HasValue)
                {
                    // ✅ already exists

                    if (
                        existingVariants.Contains(
                            variantId.Value
                        )
                    )
                    {
                        return "AlreadyExists";
                    }

                    // ✅ append variant

                    var updatedVariants =
                        existingVariants
                        .Append(variantId.Value)
                        .Distinct()
                        .ToArray();

                    // ✅ quantity = total variants

                    int quantity =
                        updatedVariants.Length;

                    var update =
                        new NpgsqlCommand(@"
UPDATE addtocart

SET
    variantids=@variants,
    quantity=@qty

WHERE id=@id
", con);

                    update.Parameters.AddWithValue(
                        "@variants",
                        updatedVariants
                    );

                    update.Parameters.AddWithValue(
                        "@qty",
                        quantity
                    );

                    update.Parameters.AddWithValue(
                        "@id",
                        cartId.Value
                    );

                    await update.ExecuteNonQueryAsync();

                    return "VariantAdded";
                }

                // =====================================
                // INSERT NEW
                // =====================================

                var insert =
                    new NpgsqlCommand(@"
INSERT INTO addtocart
(
    userid,
    ipaddress,
    productid,
    variantids,
    quantity,
    createdat
)
VALUES
(
    @uid,
    @ip,
    @pid,
    @variants,
    1,
    NOW()
)
", con);

                insert.Parameters.AddWithValue(
                    "@uid",
                    (object?)userId
                    ?? DBNull.Value
                );

                insert.Parameters.AddWithValue(
                    "@ip",
                    (object?)ipAddress
                    ?? DBNull.Value
                );

                insert.Parameters.AddWithValue(
                    "@pid",
                    productIdFromVariant
                );

                insert.Parameters.AddWithValue(
                    "@variants",
                    new int[]
                    {
                variantId.Value
                    }
                );

                await insert.ExecuteNonQueryAsync();

                return "VariantAdded";
            }

            return "InvalidRequest";
        }

        // =========================================
        // ADD MULTIPLE
        // =========================================

        public async Task<string> AddMultipleToCart(
            int? userId,
            string? ipAddress,
            List<int> productIds
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            foreach (var productId in productIds)
            {
                // =====================================
                // GET ALL VARIANTS
                // =====================================

                var variantIds =
                    new List<int>();

                var variantCmd =
                    new NpgsqlCommand(@"
SELECT id
FROM variant
WHERE productid=@pid
", con);

                variantCmd.Parameters.AddWithValue(
                    "@pid",
                    productId
                );

                using (
                    var variantReader =
                        await variantCmd.ExecuteReaderAsync()
                )
                {
                    while (
                        await variantReader.ReadAsync()
                    )
                    {
                        variantIds.Add(
                            variantReader.GetInt32(0)
                        );
                    }
                }

                // =====================================
                // PRODUCT HAS VARIANTS
                // =====================================

                if (variantIds.Any())
                {
                    int? cartId = null;

                    int[] existingVariants =
                        Array.Empty<int>();

                    // =====================================
                    // CHECK EXISTING
                    // =====================================

                    var check =
                        new NpgsqlCommand(@"
SELECT id, variantids
FROM addtocart
WHERE productid=@pid
AND (userid=@uid OR ipaddress=@ip)
LIMIT 1
", con);

                    check.Parameters.AddWithValue(
                        "@pid",
                        productId
                    );

                    check.Parameters.AddWithValue(
                        "@uid",
                        (object?)userId
                        ?? DBNull.Value
                    );

                    check.Parameters.AddWithValue(
                        "@ip",
                        (object?)ipAddress
                        ?? DBNull.Value
                    );

                    using (
                        var checkReader =
                            await check.ExecuteReaderAsync()
                    )
                    {
                        if (await checkReader.ReadAsync())
                        {
                            cartId =
                                checkReader.GetInt32(0);

                            existingVariants =
                                checkReader.IsDBNull(1)
                                ? Array.Empty<int>()
                                : (int[])checkReader.GetValue(1);
                        }
                    }

                    // =====================================
                    // UPDATE
                    // =====================================

                    if (cartId.HasValue)
                    {
                        var updatedVariants =
                            existingVariants
                            .Union(variantIds)
                            .Distinct()
                            .ToArray();

                        int quantity =
                            updatedVariants.Count();

                        var update =
                            new NpgsqlCommand(@"
UPDATE addtocart

SET
    variantids=@variants,
    quantity=@qty

WHERE id=@id
", con);

                        update.Parameters.AddWithValue(
                            "@variants",
                            updatedVariants
                        );

                        update.Parameters.AddWithValue(
                            "@qty",
                            quantity
                        );

                        update.Parameters.AddWithValue(
                            "@id",
                            cartId.Value
                        );

                        await update.ExecuteNonQueryAsync();
                    }

                    // =====================================
                    // INSERT
                    // =====================================

                    else
                    {
                        int quantity =
                            variantIds.Count();

                        var insert =
                            new NpgsqlCommand(@"
INSERT INTO addtocart
(
    userid,
    ipaddress,
    productid,
    variantids,
    quantity,
    createdat
)
VALUES
(
    @uid,
    @ip,
    @pid,
    @variants,
    @qty,
    NOW()
)
", con);

                        insert.Parameters.AddWithValue(
                            "@uid",
                            (object?)userId
                            ?? DBNull.Value
                        );

                        insert.Parameters.AddWithValue(
                            "@ip",
                            (object?)ipAddress
                            ?? DBNull.Value
                        );

                        insert.Parameters.AddWithValue(
                            "@pid",
                            productId
                        );

                        insert.Parameters.AddWithValue(
                            "@variants",
                            variantIds.ToArray()
                        );

                        insert.Parameters.AddWithValue(
                            "@qty",
                            quantity
                        );

                        await insert.ExecuteNonQueryAsync();
                    }
                }

                // =====================================
                // PRODUCT ONLY
                // =====================================

                else
                {
                    await AddToCart(
                        userId,
                        ipAddress,
                        productId,
                        null
                    );
                }
            }

            return "Success";
        }

        // =========================================
        // UPDATE QUANTITY
        // =========================================

        public async Task<string> UpdateCartQuantity(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null,
            int change = 1
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            string condition =
                userId.HasValue
                ? "userid=@uid"
                : "ipaddress=@ip";

            // =====================================
            // PRODUCT QUANTITY
            // =====================================

            if (
                productId.HasValue
                && !variantId.HasValue
            )
            {
                var cmd =
                    new NpgsqlCommand($@"
UPDATE addtocart

SET quantity =
CASE
    WHEN quantity + @change <= 1
    THEN 1

    ELSE quantity + @change
END

WHERE productid=@pid
AND variantids IS NULL
AND {condition}
", con);

                cmd.Parameters.AddWithValue(
                    "@change",
                    change
                );

                cmd.Parameters.AddWithValue(
                    "@pid",
                    productId.Value
                );

                if (userId.HasValue)
                {
                    cmd.Parameters.AddWithValue(
                        "@uid",
                        userId.Value
                    );
                }
                else
                {
                    cmd.Parameters.AddWithValue(
                        "@ip",
                        ipAddress ?? ""
                    );
                }

                int rows =
                    await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? "QuantityUpdated"
                    : "CartNotFound";
            }

            // =====================================
            // VARIANT QUANTITY
            // =====================================

            if (variantId.HasValue)
            {
                int? cartId = null;

                int[] existingVariants =
                    Array.Empty<int>();

                // =====================================
                // FIND CART
                // =====================================

                var check =
                    new NpgsqlCommand($@"
SELECT
    id,
    variantids

FROM addtocart

WHERE productid IS NOT NULL
AND @vid = ANY(variantids)
AND {condition}

LIMIT 1
", con);

                check.Parameters.AddWithValue(
                    "@vid",
                    variantId.Value
                );

                if (userId.HasValue)
                {
                    check.Parameters.AddWithValue(
                        "@uid",
                        userId.Value
                    );
                }
                else
                {
                    check.Parameters.AddWithValue(
                        "@ip",
                        ipAddress ?? ""
                    );
                }

                using (
                    var reader =
                        await check.ExecuteReaderAsync()
                )
                {
                    if (await reader.ReadAsync())
                    {
                        cartId =
                            reader.GetInt32(0);

                        existingVariants =
                            reader.IsDBNull(1)
                            ? Array.Empty<int>()
                            : (int[])reader.GetValue(1);
                    }
                }

                if (!cartId.HasValue)
                {
                    return "CartNotFound";
                }

                // =====================================
                // INCREMENT
                // =====================================

                if (change == 1)
                {
                    // ✅ duplicate variant add

                    var updatedVariants =
                        existingVariants
                        .ToList();

                    updatedVariants.Add(
                        variantId.Value
                    );

                    // ✅ quantity sync

                    int quantity =
                        updatedVariants.Count;

                    var update =
                        new NpgsqlCommand(@"
UPDATE addtocart

SET
    variantids=@variants,
    quantity=@qty

WHERE id=@id
", con);

                    update.Parameters.AddWithValue(
                        "@variants",
                        updatedVariants.ToArray()
                    );

                    update.Parameters.AddWithValue(
                        "@qty",
                        quantity
                    );

                    update.Parameters.AddWithValue(
                        "@id",
                        cartId.Value
                    );

                    await update.ExecuteNonQueryAsync();

                    return "QuantityIncreased";
                }

                // =====================================
                // DECREMENT
                // =====================================

                if (change == -1)
                {
                    var updatedVariants =
                        existingVariants
                        .ToList();

                    // ✅ remove only one variant

                    var index =
                        updatedVariants.IndexOf(
                            variantId.Value
                        );

                    if (index >= 0)
                    {
                        updatedVariants.RemoveAt(index);
                    }

                    // ✅ delete cart if empty

                    if (!updatedVariants.Any())
                    {
                        var delete =
                            new NpgsqlCommand(@"
DELETE FROM addtocart
WHERE id=@id
", con);

                        delete.Parameters.AddWithValue(
                            "@id",
                            cartId.Value
                        );

                        await delete.ExecuteNonQueryAsync();

                        return "CartDeleted";
                    }

                    int quantity =
                        updatedVariants.Count;

                    var update =
                        new NpgsqlCommand(@"
UPDATE addtocart

SET
    variantids=@variants,
    quantity=@qty

WHERE id=@id
", con);

                    update.Parameters.AddWithValue(
                        "@variants",
                        updatedVariants.ToArray()
                    );

                    update.Parameters.AddWithValue(
                        "@qty",
                        quantity
                    );

                    update.Parameters.AddWithValue(
                        "@id",
                        cartId.Value
                    );

                    await update.ExecuteNonQueryAsync();

                    return "QuantityDecreased";
                }
            }

            return "InvalidRequest";
        }

        // =========================================
        // MERGE GUEST CART
        // =========================================

        public async Task MergeGuestCart(
            int userId,
            string ipAddress
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            var cmd =
                new NpgsqlCommand(@"
UPDATE addtocart
SET userid=@uid
WHERE ipaddress=@ip
", con);

            cmd.Parameters.AddWithValue(
                "@uid",
                userId
            );

            cmd.Parameters.AddWithValue(
                "@ip",
                ipAddress
            );

            await cmd.ExecuteNonQueryAsync();
        }

        // =========================================
        // DELETE ITEM
        // =========================================

        public async Task<IActionResult> DeleteCartItem(
            int id
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            var cmd =
                new NpgsqlCommand(
                    "DELETE FROM addtocart WHERE id=@id",
                    con
                );

            cmd.Parameters.AddWithValue(
                "@id",
                id
            );

            int rows =
                await cmd.ExecuteNonQueryAsync();

            return rows > 0
                ? new OkObjectResult(new
                {
                    status = true,
                    message = "Deleted"
                })
                : new NotFoundObjectResult(new
                {
                    status = false,
                    message = "Item not found"
                });
        }

        // =========================================
        // CLEAR CART
        // =========================================

        public async Task<IActionResult> ClearCart(
            int? userId,
            string? ipAddress
        )
        {
            using var con =
                new NpgsqlConnection(DbConnection);

            await con.OpenAsync();

            var cmd =
                new NpgsqlCommand(@"
DELETE FROM addtocart
WHERE userid=@uid
OR ipaddress=@ip
", con);

            cmd.Parameters.AddWithValue(
                "@uid",
                (object?)userId
                ?? DBNull.Value
            );

            cmd.Parameters.AddWithValue(
                "@ip",
                (object?)ipAddress
                ?? DBNull.Value
            );

            await cmd.ExecuteNonQueryAsync();

            return new OkObjectResult(new
            {
                status = true,
                message = "Cart cleared"
            });
        }
    }
}