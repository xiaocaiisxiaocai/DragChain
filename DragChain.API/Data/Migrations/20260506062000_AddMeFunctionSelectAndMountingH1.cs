using DragChain.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragChain.API.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DragChainDbContext))]
    [Migration("20260506062000_AddMeFunctionSelectAndMountingH1")]
    public partial class AddMeFunctionSelectAndMountingH1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 当前库已用非破坏性方式补齐两列，避免启动迁移重复加列。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 保留历史数据列，不在回滚时删除字段。
        }
    }
}
