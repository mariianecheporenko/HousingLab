using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HousingInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Users_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "housing",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    address = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    rooms = table.Column<int>(type: "integer", nullable: true),
                    area = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("housing_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_housing_Users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    User_id = table.Column<int>(type: "integer", nullable: false),
                    Noise_level = table.Column<string>(type: "character varying", nullable: false),
                    Sleep_mode = table.Column<string>(type: "character varying", nullable: false),
                    Pets = table.Column<bool>(type: "boolean", nullable: false),
                    Guests = table.Column<string>(type: "character varying", nullable: false),
                    Clean_level = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Profiles_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "fk_profile_user",
                        column: x => x.User_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Availability",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    housing_id = table.Column<int>(type: "integer", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Availability_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_housing",
                        column: x => x.housing_id,
                        principalTable: "housing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingRequests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    housing_id = table.Column<int>(type: "integer", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("BookingRequests_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_housing",
                        column: x => x.housing_id,
                        principalTable: "housing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_user",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    housing_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reviews_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_housing",
                        column: x => x.housing_id,
                        principalTable: "housing",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reviews_user",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Availability_housing_id",
                table: "Availability",
                column: "housing_id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_housing_id",
                table: "BookingRequests",
                column: "housing_id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_user_id",
                table: "BookingRequests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_housing_owner_id",
                table: "housing",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "unique_user_profile",
                table: "Profiles",
                column: "User_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_housing_id",
                table: "reviews",
                column: "housing_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id",
                table: "reviews",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Availability");

            migrationBuilder.DropTable(
                name: "BookingRequests");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "housing");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
