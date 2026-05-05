using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Watchly.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussionRoomsAndSignalR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscussionRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FriendUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionRooms_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionRooms_AspNetUsers_FriendUserId",
                        column: x => x.FriendUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionRooms_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscussionRoomMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemMessage = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscussionRoomMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscussionRoomMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiscussionRoomMessages_DiscussionRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "DiscussionRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRoomMessages_RoomId",
                table: "DiscussionRoomMessages",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRoomMessages_SenderId",
                table: "DiscussionRoomMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_CreatedByUserId",
                table: "DiscussionRooms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_FriendUserId",
                table: "DiscussionRooms",
                column: "FriendUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscussionRooms_MovieId",
                table: "DiscussionRooms",
                column: "MovieId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscussionRoomMessages");

            migrationBuilder.DropTable(
                name: "DiscussionRooms");
        }
    }
}
