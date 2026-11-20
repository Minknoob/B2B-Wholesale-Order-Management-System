using B2B.AgentPortal.Data;
using B2B.AgentPortal.Models;
using B2B.AgentPortal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace B2B.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task PlaceOrder_EnoughStock_DeductsStockAndSavesOrder()
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setup = new B2BDbContext(options))
        {
            setup.Agents.Add(new Agent
            {
                AgentID = 1,
                AgentName = "Test Agent",
                Address = "Ho Chi Minh City",
                Phone = "0901234567"
            });

            setup.Items.Add(new Item
            {
                ItemID = 1,
                ItemName = "Rice",
                Size = "5 kg",
                Price = 95000m,
                StockQuantity = 10
            });

            await setup.SaveChangesAsync();
        }

        await using (var db = new B2BDbContext(options))
        {
            var service = new OrderService(
                db,
                NullLogger<OrderService>.Instance);

            var result = await service.PlaceOrderAsync(
                1,
                new[] { new OrderLineRequest(1, 3) });

            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.OrderId);
            Assert.Equal(285000m, result.TotalAmount);
        }

        await using var verify = new B2BDbContext(options);

        var item = await verify.Items.SingleAsync();
        var order = await verify.Orders.SingleAsync();
        var detail = await verify.OrderDetails.SingleAsync();

        Assert.Equal(7, item.StockQuantity);
        Assert.Equal(1, order.AgentID);
        Assert.Equal("Pending", order.OrderStatus);
        Assert.Equal(order.OrderID, detail.OrderID);
        Assert.Equal(1, detail.ItemID);
        Assert.Equal(3, detail.Quantity);
        Assert.Equal(95000m, detail.UnitAmount);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientStock_RejectsWithoutSaving()
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setup = new B2BDbContext(options))
        {
            setup.Agents.Add(new Agent
            {
                AgentID = 1,
                AgentName = "Test Agent",
                Address = "Ho Chi Minh City",
                Phone = "0901234567"
            });

            setup.Items.Add(new Item
            {
                ItemID = 1,
                ItemName = "Rice",
                Size = "5 kg",
                Price = 95000m,
                StockQuantity = 10
            });

            await setup.SaveChangesAsync();
        }

        await using (var db = new B2BDbContext(options))
        {
            var service = new OrderService(
                db,
                NullLogger<OrderService>.Instance);

            var result = await service.PlaceOrderAsync(
                1,
                new[] { new OrderLineRequest(1, 11) });

            Assert.False(result.Success);
            Assert.Equal(OrderErrorCode.InsufficientStock, result.ErrorCode);
            Assert.Null(result.OrderId);
        }

        await using var verify = new B2BDbContext(options);

        var item = await verify.Items.SingleAsync();

        Assert.Equal(10, item.StockQuantity);
        Assert.False(await verify.Orders.AnyAsync());
        Assert.False(await verify.OrderDetails.AnyAsync());
    }

    [Fact]
    public async Task PlaceOrder_MultipleItems_CalculatesTotalCorrectly()
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setup = new B2BDbContext(options))
        {
            setup.Agents.Add(new Agent
            {
                AgentID = 1,
                AgentName = "Test Agent",
                Address = "Ho Chi Minh City",
                Phone = "0901234567"
            });

            setup.Items.AddRange(
                new Item
                {
                    ItemID = 1,
                    ItemName = "Rice",
                    Size = "5 kg",
                    Price = 95000m,
                    StockQuantity = 10
                },
                new Item
                {
                    ItemID = 2,
                    ItemName = "Cooking Oil",
                    Size = "1 L",
                    Price = 42000m,
                    StockQuantity = 20
                });

            await setup.SaveChangesAsync();
        }

        await using (var db = new B2BDbContext(options))
        {
            var service = new OrderService(
                db,
                NullLogger<OrderService>.Instance);

            var result = await service.PlaceOrderAsync(
                1,
                new[]
                {
                new OrderLineRequest(1, 2),
                new OrderLineRequest(2, 3)
                });

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(316000m, result.TotalAmount);
            Assert.Equal(2, result.Lines.Count);
        }

        await using var verify = new B2BDbContext(options);

        var items = await verify.Items
            .OrderBy(i => i.ItemID)
            .ToListAsync();

        var details = await verify.OrderDetails.ToListAsync();

        Assert.Equal(8, items[0].StockQuantity);
        Assert.Equal(17, items[1].StockQuantity);
        Assert.Equal(2, details.Count);
        Assert.Equal(
            316000m,
            details.Sum(d => d.Quantity * d.UnitAmount));

        Assert.Equal(1, await verify.Orders.CountAsync());
    }

    [Fact]
    public async Task PlaceOrder_DuplicateItemExceedsStock_RejectsWithoutSaving()
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setup = new B2BDbContext(options))
        {
            setup.Agents.Add(new Agent
            {
                AgentID = 1,
                AgentName = "Test Agent",
                Address = "Ho Chi Minh City",
                Phone = "0901234567"
            });

            setup.Items.Add(new Item
            {
                ItemID = 1,
                ItemName = "Rice",
                Size = "5 kg",
                Price = 95000m,
                StockQuantity = 10
            });

            await setup.SaveChangesAsync();
        }

        await using (var db = new B2BDbContext(options))
        {
            var service = new OrderService(
                db,
                NullLogger<OrderService>.Instance);

            var result = await service.PlaceOrderAsync(
                1,
                new[]
                {
                new OrderLineRequest(1, 6),
                new OrderLineRequest(1, 5)
                });

            Assert.False(result.Success);
            Assert.Equal(OrderErrorCode.InsufficientStock, result.ErrorCode);
            Assert.Null(result.OrderId);
        }

        await using var verify = new B2BDbContext(options);

        var item = await verify.Items.SingleAsync();

        Assert.Equal(10, item.StockQuantity);
        Assert.False(await verify.Orders.AnyAsync());
        Assert.False(await verify.OrderDetails.AnyAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PlaceOrder_InvalidQuantity_RejectsWithoutSaving(int quantity)
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new B2BDbContext(options);

        var service = new OrderService(
            db,
            NullLogger<OrderService>.Instance);

        var result = await service.PlaceOrderAsync(
            1,
            new[] { new OrderLineRequest(1, quantity) });

        Assert.False(result.Success);
        Assert.Equal(OrderErrorCode.InvalidQuantity, result.ErrorCode);
        Assert.Null(result.OrderId);
        Assert.False(await db.Orders.AnyAsync());
        Assert.False(await db.OrderDetails.AnyAsync());
    }
}