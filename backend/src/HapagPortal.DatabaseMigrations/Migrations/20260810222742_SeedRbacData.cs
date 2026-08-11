using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class SeedRbacData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("22588ad4-e2ba-24bf-ed2c-90c1fc4611f5"), "users.manage", "users.manage" },
                    { new Guid("30ff3460-163d-d904-96b9-ed31e48baf59"), "config.global.manage", "config.global.manage" },
                    { new Guid("3c5032f7-0a0f-1637-51f3-4de0f9cfbc65"), "customs.view", "customs.view" },
                    { new Guid("421213a8-3a92-e96d-7781-9248e1f9666d"), "maintainers.manage", "maintainers.manage" },
                    { new Guid("4c4d494a-f9fe-55da-16d4-d763ee0e0d41"), "roles.manage", "roles.manage" },
                    { new Guid("575065bf-620f-96a3-552c-8cd4a06631e6"), "config.client.manage", "config.client.manage" },
                    { new Guid("6ae10bad-586a-bb93-ec27-c7e584daff6e"), "bl.upload", "bl.upload" },
                    { new Guid("6b761bce-b542-40c6-01cb-5d5b73bd1934"), "customs.transmit", "customs.transmit" },
                    { new Guid("701f2591-7f87-b7fe-588d-efa5dc0634f4"), "audit.view", "audit.view" },
                    { new Guid("7fa0cf9a-4c6b-b038-a70c-4db8df0c54b8"), "bl.view", "bl.view" },
                    { new Guid("d5ceee54-2e0e-0fdb-c7ee-2b0805d55abc"), "customs.retry", "customs.retry" },
                    { new Guid("d71d05fa-8d9e-4779-44a5-fca869915ebf"), "deadlines.view", "deadlines.view" },
                    { new Guid("ed7ca0d9-12d7-be02-9123-69b62cbb0a42"), "reports.view", "reports.view" },
                    { new Guid("ef8b113d-8bdc-f78b-6796-2565876c88b0"), "notifications.view", "notifications.view" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsSystem", "ModifiedAt", "ModifiedBy", "Name" },
                values: new object[,]
                {
                    { new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556"), "Supervisor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Supervisor" },
                    { new Guid("421b7367-0023-88ac-ba09-345afafb157e"), "AdminBA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Administrador BA" },
                    { new Guid("5193bc68-e496-eb84-a035-08fbf67789d7"), "ExternalApi", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "External API" },
                    { new Guid("a99ecf29-a1a5-f563-a992-a59574d146c8"), "Client", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Cliente" },
                    { new Guid("b02bbf39-dfb0-6123-13fa-4ea772ff1dfb"), "Administrador", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Administrador" },
                    { new Guid("c2825776-5914-a3e3-0041-d2bface9e0b9"), "CustomsAgent", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Agente de Aduana" },
                    { new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a"), "Coordinador", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Coordinador" },
                    { new Guid("e9da1089-6ad9-7f34-3937-7976d95ae7a0"), "SuperAdmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SYSTEM", null, null, true, null, null, "Super Administrador" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("03be28ba-43c4-8141-2680-97f0893a2104"), new Guid("3c5032f7-0a0f-1637-51f3-4de0f9cfbc65"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("0c15ee9d-1d0c-c5c1-b1e4-7533c0b78262"), new Guid("d5ceee54-2e0e-0fdb-c7ee-2b0805d55abc"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("1225f32b-e00b-ed6c-44d3-9bdaadac695b"), new Guid("7fa0cf9a-4c6b-b038-a70c-4db8df0c54b8"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("20337084-df89-3ec0-d150-16ba1c09645d"), new Guid("d71d05fa-8d9e-4779-44a5-fca869915ebf"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("28e962eb-d75c-55a0-1034-96768ae83b4c"), new Guid("d71d05fa-8d9e-4779-44a5-fca869915ebf"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("3c557c65-a35d-8cf7-1c04-c31f44e6c666"), new Guid("ed7ca0d9-12d7-be02-9123-69b62cbb0a42"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("3f77a688-b174-0d0c-dc44-950a3bde75ea"), new Guid("3c5032f7-0a0f-1637-51f3-4de0f9cfbc65"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("7ef908b4-fd65-6b73-fdf8-22fc511db624"), new Guid("ef8b113d-8bdc-f78b-6796-2565876c88b0"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("806771ba-d9cc-c948-c4b7-176be3197d61"), new Guid("6b761bce-b542-40c6-01cb-5d5b73bd1934"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("81956975-58a7-1ece-9090-1630d703b862"), new Guid("6b761bce-b542-40c6-01cb-5d5b73bd1934"), new Guid("5193bc68-e496-eb84-a035-08fbf67789d7") },
                    { new Guid("974d869f-4e7d-202f-b7b0-8101e09834da"), new Guid("7fa0cf9a-4c6b-b038-a70c-4db8df0c54b8"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("a9560721-003c-4583-cb9a-aeb5d799ec94"), new Guid("701f2591-7f87-b7fe-588d-efa5dc0634f4"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("b490e935-7cb3-d19e-af9b-0946e1f6c79d"), new Guid("6ae10bad-586a-bb93-ec27-c7e584daff6e"), new Guid("5193bc68-e496-eb84-a035-08fbf67789d7") },
                    { new Guid("bce6d3c0-6b56-5340-993f-ca7360bfb4be"), new Guid("ef8b113d-8bdc-f78b-6796-2565876c88b0"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") },
                    { new Guid("c06e8ceb-b97f-980f-3849-ee5827c12e82"), new Guid("ed7ca0d9-12d7-be02-9123-69b62cbb0a42"), new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556") },
                    { new Guid("d4b27ca4-185a-def1-64d7-770c870f9b80"), new Guid("6ae10bad-586a-bb93-ec27-c7e584daff6e"), new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("22588ad4-e2ba-24bf-ed2c-90c1fc4611f5"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("30ff3460-163d-d904-96b9-ed31e48baf59"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("421213a8-3a92-e96d-7781-9248e1f9666d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4c4d494a-f9fe-55da-16d4-d763ee0e0d41"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("575065bf-620f-96a3-552c-8cd4a06631e6"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("03be28ba-43c4-8141-2680-97f0893a2104"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("0c15ee9d-1d0c-c5c1-b1e4-7533c0b78262"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("1225f32b-e00b-ed6c-44d3-9bdaadac695b"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("20337084-df89-3ec0-d150-16ba1c09645d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("28e962eb-d75c-55a0-1034-96768ae83b4c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3c557c65-a35d-8cf7-1c04-c31f44e6c666"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("3f77a688-b174-0d0c-dc44-950a3bde75ea"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("7ef908b4-fd65-6b73-fdf8-22fc511db624"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("806771ba-d9cc-c948-c4b7-176be3197d61"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("81956975-58a7-1ece-9090-1630d703b862"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("974d869f-4e7d-202f-b7b0-8101e09834da"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("a9560721-003c-4583-cb9a-aeb5d799ec94"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b490e935-7cb3-d19e-af9b-0946e1f6c79d"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("bce6d3c0-6b56-5340-993f-ca7360bfb4be"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("c06e8ceb-b97f-980f-3849-ee5827c12e82"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d4b27ca4-185a-def1-64d7-770c870f9b80"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("421b7367-0023-88ac-ba09-345afafb157e"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a99ecf29-a1a5-f563-a992-a59574d146c8"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("b02bbf39-dfb0-6123-13fa-4ea772ff1dfb"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c2825776-5914-a3e3-0041-d2bface9e0b9"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e9da1089-6ad9-7f34-3937-7976d95ae7a0"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c5032f7-0a0f-1637-51f3-4de0f9cfbc65"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ae10bad-586a-bb93-ec27-c7e584daff6e"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6b761bce-b542-40c6-01cb-5d5b73bd1934"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("701f2591-7f87-b7fe-588d-efa5dc0634f4"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("7fa0cf9a-4c6b-b038-a70c-4db8df0c54b8"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5ceee54-2e0e-0fdb-c7ee-2b0805d55abc"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d71d05fa-8d9e-4779-44a5-fca869915ebf"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("ed7ca0d9-12d7-be02-9123-69b62cbb0a42"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("ef8b113d-8bdc-f78b-6796-2565876c88b0"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3bb3e862-e9d1-77dd-9c69-b1c097511556"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5193bc68-e496-eb84-a035-08fbf67789d7"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a"));
        }
    }
}
