using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitCablePipeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE PipeTypes
                SET Type = 'strong_cable'
                WHERE Type = 'cable' AND (Name LIKE '%電源%' OR Name LIKE '%电源%');
                """);

            migrationBuilder.Sql("""
                UPDATE PipeTypes
                SET Type = 'weak_cable'
                WHERE Type = 'cable';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE PipeTypes
                SET Type = 'cable'
                WHERE Type IN ('weak_cable', 'strong_cable');
                """);
        }
    }
}
