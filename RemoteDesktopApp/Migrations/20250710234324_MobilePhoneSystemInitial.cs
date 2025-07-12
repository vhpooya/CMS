using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteDesktopApp.Migrations
{
    /// <inheritdoc />
    public partial class MobilePhoneSystemInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneOnline",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPhoneActivity",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneSoundEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhoneCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallerId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    CallerPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ReceiverPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    ConnectionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVideoCall = table.Column<bool>(type: "bit", nullable: false),
                    Quality = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneCalls_Users_CallerId",
                        column: x => x.CallerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneCalls_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhoneContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ContactUserId = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastContactAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallCount = table.Column<int>(type: "int", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneContacts_Users_ContactUserId",
                        column: x => x.ContactUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhoneContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDisplayed = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RelatedId = table.Column<int>(type: "int", nullable: true),
                    RelatedType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "bit", nullable: false),
                    VibrationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnlineStatus = table.Column<bool>(type: "bit", nullable: false),
                    AutoAnswerEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoAnswerDelay = table.Column<int>(type: "int", nullable: false),
                    RingtoneUrl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NotificationSoundUrl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RingtoneVolume = table.Column<int>(type: "int", nullable: false),
                    NotificationVolume = table.Column<int>(type: "int", nullable: false),
                    DoNotDisturbEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DoNotDisturbStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    DoNotDisturbEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    CallForwardingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ForwardToPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ShowTypingIndicator = table.Column<bool>(type: "bit", nullable: false),
                    ReadReceiptsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmsMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    SenderPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ReceiverPhoneNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsDelivered = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsMessages_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmsMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true,
                filter: "[PhoneNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_CallerId_ReceiverId_StartTime",
                table: "PhoneCalls",
                columns: new[] { "CallerId", "ReceiverId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_CallerPhoneNumber",
                table: "PhoneCalls",
                column: "CallerPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_ReceiverId",
                table: "PhoneCalls",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_ReceiverPhoneNumber",
                table: "PhoneCalls",
                column: "ReceiverPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_StartTime",
                table: "PhoneCalls",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCalls_Status",
                table: "PhoneCalls",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_ContactPhoneNumber",
                table: "PhoneContacts",
                column: "ContactPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_ContactUserId",
                table: "PhoneContacts",
                column: "ContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_IsBlocked",
                table: "PhoneContacts",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_IsFavorite",
                table: "PhoneContacts",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneContacts_UserId_ContactUserId",
                table: "PhoneContacts",
                columns: new[] { "UserId", "ContactUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNotifications_CreatedAt",
                table: "PhoneNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNotifications_Priority",
                table: "PhoneNotifications",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNotifications_Type",
                table: "PhoneNotifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNotifications_UserId_IsRead",
                table: "PhoneNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneSettings_UserId",
                table: "PhoneSettings",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_IsRead",
                table: "SmsMessages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_ReceiverId",
                table: "SmsMessages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_ReceiverPhoneNumber",
                table: "SmsMessages",
                column: "ReceiverPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_SenderId_ReceiverId_SentAt",
                table: "SmsMessages",
                columns: new[] { "SenderId", "ReceiverId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_SenderPhoneNumber",
                table: "SmsMessages",
                column: "SenderPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_SentAt",
                table: "SmsMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_SmsMessages_Status",
                table: "SmsMessages",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneCalls");

            migrationBuilder.DropTable(
                name: "PhoneContacts");

            migrationBuilder.DropTable(
                name: "PhoneNotifications");

            migrationBuilder.DropTable(
                name: "PhoneSettings");

            migrationBuilder.DropTable(
                name: "SmsMessages");

            migrationBuilder.DropIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPhoneOnline",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastPhoneActivity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneSoundEnabled",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldNullable: true);
        }
    }
}
