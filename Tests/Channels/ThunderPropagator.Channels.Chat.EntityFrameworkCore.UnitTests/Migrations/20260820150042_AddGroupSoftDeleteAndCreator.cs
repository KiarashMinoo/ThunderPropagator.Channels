using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupSoftDeleteAndCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Groups",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Groups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Groups");
        }
    }
}
