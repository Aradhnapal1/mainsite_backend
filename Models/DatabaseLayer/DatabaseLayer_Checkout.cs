using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

namespace firstproject.Models.DatabaseLayer
{
    public partial interface IDatabaseLayer
    {
        Task<IActionResult> PlaceOrder(int userId, CheckoutRequestModel request, List<CartItemModel> items, decimal grandTotal);
        Task<IActionResult> GetMyOrders(int userId);
        Task<IActionResult> GetOrderDetail(int userId, int orderId);

        Task<IActionResult> GetAllOrders();
    }

    public partial class DatabaseLayer : IDatabaseLayer
    {
        // Get All orders
   

public async Task<IActionResult> GetAllOrders()
        {
            using var connection = new NpgsqlConnection(this.DbConnection);

            await connection.OpenAsync();

            var orders = new List<object>();

            using var cmd = new NpgsqlCommand(@"

        SELECT

            -- ORDER TABLE
            o.id,
            o.userid,

            o.first_name,
            o.last_name,
            o.email,
            o.mobile,
            o.pincode,
            o.address,
            o.city,
            o.state,
            o.country,

            o.total_items,
            o.grand_total,
            o.payment_method,
            o.order_status,
            o.created_at,

            -- USER TABLE
            u.firstname as user_firstname,
            u.lastname as user_lastname,
            u.email as user_email

        FROM orders o

        LEFT JOIN users u
            ON u.id = o.userid

        ORDER BY o.created_at DESC

    ", connection);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    // ====================================
                    // ORDER DETAIL
                    // ====================================

                    id = reader.GetInt32(
                        reader.GetOrdinal("id")
                    ),

                    userId = reader.GetInt32(
                        reader.GetOrdinal("userid")
                    ),

                    firstName = reader.GetString(
                        reader.GetOrdinal("first_name")
                    ),

                    lastName = reader.GetString(
                        reader.GetOrdinal("last_name")
                    ),

                    email = reader.GetString(
                        reader.GetOrdinal("email")
                    ),

                    mobile = reader.GetString(
                        reader.GetOrdinal("mobile")
                    ),

                    pincode = reader.GetString(
                        reader.GetOrdinal("pincode")
                    ),

                    address = reader.GetString(
                        reader.GetOrdinal("address")
                    ),

                    city = reader.GetString(
                        reader.GetOrdinal("city")
                    ),

                    state = reader.GetString(
                        reader.GetOrdinal("state")
                    ),

                    country = reader.GetString(
                        reader.GetOrdinal("country")
                    ),

                    totalItems = reader.GetInt32(
                        reader.GetOrdinal("total_items")
                    ),

                    grandTotal = reader.GetDecimal(
                        reader.GetOrdinal("grand_total")
                    ),

                    paymentMethod = reader.GetString(
                        reader.GetOrdinal("payment_method")
                    ),

                    orderStatus = reader.GetString(
                        reader.GetOrdinal("order_status")
                    ),

                    createdAt = reader.GetDateTime(
                        reader.GetOrdinal("created_at")
                    ),

                    // ====================================
                    // USER DETAIL
                    // ====================================

                    user = new
                    {
                        firstName =
                            reader["user_firstname"] == DBNull.Value
                            ? null
                            : reader["user_firstname"]?.ToString(),

                        lastName =
                            reader["user_lastname"] == DBNull.Value
                            ? null
                            : reader["user_lastname"]?.ToString(),

                        email =
                            reader["user_email"] == DBNull.Value
                            ? null
                            : reader["user_email"]?.ToString()
                    }
                });
            }

