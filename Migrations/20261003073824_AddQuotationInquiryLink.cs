using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace cateringflow.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationInquiryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InquiryId",
                table: "Quotations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_InquiryId",
                table: "Quotations",
                column: "InquiryId",
                unique: true,
                filter: "[InquiryId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Inquiries_InquiryId",
                table: "Quotations",
                column: "InquiryId",
                principalTable: "Inquiries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Inquiries_InquiryId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_InquiryId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "InquiryId",
                table: "Quotations");
        }
    }
}
