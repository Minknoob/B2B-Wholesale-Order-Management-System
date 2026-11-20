using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace B2B.AgentPortal.Migrations
{
    /// <inheritdoc />
    public partial class InitialAgentPortalSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Agent",
                schema: "dbo",
                columns: table => new
                {
                    AgentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agent", x => x.AgentID);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                schema: "dbo",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.ItemID);
                    table.CheckConstraint("CK_Item_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_Item_StockQuantity", "[StockQuantity] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                schema: "dbo",
                columns: table => new
                {
                    OrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "(SYSDATETIME())"),
                    AgentID = table.Column<int>(type: "int", nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.OrderID);
                    table.CheckConstraint("CK_Order_OrderStatus", "[OrderStatus] IN (N'Pending',N'Confirmed',N'Shipped',N'Delivered',N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Order_Agent",
                        column: x => x.AgentID,
                        principalSchema: "dbo",
                        principalTable: "Agent",
                        principalColumn: "AgentID");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgentID = table.Column<int>(type: "int", nullable: true),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_FailedLoginCount", "[FailedLoginCount] >= 0");
                    table.CheckConstraint("CK_Users_Role", "[Role] IN (N'Admin',N'Agent')");
                    table.CheckConstraint("CK_Users_RoleAgent", "([Role] = N'Admin' AND [AgentID] IS NULL) OR ([Role] = N'Agent' AND [AgentID] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Users_Agent",
                        column: x => x.AgentID,
                        principalSchema: "dbo",
                        principalTable: "Agent",
                        principalColumn: "AgentID");
                });

            migrationBuilder.CreateTable(
                name: "OrderDetail",
                schema: "dbo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetail", x => x.ID);
                    table.CheckConstraint("CK_OrderDetail_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_OrderDetail_UnitAmount", "[UnitAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderDetail_Item",
                        column: x => x.ItemID,
                        principalSchema: "dbo",
                        principalTable: "Item",
                        principalColumn: "ItemID");
                    table.ForeignKey(
                        name: "FK_OrderDetail_Order",
                        column: x => x.OrderID,
                        principalSchema: "dbo",
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_AgentID",
                schema: "dbo",
                table: "Order",
                column: "AgentID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_ItemID",
                schema: "dbo",
                table: "OrderDetail",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_OrderID",
                schema: "dbo",
                table: "OrderDetail",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AgentID",
                schema: "dbo",
                table: "Users",
                column: "AgentID");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_UserName",
                schema: "dbo",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetail",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Item",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Order",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Agent",
                schema: "dbo");
        }
    }
}