            return new OkObjectResult(new
            {
                status = true,
                totalOrders = orders.Count,
                data = orders
            });
        }




        public async Task<IActionResult> PlaceOrder(
            int userId,
            CheckoutRequestModel request,
            List<CartItemModel> items,
            decimal grandTotal
        )
        {
            using var connection =
                new NpgsqlConnection(this.DbConnection);

            await connection.OpenAsync();

            using var transaction =
                await connection.BeginTransactionAsync();

            try
            {
                // =====================================
                // CREATE ORDER
                // =====================================

                int orderId;

                using (
                    var cmd =
                        new NpgsqlCommand(@"
INSERT INTO orders
(
    userid,

    first_name,
    last_name,
    email,
    mobile,

    pincode,
    address,
    city,
    state,
    country,

    total_items,
    grand_total,

    payment_method
)

VALUES
(
    @UserId,

    @FirstName,
    @LastName,
    @Email,
    @Mobile,

    @Pincode,
    @Address,
    @City,
    @State,
    @Country,

    @TotalItems,
    @GrandTotal,

    @PaymentMethod
)

RETURNING id;
", connection, transaction)
                )
                {
                    cmd.Parameters.AddWithValue(
                        "@UserId",
                        userId
                    );

                    cmd.Parameters.AddWithValue(
                        "@FirstName",
                        request.FirstName
                    );

                    cmd.Parameters.AddWithValue(
                        "@LastName",
                        request.LastName
                    );

                    cmd.Parameters.AddWithValue(
                        "@Email",
                        request.Email
                    );

                    cmd.Parameters.AddWithValue(
                        "@Mobile",
                        request.Mobile
                    );

                    cmd.Parameters.AddWithValue(
                        "@Pincode",
                        request.Pincode
                    );

                    cmd.Parameters.AddWithValue(
                        "@Address",
                        request.Address
                    );

                    cmd.Parameters.AddWithValue(
                        "@City",
                        request.City
                    );

                    cmd.Parameters.AddWithValue(
                        "@State",
                        request.State
                    );

                    cmd.Parameters.AddWithValue(
                        "@Country",
                        request.Country ?? "India"
                    );

                    cmd.Parameters.AddWithValue(
                        "@TotalItems",
                        items.Count
                    );

                    cmd.Parameters.AddWithValue(
                        "@GrandTotal",
                        grandTotal
                    );

                    cmd.Parameters.AddWithValue(
                        "@PaymentMethod",
                        request.PaymentMethod ?? "COD"
                    );

                    orderId =
                        Convert.ToInt32(
                            await cmd.ExecuteScalarAsync()
                        );
                }

                // =====================================
                // INSERT ORDER ITEMS
                // =====================================

                foreach (var item in items)
                {
                    int? categoryId = null;
                    int? subCategoryId = null;
                    int? childCategoryId = null;

                    int[]? sizeIds = null;
                    int[]? colorIds = null;

                    // =================================
                    // PRODUCT DATA
                    // =================================

                    var productCmd =
                        new NpgsqlCommand(@"
SELECT

    categoryid,
    subcategoryid,
    childcategoryid,

    sizeids,
    colorids

FROM product

WHERE id=@pid
", connection, transaction);

                    productCmd.Parameters.AddWithValue(
                        "@pid",
                        item.productid!
                    );

                    using (
                        var reader =
                            await productCmd.ExecuteReaderAsync()
                    )
                    {
                        if (await reader.ReadAsync())
                        {
                            categoryId =
                                reader["categoryid"] as int?;

                            subCategoryId =
                                reader["subcategoryid"] as int?;

                            childCategoryId =
                                reader["childcategoryid"] as int?;

                            sizeIds =
                                reader["sizeids"]
                                as int[];

                            colorIds =
                                reader["colorids"]
                                as int[];
                        }
                    }

                    // =================================
                    // INSERT ORDER ITEM
                    // =================================

                    using var itemCmd =
                        new NpgsqlCommand(@"
INSERT INTO order_items
(
    order_id,

    product_id,
    variant_ids,

    product_name,
    image,

    price,
    discount_price,

    quantity,
    total_price,

    category_id,
    subcategory_id,
    childcategory_id,

    size_ids,
    color_ids
)

VALUES
(
    @OrderId,

    @ProductId,
    @VariantIds,

    @ProductName,
    @Image,

    @Price,
    @DiscountPrice,

    @Quantity,
    @TotalPrice,

    @CategoryId,
    @SubCategoryId,
    @ChildCategoryId,

    @SizeIds,
    @ColorIds
)
", connection, transaction);

                    itemCmd.Parameters.AddWithValue(
                        "@OrderId",
                        orderId
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@ProductId",
                        item.productid!
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@VariantIds",
                        (object?)item.variantids
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@ProductName",
                        item.Name ?? ""
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@Image",
                        (object?)item.Image
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@Price",
                        item.Price
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@DiscountPrice",
                        item.Price
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@Quantity",
                        item.quantity
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@TotalPrice",
                        item.totalprice
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@CategoryId",
                        (object?)categoryId
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@SubCategoryId",
                        (object?)subCategoryId
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@ChildCategoryId",
                        (object?)childCategoryId
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@SizeIds",
                        (object?)sizeIds
                        ?? DBNull.Value
                    );

                    itemCmd.Parameters.AddWithValue(
                        "@ColorIds",
                        (object?)colorIds
                        ?? DBNull.Value
                    );

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // =====================================
                // CLEAR CART
                // =====================================

                using (
                    var clearCmd =
                        new NpgsqlCommand(@"
DELETE FROM addtocart
WHERE userid=@uid
", connection, transaction)
                )
                {
                    clearCmd.Parameters.AddWithValue(
                        "@uid",
                        userId
                    );

                    await clearCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new OkObjectResult(new
                {
                    status = true,

                    message =
                        "Order placed successfully",

                    orderId = orderId,

                    totalItems =
                        items.Count,

                    grandTotal =
                        grandTotal
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new ObjectResult(new
                {
                    status = false,
                    error = ex.Message
                })
                {
                    StatusCode = 500
                };
            }
        }

        // ✅ GET MY ORDERS
        public async Task<IActionResult> GetMyOrders(int userId)
        {
            using var connection = new NpgsqlConnection(this.DbConnection);
            await connection.OpenAsync();

            var orders = new List<object>();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    id, userid, first_name, last_name, email, mobile,
                    pincode, address, city, state, country,
                    total_items, grand_total, payment_method, order_status, created_at
                FROM orders
                WHERE userid = @UserId
                ORDER BY created_at DESC;
            ", connection);

            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    id = reader.GetInt32("id"),
                    firstName = reader.GetString("first_name"),
                    lastName = reader.GetString("last_name"),
                    email = reader.GetString("email"),
                    mobile = reader.GetString("mobile"),
                    pincode = reader.GetString("pincode"),
                    address = reader.GetString("address"),
                    city = reader.GetString("city"),
                    state = reader.GetString("state"),
                    country = reader.GetString("country"),
                    totalItems = reader.GetInt32("total_items"),
                    grandTotal = reader.GetDecimal("grand_total"),
                    paymentMethod = reader.GetString("payment_method"),
                    orderStatus = reader.GetString("order_status"),
                    createdAt = reader.GetDateTime("created_at")
                });
            }

            return new OkObjectResult(new
            {
                status = true,
                totalOrders = orders.Count,
                data = orders
            });
        }

        // ✅ GET ORDER DETAIL
       public async Task<IActionResult> GetOrderDetail(
    int userId,
    int orderId
)
{
    using var connection =
        new NpgsqlConnection(this.DbConnection);

    await connection.OpenAsync();

    object? order = null;

    // =====================================
    // ORDER
    // =====================================

    using (
        var cmd =
            new NpgsqlCommand(@"
SELECT *

FROM orders

WHERE id=@oid
AND userid=@uid
", connection)
    )
    {
        cmd.Parameters.AddWithValue(
            "@oid",
            orderId
        );

        cmd.Parameters.AddWithValue(
            "@uid",
            userId
        );

        using var reader =
            await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            order = new
            {
                id = reader.GetInt32(
                    reader.GetOrdinal("id")
                ),

                grandTotal =
                    reader.GetDecimal(
                        reader.GetOrdinal(
                            "grand_total"
                        )
                    ),

                orderStatus =
                    reader["order_status"]
                    ?.ToString(),

                paymentMethod =
                    reader["payment_method"]
                    ?.ToString(),

                createdAt =
                    reader.GetDateTime(
                        reader.GetOrdinal(
                            "created_at"
                        )
                    )
            };
        }
    }

    if (order == null)
    {
        return new NotFoundObjectResult(new
        {
            status = false,
            message = "Order not found"
        });
    }

    // =====================================
    // ORDER ITEMS
    // =====================================

    var items =
        new List<object>();

    using (
        var itemCmd =
            new NpgsqlCommand(@"
SELECT

    oi.*,

    b.brandname,

    c.""Name"" AS categoryname,

    sc.""SubCategoryName""
    AS subcategoryname,

    cc.""ChildCategoryName""
    AS childcategoryname,

(
    SELECT json_agg(
        json_build_object(

            'id',
            v.id,

            'variantname',
            v.variantname,

            'price',
            v.price,

            'discountprice',
            v.discountprice,

            'image',
            v.image,

            -- SIZE NAMES

            'sizes',
            (
                SELECT array_agg(s.size_name)

                FROM sizes s

                WHERE s.id = ANY(v.sizeid)
            ),

            -- COLOR NAMES

            'colors',
            (
                SELECT array_agg(c.colorname)

                FROM color c

                WHERE c.id = ANY(v.colorid)
            )
        )
    )

    FROM variant v

    WHERE v.id = ANY(oi.variant_ids)

) AS variants,

    (
        SELECT array_agg(s.size_name)
        FROM sizes s
        WHERE s.id = ANY(oi.size_ids)
    ) AS sizenames,

    (
        SELECT array_agg(cl.colorname)
        FROM color cl
        WHERE cl.id = ANY(oi.color_ids)
    ) AS colornames

FROM order_items oi

JOIN product p
ON p.id = oi.product_id

LEFT JOIN brand b
ON b.id = p.brandid

LEFT JOIN category c
ON c.""Id"" = p.categoryid

LEFT JOIN subcategory sc
ON sc.""Id"" = p.subcategoryid

LEFT JOIN childcategory cc
ON cc.""Id"" = p.childcategoryid

WHERE oi.order_id=@oid

ORDER BY oi.id ASC
", connection)
    )
    {
        itemCmd.Parameters.AddWithValue(
            "@oid",
            orderId
        );

        using var itemReader =
            await itemCmd.ExecuteReaderAsync();

        while (
            await itemReader.ReadAsync()
        )
        {
            items.Add(new
            {
                id = itemReader.GetInt32(
                    itemReader.GetOrdinal("id")
                ),

                productId =
                    itemReader.GetInt32(
                        itemReader.GetOrdinal(
                            "product_id"
                        )
                    ),

                variantIds =
                    itemReader["variant_ids"]
                    as int[],

                productName =
                    itemReader["product_name"]
                    ?.ToString(),

                image =
                    itemReader["image"]
                    ?.ToString(),

                price =
                    itemReader.GetDecimal(
                        itemReader.GetOrdinal(
                            "price"
                        )
                    ),

                discountPrice =
                    itemReader["discount_price"]
                    as decimal?,

                quantity =
                    itemReader.GetInt32(
                        itemReader.GetOrdinal(
                            "quantity"
                        )
                    ),

                totalPrice =
                    itemReader.GetDecimal(
                        itemReader.GetOrdinal(
                            "total_price"
                        )
                    ),

                brandName =
                    itemReader["brandname"]
                    ?.ToString(),

                categoryName =
                    itemReader["categoryname"]
                    ?.ToString(),

                subCategoryName =
                    itemReader["subcategoryname"]
                    ?.ToString(),

                childCategoryName =
                    itemReader["childcategoryname"]
                    ?.ToString(),

                sizeNames =
                    itemReader["sizenames"]
                    as string[],

                colorNames =
                    itemReader["colornames"]
                    as string[],

                variants =
    itemReader["variants"] == DBNull.Value
    ? null
    : System.Text.Json.JsonSerializer.Deserialize<object>(
        itemReader["variants"].ToString()!
    )
            });
        }
    }

    return new OkObjectResult(new
    {
        status = true,
        order = order,
        orderItems = items
    });
}
    }
}