using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.UnitTests.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicTacToeGames",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Board = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false),
                    Player1Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Player1Sign = table.Column<int>(type: "INTEGER", nullable: false),
                    Player1ConnectionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Player2Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Player2Kind = table.Column<int>(type: "INTEGER", nullable: true),
                    Player2ConnectionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Player2DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentTurnSign = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicTacToeGames", x => x.SessionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicTacToeGames");
        }
    }
}
