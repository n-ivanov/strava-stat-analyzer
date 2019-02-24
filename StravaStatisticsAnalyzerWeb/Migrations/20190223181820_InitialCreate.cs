using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StravaStatisticsAnalyzer.Web.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEffort",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Distance = table.Column<double>(nullable: false),
                    MovingTime = table.Column<int>(nullable: false),
                    ElapsedTime = table.Column<int>(nullable: false),
                    AvgSpeed = table.Column<double>(nullable: false),
                    MaxSpeed = table.Column<double>(nullable: false),
                    DateTime = table.Column<DateTime>(nullable: false),
                    AthleteID = table.Column<long>(nullable: false),
                    TotalElevationGain = table.Column<double>(nullable: false),
                    ElevationHigh = table.Column<double>(nullable: false),
                    ElevationLow = table.Column<double>(nullable: false),
                    StartLatitude = table.Column<double>(nullable: false),
                    StartLongitude = table.Column<double>(nullable: false),
                    EndLatitude = table.Column<double>(nullable: false),
                    EndLongitude = table.Column<double>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Commute = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEffort", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEffort");
        }
    }
}
