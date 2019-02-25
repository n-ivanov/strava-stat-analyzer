﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StravaStatisticsAnalyzer.Web.Models;

namespace StravaStatisticsAnalyzer.Web.Migrations
{
    [DbContext(typeof(RazorPagesActivityContext))]
    [Migration("20190223181820_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity("StravaStatisticsAnalyzer.Web.Models.Activity", b =>
                {
                    b.Property<long>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("AthleteID");

                    b.Property<double>("AvgSpeed");

                    b.Property<bool>("Commute");

                    b.Property<DateTime>("DateTime");

                    b.Property<string>("Description");

                    b.Property<double>("Distance");

                    b.Property<int>("ElapsedTime");

                    b.Property<double>("ElevationHigh");

                    b.Property<double>("ElevationLow");

                    b.Property<double>("EndLatitude");

                    b.Property<double>("EndLongitude");

                    b.Property<double>("MaxSpeed");

                    b.Property<int>("MovingTime");

                    b.Property<string>("Name");

                    b.Property<double>("StartLatitude");

                    b.Property<double>("StartLongitude");

                    b.Property<double>("TotalElevationGain");

                    b.HasKey("ID");

                    b.ToTable("Activity");
                });
#pragma warning restore 612, 618
        }
    }
}
