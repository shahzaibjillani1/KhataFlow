using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhataFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceSettings",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PrimaryColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    AccentColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    FooterNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ShowBusinessAddress = table.Column<bool>(type: "bit", nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Style = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceSettings_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSettings_BusinessId",
                schema: "business",
                table: "InvoiceSettings",
                column: "BusinessId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceSettings",
                schema: "business");
        }
    }
}
