using DragChain.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DragChainDbContext))]
    [Migration("20260506061500_SimplifyTrunkingCatalog")]
    public partial class SimplifyTrunkingCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite 不支持直接 DropColumn。旧字段先保留在库里，后端模型和页面不再使用它们。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 与 Up 保持一致，不在回滚时改动历史数据结构。
        }
    }
}
