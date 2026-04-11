// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillIssue.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class AddPpIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_player_pp",
                table: "player",
                column: "pp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_player_pp",
                table: "player");
        }
    }
}
