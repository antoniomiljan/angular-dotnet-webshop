using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStockWithInStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InStock",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Backfill from the real data before Stock is gone - a product with 0 on
            // hand becomes out of stock, everything else stays in stock.
            migrationBuilder.Sql("UPDATE \"Products\" SET \"InStock\" = (\"Stock\" > 0);");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE \"Products\" SET \"Stock\" = CASE WHEN \"InStock\" THEN 100 ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "InStock",
                table: "Products");
        }
    }
}
