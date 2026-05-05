using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrunkingCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrunkingCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Width = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Height = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    InnerWidth = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    InnerHeight = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    CrossSection = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Material = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrunkingCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrunkingCatalog_Model",
                table: "TrunkingCatalog",
                column: "Model");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrunkingCatalog");
        }
    }
}
