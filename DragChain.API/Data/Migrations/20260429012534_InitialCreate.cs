using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseModel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InnerHeight = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    InnerWidth = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    R1 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    R2 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    R3 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    R1Suffix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    R2Suffix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    R3Suffix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Lp1 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Lp2 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Lp3 = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    InnerArea = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SpanBase = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SpanSlope = table.Column<decimal>(type: "decimal(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeCatalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Diameter = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    BendMultiplier = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WzlCatalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Function = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Stroke = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InnerHeight = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    InnerWidth = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    OuterHeight = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    OuterWidth = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    MinRadius = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    RecRadius = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    ReservedK = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    BendLength = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    MountingH1 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InterferenceH2 = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InnerArea = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    AppPipes = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WzlCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeCatalog_BaseModel",
                table: "MeCatalog",
                column: "BaseModel");

            migrationBuilder.CreateIndex(
                name: "IX_PipeTypes_Name",
                table: "PipeTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PipeTypes_Type",
                table: "PipeTypes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_WzlCatalog_Function",
                table: "WzlCatalog",
                column: "Function");

            migrationBuilder.CreateIndex(
                name: "IX_WzlCatalog_Model",
                table: "WzlCatalog",
                column: "Model");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeCatalog");

            migrationBuilder.DropTable(
                name: "PipeTypes");

            migrationBuilder.DropTable(
                name: "WzlCatalog");
        }
    }
}
