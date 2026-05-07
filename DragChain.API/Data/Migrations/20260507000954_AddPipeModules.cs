using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPipeModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PipeModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipeModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PipeModuleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PipeModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PipeTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Qty = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipeModuleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipeModuleItems_PipeModules_PipeModuleId",
                        column: x => x.PipeModuleId,
                        principalTable: "PipeModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PipeModuleItems_PipeTypes_PipeTypeId",
                        column: x => x.PipeTypeId,
                        principalTable: "PipeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PipeModuleItems_PipeModuleId",
                table: "PipeModuleItems",
                column: "PipeModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PipeModuleItems_PipeTypeId",
                table: "PipeModuleItems",
                column: "PipeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PipeModules_Name",
                table: "PipeModules",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PipeModuleItems");

            migrationBuilder.DropTable(
                name: "PipeModules");
        }
    }
}
