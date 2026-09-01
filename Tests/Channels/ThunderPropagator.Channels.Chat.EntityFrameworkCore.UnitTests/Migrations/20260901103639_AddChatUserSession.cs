using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThunderPropagator.Channels.Chat.EntityFrameworkCore.UnitTests.Migrations
{
    /// <inheritdoc />
    public partial class AddChatUserSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatUserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatUserSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatUserSessions_ConnectionId",
                table: "ChatUserSessions",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatUserSessions_UserId",
                table: "ChatUserSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatUserSessions");
        }
    }
}
