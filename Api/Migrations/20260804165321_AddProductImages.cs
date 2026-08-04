using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            // Backfill before Products.ImageUrl is gone: each existing image becomes the
            // first (and only) image for its product.
            migrationBuilder.Sql(
                "INSERT INTO \"ProductImages\" (\"ImageUrl\", \"SortOrder\", \"ProductId\") " +
                "SELECT \"ImageUrl\", 0, \"Id\" FROM \"Products\" WHERE \"ImageUrl\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "text",
                nullable: true);

            // Lossy for products with more than one image - keeps only the first
            // (lowest SortOrder) as the restored scalar ImageUrl.
            migrationBuilder.Sql(
                "UPDATE \"Products\" p SET \"ImageUrl\" = (" +
                "SELECT pi.\"ImageUrl\" FROM \"ProductImages\" pi " +
                "WHERE pi.\"ProductId\" = p.\"Id\" ORDER BY pi.\"SortOrder\" LIMIT 1);");

            migrationBuilder.DropTable(
                name: "ProductImages");
        }
    }
}
