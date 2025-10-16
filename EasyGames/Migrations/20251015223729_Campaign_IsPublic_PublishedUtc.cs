using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyGames.Migrations
{
    /// <inheritdoc />
    public partial class Campaign_IsPublic_PublishedUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "EmailCampaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedUtc",
                table: "EmailCampaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_IsPublic",
                table: "EmailCampaigns",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_PublishedUtc",
                table: "EmailCampaigns",
                column: "PublishedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailCampaigns_IsPublic",
                table: "EmailCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_EmailCampaigns_PublishedUtc",
                table: "EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "PublishedUtc",
                table: "EmailCampaigns");
        }
    }
}
