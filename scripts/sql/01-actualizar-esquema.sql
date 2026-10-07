-- 1) Actualiza el esquema de la base de producción desde la versión de main (migración 20260810234500_AddNotifications)
--    hasta la versión del cierre de Fase 1 (20261007135717_AddPaymentGatewayRedirectFormAndCheck): 12 migraciones.
-- Es idempotente (aplica solo las migraciones que falten) y equivale a lo que hace la API al iniciar.
-- Ejecútelo solo si quiere actualizar la base a mano ANTES de desplegar; si despliega primero, la API ya lo hace.
-- Después ejecute 02-usuario-demo-unico.sql.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    DROP INDEX "IX_Users_ClientId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Users" ADD "MembershipDecidedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Users" ADD "MembershipDecidedBy" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Users" ADD "MembershipStatus" character varying(20) NOT NULL DEFAULT 'Active';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ArCheckedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ArCheckedBy" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ArReference" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "MatchCode" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "OperatingCountries" character varying(20) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "OrganizationType" character varying(30) NOT NULL DEFAULT 'Customer';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "RegistrationStatus" character varying(30) NOT NULL DEFAULT 'Approved';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "RejectedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ReviewNotes" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ValidatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "Clients" ADD "ValidatedBy" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Clients" SET "OrganizationType" = CASE "ClientType"
        WHEN 'Internal' THEN 'Internal'
        WHEN 'Agent' THEN 'CustomsAgency'
        WHEN 'CustomsAgent' THEN 'CustomsAgency'
        ELSE 'Customer' END;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    ALTER TABLE "BillsOfLading" ADD "BookingNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE TABLE "OrganizationDocuments" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "DocumentType" character varying(40) NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "StorageKey" character varying(300) NOT NULL,
        "UploadedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_OrganizationDocuments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrganizationDocuments_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE TABLE "ShipmentActions" (
        "Id" uuid NOT NULL,
        "Code" character varying(100) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Category" character varying(30) NOT NULL,
        "Kind" character varying(20) NOT NULL,
        "Scope" character varying(20) NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentActions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE TABLE "ShipmentRoles" (
        "Id" uuid NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "Role" character varying(30) NOT NULL,
        "Source" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ShipmentRoles_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ShipmentRoles_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE TABLE "ShipmentAccessRules" (
        "Id" uuid NOT NULL,
        "ShipmentActionId" uuid NOT NULL,
        "Role" character varying(30) NOT NULL,
        "OrganizationType" character varying(30),
        "Level" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentAccessRules" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ShipmentAccessRules_ShipmentActions_ShipmentActionId" FOREIGN KEY ("ShipmentActionId") REFERENCES "ShipmentActions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "BillsOfLading" SET "BookingNumber" = 'HLCUBKG2501001'
    WHERE "Id" = '11111111-0007-0007-0007-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "BillsOfLading" SET "BookingNumber" = 'HLCUBKG2502004'
    WHERE "Id" = '11111111-0007-0007-0007-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "BillsOfLading" SET "BookingNumber" = 'HLCUBKG2503007'
    WHERE "Id" = '11111111-0007-0007-0007-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "BillsOfLading" SET "BookingNumber" = 'HLCUBKG2601045'
    WHERE "Id" = '11111111-0007-0007-0007-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "BillsOfLading" SET "BookingNumber" = 'HLCUBKG2602078'
    WHERE "Id" = '11111111-0007-0007-0007-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000006', 'HLCUSAI260300610', NULL, 'HLCUBKG2603061', 'c3d4e5f6-0003-0003-0003-000000000010', 'Fruit Import BV', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-11-25T00:00:00Z', TIMESTAMPTZ '2026-10-20T00:00:00Z', 3900.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Rotterdam, Netherlands', 'Rotterdam (NLRTM)', 'San Antonio (CLSAI)', 'Export', 'Importadora Demo SpA', 'Booked', 'Valparaiso Express', NULL, '2610S');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000008', 'HLCUARI260300830', NULL, 'HLCUBKG2603083', 'c3d4e5f6-0003-0003-0003-000000000020', 'Andes Foods SAC', 'BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-11-12T00:00:00Z', TIMESTAMPTZ '2026-11-05T00:00:00Z', 1450.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Lima, Peru', 'Callao (PECLL)', 'Arica (CLARI)', 'Export', 'Comercial Altiplano SRL', 'Booked', 'Antofagasta Express', NULL, '2612S');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Clients" SET "ApprovedAt" = TIMESTAMPTZ '2026-01-01T00:00:00Z', "ArCheckedAt" = NULL, "ArCheckedBy" = NULL, "ArReference" = NULL, "MatchCode" = NULL, "OperatingCountries" = 'CL,BO', "OrganizationType" = 'Internal', "RegistrationStatus" = 'Approved', "RejectedAt" = NULL, "ReviewNotes" = NULL, "ValidatedAt" = NULL, "ValidatedBy" = NULL
    WHERE "Id" = 'c3d4e5f6-0003-0003-0003-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Clients" SET "ApprovedAt" = TIMESTAMPTZ '2026-01-01T00:00:00Z', "ArCheckedAt" = NULL, "ArCheckedBy" = NULL, "ArReference" = NULL, "MatchCode" = 'MC100010', "OperatingCountries" = 'CL,BO', "OrganizationType" = 'Customer', "RegistrationStatus" = 'Approved', "RejectedAt" = NULL, "ReviewNotes" = NULL, "ValidatedAt" = NULL, "ValidatedBy" = NULL
    WHERE "Id" = 'c3d4e5f6-0003-0003-0003-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Clients" SET "ApprovedAt" = TIMESTAMPTZ '2026-01-01T00:00:00Z', "ArCheckedAt" = NULL, "ArCheckedBy" = NULL, "ArReference" = NULL, "MatchCode" = 'MC100020', "OperatingCountries" = 'BO', "OrganizationType" = 'Customer', "RegistrationStatus" = 'Approved', "RejectedAt" = NULL, "ReviewNotes" = NULL, "ValidatedAt" = NULL, "ValidatedBy" = NULL
    WHERE "Id" = 'c3d4e5f6-0003-0003-0003-000000000020';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Clients" SET "ApprovedAt" = TIMESTAMPTZ '2026-01-01T00:00:00Z', "ArCheckedAt" = NULL, "ArCheckedBy" = NULL, "ArReference" = NULL, "MatchCode" = 'MC100030', "OperatingCountries" = 'CL', "OrganizationType" = 'CustomsAgency', "RegistrationStatus" = 'Approved', "RejectedAt" = NULL, "ReviewNotes" = NULL, "ValidatedAt" = NULL, "ValidatedBy" = NULL
    WHERE "Id" = 'c3d4e5f6-0003-0003-0003-000000000030';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000040', NULL, NULL, TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'contacto@pacifictrading.cl', TRUE, TRUE, 'MC100040', NULL, NULL, 'Pacific Trading Co.', 'CL', 'Customer', NULL, 'Approved', NULL, NULL, '77.888.999-0', 'RUT', NULL, NULL);
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000050', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'registro@logisticaandina.cl', TRUE, TRUE, NULL, NULL, NULL, 'Logística Andina SpA', 'CL', 'FreightForwarder', '+56 2 2999 1234', 'PendingValidation', NULL, NULL, '76.555.111-2', 'RUT', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('208dfafe-5331-55c8-f20c-1fedbb16b3cb', 'org.users.manage', 'org.users.manage');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('2477f174-66b3-8275-4ef4-b84902cdbb81', 'shipments.operate', 'shipments.operate');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('579700e8-1338-3738-63e6-8e8b8b7cad71', 'shipments.view-all', 'shipments.view-all');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('59a9ccfd-f3a1-90b7-b694-bd9eafb81228', 'organizations.review', 'organizations.review');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('920581cb-9857-6ad8-3406-9f8f54fd25fc', 'organizations.ar-check', 'organizations.ar-check');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('c474e6b8-5abc-1ffc-bf2a-eeb14f3dd7a7', 'access-matrix.manage', 'access-matrix.manage');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('d7f60fcb-88d4-54d3-54fb-d63def5c9613', 'org.requests.approve', 'org.requests.approve');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "Roles" ("Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsSystem", "ModifiedAt", "ModifiedBy", "Name")
    VALUES ('227e87c5-a8d9-51c6-e0e9-373dc6d2c60b', 'OrgOperator', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'Operador de organización');
    INSERT INTO "Roles" ("Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsSystem", "ModifiedAt", "ModifiedBy", "Name")
    VALUES ('a084c0fb-db38-005b-3d5f-926ed28ed6df', 'OrgViewer', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'Consulta de organización');
    INSERT INTO "Roles" ("Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsSystem", "ModifiedAt", "ModifiedBy", "Name")
    VALUES ('e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'Administrador de organización');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('032cc54e-cbbe-5272-cab1-32a3bb291f82', 'Information', 'tatc.download', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 80, TRUE, 'View', NULL, NULL, 'Descargar documento TATC', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('0334bbd0-ffd8-1995-7db7-fdf3d95d6729', 'Information', 'bl-copy-unvalued.request', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 50, TRUE, 'Operate', NULL, NULL, 'Solicitar copia de BL no valorada', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('07343dd5-fa12-c8e8-a80f-26f110926989', 'Information', 'freight.pay', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 90, TRUE, 'Operate', NULL, NULL, 'Visualizar y pagar montos de flete', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('1145e12a-7014-9221-40ff-2d3754ffd29f', 'Administration', 'join-requests.approve', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 380, TRUE, 'Operate', NULL, NULL, 'Revisar y aprobar solicitudes de registro a la organización', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('122ef33e-f8f5-fb84-25cb-5176d10c40dc', 'Information', 'release-letter.generate', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 120, TRUE, 'Operate', NULL, NULL, 'Generar y descargar carta de liberación y desconsolidado', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('160ab007-ea1a-063f-52dd-e10cf1dcd00b', 'Administration', 'organization-users.own', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 370, TRUE, 'Operate', NULL, NULL, 'Tener usuarios propios asociados a la organización', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('19ef0081-eb79-5a7c-e09c-62482731cd52', 'Information', 'export-depot.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 240, TRUE, 'View', NULL, NULL, 'Ver depósito asignado en exportación', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('1c3dd706-407f-2945-5b68-bea5dfd584e9', 'Administration', 'third-party-query.notify', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 320, TRUE, 'View', NULL, NULL, 'Recibir notificación cuando un tercero consulta un BL', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('2ac5eecb-38d8-6ec0-6d82-f03f854e976c', 'Information', 'import-depot.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 230, TRUE, 'View', NULL, NULL, 'Ver depósito asignado en importación', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('3eac8579-97de-40ff-10b5-ba572613f061', 'Information', 'shipment.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 10, TRUE, 'View', NULL, NULL, 'Ver listado y detalle de BL o booking', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('3ff1f234-1eda-6801-33a1-ce8538693d6e', 'Information', 'no-debt-certificate.download', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 70, TRUE, 'View', NULL, NULL, 'Descargar certificado de libre deuda', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('4900513d-d3e1-f3e2-5b43-c0e000a389f0', 'Information', 'local-charges-mandatory.pay', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 100, TRUE, 'Operate', NULL, NULL, 'Visualizar y pagar recargos locales mandatorios', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('557bba04-d0ce-91b0-c765-277b91222bf7', 'Administration', 'data-visibility.extend', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 350, TRUE, 'Operate', NULL, NULL, 'Ampliar la visibilidad de un dato del BL a otro rol', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('5810f4b9-0e60-a000-5697-53177df94bb9', 'Information', 'collect-receipt.download', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 220, TRUE, 'View', NULL, NULL, 'Visualizar y descargar comprobante Collect', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('5fa67043-8f48-599c-fded-6ea2b5ca1488', 'Administration', 'assistant.use', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 420, TRUE, 'View', NULL, NULL, 'Utilizar el asistente del portal y ver los comunicados', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('65dfa9ad-25d2-4a79-f36c-410bf77ddddc', 'Administration', 'open-access.enable', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 290, TRUE, 'Operate', NULL, NULL, 'Activar el acceso abierto por número de BL', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('6678139a-eb5f-24c3-d404-7ffcfbf1dde8', 'Information', 'release-requirements.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 20, TRUE, 'View', NULL, NULL, 'Consultar estado de los requisitos de liberación', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('72baee95-6d9c-f671-270d-67a5ddbb42e5', 'Administration', 'open-access.self-associate', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 310, TRUE, 'Operate', NULL, NULL, 'Autoasociarse a un BL consultado con acceso abierto', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('72c736f3-0830-08da-2ba1-aef0b2ec3e81', 'Administration', 'country.select', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 430, TRUE, 'View', NULL, NULL, 'Seleccionar el país de operación y consultar la clasificación DG', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('741f2950-8b45-5e2d-8065-fe163db727a6', 'Administration', 'access-validity.set', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 270, TRUE, 'Operate', NULL, NULL, 'Definir la vigencia de un acceso', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('7cc9c738-4673-e0c2-9a01-df7b48b28b7c', 'Information', 'account-statement.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 190, TRUE, 'View', NULL, NULL, 'Consultar el estado de cuenta en línea', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('81d22eff-dca4-0ac0-bf3a-4f3df458d908', 'Information', 'tracking.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 30, TRUE, 'View', NULL, NULL, 'Ver el seguimiento del embarque', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('9119a3f4-394d-695c-5267-b7cd696f7ba6', 'Administration', 'access.revoke', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 260, TRUE, 'Operate', NULL, NULL, 'Revocar un acceso ya otorgado', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('93f658d0-ee65-42f2-a3da-646f54feb2d6', 'Administration', 'default-agents.configure', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 280, TRUE, 'Operate', NULL, NULL, 'Configurar agencia de aduanas o transportista por defecto', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('947b0092-be0a-4670-bf9f-46eda7d14ab0', 'Administration', 'distribution-list.update', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 400, TRUE, 'Operate', NULL, NULL, 'Actualizar la lista de distribución de correos propia', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('95d43dff-917c-dd4d-eeb5-c49731913c7c', 'Administration', 'open-access.search', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 300, TRUE, 'View', NULL, NULL, 'Buscar un BL con acceso abierto por su número', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('a155eb78-8542-4978-a8dd-bd2b28929738', 'Administration', 'parent-company-visibility.enable', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 340, TRUE, 'Operate', NULL, NULL, 'Habilitar la visibilidad de los BL hacia la empresa matriz', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('a2d100d7-15e0-44de-45a3-3d732cef7332', 'Administration', 'early-booking-access.grant', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 360, TRUE, 'Operate', NULL, NULL, 'Otorgar acceso anticipado por booking a un futuro shipper', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('a6e130a7-a1ea-abd1-aa46-e6b5c5b63391', 'Information', 'bl-issuance.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 40, TRUE, 'View', NULL, NULL, 'Consultar el estado de emisión del BL', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('ac5533f1-b185-fe85-07a0-89a89adf382d', 'Information', 'invoices-billed.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 200, TRUE, 'View', NULL, NULL, 'Ver facturas como cliente facturado', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('bd6cab83-dabc-1619-6d2c-e324cd7d01a1', 'Administration', 'carrier.pre-create', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 390, TRUE, 'Operate', NULL, NULL, 'Pre-crear el perfil de un transportista sin cuenta', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('bdcf5a98-4a99-386e-fe2f-1da80f99aa1e', 'Information', 'invoices-payer.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 210, TRUE, 'View', NULL, NULL, 'Ver facturas como pagador distinto del facturado', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('c8157abe-83ae-64bb-ddbb-b4681e99c33f', 'Information', 'drop-off.request', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 170, TRUE, 'Operate', NULL, NULL, 'Solicitar y pagar Drop Off', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('ca92d241-6346-1541-212a-e1403a95cb9f', 'Information', 'transshipment-certificate.generate', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 130, TRUE, 'Operate', NULL, NULL, 'Generar y descargar certificado de transbordo', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('cbecbaf4-b71c-43d6-aae2-c344d6bf19ad', 'Information', 'local-charges-on-demand.pay', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 110, TRUE, 'Operate', NULL, NULL, 'Visualizar y pagar recargos locales on demand', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('cc4c1544-f4f5-8f00-9bb1-8241a64e8d54', 'Information', 'freight-certificate.generate', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 140, TRUE, 'Operate', NULL, NULL, 'Generar y descargar certificado de flete', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('cc502303-816b-de21-d681-447330a0e8c9', 'Information', 'responsibility-letter.generate', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 150, TRUE, 'Operate', NULL, NULL, 'Generar y descargar carta de responsabilidad', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('cfd10f40-3f50-8ebf-fed3-ec080f6c29b5', 'Information', 'bl-copy-valued.request', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 60, TRUE, 'Operate', NULL, NULL, 'Solicitar copia de BL valorada', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('e112508b-beea-c895-9061-da03cb427883', 'Administration', 'access.grant', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 250, TRUE, 'Operate', NULL, NULL, 'Otorgar acceso a un BL o booking, individual o masivo', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('e7eb07a5-bc20-230e-0e88-997310452f02', 'Information', 'import-demurrage.pay', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 160, TRUE, 'Operate', NULL, NULL, 'Consultar y pagar demurrage de importación', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0', 'Administration', 'access-audit.view', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 330, TRUE, 'View', NULL, NULL, 'Consultar la auditoría de accesos de un BL o booking', 'Shipment');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('f047ab5d-f553-96e9-9003-6fe15a0a5114', 'Administration', 'administration-area.access', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 410, TRUE, 'Operate', NULL, NULL, 'Acceder al área de administración del portal', 'Organization');
    INSERT INTO "ShipmentActions" ("Id", "Category", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "Kind", "ModifiedAt", "ModifiedBy", "Name", "Scope")
    VALUES ('fe2d4e42-d3b0-aac5-9026-5f918c61aca6', 'Information', 'warehouse-change.request', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 180, TRUE, 'Operate', NULL, NULL, 'Solicitar cambio de almacén, individual o masivo', 'Shipment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('30d59ecf-65b1-c621-f6ed-1efcf3d5b75d', '11111111-0007-0007-0007-000000000004', 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('49b4cece-2a9e-c46e-4156-6fc62587c5c5', '11111111-0007-0007-0007-000000000002', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('79bda1ef-7050-21dd-b714-f13f9efba29d', '11111111-0007-0007-0007-000000000005', 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('b424feb8-e445-a6b5-5dd1-001214133c6a', '11111111-0007-0007-0007-000000000001', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Users" SET "MembershipDecidedAt" = NULL, "MembershipDecidedBy" = NULL, "MembershipStatus" = 'Active'
    WHERE "Id" = 'd4e5f6a7-0004-0004-0004-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Users" SET "MembershipDecidedAt" = NULL, "MembershipDecidedBy" = NULL, "MembershipStatus" = 'Active'
    WHERE "Id" = 'd4e5f6a7-0004-0004-0004-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Users" SET "MembershipDecidedAt" = NULL, "MembershipDecidedBy" = NULL, "MembershipStatus" = 'Active'
    WHERE "Id" = 'd4e5f6a7-0004-0004-0004-000000000020';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    UPDATE "Users" SET "MembershipDecidedAt" = NULL, "MembershipDecidedBy" = NULL, "MembershipStatus" = 'Active'
    WHERE "Id" = 'd4e5f6a7-0004-0004-0004-000000000030';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000011', 'c3d4e5f6-0003-0003-0003-000000000010', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'consulta@importadorademo.cl', NULL, NULL, 'Carla', TRUE, FALSE, NULL, 'Consulta', NULL, NULL, 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'consulta@importadorademo.cl');
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000012', 'c3d4e5f6-0003-0003-0003-000000000010', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'solicitud@importadorademo.cl', NULL, NULL, 'Sergio', TRUE, FALSE, NULL, 'Solicitante', NULL, NULL, 'Pending', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'solicitud@importadorademo.cl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000008', '11111111-0007-0007-0007-000000000006', 'HLXU2023001', '40RF', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-020301', 'GateIn', NULL, NULL, 26800.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000010', '11111111-0007-0007-0007-000000000008', 'HLXU2023003', '20DV', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-020303', 'Empty', NULL, NULL, 16900.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000007', 'HLCUVAP260300720', NULL, 'HLCUBKG2603072', 'c3d4e5f6-0003-0003-0003-000000000040', 'Shanghai Wine Trading Ltd', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-12-02T00:00:00Z', TIMESTAMPTZ '2026-10-28T00:00:00Z', 4100.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Shanghai, China', 'Shanghai (CNSHA)', 'Valparaiso (CLVAP)', 'Export', 'Importadora Demo SpA', 'Loaded', 'Santos Express', NULL, '2611N');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000009', 95000.0, '11111111-0007-0007-0007-000000000006', 'GateIn', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate In - 40RF (San Antonio)', TRUE, NULL, NULL, 'Pending', 18050.0, 19.0, 113050.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000010', 45000.0, '11111111-0007-0007-0007-000000000006', 'BL_FEE', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'BL Documentation Fee (export)', TRUE, NULL, NULL, 'Pending', 8550.0, 19.0, 53550.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000012', 820.0, '11111111-0007-0007-0007-000000000008', 'GateIn', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Gate In - 20DV (Arica)', TRUE, NULL, NULL, 'Pending', 106.6, 13.0, 926.6);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('5930f642-9936-0c5d-e985-2746431ae506', '2477f174-66b3-8275-4ef4-b84902cdbb81', '227e87c5-a8d9-51c6-e0e9-373dc6d2c60b');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('9aad4ae3-e43b-2fb4-92a5-bb181af7f9f4', '208dfafe-5331-55c8-f20c-1fedbb16b3cb', 'e200b49e-343b-36a4-fbcc-e10fa786728c');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('bb9d489a-77a2-b2d6-c0f6-2df2ff63a1ff', '2477f174-66b3-8275-4ef4-b84902cdbb81', 'e200b49e-343b-36a4-fbcc-e10fa786728c');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('f5904c22-d3b8-747e-062e-48c5d83ccc9d', 'd7f60fcb-88d4-54d3-54fb-d63def5c9613', 'e200b49e-343b-36a4-fbcc-e10fa786728c');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ServiceOrders" ("Id", "BillOfLadingId", "ClientId", "CompletedAt", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ModifiedAt", "ModifiedBy", "OrderNumber", "OrderType", "RequestedAt", "Status")
    VALUES ('aaaaaaaa-0010-0010-0010-000000000004', '11111111-0007-0007-0007-000000000006', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Recepción de contenedor reefer HLXU2023001 para exportación', NULL, NULL, 'SO-2026-00004', 'GateIn', TIMESTAMPTZ '2026-10-15T09:00:00Z', 'Pending');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('010d51d4-8d8a-cdee-e387-383102425314', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('027d8ece-d35e-1127-2684-ab43766cda78', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('03889b31-de43-5ee6-6c2c-b20949ba0b68', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('040ee41b-f92d-b018-afb7-a8a6bb53fdfb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('041b6896-937b-270e-a33a-f7493f2cadaf', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0472449f-0179-c774-debc-2e4ccb57d8d1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('06469c53-5eba-efc9-7230-1fd645f76ffb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('06da8896-2839-fabf-39bb-f8615fb01826', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0780de5d-f606-3521-8200-a7e75edb52d5', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('07a5e3b1-ac08-3d1d-7da0-50a78f5527f9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0abc8a0c-ba8a-a05f-180a-6597ea4301fb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0afc2185-32f2-5fb2-74f4-5c12b1f165f2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0c0b2d4b-c674-7c51-6f8d-e54acf0cb89f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0d2c2750-0e0c-b7c7-5811-6bfa26c36d77', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0dba0bf2-e494-61db-33b1-be24cd21c0ef', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('0ee2c60a-5eca-e8b1-52dd-11e339055a41', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('10777d2e-ff0f-dfe2-f5a8-cb1f7d92d4f3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('14bd1a99-3b3d-6563-f136-c5be8e6da3b4', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('15322d9c-787e-cd71-4a5e-e61145250aa0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1656005a-c180-e36b-5fb6-b37ee190f74b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('17f3977e-2817-3583-b9f6-a21229905b19', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1a906fed-f454-454e-6318-19b31e95c00d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1b4ecae0-51e4-b54b-4469-81f8e1ae8568', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1b67de2e-c9b3-2b49-b206-56b50045032d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1d631bb0-e662-507c-093e-3a1f944edaf2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('1dbec5e1-7b9b-18ac-22a7-c282c6cdb245', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2071e182-5129-175f-6d04-319ba61f6086', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('20a66bc3-53e5-8e79-a5cd-4ce5856c32f5', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('24e69e40-eeab-54a8-3d4e-e857a93e17b1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('251753ba-5d1b-40ca-85a3-761ebb5997d5', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('27b837d7-6166-08af-c723-3d57348607cd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('294c84c4-4b81-1937-48ca-586e5d535d69', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('29c910f8-86b9-0c9e-ea00-cbbb7d47cd89', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2aad96e9-dc7c-0e06-d42c-696a4150964a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2b149e4a-a05c-1c2b-2ba2-c8141ef73e9c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2c38402f-99c3-d95c-eb1b-c8a31a911979', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2e45c4f1-5b9c-a815-ea89-48ff72d0803e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2e9526d1-5e70-86e0-2a71-6f259d29221a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('2f0163ce-15d4-e791-7cb9-5febfbf61145', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3072d047-9ee7-d892-5ff6-4adf7ecd0b54', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, 'FreightForwarder', 'ThirdParty', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('30928b02-6326-dc13-7a99-d91b7fdd5bc3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('30d952cc-25ba-d6b4-d5b1-e5c88eaaf591', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('31180664-fea8-336a-07eb-b4cae5e5fc2f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('32690a57-b1b1-c8a7-7f3f-e55b7511f6f9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('32a1438b-8958-e589-80c6-17ef8fd1b20c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('34cb5bed-66c7-edad-f228-d31fe86bbb69', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('34ffd440-fb10-56d5-6c98-eb3cb9ff12f3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('36344a4a-6760-075e-efbd-ecb945d444dd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('372eedc2-342b-19ff-0b5c-92f6e03f2c5e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3971f911-1710-2a19-a015-e642b88e4f9f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('39ca777e-10d1-2692-40d0-20abdb4c1d1f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3a26cc23-eea8-5033-d004-1d0d9a65b7d0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3aa44a80-9f63-6f75-4802-df51e75ac991', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3b3f547b-ae4b-f532-d938-15b23330aa7a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3ba841d2-ab0f-ed1b-0312-336875569e29', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3c4a641a-5e8b-3c23-3400-3738934dba37', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('3e6f1f78-d711-7f8f-9fcb-7f22e222a15c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4042f116-b438-28d4-0b36-efde4ca07061', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('40e1d283-26fc-e646-05f6-04daaa579f00', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('421a7754-907f-1236-c71e-4c236102c701', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('42797c44-5ef1-636d-2b04-4b4126fb59bd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4298ebf0-e400-92b8-4012-d5189a19c86c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('429c0335-4e9c-1cbd-4413-027708584384', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('42a6b84b-21c5-5337-0557-d60952e56ddf', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('42e3b162-8199-d447-35cf-04f3ab6d1bb9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('43205b4a-bc44-340e-779c-229bdfbad023', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4371663b-2c32-eb51-3231-cab8986972af', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('43a61aeb-5499-b695-6d2e-d353a26e2807', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4524e51e-00c0-978b-f8a4-1d0cc5f7fc56', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4552373e-8b24-a588-560e-17eda0460137', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('45987d46-1370-e066-5b82-5d4052a0fab1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('45c781d9-24f8-91c6-ac62-eaacff01f315', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4649623b-1041-4904-b0de-1ef347361ced', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('46adbe3c-20ab-260a-f4c1-31a321e8591e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('47e25292-4619-bac0-0ece-379958f1524f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Shipper', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('48a49c20-2f55-70e9-5a32-30420ff4f191', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('48cdf5fe-df57-6259-5624-c70f64d51d56', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('49cbf101-bc3d-5e3f-da09-58c328286f0e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4a2db5f4-b95a-dc7f-1af9-d527c01e4cec', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4cb06fb4-757a-ec71-7e15-a181930a458a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4d076896-7c12-8639-7317-f190b7829b4e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4d50588c-b82a-0108-ffe9-91f76b427b52', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4da9ce71-a545-4bd4-3ca3-aec28a330570', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('4f708bb5-54b4-88cc-060d-1df522408281', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('505eebc1-bd10-900f-0554-03d2cb6f4ce3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5156db2e-2806-e2e4-b3d5-6f28bd6926c2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5224b44f-6397-ffb6-4824-172308024f03', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('538ffd2c-4040-d01f-546d-6e6701b29d8f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5479e129-31cc-3199-a790-25ab399cf5d3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('56477c5b-146d-5936-ab86-ce2947c28400', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('56e99889-a543-6cd3-6923-a1d96b527705', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('57d921ca-471a-7daa-4dc8-cdc7326f9916', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('58e014aa-d2e9-f2da-7606-f775b608af9a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('590275eb-74e7-f6b0-6b77-16a2ea4df1be', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5af0a6a1-44e1-c571-a3d4-e713a4c28c47', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5c43e351-0ef6-ed31-eede-a70f0d95e955', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5d7a906f-0457-debf-16cc-8cc7d3591cc9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5ddcc49d-e7e4-4361-8e23-784af9d09b59', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5e581018-b0aa-f5af-a276-04bce7c91ee2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5f020f3a-d3a7-5134-b547-331572363e8e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('5f5e7f62-a9ef-f788-7d07-afabbadb0fad', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6046e3eb-e5f8-fe89-4dab-66435ec03f34', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('614d25ef-5223-81a4-348b-bcfdf2897acd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('621d5ee2-ec4d-c1db-49cf-a49b3d72693d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('63ecfc33-601b-ad09-9d78-112aa1ef13f4', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('647598ba-60d9-01d8-760c-24592ffc6c27', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('64a91391-ce1e-501f-4bda-2ebba3452cc3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('65daff7d-bee6-9eb0-6b14-73e6600829ad', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6609d9e9-c604-2163-6458-89bd9bb93bd1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('66c7c7ed-0fb1-b53a-9a65-ab29dd47e2e8', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6766088f-1bb3-bcbb-b05b-c8f71c1c2924', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('67c9ec3a-5f77-cd99-4a80-89420ec9e32e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('69d37a7e-cc1f-500e-5768-e57b6fabb0dc', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6baa464d-0fde-c9b3-c91c-db76ee89e118', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6c64e880-71dc-9acc-f65c-b83b10e15a29', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6c882143-7f2c-6b4c-22a3-1efd49850382', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6d94fa6b-5851-b680-e57e-b76376d3712c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6e60cc41-5c2d-5dd6-331b-2b9ddc3680ad', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6e918218-fcff-0616-7960-883141a8d6ca', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('6ed3bea3-c94c-fbd1-ac02-f6b389e5fe51', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('71697283-91d4-1c52-b6db-3348c4690813', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('726b16f2-fa0d-1968-8e15-3023810576bb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('74268720-8e17-8c02-727a-9ab210344516', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('74f6f58b-5ef0-2510-a464-821e48c21042', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('757e48dd-3e30-eb14-d402-d169df515eb8', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7795c572-e951-5f14-f881-cf2f07872b82', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('78ac3e40-5908-b351-4a8e-bd6a9ebd40d0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('79a0267e-3384-8876-7473-ab1f55fdbc83', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7c1d56bf-3a00-c127-68a5-6f3a6fc2727a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7c3379d3-9575-df91-4a5d-78621bd7c7ea', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7daf3c02-9061-4ff6-2877-a1fa44fa4a65', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7e1c042b-476a-3c9e-1e7b-2c5df3c89348', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7f6bb3c7-c81f-b439-e804-1caeefa025dd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('7fe2dc65-4508-baf6-14cf-051be7083497', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('821d828e-6133-9e4e-a921-94b4c526ca27', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8242b4a9-657d-a34b-ab6f-2a07dbf4895c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8362f856-f5cd-289b-00cd-d45c84a84780', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('847ca6d5-fef3-d970-a9b4-6558953783f9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('87b37bd6-2edc-d8f0-0e61-4f8f22d8f6d9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8825579f-fc7d-412c-bc32-74034b16b5de', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('88805483-ef38-f504-0b87-9490722d88fe', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('896bb155-f937-acc3-8766-97e295937dad', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('897dadab-8beb-2996-f9e0-4992031c4e12', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8c4543b4-45e6-28c6-407e-d8203b677242', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8c511f1d-9a17-aa9e-5b7b-6917943ca3ac', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', 'a6e130a7-a1ea-abd1-aa46-e6b5c5b63391');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8c7a9390-a08d-3d82-7473-01ea6526ccf9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8cfbd24a-af9a-c215-4cd1-4aaa0cde2533', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8ded4ee9-81c3-f5d0-8855-6dfd251ada29', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8ee57936-9694-c99d-5e86-fb2a9e34fb05', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('8fac5dd6-bc91-bd85-02fb-d13cca171026', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('909211e6-5af5-5a5d-f1fd-a6adcc2c9202', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9287fdb1-6268-b086-80bd-94119a7681fa', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('94089062-2d6a-1d10-48be-5750cbb89efc', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('940c605b-65d7-70e9-d0ec-154024f84808', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('94b2afd5-6b57-26e9-c1d8-7fa668b08cf1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('94efb966-f5b2-29c8-4382-ded15360f3e9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('95060891-2e57-1691-1406-a6ae53955e4f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9574b879-7550-caf5-4571-430001fe7ccf', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('957b2f32-5149-4b3f-d877-d9d63c8f84a1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('973c4872-c7ac-84f5-23b8-c359071d62d9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('98bcc1b9-d949-c3f3-646e-368c32f3ded3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('99a492a0-de45-e49c-63c5-43089474bc33', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9a221b20-6552-1f28-c587-a9d3cbd156d0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9a4d5735-85ec-1b50-ce95-2d7c2d42ed82', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9c642deb-1722-9fc9-4051-67860f8f3a94', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', 'f047ab5d-f553-96e9-9003-6fe15a0a5114');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9e615364-af15-4484-4ad7-029b05c0c717', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('9e77dd6e-ec7d-357e-9ec9-0f5872f40b6b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a0251783-0ed3-accb-407c-4bcc84d98acb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a2b4577d-6fd2-5bab-e6e2-5f2e5936462c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a3291c2f-76c6-efe4-0faf-b2008c4c43be', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a603b74d-ed71-4582-028d-292d126b5ce8', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a952cb8b-e76f-478b-3944-8a20210a0021', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a96cf84e-2d74-c8d7-08a1-fac00d3a3e50', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('a9f23aed-68a5-e6bd-966d-5009cdfc5d38', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('aaf55402-dcf0-c382-bad0-7864e2e45c76', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'ca92d241-6346-1541-212a-e1403a95cb9f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ab19fe50-c320-938b-3771-ad86898eb672', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'c8157abe-83ae-64bb-ddbb-b4681e99c33f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('abc072ab-4bc9-1c55-6482-33d34f31a9f1', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('af249dea-7de3-74a2-1433-014fce1cce5b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('aff9c28f-9185-18a7-7b8f-41959210f1ea', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b0ca6dd6-a58e-35a3-5459-de9a19117992', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '7cc9c738-4673-e0c2-9a01-df7b48b28b7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b117fe73-e202-0085-2977-0f10b6f8dc6b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b130d10f-b64f-2e58-8365-81cc9fc40800', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b1e422f0-7170-90a4-78ee-0b68f99be754', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b24dec4a-cd0c-9106-0c12-f4bb68bd3cbb', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b2696b0d-e253-4de4-7fff-8fccc68320b2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Consignee', 'a2d100d7-15e0-44de-45a3-3d732cef7332');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b30a8f44-62dc-824b-0a29-5abe231960cf', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b456458b-9b60-bbd6-f93d-b46d00e54530', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b470fe19-5297-c9cb-b037-39178256df53', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b496394a-7d2b-5faf-6700-edf0e478bc50', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b55d88d9-efe7-43fd-bc11-279be867f93c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b59380f8-1560-c3ad-9f59-8bb1c46b0f35', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b697cbb0-6ce6-5558-9f99-f60d9c2bc6ec', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b6a9e689-e0ca-2201-ec70-f04f9d2385ae', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b6ebbb91-c28c-f156-2859-b065dccd4dca', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b78b7f5a-800a-db8f-3f30-aa7ef4dbb34b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('b7b14efa-dcea-e412-85d4-8b72fbadc2cc', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ba142a8c-e51a-92ff-25a2-3baa3e567086', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bad28780-396c-e7e1-8307-031ef5265f06', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('baf7e271-30b8-8d8a-b3d0-0b2b1d212e7d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bc3d006a-fdc8-f595-0209-2dd959b3366a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '9119a3f4-394d-695c-5267-b7cd696f7ba6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bd154f5f-8d67-519e-570a-8112d5385e39', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bd4d4477-d42d-01a8-35ef-48330dc161ba', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bd92ab53-8c00-df93-d90d-fc31bd1c44c7', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '5810f4b9-0e60-a000-5697-53177df94bb9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bf0f554a-88fd-b0af-9528-f20d9652bde6', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('bfbbb097-ad4d-b483-42ca-3cd4f4b4f33e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c0cce1f2-bc75-ba23-f821-df85fa227bef', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'a155eb78-8542-4978-a8dd-bd2b28929738');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c120a136-1c7c-16c7-3ffc-1b4d687a9ad8', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Shipper', '3ff1f234-1eda-6801-33a1-ce8538693d6e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c141b448-7b07-b9ff-1419-f42969002eef', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '81d22eff-dca4-0ac0-bf3a-4f3df458d908');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c27e7f95-c695-de79-1a6c-77f83520649c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c50d5427-463b-3112-578d-f4443af0de0f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c55e11c1-445b-2ceb-fa9c-42fa7d32a375', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c638329f-82cc-c199-4452-fb571b1e365f', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('c63962d0-bebe-bba3-bd58-72e8ff1ff20c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'ee5f4e49-e6b9-7f9e-ded2-c27c84a76dc0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('caf8c270-a9dd-ea27-05cf-946f86c3afa2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', '122ef33e-f8f5-fb84-25cb-5176d10c40dc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('cb7965bb-3d07-6a52-ec63-2e6e798011a8', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ce4c2405-a127-ce82-7591-29e60d63c47e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '65dfa9ad-25d2-4a79-f36c-410bf77ddddc');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('cfd48abf-7e89-405a-fa17-6ec0c7b9857a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d1985ab7-5eab-a949-b01a-5b6057cf2274', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d1c40053-b297-3747-07fa-86cfa2eb4e46', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '93f658d0-ee65-42f2-a3da-646f54feb2d6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d39f04a9-f04e-a4c7-e1fc-bacee5a3d6d2', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d44353bb-5eac-b868-7861-6f5878860718', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d46df13e-3333-6765-0a90-e3793343be54', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '160ab007-ea1a-063f-52dd-e10cf1dcd00b');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d4c971c8-dc75-0e08-8eb3-6140558f93e4', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'bdcf5a98-4a99-386e-fe2f-1da80f99aa1e');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d58ce3f5-9e33-fe8a-4981-65037d44af2d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d5fb8f14-501b-77c6-6532-dd5c707bcfb6', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '1145e12a-7014-9221-40ff-2d3754ffd29f');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d63cebae-75bb-a0a1-e8e2-a091564a59b9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '72c736f3-0830-08da-2ba1-aef0b2ec3e81');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d6d69f1f-2b54-1101-8bc1-aa035746dac0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Carrier', '95d43dff-917c-dd4d-eeb5-c49731913c7c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('d9c3071a-b3cc-d307-9ae7-bf179e989491', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'e112508b-beea-c895-9061-da03cb427883');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('da427540-6fc5-453a-337a-f95f7789891e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '557bba04-d0ce-91b0-c765-277b91222bf7');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('daa5a48e-7faf-f440-00ed-d6f290ef53d6', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Customer', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('db2d9016-197b-30b9-34ed-f61a6aa010c6', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', '741f2950-8b45-5e2d-8065-fe163db727a6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('dcc8ba2c-2edb-5dd3-d347-3febfaae31dd', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('de251b6a-681e-f468-c759-ba9e3a135a0c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', '3eac8579-97de-40ff-10b5-ba572613f061');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('dfa110eb-b551-ad55-b4bb-b66a15c62f0a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', 'e7eb07a5-bc20-230e-0e88-997310452f02');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e29f23be-4fe7-e2d5-fab5-17f0d6ee3221', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e2c15bd3-cd74-a740-c820-d98d7f2c1755', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'ThirdParty', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e4eff5c6-31ee-5c3e-5a4a-31abe13b6b86', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'bd6cab83-dabc-1619-6d2c-e324cd7d01a1');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e52c2c06-c8d4-1643-0c5a-142353c4ae09', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', '0334bbd0-ffd8-1995-7db7-fdf3d95d6729');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e5fc0f1b-12f4-d1f7-737c-9a3781cb5b2a', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '5fa67043-8f48-599c-fded-6ea2b5ca1488');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e6a7d610-7db4-1053-729a-a996c02bbae7', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'CustomsAgency', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('e9d56994-87d7-dc46-902e-f0ba14c43496', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '947b0092-be0a-4670-bf9f-46eda7d14ab0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ead4bb59-96b2-a7f7-0cb0-e1b86d186e0b', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ec7b64b1-483f-47eb-f7eb-de0b24bdc782', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', '4900513d-d3e1-f3e2-5b43-c0e000a389f0');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('eca7e100-5a9b-2cc8-59b7-e23ac0083c56', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Consignee', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ed906fdc-af9a-3f8b-978c-86c602fbccf9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('edb8d86b-9a08-b675-7f36-4325d664c9f9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Shipper', 'cfd10f40-3f50-8ebf-fed3-ec080f6c29b5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('ef578090-e366-8eed-a9a6-648604da07b0', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, 'FreightForwarder', 'Consignee', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f08e09ec-8049-1256-1870-600628cf1a60', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', '07343dd5-fa12-c8e8-a80f-26f110926989');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f1109e2e-2cd5-3c56-7db7-22941a2771d6', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'CustomsAgency', '19ef0081-eb79-5a7c-e09c-62482731cd52');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f40bcf95-a335-b13c-1a3a-6bc1870df8b9', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'Carrier', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f5cb4db0-d3a1-3725-3473-fe15b924dba3', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', '032cc54e-cbbe-5272-cab1-32a3bb291f82');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f6832259-179f-a8fb-8ffe-b6abc4c3a44e', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Customer', 'fe2d4e42-d3b0-aac5-9026-5f918c61aca6');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f69dcfc4-e4a6-f436-b169-b9322daad293', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '1c3dd706-407f-2945-5b68-bea5dfd584e9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f700f67e-c7c1-2951-e930-b482ff2b6eed', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Denied', NULL, NULL, NULL, 'Carrier', 'cc502303-816b-de21-d681-447330a0e8c9');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('f9c715ab-5fe7-8cc0-cd61-f45973771151', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'OnGrant', NULL, NULL, NULL, 'ThirdParty', 'cc4c1544-f4f5-8f00-9bb1-8241a64e8d54');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('fa22d583-b3e9-dc80-c89c-7637781279c5', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'Shipper', '72baee95-6d9c-f671-270d-67a5ddbb42e5');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('fb8f7c92-ff67-bb8b-de6a-41ebfe461a18', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'ThirdParty', 'ac5533f1-b185-fe85-07a0-89a89adf382d');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('fc9814e8-e8cc-95ce-a023-248a48704c2c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '2ac5eecb-38d8-6ec0-6d82-f03f854e976c');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('fe48f4fa-7627-4176-7134-0cc98899776d', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', '6678139a-eb5f-24c3-d404-7ffcfbf1dde8');
    INSERT INTO "ShipmentAccessRules" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Level", "ModifiedAt", "ModifiedBy", "OrganizationType", "Role", "ShipmentActionId")
    VALUES ('febe8c1e-5992-1534-a39c-ed590f98418c', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'Allowed', NULL, NULL, NULL, 'CustomsAgency', 'cbecbaf4-b71c-43d6-aae2-c344d6bf19ad');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('920f9473-1eaa-e2fa-664c-630fda43dcb3', '11111111-0007-0007-0007-000000000006', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('b65a3c60-f2d7-5b2c-22d2-c24a2594c519', '11111111-0007-0007-0007-000000000008', 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('1c70376e-431c-3994-dd5c-464e0f1405f9', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000030');
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('3c47e0d2-5bc6-59f8-dc0c-12f7eb10e017', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000010');
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('3f8e776c-1c97-8168-2886-4ab337a4749e', 'a084c0fb-db38-005b-3d5f-926ed28ed6df', 'OrgViewer', 'd4e5f6a7-0004-0004-0004-000000000011');
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('a87fd524-45a0-5b06-c5bc-b848addc0849', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000020');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000050', 'c3d4e5f6-0003-0003-0003-000000000050', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'admin@logisticaandina.cl', NULL, NULL, 'Andrea', TRUE, FALSE, NULL, 'Andina', NULL, NULL, 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'admin@logisticaandina.cl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000009', '11111111-0007-0007-0007-000000000007', 'HLXU2023002', '20DV', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-020302', 'OnBoard', NULL, NULL, 17400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000011', 150000.0, '11111111-0007-0007-0007-000000000007', 'THC', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Terminal Handling Charge - 20DV (export)', TRUE, NULL, NULL, 'Pending', 28500.0, 19.0, 178500.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('81adf541-7dc4-a75b-098f-d639ee7f15ab', '11111111-0007-0007-0007-000000000007', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('e2805dbb-3327-1b7d-aecd-7165a75c5d82', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000050');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE INDEX "IX_Users_ClientId_MembershipStatus" ON "Users" ("ClientId", "MembershipStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE UNIQUE INDEX "IX_Clients_MatchCode" ON "Clients" ("MatchCode") WHERE "MatchCode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE INDEX "IX_Clients_RegistrationStatus" ON "Clients" ("RegistrationStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE INDEX "IX_BillsOfLading_BookingNumber" ON "BillsOfLading" ("BookingNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE INDEX "IX_OrganizationDocuments_ClientId" ON "OrganizationDocuments" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE UNIQUE INDEX "IX_ShipmentAccessRules_ShipmentActionId_Role_OrganizationType" ON "ShipmentAccessRules" ("ShipmentActionId", "Role", "OrganizationType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE UNIQUE INDEX "IX_ShipmentActions_Code" ON "ShipmentActions" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE UNIQUE INDEX "IX_ShipmentRoles_BillOfLadingId_ClientId_Role" ON "ShipmentRoles" ("BillOfLadingId", "ClientId", "Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    CREATE INDEX "IX_ShipmentRoles_ClientId" ON "ShipmentRoles" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "UserRoles" ("Id", "RoleName", "RoleId", "UserId")
    SELECT gen_random_uuid(), 'OrgAdmin', r."Id", u."Id"
    FROM "Users" u
    JOIN "Clients" c ON c."Id" = u."ClientId"
    LEFT JOIN "Roles" r ON r."Code" = 'OrgAdmin'
    WHERE c."OrganizationType" <> 'Internal'
      AND u."MembershipStatus" = 'Active'
      AND u."DeletedAt" IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM "UserRoles" ur
          WHERE ur."UserId" = u."Id"
            AND ur."RoleName" IN ('OrgAdmin', 'OrgOperator', 'OrgViewer'));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005230422_AddOrganizationsAndShipmentAccess') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261005230422_AddOrganizationsAndShipmentAccess', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    DELETE FROM "ShipmentAccessRules"
    WHERE "Id" = '3072d047-9ee7-d892-5ff6-4adf7ecd0b54';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    ALTER TABLE "Payments" ADD "AccessGrantId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    ALTER TABLE "Payments" ADD "OnBehalfOfClientId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "AccessAuditEntries" (
        "Id" uuid NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "EventType" character varying(50) NOT NULL,
        "BillOfLadingId" uuid,
        "BlNumber" character varying(50),
        "BookingNumber" character varying(50),
        "AccessGrantId" uuid,
        "VisibilityWideningId" uuid,
        "GrantorClientId" uuid,
        "GranteeClientId" uuid,
        "ActorUserId" uuid,
        "ActorEmail" character varying(256),
        "ActorClientId" uuid,
        "Details" text,
        CONSTRAINT "PK_AccessAuditEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "AccessGrants" (
        "Id" uuid NOT NULL,
        "GrantorClientId" uuid NOT NULL,
        "GrantorRole" character varying(30) NOT NULL,
        "GranteeClientId" uuid NOT NULL,
        "BillOfLadingId" uuid,
        "BookingNumber" character varying(50),
        "GrantType" character varying(20) NOT NULL,
        "IntendedRole" character varying(30),
        "ActionCodes" character varying(4000),
        "CeilingActionCodes" character varying(4000) NOT NULL,
        "ValidityType" character varying(20) NOT NULL,
        "ValidFrom" timestamp with time zone NOT NULL,
        "ValidTo" timestamp with time zone,
        "DurationDays" integer,
        "Status" character varying(20) NOT NULL,
        "GrantedByUserId" uuid,
        "ParentGrantId" uuid,
        "DefaultGranteeId" uuid,
        "IsMandate" boolean NOT NULL,
        "TermsVersion" character varying(50),
        "TermsAcceptedAt" timestamp with time zone,
        "TermsAcceptedByUserId" uuid,
        "EndedAt" timestamp with time zone,
        "EndedByUserId" uuid,
        "EndReason" character varying(20),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_AccessGrants" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AccessGrants_AccessGrants_ParentGrantId" FOREIGN KEY ("ParentGrantId") REFERENCES "AccessGrants" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AccessGrants_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AccessGrants_Clients_GranteeClientId" FOREIGN KEY ("GranteeClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_AccessGrants_Clients_GrantorClientId" FOREIGN KEY ("GrantorClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "DefaultGrantees" (
        "Id" uuid NOT NULL,
        "GrantorClientId" uuid NOT NULL,
        "GranteeClientId" uuid NOT NULL,
        "ActionCodes" character varying(4000),
        "DurationDays" integer,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_DefaultGrantees" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DefaultGrantees_Clients_GranteeClientId" FOREIGN KEY ("GranteeClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_DefaultGrantees_Clients_GrantorClientId" FOREIGN KEY ("GrantorClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "OpenAccessSettings" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "ActionCodes" character varying(4000),
        "ChangedAt" timestamp with time zone,
        "ChangedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_OpenAccessSettings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OpenAccessSettings_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "ShipmentAssociations" (
        "Id" uuid NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "AssociatedByUserId" uuid NOT NULL,
        "AssociatedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentAssociations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ShipmentAssociations_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ShipmentAssociations_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE TABLE "VisibilityWidenings" (
        "Id" uuid NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "GrantorClientId" uuid NOT NULL,
        "GrantorRole" character varying(30) NOT NULL,
        "TargetRole" character varying(30) NOT NULL,
        "ActionCode" character varying(100) NOT NULL,
        "OriginGrantId" uuid,
        "Status" character varying(20) NOT NULL,
        "GrantedByUserId" uuid,
        "EndedAt" timestamp with time zone,
        "EndedByUserId" uuid,
        "EndReason" character varying(20),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_VisibilityWidenings" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VisibilityWidenings_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VisibilityWidenings_Clients_GrantorClientId" FOREIGN KEY ("GrantorClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('cccccccc-0012-0012-0012-000000000031', 'cccccccc-0012-0012-0012-000000000001', 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', '11111111-0007-0007-0007-000000000001', 'HLCUVAL250100123', 'HLCUBKG2501001', '{"grantType":"Individual","source":"seed"}', 'GrantCreated', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('cccccccc-0012-0012-0012-000000000032', 'cccccccc-0012-0012-0012-000000000002', 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', '11111111-0007-0007-0007-000000000002', 'HLCUVAL250200456', 'HLCUBKG2502004', '{"grantType":"Individual","isMandate":true,"source":"seed"}', 'GrantCreated', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('cccccccc-0012-0012-0012-000000000033', 'cccccccc-0012-0012-0012-000000000002', 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', '11111111-0007-0007-0007-000000000002', 'HLCUVAL250200456', 'HLCUBKG2502004', '{"termsVersion":"MANDATO-2026-10"}', 'MandateTermsAccepted', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('cccccccc-0012-0012-0012-000000000034', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, NULL, '{"durationDays":180,"source":"seed"}', 'DefaultGranteeAdded', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('cccccccc-0012-0012-0012-000000000035', NULL, NULL, 'seed', NULL, NULL, NULL, NULL, '{"isEnabled":true,"source":"seed"}', 'OpenAccessEnabled', NULL, 'c3d4e5f6-0003-0003-0003-000000000040', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "AccessGrants" ("Id", "ActionCodes", "BillOfLadingId", "BookingNumber", "CeilingActionCodes", "CreatedAt", "CreatedBy", "DefaultGranteeId", "DeletedAt", "DeletedBy", "DurationDays", "EndReason", "EndedAt", "EndedByUserId", "GrantType", "GrantedByUserId", "GranteeClientId", "GrantorClientId", "GrantorRole", "IntendedRole", "IsMandate", "ModifiedAt", "ModifiedBy", "ParentGrantId", "Status", "TermsAcceptedAt", "TermsAcceptedByUserId", "TermsVersion", "ValidFrom", "ValidTo", "ValidityType")
    VALUES ('cccccccc-0012-0012-0012-000000000001', 'bl-issuance.view,import-demurrage.pay,local-charges-mandatory.pay,release-requirements.view,shipment.view,tracking.view', '11111111-0007-0007-0007-000000000001', 'HLCUBKG2501001', 'access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Individual', 'd4e5f6a7-0004-0004-0004-000000000010', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', 'Customer', NULL, FALSE, NULL, NULL, NULL, 'Active', NULL, NULL, NULL, TIMESTAMPTZ '2026-10-01T12:00:00Z', TIMESTAMPTZ '2027-03-31T23:59:00Z', 'UntilDate');
    INSERT INTO "AccessGrants" ("Id", "ActionCodes", "BillOfLadingId", "BookingNumber", "CeilingActionCodes", "CreatedAt", "CreatedBy", "DefaultGranteeId", "DeletedAt", "DeletedBy", "DurationDays", "EndReason", "EndedAt", "EndedByUserId", "GrantType", "GrantedByUserId", "GranteeClientId", "GrantorClientId", "GrantorRole", "IntendedRole", "IsMandate", "ModifiedAt", "ModifiedBy", "ParentGrantId", "Status", "TermsAcceptedAt", "TermsAcceptedByUserId", "TermsVersion", "ValidFrom", "ValidTo", "ValidityType")
    VALUES ('cccccccc-0012-0012-0012-000000000002', 'freight.pay,local-charges-mandatory.pay,shipment.view', '11111111-0007-0007-0007-000000000002', 'HLCUBKG2502004', 'access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Individual', 'd4e5f6a7-0004-0004-0004-000000000010', 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', 'Customer', NULL, TRUE, NULL, NULL, NULL, 'Active', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'd4e5f6a7-0004-0004-0004-000000000010', 'MANDATO-2026-10', TIMESTAMPTZ '2026-10-01T12:00:00Z', TIMESTAMPTZ '2027-03-31T23:59:00Z', 'UntilDate');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "DefaultGrantees" ("Id", "ActionCodes", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DurationDays", "GranteeClientId", "GrantorClientId", "IsActive", "ModifiedAt", "ModifiedBy")
    VALUES ('cccccccc-0012-0012-0012-000000000011', NULL, TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 180, 'c3d4e5f6-0003-0003-0003-000000000030', 'c3d4e5f6-0003-0003-0003-000000000010', TRUE, NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "OpenAccessSettings" ("Id", "ActionCodes", "ChangedAt", "ChangedByUserId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('cccccccc-0012-0012-0012-000000000021', 'bl-issuance.view,local-charges-mandatory.pay,release-requirements.view,shipment.view,tracking.view', TIMESTAMPTZ '2026-10-01T12:00:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000040', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    UPDATE "Payments" SET "AccessGrantId" = NULL, "OnBehalfOfClientId" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('ddb82640-b032-a4fd-8e31-17c5f6b0ca8a', 'org.access.manage', 'org.access.manage');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('fd185e2b-35f8-cf89-f39c-5d8158027c0a', 'ddb82640-b032-a4fd-8e31-17c5f6b0ca8a', 'e200b49e-343b-36a4-fbcc-e10fa786728c');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessAuditEntries_AccessGrantId" ON "AccessAuditEntries" ("AccessGrantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessAuditEntries_BillOfLadingId_OccurredAt" ON "AccessAuditEntries" ("BillOfLadingId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessAuditEntries_BookingNumber" ON "AccessAuditEntries" ("BookingNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessAuditEntries_GranteeClientId" ON "AccessAuditEntries" ("GranteeClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessAuditEntries_GrantorClientId" ON "AccessAuditEntries" ("GrantorClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE UNIQUE INDEX "IX_AccessGrants_BillOfLadingId_GrantorClientId_GranteeClientId" ON "AccessGrants" ("BillOfLadingId", "GrantorClientId", "GranteeClientId") WHERE "Status" IN ('Active', 'PendingAcceptance') AND "BillOfLadingId" IS NOT NULL AND "GrantType" <> 'EarlyBooking';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessGrants_BookingNumber" ON "AccessGrants" ("BookingNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessGrants_GranteeClientId_Status_ValidTo" ON "AccessGrants" ("GranteeClientId", "Status", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessGrants_GrantorClientId_Status" ON "AccessGrants" ("GrantorClientId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessGrants_ParentGrantId" ON "AccessGrants" ("ParentGrantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_AccessGrants_Status_ValidTo" ON "AccessGrants" ("Status", "ValidTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_DefaultGrantees_GranteeClientId" ON "DefaultGrantees" ("GranteeClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE UNIQUE INDEX "IX_DefaultGrantees_GrantorClientId_GranteeClientId" ON "DefaultGrantees" ("GrantorClientId", "GranteeClientId") WHERE "IsActive" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE UNIQUE INDEX "IX_OpenAccessSettings_ClientId" ON "OpenAccessSettings" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE UNIQUE INDEX "IX_ShipmentAssociations_BillOfLadingId_ClientId" ON "ShipmentAssociations" ("BillOfLadingId", "ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_ShipmentAssociations_ClientId" ON "ShipmentAssociations" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_VisibilityWidenings_BillOfLadingId_TargetRole_Status" ON "VisibilityWidenings" ("BillOfLadingId", "TargetRole", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_VisibilityWidenings_GrantorClientId" ON "VisibilityWidenings" ("GrantorClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    CREATE INDEX "IX_VisibilityWidenings_OriginGrantId" ON "VisibilityWidenings" ("OriginGrantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006000748_AddThirdPartyAccessGrants') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006000748_AddThirdPartyAccessGrants', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    DROP INDEX "IX_WarehouseChanges_BillOfLadingId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "BatchId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "CompletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "ContainerNumber" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "EntitlementReference" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "EntitlementSource" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "IsFree" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "RequestedByClientId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "RequestedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "TariffCode" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "WarehouseChanges" ADD "TariffSource" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "DemurrageCharges" ADD "InvoiceDueDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "DemurrageCharges" ADD "InvoiceNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    ALTER TABLE "DemurrageCharges" ADD "InvoicedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "AppliedExemptions" (
        "Id" uuid NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "LocalChargeId" uuid,
        "ConceptCode" character varying(50) NOT NULL,
        "ExemptParty" character varying(30) NOT NULL,
        "PartyTaxId" character varying(20) NOT NULL,
        "PartyMatchCode" character varying(20),
        "ExemptAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "ConditionAmount" numeric(18,2),
        "ConditionCurrency" character varying(5),
        "ConditionValidFrom" date NOT NULL,
        "ConditionValidTo" date,
        "Source" character varying(20) NOT NULL,
        "PayerClientId" uuid,
        "AppliedByUserId" uuid,
        "AppliedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AppliedExemptions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AppliedExemptions_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "BusinessHolidays" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "Date" date NOT NULL,
        "Name" character varying(150) NOT NULL,
        CONSTRAINT "PK_BusinessHolidays" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "ChargeConcepts" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Category" character varying(30) NOT NULL,
        "Countries" character varying(20) NOT NULL,
        "NexusTariff" boolean NOT NULL,
        "NexusExemptible" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ChargeConcepts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "ExchangeRateRecords" (
        "Id" uuid NOT NULL,
        "TransactionType" character varying(30) NOT NULL,
        "TransactionId" uuid NOT NULL,
        "FromCurrency" character varying(5) NOT NULL,
        "ToCurrency" character varying(5) NOT NULL,
        "Rate" numeric(18,6) NOT NULL,
        "EffectiveDate" date NOT NULL,
        "Source" character varying(30) NOT NULL,
        "Approved" boolean NOT NULL,
        "SourceAmount" numeric(18,2) NOT NULL,
        "ConvertedAmount" numeric(18,2) NOT NULL,
        "CapturedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExchangeRateRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "InternalChargeRules" (
        "Id" uuid NOT NULL,
        "RuleType" character varying(40) NOT NULL,
        "Country" character varying(5) NOT NULL,
        "TaxId" character varying(20),
        "MatchCode" character varying(20),
        "AccountName" character varying(200),
        "Reason" character varying(500),
        "MaxUsesPerBl" integer,
        "ValidFrom" date NOT NULL,
        "ValidTo" date,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_InternalChargeRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "MaintainerChangeLogs" (
        "Id" uuid NOT NULL,
        "Maintainer" character varying(50) NOT NULL,
        "EntityId" uuid NOT NULL,
        "Action" character varying(20) NOT NULL,
        "PreviousValue" text,
        "NewValue" text,
        "ChangedAt" timestamp with time zone NOT NULL,
        "ChangedByUserId" uuid,
        "ChangedBy" character varying(256) NOT NULL,
        CONSTRAINT "PK_MaintainerChangeLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "Tariffs" (
        "Id" uuid NOT NULL,
        "ConceptCode" character varying(50) NOT NULL,
        "Code" character varying(20),
        "Country" character varying(5) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "ContainerType" character varying(10),
        "Description" character varying(300),
        "Amount" numeric(18,2) NOT NULL,
        "TierUnit" character varying(20) NOT NULL,
        "TierMode" character varying(20) NOT NULL,
        "ValidFrom" date NOT NULL,
        "ValidTo" date,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_Tariffs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "WarehouseChangeBatches" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "RequestedByUserId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL,
        "TotalItems" integer NOT NULL,
        "ProcessedItems" integer NOT NULL,
        "SucceededItems" integer NOT NULL,
        "FailedItems" integer NOT NULL,
        "StartedAt" timestamp with time zone,
        "CompletedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_WarehouseChangeBatches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "TariffTiers" (
        "Id" uuid NOT NULL,
        "TariffId" uuid NOT NULL,
        "FromUnit" integer NOT NULL,
        "ToUnit" integer,
        "Amount" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_TariffTiers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TariffTiers_Tariffs_TariffId" FOREIGN KEY ("TariffId") REFERENCES "Tariffs" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE TABLE "WarehouseChangeBatchItems" (
        "Id" uuid NOT NULL,
        "BatchId" uuid NOT NULL,
        "LineNumber" integer NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "BillOfLadingId" uuid,
        "ContainerNumber" character varying(20),
        "FromWarehouse" character varying(100),
        "ToWarehouse" character varying(100) NOT NULL,
        "TariffCode" character varying(20),
        "Status" character varying(20) NOT NULL,
        "ErrorCode" character varying(100),
        "ErrorMessage" character varying(500),
        "WarehouseChangeId" uuid,
        "ProcessedAt" timestamp with time zone,
        CONSTRAINT "PK_WarehouseChangeBatchItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WarehouseChangeBatchItems_WarehouseChangeBatches_BatchId" FOREIGN KEY ("BatchId") REFERENCES "WarehouseChangeBatches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000009', 'HLCUSAI260400910', NULL, 'HLCUBKG2604091', 'c3d4e5f6-0003-0003-0003-000000000010', 'Importadora Demo SpA', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-08-20T00:00:00Z', TIMESTAMPTZ '2026-07-15T00:00:00Z', 4200.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Santiago, Chile', 'San Antonio (CLSAI)', 'Hamburg (DEHAM)', 'Import', 'Hamburg Industrial Supplies GmbH', 'Arrived', 'Rio de Janeiro Express', NULL, '2608E');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000010', 'HLCUSAI260401020', 'Master', 'HLCUBKG2604102', 'c3d4e5f6-0003-0003-0003-000000000010', 'Delfin Logística SpA', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-09-28T00:00:00Z', TIMESTAMPTZ '2026-08-20T00:00:00Z', 3100.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Santiago, Chile', 'San Antonio (CLSAI)', 'Ningbo (CNNGB)', 'Import', 'Ningbo Home Goods Co.', 'Arrived', 'Valparaiso Express', NULL, '2609E');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('00667b14-f55b-c1ab-079d-cfaf973e784f', 'BO', DATE '2026-01-01', 'Año Nuevo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('0345893c-bf3c-20b2-929c-4c6760c12ea6', 'CL', DATE '2026-09-18', 'Independencia Nacional');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('04647fd3-474f-5b22-d730-b54109e14665', 'CL', DATE '2026-08-15', 'Asunción de la Virgen');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('0921c177-75bf-dc52-bcde-695270c9da2b', 'CL', DATE '2026-01-01', 'Año Nuevo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('0b11ee5e-ed51-d918-8fd8-86819bad595b', 'CL', DATE '2026-07-16', 'Virgen del Carmen');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('1fe1f1c8-cc7a-56d2-a4d5-b4300f67b825', 'BO', DATE '2026-04-03', 'Viernes Santo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('25f095d5-b815-34db-4917-f01f716a83a4', 'BO', DATE '2026-11-02', 'Día de Todos los Difuntos');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('2bf84b88-d4f8-89e6-50ed-ef3e8446b9d7', 'CL', DATE '2026-04-04', 'Sábado Santo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('375bf084-1ff5-be44-5b63-78ed3951d5cc', 'CL', DATE '2026-09-19', 'Glorias del Ejército');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('554e98ef-1bbc-9240-df24-5df13af1398f', 'BO', DATE '2026-06-21', 'Año Nuevo Andino Amazónico');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('56b5e7cc-26d5-c636-17c9-121b6a9951ff', 'BO', DATE '2026-08-06', 'Día de la Independencia');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('5c0a0896-aa52-757f-2738-df5b08cbba02', 'CL', DATE '2026-10-31', 'Día de las Iglesias Evangélicas');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('5f3c085f-eaad-76cb-8bc0-ec225e6b6af3', 'CL', DATE '2026-12-08', 'Inmaculada Concepción');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('61669654-f613-9ae9-e1a5-cdc9a5314569', 'CL', DATE '2026-10-12', 'Encuentro de Dos Mundos');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('6dd776f4-26aa-9450-5270-b96aa13d7e3f', 'BO', DATE '2026-02-17', 'Carnaval');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('70795c68-c039-4652-1bf2-ae781c1991bf', 'CL', DATE '2026-06-29', 'San Pedro y San Pablo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('70913438-b0f5-b158-5551-b21fb3ec7f06', 'BO', DATE '2026-05-01', 'Día del Trabajo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('7a29e195-fc46-f98f-543c-411f66e2769c', 'BO', DATE '2026-01-22', 'Día del Estado Plurinacional');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('904e1cbd-fff3-71ee-0e11-51ffddcdcb0a', 'BO', DATE '2026-02-16', 'Carnaval');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('96dab692-42ad-702b-ff41-67c84a038868', 'CL', DATE '2026-12-25', 'Navidad');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('9b73b249-c4b8-8bad-7600-b824e9f8b535', 'CL', DATE '2026-11-01', 'Día de Todos los Santos');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('c9119139-f40f-ce2b-3016-b5fd7f6eabf9', 'CL', DATE '2026-05-21', 'Día de las Glorias Navales');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('d8361636-b0a2-28a8-00e4-f0d30ed0c028', 'BO', DATE '2026-12-25', 'Navidad');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('ea454567-9647-98ec-1622-1e19115576c7', 'CL', DATE '2026-05-01', 'Día del Trabajo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('edd6c2a7-a705-72e7-7701-5f5f850193b5', 'CL', DATE '2026-04-03', 'Viernes Santo');
    INSERT INTO "BusinessHolidays" ("Id", "Country", "Date", "Name")
    VALUES ('f03996d9-d5d3-b056-8dad-6a2d2e4c02c4', 'BO', DATE '2026-06-04', 'Corpus Christi');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('1da4f295-4e5f-e846-1734-a1aa14450474', 'LocalCharge', 'GATE_OUT', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 30, TRUE, NULL, NULL, 'Gate Out', TRUE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('24bc9c98-bd06-08fb-4e1a-cadf22532fc1', 'LocalCharge', 'IPO', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 40, TRUE, NULL, NULL, 'Recargo IPO', FALSE, TRUE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('2d69da90-bad5-0a9d-8dbd-c19af4d4dd3f', 'LocalCharge', 'TRANSIT_FEE', 'BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 90, TRUE, NULL, NULL, 'Documentación de tránsito Bolivia', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('2f0b15d3-5a45-9082-ca75-e3498d34d226', 'Demurrage', 'ADVANCE_DEMURRAGE_BO', 'BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 120, TRUE, NULL, NULL, 'Demoras anticipadas', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('31440d52-413f-6736-6b57-cfa6bd491b3a', 'Demurrage', 'DEMURRAGE', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 110, TRUE, NULL, NULL, 'Demurrage (sobreestadía)', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('55630f14-a5e2-fc47-0de6-3eee5fa6b0ff', 'LocalCharge', 'ISPS', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 80, TRUE, NULL, NULL, 'Recargo de seguridad ISPS', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('5f2a94ad-43d2-ecc1-039d-ce031cc98373', 'LocalCharge', 'EDS', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 20, TRUE, NULL, NULL, 'EDS', TRUE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('5f6f4be2-432f-bac4-ff92-4619f9d91214', 'LocalCharge', 'BL_FEE', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 70, TRUE, NULL, NULL, 'Emisión de BL', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('61652e31-3c9a-7798-fa4d-80996f4703a6', 'Service', 'LATE_ARRIVAL', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 140, TRUE, NULL, NULL, 'Late Arrival', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('7ff3d950-52ea-607a-68ea-3ef09e8345c1', 'LocalCharge', 'THC_RF', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 60, TRUE, NULL, NULL, 'Terminal Handling Charge Reefer', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('a2d1533d-a372-0df8-52cc-17a7b169e953', 'LocalCharge', 'THC', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 50, TRUE, NULL, NULL, 'Terminal Handling Charge', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('b6480268-68ac-c09e-0f6b-3335d8cd7b7e', 'LocalCharge', 'GATE_IN', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 10, TRUE, NULL, NULL, 'Gate In', TRUE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('e6a87f12-1a29-20ca-f10f-2dc2b24d74ed', 'Service', 'WAREHOUSE_CHANGE', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 130, TRUE, NULL, NULL, 'Cambio de almacén', FALSE, TRUE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('fa247c08-999c-0d02-1d37-708d43c0a03c', 'Demurrage', 'MHD', 'CL,BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 100, TRUE, NULL, NULL, 'MHD', FALSE, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'contacto@distribuidoraandes.cl', TRUE, TRUE, 'MC000202', NULL, NULL, 'Distribuidora Andes Crédito SpA', 'CL', 'Customer', NULL, 'Approved', NULL, NULL, '76.000.002-2', 'RUT', NULL, NULL);
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000070', NULL, NULL, TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 'operaciones@globalforwarding.cl', TRUE, TRUE, 'MC000303', NULL, NULL, 'Global Forwarding Chile SpA', 'CL', 'FreightForwarder', NULL, 'Approved', NULL, NULL, '76.000.003-3', 'RUT', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "DemurrageCharges" SET "InvoiceDueDate" = NULL, "InvoiceNumber" = NULL, "InvoicedAt" = NULL
    WHERE "Id" = '44444444-000a-000a-000a-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "DemurrageCharges" SET "InvoiceDueDate" = NULL, "InvoiceNumber" = NULL, "InvoicedAt" = NULL
    WHERE "Id" = '44444444-000a-000a-000a-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "DemurrageCharges" SET "InvoiceDueDate" = NULL, "InvoiceNumber" = NULL, "InvoicedAt" = NULL
    WHERE "Id" = '44444444-000a-000a-000a-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "InternalChargeRules" ("Id", "AccountName", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "MatchCode", "MaxUsesPerBl", "ModifiedAt", "ModifiedBy", "Reason", "RuleType", "TaxId", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0015-0015-0015-000000000001', 'Importadora Demo SpA', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'MC100010', 1, NULL, NULL, 'Convenio comercial: un cambio de almacén gratuito por BL', 'FreeWarehouseChange', '76123456-7', DATE '2026-01-01', NULL);
    INSERT INTO "InternalChargeRules" ("Id", "AccountName", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "MatchCode", "MaxUsesPerBl", "ModifiedAt", "ModifiedBy", "Reason", "RuleType", "TaxId", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0015-0015-0015-000000000002', 'Comercial Altiplano SRL', 'BO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'MC100020', NULL, NULL, NULL, 'Regla interna Bolivia: demoras anticipadas obligatorias antes del CLD', 'AdvanceDemurrageRequired', '1023456017', DATE '2026-01-01', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "LocalCharges" SET "ChargeType" = 'GATE_IN'
    WHERE "Id" = '33333333-0009-0009-0009-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "LocalCharges" SET "ChargeType" = 'GATE_IN'
    WHERE "Id" = '33333333-0009-0009-0009-000000000012';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000016', 150.0, '11111111-0007-0007-0007-000000000001', 'IPO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Recargo IPO', FALSE, NULL, NULL, 'Pending', 0.0, 0.0, 150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000017', 85000.0, '11111111-0007-0007-0007-000000000001', 'MHD', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'MHD - HLXU1234567 / HLXU7654321', TRUE, NULL, NULL, 'Pending', 16150.0, 19.0, 101150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000021', 450.0, '11111111-0007-0007-0007-000000000004', 'MHD', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'MHD - HLXU8899001', FALSE, NULL, NULL, 'Pending', 0.0, 0.0, 450.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000022', 60000.0, '11111111-0007-0007-0007-000000000006', 'GATE_OUT', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate Out - 40RF (San Antonio)', TRUE, NULL, NULL, 'Pending', 11400.0, 19.0, 71400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('00262c9e-81f8-3ef1-63fc-3e4e25484381', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000002', 'Tariff', '{"conceptCode":"WAREHOUSE_CHANGE","code":"KTF","country":"CL","currency":"CLP","containerType":null,"description":"Cambio de almac\u00E9n (KTF)","amount":110910,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('4af7222d-2559-d657-c911-d47dc1843208', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000006', 'Tariff', '{"conceptCode":"DEMURRAGE","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Demurrage 40\u0027 y especiales por d\u00EDa desde la descarga","amount":0,"tierUnit":"CalendarDays","tierMode":"PerUnit","tiers":[{"fromUnit":1,"toUnit":7,"amount":0},{"fromUnit":8,"toUnit":14,"amount":45000},{"fromUnit":15,"toUnit":null,"amount":65000}],"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5bf554e0-9a9d-0205-6b6a-9a82eb8f2e8f', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000001', 'Tariff', '{"conceptCode":"WAREHOUSE_CHANGE","code":"KTE","country":"CL","currency":"CLP","containerType":null,"description":"Cambio de almac\u00E9n (KTE)","amount":9940,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5ccf1aaa-2d67-6507-44f1-d222a6bd2de0', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000008', 'Tariff', '{"conceptCode":"ADVANCE_DEMURRAGE_BO","code":null,"country":"BO","currency":"USD","containerType":null,"description":"Demoras anticipadas por contenedor","amount":150,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('6b2cd9c6-cb6f-38cb-c161-b531902c4f5f', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000005', 'Tariff', '{"conceptCode":"DEMURRAGE","code":null,"country":"CL","currency":"CLP","containerType":"20DV","description":"Demurrage 20\u0027 por d\u00EDa desde la descarga","amount":0,"tierUnit":"CalendarDays","tierMode":"PerUnit","tiers":[{"fromUnit":1,"toUnit":7,"amount":0},{"fromUnit":8,"toUnit":14,"amount":35000},{"fromUnit":15,"toUnit":null,"amount":50000}],"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('7ab0cffc-981f-77ae-99a1-b8b068b9f60e', 'Created', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0015-0015-0015-000000000002', 'InternalChargeRule', '{"ruleType":"AdvanceDemurrageRequired","country":"BO","taxId":"1023456017","matchCode":"MC100020","accountName":"Comercial Altiplano SRL","reason":"Regla interna Bolivia: demoras anticipadas obligatorias antes del CLD","maxUsesPerBl":null,"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('836c5b07-4eed-d9cf-301e-0b003b44e4ed', 'Created', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0015-0015-0015-000000000001', 'InternalChargeRule', '{"ruleType":"FreeWarehouseChange","country":"CL","taxId":"76123456-7","matchCode":"MC100010","accountName":"Importadora Demo SpA","reason":"Convenio comercial: un cambio de almac\u00E9n gratuito por BL","maxUsesPerBl":1,"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('b7780317-c00e-3ebe-3d0b-606bf60647bf', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000007', 'Tariff', '{"conceptCode":"DEMURRAGE","code":null,"country":"BO","currency":"BOB","containerType":null,"description":"Demurrage Bolivia por d\u00EDa desde la descarga","amount":0,"tierUnit":"CalendarDays","tierMode":"PerUnit","tiers":[{"fromUnit":1,"toUnit":10,"amount":0},{"fromUnit":11,"toUnit":20,"amount":310},{"fromUnit":21,"toUnit":null,"amount":450}],"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('c0f7f17b-bf2c-d265-7870-e857f36d9ce5', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000003', 'Tariff', '{"conceptCode":"WAREHOUSE_CHANGE","code":null,"country":"BO","currency":"BOB","containerType":null,"description":"Cambio de almac\u00E9n Bolivia","amount":850,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-01-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('f2dac0e5-7721-dda5-b0cb-e740dcce6e9d', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000004', 'Tariff', '{"conceptCode":"LATE_ARRIVAL","code":null,"country":"CL","currency":"USD","containerType":null,"description":"Late Arrival por horas desde el cierre de recepci\u00F3n","amount":0,"tierUnit":"Hours","tierMode":"Flat","tiers":[{"fromUnit":0,"toUnit":24,"amount":100},{"fromUnit":25,"toUnit":48,"amount":200},{"fromUnit":49,"toUnit":null,"amount":350}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000001', 9940.0, 'KTE', 'WAREHOUSE_CHANGE', NULL, 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Cambio de almacén (KTE)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000002', 110910.0, 'KTF', 'WAREHOUSE_CHANGE', NULL, 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Cambio de almacén (KTF)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000003', 850.0, NULL, 'WAREHOUSE_CHANGE', NULL, 'BO', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Cambio de almacén Bolivia', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-01-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000004', 0.0, NULL, 'LATE_ARRIVAL', NULL, 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Late Arrival por horas desde el cierre de recepción', TRUE, NULL, NULL, 'Flat', 'Hours', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000005', 0.0, NULL, 'DEMURRAGE', '20DV', 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Demurrage 20'' por día desde la descarga', TRUE, NULL, NULL, 'PerUnit', 'CalendarDays', DATE '2026-01-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000006', 0.0, NULL, 'DEMURRAGE', NULL, 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Demurrage 40'' y especiales por día desde la descarga', TRUE, NULL, NULL, 'PerUnit', 'CalendarDays', DATE '2026-01-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000007', 0.0, NULL, 'DEMURRAGE', NULL, 'BO', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Demurrage Bolivia por día desde la descarga', TRUE, NULL, NULL, 'PerUnit', 'CalendarDays', DATE '2026-01-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000008', 150.0, NULL, 'ADVANCE_DEMURRAGE_BO', NULL, 'BO', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Demoras anticipadas por contenedor', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-01-01', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "WarehouseChanges" SET "BatchId" = NULL, "CompletedAt" = NULL, "ContainerNumber" = NULL, "EntitlementReference" = NULL, "EntitlementSource" = NULL, "IsFree" = FALSE, "RequestedByClientId" = NULL, "RequestedByUserId" = NULL, "TariffCode" = NULL, "TariffSource" = NULL
    WHERE "Id" = '99999999-000f-000f-000f-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    UPDATE "WarehouseChanges" SET "BatchId" = NULL, "CompletedAt" = NULL, "ContainerNumber" = NULL, "EntitlementReference" = NULL, "EntitlementSource" = NULL, "IsFree" = FALSE, "RequestedByClientId" = NULL, "RequestedByUserId" = NULL, "TariffCode" = NULL, "TariffSource" = NULL
    WHERE "Id" = '99999999-000f-000f-000f-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000011', '11111111-0007-0007-0007-000000000009', 'HLXU3034001', '40HC', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-030401', 'Discharged', NULL, NULL, 23900.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000012', '11111111-0007-0007-0007-000000000010', 'HLXU3034002', '20DV', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-030402', 'Discharged', NULL, NULL, 16800.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000013', '11111111-0007-0007-0007-000000000010', 'HLXU3034003', '40HC', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-030403', 'Discharged', NULL, NULL, 24100.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BLParties" ("Id", "Address", "BillOfLadingId", "CountryCode", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Name", "Role", "TaxId", "TaxIdType")
    VALUES ('dddddddd-0013-0013-0013-000000000001', NULL, '11111111-0007-0007-0007-000000000010', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Delfin Logística SpA', 'Consignee', '76000001-1', 'RUT');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000011', 'HLCUVAP260401130', NULL, 'HLCUBKG2604113', 'c3d4e5f6-0003-0003-0003-000000000060', 'Global Forwarding Chile SpA', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-09-15T00:00:00Z', TIMESTAMPTZ '2026-08-25T00:00:00Z', 2600.0, 'USD', NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Valparaiso, Chile', 'Valparaiso (CLVAP)', 'Santos (BRSSZ)', 'Import', 'Santos Coffee Exporters Ltda', 'Arrived', 'Santos Express', NULL, '2609N');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "DemurrageCharges" ("Id", "BillOfLadingId", "ContainerNumber", "CreatedAt", "CreatedBy", "Currency", "DailyRate", "DeletedAt", "DeletedBy", "DemurrageDays", "EndDate", "ExemptReason", "FreeDays", "InvoiceDueDate", "InvoiceNumber", "InvoicedAt", "IsExempt", "ModifiedAt", "ModifiedBy", "StartDate", "Status", "TotalAmount")
    VALUES ('44444444-000a-000a-000a-000000000004', '11111111-0007-0007-0007-000000000009', 'HLXU3034001', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', 51000.0, NULL, NULL, 10, TIMESTAMPTZ '2026-09-06T00:00:00Z', NULL, 7, TIMESTAMPTZ '2026-10-15T03:00:00Z', 'FAC-DEM-2026-0915', TIMESTAMPTZ '2026-09-15T12:00:00Z', FALSE, NULL, NULL, TIMESTAMPTZ '2026-08-20T00:00:00Z', 'Invoiced', 510000.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000013', 85000.0, '11111111-0007-0007-0007-000000000009', 'MHD', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'MHD - HLXU3034001', TRUE, NULL, NULL, 'Pending', 16150.0, 19.0, 101150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000014', 95000.0, '11111111-0007-0007-0007-000000000010', 'GATE_IN', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate In - devolución de vacíos (San Antonio)', TRUE, NULL, NULL, 'Pending', 18050.0, 19.0, 113050.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000015', 38000.0, '11111111-0007-0007-0007-000000000010', 'EDS', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'EDS', TRUE, NULL, NULL, 'Pending', 7220.0, 19.0, 45220.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('6b4eb3e7-26d3-48e5-8ad2-f1f5822756a3', '11111111-0007-0007-0007-000000000009', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('ccf652e1-278e-3ce7-eb83-c2a14d706449', '11111111-0007-0007-0007-000000000010', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('2261b5bc-635c-2ddc-27a9-bf7d66747d55', 450.0, 21, 'eeeeeeee-0014-0014-0014-000000000007', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('49ba8cc8-a39b-5996-22f0-a1ddfcacc0fc', 310.0, 11, 'eeeeeeee-0014-0014-0014-000000000007', 20);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('5fe677cf-b251-4573-d54b-a395044f3f66', 0.0, 1, 'eeeeeeee-0014-0014-0014-000000000005', 7);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('64243965-3f4c-060c-c6d0-f4ac9992012a', 45000.0, 8, 'eeeeeeee-0014-0014-0014-000000000006', 14);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('674264eb-2ffd-1ddc-0d52-be33d9bd0469', 50000.0, 15, 'eeeeeeee-0014-0014-0014-000000000005', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('70dbb6e7-87a3-2817-6346-f9879a242a2f', 65000.0, 15, 'eeeeeeee-0014-0014-0014-000000000006', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('76a040c4-94e3-5e10-9026-016e4521572a', 0.0, 1, 'eeeeeeee-0014-0014-0014-000000000007', 10);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('99179f3b-5c8d-40e8-6571-1673dc54b54d', 35000.0, 8, 'eeeeeeee-0014-0014-0014-000000000005', 14);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('a11c263f-1a4f-4b44-6a53-28b1f7182f35', 0.0, 1, 'eeeeeeee-0014-0014-0014-000000000006', 7);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('a9820f36-7222-b459-9737-1738a226406c', 200.0, 25, 'eeeeeeee-0014-0014-0014-000000000004', 48);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('c2061609-752b-0914-6d80-3d88b0ba4978', 100.0, 0, 'eeeeeeee-0014-0014-0014-000000000004', 24);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('deb552c4-3437-7294-3ce0-592446a7712e', 350.0, 49, 'eeeeeeee-0014-0014-0014-000000000004', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000060', 'c3d4e5f6-0003-0003-0003-000000000060', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'credito@distribuidoraandes.cl', NULL, NULL, 'Camila', TRUE, FALSE, NULL, 'Crédito', NULL, NULL, 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'credito@distribuidoraandes.cl');
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000070', 'c3d4e5f6-0003-0003-0003-000000000070', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'ffww@globalforwarding.cl', NULL, NULL, 'Felipe', TRUE, FALSE, NULL, 'Forwarder', NULL, NULL, 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'ffww@globalforwarding.cl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000014', '11111111-0007-0007-0007-000000000011', 'HLXU3034004', '20DV', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-030404', 'Discharged', NULL, NULL, 17200.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000018', 185000.0, '11111111-0007-0007-0007-000000000011', 'THC', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Terminal Handling Charge - 20DV', TRUE, NULL, NULL, 'Pending', 35150.0, 19.0, 220150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000019', 150.0, '11111111-0007-0007-0007-000000000011', 'IPO', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Recargo IPO', FALSE, NULL, NULL, 'Pending', 0.0, 0.0, 150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000020', 60000.0, '11111111-0007-0007-0007-000000000011', 'GATE_OUT', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate Out - 20DV (Valparaíso)', TRUE, NULL, NULL, 'Pending', 11400.0, 19.0, 71400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('764cdd78-0b15-3757-b5a8-5f806a2e432e', '11111111-0007-0007-0007-000000000011', 'c3d4e5f6-0003-0003-0003-000000000070', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('9669f2e2-7ad8-52c7-ad43-b348fecd7d85', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000070');
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('f9a0f3f4-6698-e1c1-73e6-b36bbf9f69e1', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000060');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_WarehouseChanges_BatchId" ON "WarehouseChanges" ("BatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_WarehouseChanges_BillOfLadingId_IsFree" ON "WarehouseChanges" ("BillOfLadingId", "IsFree");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_AppliedExemptions_BillOfLadingId" ON "AppliedExemptions" ("BillOfLadingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_AppliedExemptions_LocalChargeId" ON "AppliedExemptions" ("LocalChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE UNIQUE INDEX "IX_BusinessHolidays_Country_Date" ON "BusinessHolidays" ("Country", "Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE UNIQUE INDEX "IX_ChargeConcepts_Code" ON "ChargeConcepts" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_ExchangeRateRecords_TransactionType_TransactionId" ON "ExchangeRateRecords" ("TransactionType", "TransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_InternalChargeRules_RuleType_Country_IsActive" ON "InternalChargeRules" ("RuleType", "Country", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_MaintainerChangeLogs_ChangedAt" ON "MaintainerChangeLogs" ("ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_MaintainerChangeLogs_Maintainer_EntityId_ChangedAt" ON "MaintainerChangeLogs" ("Maintainer", "EntityId", "ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_Tariffs_ConceptCode_Country_IsActive_ValidFrom" ON "Tariffs" ("ConceptCode", "Country", "IsActive", "ValidFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_TariffTiers_TariffId_FromUnit" ON "TariffTiers" ("TariffId", "FromUnit");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_WarehouseChangeBatches_ClientId" ON "WarehouseChangeBatches" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_WarehouseChangeBatches_Status_CreatedAt" ON "WarehouseChangeBatches" ("Status", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    CREATE INDEX "IX_WarehouseChangeBatchItems_BatchId_Status_LineNumber" ON "WarehouseChangeBatchItems" ("BatchId", "Status", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006012345_AddChargeRulesAndTariffs') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006012345_AddChargeRulesAndTariffs', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    DROP INDEX "IX_Payments_ClientId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ALTER COLUMN "BillOfLadingId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CancellationReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CancelledAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CancelledBy" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CancelledByRole" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CancelledByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "CreatedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "FailureReason" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "IdempotencyKey" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "Origin" character varying(20) NOT NULL DEFAULT 'Legacy';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "PayerName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "PayerTaxId" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "PaymentMethodCode" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "ProviderKey" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "ProviderReference" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "ProviderTransactionId" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "RedirectUrl" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "RequestFingerprint" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "SlipIssuedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "SlipNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "Payments" ADD "StatusChangedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "AccessGrantId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "BillOfLadingId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "BillingName" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "BillingTaxId" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "BlNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "BookingNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "ExchangeRate" numeric(18,6);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "ItemType" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "OnBehalfOfClientId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "OriginalAmount" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "OriginalCurrency" character varying(5);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "ReleasedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "PaymentDetails" ADD "SourceId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    ALTER TABLE "BillsOfLading" ADD "FreightPaidAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "Carts" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_Carts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "CustomerInvoices" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "SiiNumber" character varying(30),
        "SourceNumber" character varying(50) NOT NULL,
        "DocumentType" character varying(30) NOT NULL,
        "IssueDate" date NOT NULL,
        "DueDate" date,
        "BillOfLadingId" uuid,
        "BlNumber" character varying(50),
        "BookingNumber" character varying(50),
        "LegalName" character varying(200) NOT NULL,
        "TaxId" character varying(20) NOT NULL,
        "NetAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "SiiStatus" character varying(30),
        "IsPayable" boolean NOT NULL,
        "Country" character varying(5) NOT NULL,
        "ConceptCode" character varying(50),
        "PaidAt" timestamp with time zone,
        "PaymentId" uuid,
        "SyncedAt" timestamp with time zone NOT NULL,
        "Source" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_CustomerInvoices" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CustomerInvoices_Clients_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "PaymentBlockWindows" (
        "Id" uuid NOT NULL,
        "Country" character varying(5),
        "StartDate" date NOT NULL,
        "StartTime" time without time zone NOT NULL,
        "EndDate" date NOT NULL,
        "EndTime" time without time zone NOT NULL,
        "Reason" character varying(300) NOT NULL,
        "ClientMessage" character varying(500) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_PaymentBlockWindows" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "PaymentCurrencyRules" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "ConceptCode" character varying(50) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_PaymentCurrencyRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "PaymentMethodConfigs" (
        "Id" uuid NOT NULL,
        "Code" character varying(40) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(300),
        "Country" character varying(5) NOT NULL,
        "Kind" character varying(20) NOT NULL,
        "ProviderKey" character varying(30),
        "Currencies" character varying(100) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_PaymentMethodConfigs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "PaymentOutboxMessages" (
        "Id" uuid NOT NULL,
        "PaymentId" uuid NOT NULL,
        "JobType" character varying(30) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Attempts" integer NOT NULL,
        "MaxAttempts" integer NOT NULL,
        "NextAttemptAt" timestamp with time zone NOT NULL,
        "LastAttemptAt" timestamp with time zone,
        "LastError" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        CONSTRAINT "PK_PaymentOutboxMessages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentOutboxMessages_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "PaymentStatusChanges" (
        "Id" uuid NOT NULL,
        "PaymentId" uuid NOT NULL,
        "FromStatus" character varying(30),
        "ToStatus" character varying(30) NOT NULL,
        "ChangedAt" timestamp with time zone NOT NULL,
        "ChangedBy" character varying(256) NOT NULL,
        "ChangedByUserId" uuid,
        "Reason" character varying(500),
        CONSTRAINT "PK_PaymentStatusChanges" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentStatusChanges_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE TABLE "CartItems" (
        "Id" uuid NOT NULL,
        "CartId" uuid NOT NULL,
        "ItemType" character varying(30) NOT NULL,
        "SourceId" uuid NOT NULL,
        "BillOfLadingId" uuid,
        "BlNumber" character varying(50),
        "BookingNumber" character varying(50),
        "Country" character varying(5) NOT NULL,
        "ConceptCode" character varying(50) NOT NULL,
        "Description" character varying(500),
        "Amount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "PaymentCurrency" character varying(5) NOT NULL,
        "PaymentAmount" numeric(18,2) NOT NULL,
        "ExchangeRate" numeric(18,6),
        "RateEffectiveDate" date,
        "RateSource" character varying(30),
        "BillingTaxId" character varying(20) NOT NULL,
        "BillingName" character varying(200) NOT NULL,
        "BillingOrganizationId" uuid,
        "OnBehalfOfClientId" uuid,
        "AccessGrantId" uuid,
        "AddedByUserId" uuid NOT NULL,
        "AddedAt" timestamp with time zone NOT NULL,
        "LockedByPaymentId" uuid,
        CONSTRAINT "PK_CartItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") REFERENCES "Carts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = TIMESTAMPTZ '2026-09-20T14:05:00Z'
    WHERE "Id" = '11111111-0007-0007-0007-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "BillsOfLading" SET "FreightPaidAt" = NULL
    WHERE "Id" = '11111111-0007-0007-0007-000000000011';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "Currencies" ("Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ExchangeRateToUSD", "LastUpdated", "ModifiedAt", "ModifiedBy", "Name", "Symbol")
    VALUES ('a1b2c3d4-0001-0001-0001-000000000004', 'EUR', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 0.92, TIMESTAMPTZ '2026-01-01T00:00:00Z', NULL, NULL, 'Euro', '€');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000001', '11111111-0007-0007-0007-000000000009', 'HLCUSAI260400910', 'HLCUBKG2604091', 'DEMURRAGE', 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'ExemptInvoice', DATE '2026-10-15', TRUE, DATE '2026-09-15', 'Importadora Demo SpA', NULL, NULL, 510000.0, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '100245', 'ACCEPTED', 'DUMMY', 'FAC-DEM-2026-0915', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 0.0, '76123456-7', 510000.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000002', '11111111-0007-0007-0007-000000000001', 'HLCUVAL250100123', 'HLCUBKG2501001', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-09-19', TRUE, DATE '2026-08-20', 'Importadora Demo SpA', NULL, NULL, 120000.0, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '100198', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-003987', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 22800.0, '76123456-7', 142800.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000003', '11111111-0007-0007-0007-000000000002', 'HLCUVAL250200456', 'HLCUBKG2502004', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-08-09', FALSE, DATE '2026-07-10', 'Importadora Demo SpA', NULL, NULL, 185000.0, 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-08-05T15:00:00Z', NULL, '100150', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-003501', 'Paid', TIMESTAMPTZ '2026-10-05T11:00:00Z', 35150.0, '76123456-7', 220150.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000004', '11111111-0007-0007-0007-000000000006', 'HLCUSAI260300610', 'HLCUBKG2603061', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'ExemptInvoice', DATE '2026-11-01', TRUE, DATE '2026-10-02', 'Importadora Demo SpA', NULL, NULL, 350.0, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, NULL, NULL, 'DUMMY', 'HL-CL-2026-004601', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 0.0, '76123456-7', 350.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000005', '11111111-0007-0007-0007-000000000001', 'HLCUVAL250100123', 'HLCUBKG2501001', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'CreditNote', NULL, FALSE, DATE '2026-09-25', 'Importadora Demo SpA', NULL, NULL, 15000.0, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '100301', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004700', 'Paid', TIMESTAMPTZ '2026-10-05T11:00:00Z', 2850.0, '76123456-7', 17850.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000006', '11111111-0007-0007-0007-000000000010', 'HLCUSAI260401020', 'HLCUBKG2604102', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'EUR', NULL, NULL, 'ExemptInvoice', DATE '2026-10-28', TRUE, DATE '2026-09-28', 'Importadora Demo SpA', NULL, NULL, 480.0, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '100260', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004530', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 0.0, '76123456-7', 480.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000007', '11111111-0007-0007-0007-000000000004', 'HLCUARI260100045', 'HLCUBKG2601045', NULL, 'BO', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Invoice', DATE '2026-10-10', TRUE, DATE '2026-09-10', 'Comercial Altiplano SRL', NULL, NULL, 1280.0, 'c3d4e5f6-0003-0003-0003-000000000020', NULL, NULL, '2026-000812', 'ACCEPTED', 'DUMMY', 'HL-BO-2026-000812', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 166.4, '1023456017', 1446.4);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000008', '11111111-0007-0007-0007-000000000005', 'HLCUIQQ260200078', 'HLCUBKG2602078', NULL, 'BO', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Invoice', DATE '2026-09-29', FALSE, DATE '2026-08-30', 'Comercial Altiplano SRL', NULL, NULL, 690.0, 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-09-05T14:00:00Z', NULL, '2026-000790', 'ACCEPTED', 'DUMMY', 'HL-BO-2026-000790', 'Paid', TIMESTAMPTZ '2026-10-05T11:00:00Z', 89.7, '1023456017', 779.7);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000009', '11111111-0007-0007-0007-000000000011', 'HLCUVAP260401130', 'HLCUBKG2604113', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-10-26', TRUE, DATE '2026-09-26', 'Distribuidora Andes Crédito SpA', NULL, NULL, 95000.0, 'c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, '100277', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004588', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 18050.0, '76000002-2', 113050.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000010', '11111111-0007-0007-0007-000000000011', 'HLCUVAP260401130', 'HLCUBKG2604113', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'ExemptInvoice', DATE '2026-10-26', TRUE, DATE '2026-09-26', 'Distribuidora Andes Crédito SpA', NULL, NULL, 380.0, 'c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, '100278', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004589', 'Pending', TIMESTAMPTZ '2026-10-05T11:00:00Z', 0.0, '76000002-2', 380.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "ExchangeRateRecords" ("Id", "Approved", "CapturedAt", "ConvertedAmount", "EffectiveDate", "FromCurrency", "Rate", "Source", "SourceAmount", "ToCurrency", "TransactionId", "TransactionType")
    VALUES ('b3a141c8-4669-b188-6d5a-edaf7cacd5a5', TRUE, TIMESTAMPTZ '2026-09-20T14:02:00Z', 4940000.0, DATE '2026-09-20', 'USD', 950.0, 'DUMMY', 5200.0, 'CLP', '55555555-000b-000b-000b-000000000010', 'Payment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('009d0f99-085a-ba06-8922-5a0493f16195', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '4ae9a380-c47c-76a4-0462-742d9ebd1d52', 'PaymentCurrency', '{"country":"BO","conceptCode":"EDS","currency":"BOB","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('21fe52a2-53f3-ea6f-16e7-786a3d0e0356', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '9d493c1f-625d-2ddd-c5cd-d2f6ced94174', 'PaymentCurrency', '{"country":"BO","conceptCode":"ADVANCE_DEMURRAGE_BO","currency":"USD","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('25c18b0f-18ba-3150-1be7-88545cabf81d', 'Created', TIMESTAMPTZ '2026-09-26T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0016-0016-0016-000000000001', 'PaymentBlockWindow', '{"country":"CL","startDate":"2026-09-30","startTime":"20:00:00","endDate":"2026-09-30","endTime":"23:59:00","reason":"Cierre contable de septiembre","clientMessage":"Los pagos est\u00E1n suspendidos temporalmente por el cierre contable mensual. Podr\u00E1 pagar nuevamente desde las 23:59 (hora de Chile).","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('2cf55acb-0012-8fc4-a30b-b8e6b5dfba2f', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0016-0016-0016-000000000003', 'PaymentBlockWindow', '{"country":"BO","startDate":"2026-12-24","startTime":"18:00:00","endDate":"2026-12-26","endTime":"08:00:00","reason":"Mantenimiento de la conciliaci\u00F3n bancaria de fin de a\u00F1o","clientMessage":"Los pagos en l\u00EDnea no est\u00E1n disponibles por mantenimiento hasta el 26-12 a las 08:00 (hora de Bolivia).","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('301dfc03-3a4c-659e-f88b-048be09d4b13', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '70274a14-0ca5-1374-71dd-1a757cbcda98', 'PaymentCurrency', '{"country":"CL","conceptCode":"FREIGHT","currency":"USD","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('3153499e-1612-eefd-7af8-7804b5d8f54d', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '95ce57ff-9e48-2759-c9c9-b25672f9ac6b', 'PaymentMethod', '{"code":"BANK_BUTTON_BCI","name":"Bot\u00F3n de pago Bci","description":"Pago en l\u00EDnea desde la banca de Bci","country":"CL","kind":"Online","providerKey":"Bci","currencies":["CLP"],"isEnabled":true,"displayOrder":40}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('3b8aa0fa-17d0-516c-5957-2018b8e21757', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'deea0f57-7ac8-473d-1b7f-821284393535', 'PaymentMethod', '{"code":"DIGITAL_USD","name":"D\u00F3lares digitales","description":"Reservado (M5-03): se habilita cuando se defina el proveedor","country":"BO","kind":"Online","providerKey":null,"currencies":["USD"],"isEnabled":false,"displayOrder":90}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('48c2038f-b24f-3b7f-1210-08fc6d7c45df', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'd72f12ae-5666-dd92-37dc-6284173fa6ee', 'PaymentMethod', '{"code":"DIGITAL_USD","name":"D\u00F3lares digitales","description":"Reservado (M5-03): se habilita cuando se defina el proveedor","country":"CL","kind":"Online","providerKey":null,"currencies":["USD"],"isEnabled":false,"displayOrder":90}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5f20b6bf-6258-7dfc-9337-83786ed0c2e6', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'cc949890-ea1d-885b-57ab-3067028e5c54', 'PaymentCurrency', '{"country":"BO","conceptCode":"DEMURRAGE","currency":"BOB","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('67823a49-3a13-1a79-15f6-097407733cc1', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'a886a06b-0336-a30f-36c1-dff222504ce2', 'PaymentMethod', '{"code":"DEPOSIT","name":"Dep\u00F3sito bancario (boleta)","description":"Boleta para dep\u00F3sito o transferencia; Finanzas confirma el abono","country":"CL","kind":"Deposit","providerKey":null,"currencies":["CLP","USD","EUR"],"isEnabled":true,"displayOrder":50}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('6e624596-577b-cfa9-7f30-a43d2de5f5cd', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '86aec5dc-4971-4b36-8482-52ce8c3f8732', 'PaymentMethod', '{"code":"BANK_BUTTON_SANTANDER","name":"Bot\u00F3n de pago Santander","description":"Pago en l\u00EDnea desde la banca de Santander","country":"CL","kind":"Online","providerKey":"Santander","currencies":["CLP"],"isEnabled":true,"displayOrder":30}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('6ee41684-f5b7-0176-23ed-ab6b94f0815f', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '9022d3e1-11fc-7493-a0c4-de6760961bb3', 'PaymentMethod', '{"code":"BANK_BUTTON_BCH","name":"Bot\u00F3n de pago Banco de Chile","description":"Pago en l\u00EDnea desde la banca del Banco de Chile","country":"CL","kind":"Online","providerKey":"BancoChile","currencies":["CLP","USD"],"isEnabled":true,"displayOrder":20}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('7126d08c-77ab-5a25-a793-ea20c39fbf0e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'a6cb3763-7416-78bc-99f2-75cf1e99d2ec', 'PaymentCurrency', '{"country":"CL","conceptCode":"DEMURRAGE","currency":"USD","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('8806e939-b593-2530-fda8-1e06219d03c3', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'dce8145f-b0ce-739e-c803-950f92f73222', 'PaymentMethod', '{"code":"DEPOSIT","name":"Dep\u00F3sito o transferencia bancaria (boleta)","description":"Boleta para dep\u00F3sito o transferencia; Finanzas confirma el abono","country":"BO","kind":"Deposit","providerKey":null,"currencies":["BOB","USD"],"isEnabled":true,"displayOrder":10}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('9516cb88-c63f-aee3-2979-8ac69d5688c9', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '96eed327-ce91-3e86-e713-0bdcbd30c481', 'PaymentMethod', '{"code":"KHIPU","name":"Khipu","description":"Transferencia simplificada con Khipu","country":"CL","kind":"Online","providerKey":"Khipu","currencies":["CLP"],"isEnabled":true,"displayOrder":10}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('963f22f1-c90b-4145-9d68-d23b271dbef5', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '39797e93-73d2-b3ca-5043-aa9df7cf517d', 'PaymentCurrency', '{"country":"CL","conceptCode":"DEMURRAGE","currency":"CLP","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('9707c44c-468c-fba3-bac1-d2b3e24d55a6', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'c2a0e616-edb8-7916-784c-9a2d7a9acb8d', 'PaymentCurrency', '{"country":"CL","conceptCode":"GATE_IN","currency":"CLP","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('99e1ee9a-4cb8-6a8f-7834-61b67cca8c41', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '323b3d52-1913-dfa7-b527-56610206d402', 'PaymentCurrency', '{"country":"BO","conceptCode":"GATE_IN","currency":"BOB","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a006e2af-63e5-00bb-7f00-8741b4d8b17e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '4398a676-9729-32fe-fd7a-8864d3f969ae', 'PaymentCurrency', '{"country":"BO","conceptCode":"FREIGHT","currency":"BOB","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a0d3fb65-c917-d6fe-11d7-97d7ab5fa000', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '89707cfa-2d11-e597-117a-6925557cdd58', 'PaymentCurrency', '{"country":"BO","conceptCode":"ADVANCE_DEMURRAGE_BO","currency":"BOB","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a817ee90-0e4b-823a-f85e-86d26390a1f6', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'b83dc854-2b7a-489f-855c-ce679da9658b', 'PaymentCurrency', '{"country":"BO","conceptCode":"FREIGHT","currency":"USD","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('bcafbb5c-b285-5fd1-9598-f72a60f20ca7', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '317d9770-1994-f770-232f-44f9a1f6d995', 'PaymentCurrency', '{"country":"BO","conceptCode":"DEMURRAGE","currency":"USD","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('cdeac7b5-d7b7-92d9-a901-a94bdedd39ab', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0016-0016-0016-000000000002', 'PaymentBlockWindow', '{"country":null,"startDate":"2026-10-31","startTime":"21:00:00","endDate":"2026-11-01","endTime":"06:00:00","reason":"Cierre contable de octubre","clientMessage":"Los pagos est\u00E1n suspendidos temporalmente por el cierre contable mensual. Podr\u00E1 pagar nuevamente a partir de las 06:00 (hora local).","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('d0f6487e-45c1-3e34-5a1d-26a1d0ed6f70', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '20ab80b2-72d5-0892-de39-0ad92903130f', 'PaymentCurrency', '{"country":"CL","conceptCode":"EDS","currency":"CLP","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('d271dc82-d532-ec69-794c-0d0d2193e12f', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '0dabc090-4865-6f5e-3d2a-10da65adb6f7', 'PaymentCurrency', '{"country":"CL","conceptCode":"FREIGHT","currency":"EUR","isEnabled":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e0b8fcc8-f790-c0b9-6959-b0c38b58753c', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '7d6dbb49-443c-1eb5-790a-d5e2aa6594b1', 'PaymentCurrency', '{"country":"CL","conceptCode":"FREIGHT","currency":"CLP","isEnabled":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "PaymentBlockWindows" ("Id", "ClientMessage", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EndDate", "EndTime", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "StartDate", "StartTime")
    VALUES ('ffffffff-0016-0016-0016-000000000001', 'Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente desde las 23:59 (hora de Chile).', 'CL', TIMESTAMPTZ '2026-09-26T12:00:00Z', 'SYSTEM', NULL, NULL, DATE '2026-09-30', TIME '23:59:00', TRUE, NULL, NULL, 'Cierre contable de septiembre', DATE '2026-09-30', TIME '20:00:00');
    INSERT INTO "PaymentBlockWindows" ("Id", "ClientMessage", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EndDate", "EndTime", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "StartDate", "StartTime")
    VALUES ('ffffffff-0016-0016-0016-000000000002', 'Los pagos están suspendidos temporalmente por el cierre contable mensual. Podrá pagar nuevamente a partir de las 06:00 (hora local).', NULL, TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, DATE '2026-11-01', TIME '06:00:00', TRUE, NULL, NULL, 'Cierre contable de octubre', DATE '2026-10-31', TIME '21:00:00');
    INSERT INTO "PaymentBlockWindows" ("Id", "ClientMessage", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "EndDate", "EndTime", "IsActive", "ModifiedAt", "ModifiedBy", "Reason", "StartDate", "StartTime")
    VALUES ('ffffffff-0016-0016-0016-000000000003', 'Los pagos en línea no están disponibles por mantenimiento hasta el 26-12 a las 08:00 (hora de Bolivia).', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, DATE '2026-12-26', TIME '08:00:00', TRUE, NULL, NULL, 'Mantenimiento de la conciliación bancaria de fin de año', DATE '2026-12-24', TIME '18:00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('0dabc090-4865-6f5e-3d2a-10da65adb6f7', 'FREIGHT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'EUR', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('20ab80b2-72d5-0892-de39-0ad92903130f', 'EDS', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('317d9770-1994-f770-232f-44f9a1f6d995', 'DEMURRAGE', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('323b3d52-1913-dfa7-b527-56610206d402', 'GATE_IN', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('39797e93-73d2-b3ca-5043-aa9df7cf517d', 'DEMURRAGE', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('4398a676-9729-32fe-fd7a-8864d3f969ae', 'FREIGHT', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('4ae9a380-c47c-76a4-0462-742d9ebd1d52', 'EDS', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('70274a14-0ca5-1374-71dd-1a757cbcda98', 'FREIGHT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('7d6dbb49-443c-1eb5-790a-d5e2aa6594b1', 'FREIGHT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('89707cfa-2d11-e597-117a-6925557cdd58', 'ADVANCE_DEMURRAGE_BO', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('9d493c1f-625d-2ddd-c5cd-d2f6ced94174', 'ADVANCE_DEMURRAGE_BO', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('a6cb3763-7416-78bc-99f2-75cf1e99d2ec', 'DEMURRAGE', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('b83dc854-2b7a-489f-855c-ce679da9658b', 'FREIGHT', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('c2a0e616-edb8-7916-784c-9a2d7a9acb8d', 'GATE_IN', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, TRUE, NULL, NULL);
    INSERT INTO "PaymentCurrencyRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy")
    VALUES ('cc949890-ea1d-885b-57ab-3067028e5c54', 'DEMURRAGE', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, TRUE, NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "PaymentDetails" SET "AccessGrantId" = NULL, "BillOfLadingId" = NULL, "BillingName" = NULL, "BillingTaxId" = NULL, "BlNumber" = NULL, "BookingNumber" = NULL, "ExchangeRate" = NULL, "ItemType" = NULL, "OnBehalfOfClientId" = NULL, "OriginalAmount" = NULL, "OriginalCurrency" = NULL, "ReleasedAt" = NULL, "SourceId" = NULL
    WHERE "Id" = '66666666-000c-000c-000c-000000000011';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('86aec5dc-4971-4b36-8482-52ce8c3f8732', 'BANK_BUTTON_SANTANDER', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Pago en línea desde la banca de Santander', 30, TRUE, 'Online', NULL, NULL, 'Botón de pago Santander', 'Santander');
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('9022d3e1-11fc-7493-a0c4-de6760961bb3', 'BANK_BUTTON_BCH', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP,USD', NULL, NULL, 'Pago en línea desde la banca del Banco de Chile', 20, TRUE, 'Online', NULL, NULL, 'Botón de pago Banco de Chile', 'BancoChile');
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('95ce57ff-9e48-2759-c9c9-b25672f9ac6b', 'BANK_BUTTON_BCI', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Pago en línea desde la banca de Bci', 40, TRUE, 'Online', NULL, NULL, 'Botón de pago Bci', 'Bci');
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('96eed327-ce91-3e86-e713-0bdcbd30c481', 'KHIPU', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Transferencia simplificada con Khipu', 10, TRUE, 'Online', NULL, NULL, 'Khipu', 'Khipu');
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('a886a06b-0336-a30f-36c1-dff222504ce2', 'DEPOSIT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP,USD,EUR', NULL, NULL, 'Boleta para depósito o transferencia; Finanzas confirma el abono', 50, TRUE, 'Deposit', NULL, NULL, 'Depósito bancario (boleta)', NULL);
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('d72f12ae-5666-dd92-37dc-6284173fa6ee', 'DIGITAL_USD', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Reservado (M5-03): se habilita cuando se defina el proveedor', 90, FALSE, 'Online', NULL, NULL, 'Dólares digitales', NULL);
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('dce8145f-b0ce-739e-c803-950f92f73222', 'DEPOSIT', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB,USD', NULL, NULL, 'Boleta para depósito o transferencia; Finanzas confirma el abono', 10, TRUE, 'Deposit', NULL, NULL, 'Depósito o transferencia bancaria (boleta)', NULL);
    INSERT INTO "PaymentMethodConfigs" ("Id", "Code", "Country", "CreatedAt", "CreatedBy", "Currencies", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "IsEnabled", "Kind", "ModifiedAt", "ModifiedBy", "Name", "ProviderKey")
    VALUES ('deea0f57-7ac8-473d-1b7f-821284393535', 'DIGITAL_USD', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Reservado (M5-03): se habilita cuando se defina el proveedor', 90, FALSE, 'Online', NULL, NULL, 'Dólares digitales', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    UPDATE "Payments" SET "CancellationReason" = NULL, "CancelledAt" = NULL, "CancelledBy" = NULL, "CancelledByRole" = NULL, "CancelledByUserId" = NULL, "CreatedByUserId" = NULL, "FailureReason" = NULL, "IdempotencyKey" = NULL, "Origin" = 'Legacy', "PayerName" = NULL, "PayerTaxId" = NULL, "PaymentMethodCode" = NULL, "ProviderKey" = NULL, "ProviderReference" = NULL, "ProviderTransactionId" = NULL, "RedirectUrl" = NULL, "RequestFingerprint" = NULL, "SlipIssuedAt" = NULL, "SlipNumber" = NULL, "StatusChangedAt" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000009', NULL, 70000.0, NULL, NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-09-12T15:20:00Z', 'KHIPU_WEBHOOK', 'CL', TIMESTAMPTZ '2026-09-12T15:20:00Z', 'd4e5f6a7-0004-0004-0004-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20260912-1A2B3C4D', NULL, NULL, NULL, NULL, NULL, 'Cart', 'Importadora Demo SpA', '76123456-7', TIMESTAMPTZ '2026-09-12T15:20:00Z', 'KHIPU', 'KHIPU', 'PAY-20260912-1A2B3C4D', 'Cart', 'Khipu', 'DUMMY-KHIPU-PAY-20260912-1A2B3C4D', 'KHP-TXN-8812345', 'RCP-20260912-7F3A21C4', NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-09-12T15:20:00Z', 13300.0, 83300.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000010', 'cccccccc-0012-0012-0012-000000000002', 4940000.0, '11111111-0007-0007-0007-000000000002', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000030', TIMESTAMPTZ '2026-09-20T14:05:00Z', 'BANCOCHILE_WEBHOOK', 'CL', TIMESTAMPTZ '2026-09-20T14:05:00Z', 'd4e5f6a7-0004-0004-0004-000000000030', 'd4e5f6a7-0004-0004-0004-000000000030', 'CLP', NULL, NULL, NULL, 950.0, 'PAY-20260920-5E6F7A8B', NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'Cart', 'Agencia Marítima del Pacífico Ltda', '96555444-3', TIMESTAMPTZ '2026-09-20T14:05:00Z', 'BANK_BUTTON_BCH', 'BANK_BUTTON_BCH', 'PAY-20260920-5E6F7A8B', 'Cart', 'BancoChile', 'DUMMY-BANCOCHILE-PAY-20260920-5E6F7A8B', 'BCH-TXN-55100231', 'RCP-20260920-2B4D6F80', NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-09-20T14:05:00Z', 0.0, 4940000.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000011', NULL, 45000.0, '11111111-0007-0007-0007-000000000006', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, 'CL', TIMESTAMPTZ '2026-10-03T13:30:00Z', 'd4e5f6a7-0004-0004-0004-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20261003-9C8B7A6D', NULL, NULL, NULL, NULL, NULL, 'Cart', 'Importadora Demo SpA', '76123456-7', TIMESTAMPTZ '2026-10-03T13:30:00Z', 'DEPOSIT', 'DEPOSIT', 'PAY-20261003-9C8B7A6D', 'Cart', NULL, NULL, NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-10-03T13:30:00Z', 'BDP-20261003-5C7D9E1F', 'PendingVerification', TIMESTAMPTZ '2026-10-03T13:30:00Z', 8550.0, 53550.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000012', NULL, 85000.0, '11111111-0007-0007-0007-000000000009', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, 'CL', TIMESTAMPTZ '2026-10-04T16:45:00Z', 'd4e5f6a7-0004-0004-0004-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20261004-3D2C1B0A', 'PROVIDER_UNAVAILABLE', NULL, NULL, NULL, NULL, 'Cart', 'Importadora Demo SpA', '76123456-7', TIMESTAMPTZ '2026-10-04T16:45:00Z', 'KHIPU', 'KHIPU', 'PAY-20261004-3D2C1B0A', 'Cart', 'Khipu', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Failed', TIMESTAMPTZ '2026-10-04T16:45:00Z', 16150.0, 101150.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000013', NULL, 820.0, NULL, NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-09-05T14:00:00Z', 'admin@hapag-lloyd.cl', 'BO', TIMESTAMPTZ '2026-09-05T14:00:00Z', 'd4e5f6a7-0004-0004-0004-000000000020', 'd4e5f6a7-0004-0004-0004-000000000020', 'BOB', NULL, NULL, NULL, NULL, 'PAY-20260903-4A5B6C7D', NULL, NULL, NULL, NULL, NULL, 'Cart', 'Comercial Altiplano SRL', '1023456017', TIMESTAMPTZ '2026-09-05T14:00:00Z', 'DEPOSIT', 'DEPOSIT', 'PAY-20260903-4A5B6C7D', 'Cart', NULL, NULL, NULL, 'RCP-20260905-8E9F0A1B', NULL, NULL, TIMESTAMPTZ '2026-09-03T14:00:00Z', 'BDP-20260903-1F2E3D4C', 'Confirmed', TIMESTAMPTZ '2026-09-05T14:00:00Z', 106.6, 926.6);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('6a7c0141-0c82-5803-bf14-39afd9893f3f', 'payments.finance', 'payments.finance');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('c2c32ec5-cb1d-c50e-2302-d037eec631e9', 'payment-blocks.manage', 'payment-blocks.manage');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('2d670ee7-847a-b99b-7aaf-7c56965a2a3b', NULL, 25000.0, '11111111-0007-0007-0007-000000000002', 'Importadora Demo SpA', '76123456-7', 'HLCUVAL250200456', 'HLCUBKG2502004', 'ISPS', 'CLP', 'ISPS - histórico', NULL, 'LocalCharge', NULL, 29750.0, 'CLP', '55555555-000b-000b-000b-000000000009', TIMESTAMPTZ '2026-09-12T15:20:00Z', NULL, 4750.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('6b2c23c2-b5a7-d926-50a2-90eb9c24c650', NULL, 45000.0, '11111111-0007-0007-0007-000000000006', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260300610', 'HLCUBKG2603061', 'BL_FEE', 'CLP', 'BL Documentation Fee (export)', NULL, 'LocalCharge', NULL, 53550.0, 'CLP', '55555555-000b-000b-000b-000000000011', NULL, '33333333-0009-0009-0009-000000000010', 8550.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('6d3b520b-5b65-58aa-66d8-66ff3648e5a6', NULL, 820.0, '11111111-0007-0007-0007-000000000005', 'Comercial Altiplano SRL', '1023456017', 'HLCUIQQ260200078', 'HLCUBKG2602078', 'GATE_IN', 'BOB', 'Gate In - histórico', NULL, 'LocalCharge', NULL, 926.6, 'BOB', '55555555-000b-000b-000b-000000000013', TIMESTAMPTZ '2026-09-05T14:00:00Z', NULL, 106.6);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('cf9509cc-b6c2-728d-6de8-084ee4dc8ee0', NULL, 85000.0, '11111111-0007-0007-0007-000000000009', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260400910', 'HLCUBKG2604091', 'MHD', 'CLP', 'MHD - HLXU3034001', NULL, 'LocalCharge', NULL, 101150.0, 'CLP', '55555555-000b-000b-000b-000000000012', NULL, '33333333-0009-0009-0009-000000000013', 16150.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('e36dea56-a5e1-3b71-8e9f-56a9c02494dc', NULL, 45000.0, '11111111-0007-0007-0007-000000000006', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260300610', 'HLCUBKG2603061', 'BL_FEE', 'CLP', 'Emisión de BL - histórico', NULL, 'LocalCharge', NULL, 53550.0, 'CLP', '55555555-000b-000b-000b-000000000009', TIMESTAMPTZ '2026-09-12T15:20:00Z', NULL, 8550.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('f5437389-1088-31f7-8453-a072eb133450', NULL, 4940000.0, '11111111-0007-0007-0007-000000000002', 'Importadora Demo SpA', '76123456-7', 'HLCUVAL250200456', 'HLCUBKG2502004', 'FREIGHT', 'CLP', 'Flete Busan - Valparaíso', 950.0, 'Freight', NULL, 5200.0, 'USD', '55555555-000b-000b-000b-000000000010', TIMESTAMPTZ '2026-09-20T14:05:00Z', '11111111-0007-0007-0007-000000000002', 0.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('098ec853-d62a-8529-61a7-c4a940b36b57', TIMESTAMPTZ '2026-10-04T16:44:30Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, '55555555-000b-000b-000b-000000000012', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('1650f54c-de7a-f6c0-6ebb-482ea121f9bb', TIMESTAMPTZ '2026-09-03T14:00:00Z', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Pending', '55555555-000b-000b-000b-000000000013', 'Deposit slip issued', 'PendingVerification');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('18589bd7-e7f5-2229-c22d-fda464a415b5', TIMESTAMPTZ '2026-10-04T16:45:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000012', 'PROVIDER_UNAVAILABLE', 'Failed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('310b1a9e-24ef-a762-ada1-81b38cd80393', TIMESTAMPTZ '2026-09-03T13:55:00Z', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, '55555555-000b-000b-000b-000000000013', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('32849ef5-77ee-6b91-da24-9d0d642dd46c', TIMESTAMPTZ '2026-09-20T14:02:00Z', 'agente@maritimpacifico.cl', 'd4e5f6a7-0004-0004-0004-000000000030', NULL, '55555555-000b-000b-000b-000000000010', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('4afe91c7-53b4-50b0-5ccf-5f4fac86b014', TIMESTAMPTZ '2026-10-03T13:20:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, '55555555-000b-000b-000b-000000000011', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('710fc5b6-a180-5f74-3ce4-b685465903f0', TIMESTAMPTZ '2026-09-20T14:02:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000010', 'Initiated in BancoChile', 'Processing');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('b2ace45d-692c-b548-5d2b-4d3d1d7ec3e0', TIMESTAMPTZ '2026-09-12T15:20:00Z', 'KHIPU_WEBHOOK', NULL, 'Processing', '55555555-000b-000b-000b-000000000009', NULL, 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('bdaa1d52-0e83-a373-93f7-26891c116e1e', TIMESTAMPTZ '2026-09-12T15:18:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000009', 'Initiated in Khipu', 'Processing');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('c947ed71-f5c6-e78e-f436-4341e97c31e4', TIMESTAMPTZ '2026-10-03T13:30:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Pending', '55555555-000b-000b-000b-000000000011', 'Deposit slip issued', 'PendingVerification');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('dab3029d-48f1-ac60-266b-ecb0d1c5f3be', TIMESTAMPTZ '2026-09-12T15:18:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, '55555555-000b-000b-000b-000000000009', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('e4d4eb7a-b86d-6e75-35cf-39ecdf9d9182', TIMESTAMPTZ '2026-09-20T14:05:00Z', 'BANCOCHILE_WEBHOOK', NULL, 'Processing', '55555555-000b-000b-000b-000000000010', NULL, 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('fe438c5a-1da7-bfa9-d71b-7648dccfb8fb', TIMESTAMPTZ '2026-09-05T14:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'PendingVerification', '55555555-000b-000b-000b-000000000013', NULL, 'Confirmed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_Payments_ClientId_PaymentDate" ON "Payments" ("ClientId", "PaymentDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_Payments_CreatedByUserId_IdempotencyKey" ON "Payments" ("CreatedByUserId", "IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_Payments_ExternalReference" ON "Payments" ("ExternalReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_PaymentDetails_ItemType_SourceId" ON "PaymentDetails" ("ItemType", "SourceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_CartItems_CartId_ItemType_SourceId" ON "CartItems" ("CartId", "ItemType", "SourceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_CartItems_LockedByPaymentId" ON "CartItems" ("LockedByPaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_Carts_UserId_OrganizationId" ON "Carts" ("UserId", "OrganizationId") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_CustomerInvoices_BillOfLadingId" ON "CustomerInvoices" ("BillOfLadingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_CustomerInvoices_OrganizationId_IssueDate" ON "CustomerInvoices" ("OrganizationId", "IssueDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_CustomerInvoices_OrganizationId_SourceNumber" ON "CustomerInvoices" ("OrganizationId", "SourceNumber") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_PaymentBlockWindows_IsActive_EndDate" ON "PaymentBlockWindows" ("IsActive", "EndDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_PaymentCurrencyRules_Country_ConceptCode_Currency" ON "PaymentCurrencyRules" ("Country", "ConceptCode", "Currency") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE UNIQUE INDEX "IX_PaymentMethodConfigs_Country_Code" ON "PaymentMethodConfigs" ("Country", "Code") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_PaymentOutboxMessages_PaymentId" ON "PaymentOutboxMessages" ("PaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_PaymentOutboxMessages_Status_NextAttemptAt" ON "PaymentOutboxMessages" ("Status", "NextAttemptAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    CREATE INDEX "IX_PaymentStatusChanges_PaymentId_ChangedAt" ON "PaymentStatusChanges" ("PaymentId", "ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006030423_AddCartPaymentsAndInvoices') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006030423_AddCartPaymentsAndInvoices', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006031634_AddCartItemConcurrencyStamp') THEN
    ALTER TABLE "CartItems" ADD "ConcurrencyStamp" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006031634_AddCartItemConcurrencyStamp') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006031634_AddCartItemConcurrencyStamp', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE TABLE "ShipmentDocuments" (
        "Id" uuid NOT NULL,
        "DocumentType" character varying(50) NOT NULL,
        "DocumentNumber" character varying(50) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "BookingNumber" character varying(50),
        "Country" character varying(5) NOT NULL,
        "ContainerNumbers" character varying(2000),
        "IssuedAt" timestamp with time zone NOT NULL,
        "IssuedForOrganizationId" uuid,
        "IssuedByUserId" uuid,
        "IssuedByEmail" character varying(256),
        "OnBehalfOfOrganizationId" uuid,
        "AccessGrantId" uuid,
        "Origin" character varying(20) NOT NULL,
        "PaymentId" uuid,
        "PaymentDetailId" uuid,
        "GenerationKey" character varying(150),
        "StorageKey" character varying(200),
        "FileName" character varying(255) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "ContentHash" character varying(64),
        "VerificationCode" character varying(30) NOT NULL,
        "SignatureId" character varying(100),
        "SignatureProvider" character varying(50),
        "SignatureLevel" character varying(20),
        "SignedAt" timestamp with time zone,
        "TemplateJson" text NOT NULL,
        "RecipientEmails" character varying(1000),
        "DeliveredAt" timestamp with time zone,
        "TermsVersion" character varying(50),
        "TermsAcceptedAt" timestamp with time zone,
        "ValidUntil" timestamp with time zone,
        "RetainUntil" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentDocuments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ShipmentDocuments_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE TABLE "ShipmentDocumentEvents" (
        "Id" uuid NOT NULL,
        "ShipmentDocumentId" uuid NOT NULL,
        "EventType" character varying(20) NOT NULL,
        "Channel" character varying(20) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "UserId" uuid,
        "UserEmail" character varying(256),
        "OrganizationId" uuid,
        "OnBehalfOfOrganizationId" uuid,
        "Recipient" character varying(256),
        "Details" character varying(2000),
        CONSTRAINT "PK_ShipmentDocumentEvents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ShipmentDocumentEvents_ShipmentDocuments_ShipmentDocumentId" FOREIGN KEY ("ShipmentDocumentId") REFERENCES "ShipmentDocuments" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000012', 'HLCUSAI260501240', NULL, 'HLCUBKG2605124', 'c3d4e5f6-0003-0003-0003-000000000010', 'Importadora Demo SpA', 'CL', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-10-02T00:00:00Z', TIMESTAMPTZ '2026-08-28T00:00:00Z', 4800.0, 'USD', TIMESTAMPTZ '2026-10-03T15:00:00Z', 'Collect', NULL, FALSE, FALSE, NULL, NULL, 'Agencia Marítima del Pacífico Ltda', NULL, NULL, 'Santiago, Chile', 'San Antonio (CLSAI)', 'Yokohama (JPYOK)', 'Import', 'Yokohama Machinery Co.', 'Arrived', 'Cartagena Express', NULL, '2611E');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ETA", "ETD", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfLoading", "ShipmentType", "Shipper", "Status", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000013', 'HLCUVAP260501350', NULL, 'HLCUBKG2605135', 'c3d4e5f6-0003-0003-0003-000000000070', 'Global Forwarding Chile SpA', 'CL', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-10-01T00:00:00Z', TIMESTAMPTZ '2026-08-30T00:00:00Z', 2900.0, 'USD', NULL, NULL, NULL, FALSE, FALSE, NULL, NULL, NULL, NULL, NULL, 'Valparaiso, Chile', 'Valparaiso (CLVAP)', 'Shanghai (CNSHA)', 'Import', 'Shanghai Furniture Ltd', 'Arrived', 'Callao Express', NULL, '2611N');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('3f585118-c4e9-dfe7-55a8-9ccdf3ad7b01', 'Service', 'TRANSSHIPMENT_CERT', 'CL', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'SYSTEM', NULL, NULL, 150, TRUE, NULL, NULL, 'Certificado de transbordo', FALSE, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000024', 35000.0, '11111111-0007-0007-0007-000000000006', 'TRANSSHIPMENT_CERT', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Certificado de transbordo', TRUE, NULL, NULL, 'Paid', 6650.0, 19.0, 41650.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000025', 150.0, '11111111-0007-0007-0007-000000000005', 'ADVANCE_DEMURRAGE_BO', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Demoras anticipadas (1 contenedor(es))', FALSE, NULL, NULL, 'Paid', 0.0, 0.0, 150.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('65387c59-9c85-0b9a-cbfb-175c8bde0570', 'Created', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', NULL, 'eeeeeeee-0014-0014-0014-000000000009', 'Tariff', '{"conceptCode":"TRANSSHIPMENT_CERT","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Certificado de transbordo (M6-01)","amount":35000,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000001', NULL, '11111111-0007-0007-0007-000000000006', 'HLCUSAI260300610', 'HLCUBKG2603061', 'HLXU2023001', NULL, 'application/pdf', 'CL', TIMESTAMPTZ '2026-10-04T13:00:00Z', 'SYSTEM', NULL, NULL, TIMESTAMPTZ '2026-10-04T13:00:00Z', 'CTB-20261004-5A1B2C3D', 'TransshipmentCertificate', 'certificado-transbordo-CTB-20261004-5A1B2C3D.pdf', NULL, TIMESTAMPTZ '2026-10-04T13:00:00Z', NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, NULL, 'Seed', NULL, NULL, 'demo@importadorademo.cl', TIMESTAMPTZ '2036-10-04T13:00:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Certificado de transbordo","subtitle":"Operaci\u00F3n de exportaci\u00F3n","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CTB-20261004-5A1B2C3D","issuedAt":"2026-10-04T13:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260300610"},{"label":"Booking","value":"HLCUBKG2603061"},{"label":"Operaci\u00F3n","value":"Exportaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Valparaiso Express / 2610S"},{"label":"Ruta","value":"San Antonio (CLSAI) - Rotterdam (NLRTM)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Importadora Demo SpA"},{"label":"Consignee","value":"Fruit Import BV"},{"label":"Notify","value":null}],"table":null,"paragraphs":null},{"heading":"Cliente","fields":[{"label":"Raz\u00F3n social","value":"Importadora Demo SpA"},{"label":"RUT / NIT","value":"76123456-7"},{"label":"Pago","value":null}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU2023001","40RF","SL-020301","26.800,00","GateIn"]],"numericColumns":[3]},"paragraphs":null},{"heading":"Certificaci\u00F3n","fields":null,"table":null,"paragraphs":["Hapag-Lloyd Chile SpA certifica que la carga amparada en el BL HLCUSAI260300610, transportada en la nave Valparaiso Express viaje 2610S, con origen en San Antonio (CLSAI) y destino Rotterdam, Netherlands, fue objeto de transbordo en el puerto de Rotterdam (NLRTM) en las unidades individualizadas en este documento.","Se emite a solicitud del interesado para los fines que estime convenientes."]}],"verificationCode":"EF67-5D1A-FCD1-C29B","signatureNote":"Documento firmado electr\u00F3nicamente. La validez de la firma se acredita seg\u00FAn el mecanismo publicado por Hapag-Lloyd.","footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', NULL, NULL, NULL, 'EF67-5D1A-FCD1-C29B');
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000002', NULL, '11111111-0007-0007-0007-000000000001', 'HLCUVAL250100123', 'HLCUBKG2501001', 'HLXU1234567,HLXU7654321', NULL, 'application/pdf', 'CL', TIMESTAMPTZ '2026-10-03T16:30:00Z', 'demo@importadorademo.cl', NULL, NULL, TIMESTAMPTZ '2026-10-03T16:30:00Z', 'CBL-20261003-7E8F9A0B', 'BlCopyNonValued', 'copia-bl-no-valorada-CBL-20261003-7E8F9A0B.pdf', NULL, TIMESTAMPTZ '2026-10-03T16:30:00Z', 'demo@importadorademo.cl', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, NULL, 'Seed', NULL, NULL, 'demo@importadorademo.cl', TIMESTAMPTZ '2036-10-03T16:30:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Copia de BL - no valorada","subtitle":"Copia informativa, no negociable","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CBL-20261003-7E8F9A0B","issuedAt":"2026-10-03T16:30:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUVAL250100123"},{"label":"Booking","value":"HLCUBKG2501001"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Hamburg Express / 025E"},{"label":"Ruta","value":"Shanghai (CNSHA) - San Antonio (CLSAI)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Shanghai Electronics Co. Ltd"},{"label":"Consignee","value":"Importadora Demo SpA"},{"label":"Notify","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"}],"table":null,"paragraphs":null},{"heading":"Transporte","fields":[{"label":"Nave / viaje","value":"Hamburg Express / 025E"},{"label":"Puerto de carga","value":"Shanghai (CNSHA)"},{"label":"Puerto de descarga","value":"San Antonio (CLSAI)"},{"label":"Lugar de entrega","value":"Santiago, Chile"},{"label":"ETD","value":"01-03-2026"},{"label":"ETA","value":"05-04-2026"},{"label":"Tipo de BL","value":null},{"label":"Incoterm","value":null}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU1234567","40HC","SL-001234","24.500,00","Discharged"],["HLXU7654321","20DV","SL-005678","18.200,00","Discharged"]],"numericColumns":[3]},"paragraphs":null},{"heading":"Mercanc\u00EDa","fields":null,"table":null,"paragraphs":["Sin detalle de mercanc\u00EDa registrado."]},{"heading":"Valores comerciales","fields":null,"table":null,"paragraphs":["Copia no valorada: no incluye el flete ni los cargos del embarque."]},{"heading":"Solicitud","fields":[{"label":"Solicitada por","value":"Importadora Demo SpA (demo@importadorademo.cl)"}],"table":null,"paragraphs":null}],"verificationCode":"90FF-9F4C-557C-3C9D","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', NULL, NULL, NULL, '90FF-9F4C-557C-3C9D');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('eeeeeeee-0014-0014-0014-000000000009', 35000.0, NULL, 'TRANSSHIPMENT_CERT', NULL, 'CL', TIMESTAMPTZ '2026-09-30T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Certificado de transbordo (M6-01)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000015', '11111111-0007-0007-0007-000000000012', 'HLXU3045001', '40HC', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-045001', 'Discharged', NULL, NULL, 25100.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000016', '11111111-0007-0007-0007-000000000013', 'HLXU3045002', '20DV', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-045002', 'Discharged', NULL, NULL, 17900.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000023', 185000.0, '11111111-0007-0007-0007-000000000013', 'THC', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Terminal Handling Charge - 20DV (Valparaíso)', TRUE, NULL, NULL, 'Pending', 35150.0, 19.0, 220150.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('81704c0f-1990-fcac-1ea8-2a34ced3fb44', 'Portal', NULL, 'Issued', TIMESTAMPTZ '2026-10-03T16:30:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, 'ffffffff-0018-0018-0018-000000000002', 'demo@importadorademo.cl', NULL);
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('93928381-c81e-f01c-77a0-ab21529781c6', 'System', NULL, 'Issued', TIMESTAMPTZ '2026-10-04T13:00:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', NULL, 'ffffffff-0018-0018-0018-000000000001', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000003', NULL, '11111111-0007-0007-0007-000000000012', 'HLCUSAI260501240', 'HLCUBKG2605124', 'HLXU3045001', NULL, 'application/pdf', 'CL', TIMESTAMPTZ '2026-10-03T15:05:00Z', 'SYSTEM', NULL, NULL, NULL, 'CCO-20261003-1C2D3E4F', 'CollectReceipt', 'comprobante-collect-CCO-20261003-1C2D3E4F.pdf', NULL, TIMESTAMPTZ '2026-10-03T15:05:00Z', NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000030', NULL, NULL, NULL, 'Seed', NULL, NULL, NULL, TIMESTAMPTZ '2036-10-03T15:05:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Comprobante de flete Collect","subtitle":null,"issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CCO-20261003-1C2D3E4F","issuedAt":"2026-10-03T15:05:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260501240"},{"label":"Booking","value":"HLCUBKG2605124"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Cartagena Express / 2611E"},{"label":"Ruta","value":"Yokohama (JPYOK) - San Antonio (CLSAI)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Yokohama Machinery Co."},{"label":"Consignee","value":"Importadora Demo SpA"},{"label":"Notify","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"}],"table":null,"paragraphs":null},{"heading":"Pago del flete","fields":[{"label":"Condici\u00F3n del flete","value":"Collect"},{"label":"Monto pagado","value":"4.800,00 USD"},{"label":"Pagador","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"},{"label":"RUT / NIT del pagador","value":"96555444-3"},{"label":"Pago","value":null},{"label":"Comprobante de pago","value":null},{"label":"Fecha de pago","value":"03-10-2026 12:00 (America/Santiago)"}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3045001","40HC","SL-045001","25.100,00","Discharged"]],"numericColumns":[3]},"paragraphs":null}],"verificationCode":"B3D6-27E7-7995-FF24","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', NULL, NULL, NULL, 'B3D6-27E7-7995-FF24');
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000004', NULL, '11111111-0007-0007-0007-000000000013', 'HLCUVAP260501350', 'HLCUBKG2605135', 'HLXU3045002', NULL, 'application/pdf', 'CL', TIMESTAMPTZ '2026-10-02T14:00:00Z', 'ffww@globalforwarding.cl', NULL, NULL, NULL, 'CRE-20261002-9F8E7D6C', 'ResponsibilityLetter', 'carta-responsabilidad-CRE-20261002-9F8E7D6C.pdf', NULL, TIMESTAMPTZ '2026-10-02T14:00:00Z', 'ffww@globalforwarding.cl', NULL, 'c3d4e5f6-0003-0003-0003-000000000070', NULL, NULL, NULL, 'Seed', NULL, NULL, NULL, TIMESTAMPTZ '2036-10-02T14:00:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Carta de responsabilidad","subtitle":"T\u00E9rminos CARTA-RESP-2026-10","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CRE-20261002-9F8E7D6C","issuedAt":"2026-10-02T14:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUVAP260501350"},{"label":"Booking","value":"HLCUBKG2605135"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Callao Express / 2611N"},{"label":"Ruta","value":"Shanghai (CNSHA) - Valparaiso (CLVAP)"}],"sections":[{"heading":"Organizaci\u00F3n responsable","fields":[{"label":"Raz\u00F3n social","value":"Global Forwarding Chile SpA"},{"label":"RUT / NIT","value":"76000003-3"}],"table":null,"paragraphs":null},{"heading":"Firmante","fields":[{"label":"Nombre","value":"Felipe Forwarder"},{"label":"Documento de identidad","value":"12.345.678-5"},{"label":"Cargo","value":"Gerente de Operaciones"},{"label":"Correo de contacto","value":"ffww@globalforwarding.cl"},{"label":"Tel\u00E9fono","value":null}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3045002","20DV","SL-045002","17.900,00","Discharged"]],"numericColumns":[3]},"paragraphs":null},{"heading":"Declaraci\u00F3n","fields":[{"label":"Mercanc\u00EDa","value":"Muebles de madera"},{"label":"Observaciones","value":null}],"table":null,"paragraphs":["El firmante, en representaci\u00F3n de la organizaci\u00F3n indicada, declara que los datos ingresados son ver\u00EDdicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulaci\u00F3n, liberando a Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.","T\u00E9rminos aceptados en el portal el 02-10-2026 11:00 (America/Santiago) (versi\u00F3n CARTA-RESP-2026-10)."]}],"verificationCode":"94E7-43DA-1F4E-8AA7","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', TIMESTAMPTZ '2026-10-02T14:00:00Z', 'CARTA-RESP-2026-10', NULL, '94E7-43DA-1F4E-8AA7');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('464a1f72-cc7e-cb5e-aab3-e55e60f9a7bd', '11111111-0007-0007-0007-000000000012', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('6195e2af-bf9f-a6ca-ff6f-e3311b80ac45', '11111111-0007-0007-0007-000000000012', 'c3d4e5f6-0003-0003-0003-000000000030', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'CustomsAgency', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('e3596ff0-7c41-8ca2-284a-837b029aed7f', '11111111-0007-0007-0007-000000000013', 'c3d4e5f6-0003-0003-0003-000000000070', TIMESTAMPTZ '2026-10-01T00:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('853b0d16-b00e-352a-fa93-99c6d48f2438', 'Portal', NULL, 'Issued', TIMESTAMPTZ '2026-10-02T14:00:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000070', NULL, 'ffffffff-0018-0018-0018-000000000004', 'ffww@globalforwarding.cl', NULL);
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('c63bfeee-1240-a9e9-1e0a-56b43357013b', 'System', NULL, 'Issued', TIMESTAMPTZ '2026-10-03T15:05:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000030', NULL, 'ffffffff-0018-0018-0018-000000000003', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE INDEX "IX_ShipmentDocumentEvents_OrganizationId_OccurredAt" ON "ShipmentDocumentEvents" ("OrganizationId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE INDEX "IX_ShipmentDocumentEvents_ShipmentDocumentId_OccurredAt" ON "ShipmentDocumentEvents" ("ShipmentDocumentId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE INDEX "IX_ShipmentDocuments_BillOfLadingId_DocumentType_IssuedAt" ON "ShipmentDocuments" ("BillOfLadingId", "DocumentType", "IssuedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE UNIQUE INDEX "IX_ShipmentDocuments_DocumentNumber" ON "ShipmentDocuments" ("DocumentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE UNIQUE INDEX "IX_ShipmentDocuments_GenerationKey" ON "ShipmentDocuments" ("GenerationKey") WHERE "GenerationKey" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE INDEX "IX_ShipmentDocuments_IssuedForOrganizationId_DocumentType_Stat~" ON "ShipmentDocuments" ("IssuedForOrganizationId", "DocumentType", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    CREATE INDEX "IX_ShipmentDocuments_PaymentId" ON "ShipmentDocuments" ("PaymentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006043944_AddShipmentDocuments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006043944_AddShipmentDocuments', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "DifuCode" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "DifuLocationCode" character varying(10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "EblPlatform" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "FinalDestinationCode" character varying(10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "IssuanceStatus" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "IssuanceStatusAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "PortOfDischargeCode" character varying(10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    ALTER TABLE "BillsOfLading" ADD "TransportDocumentType" character varying(10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "AssistantMailboxes" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "Topic" character varying(30) NOT NULL,
        "Email" character varying(256) NOT NULL,
        "Notes" character varying(300),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_AssistantMailboxes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "AssistantSessions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "OrganizationId" uuid,
        "UserEmail" character varying(256) NOT NULL,
        "Country" character varying(5) NOT NULL,
        "Language" character varying(5) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "EngineMode" character varying(20) NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "LastActivityAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone,
        "TranscriptSentTo" character varying(256),
        "TranscriptSentAt" timestamp with time zone,
        CONSTRAINT "PK_AssistantSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "DangerousGoods" (
        "Id" uuid NOT NULL,
        "UnNumber" character varying(4),
        "ProperShippingNameEs" character varying(300) NOT NULL,
        "ProperShippingNameEn" character varying(300) NOT NULL,
        "HazardClass" character varying(5),
        "SubsidiaryRisk" character varying(20),
        "PackingGroup" character varying(3),
        "Notes" character varying(500),
        "Keywords" character varying(500),
        "IsClassified" boolean NOT NULL,
        "Source" character varying(20) NOT NULL,
        "SearchText" character varying(1500) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_DangerousGoods" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "KnowledgeArticles" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "Topic" character varying(30) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Content" character varying(4000) NOT NULL,
        "Keywords" character varying(500),
        "SortOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "SourceFaqId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_KnowledgeArticles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "ShipmentPublicationRules" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "FinalDestinationCode" character varying(10) NOT NULL,
        "FinalDestinationName" character varying(100),
        "DischargePortCode" character varying(10),
        "Description" character varying(500),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ShipmentPublicationRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "TatcBatches" (
        "Id" uuid NOT NULL,
        "ClientId" uuid NOT NULL,
        "RequestedByUserId" uuid NOT NULL,
        "RequestedByEmail" character varying(256) NOT NULL,
        "Country" character varying(5) NOT NULL,
        "LocationCode" character varying(10) NOT NULL,
        "Status" character varying(30) NOT NULL,
        "SourceRequestId" character varying(100),
        "ErrorCode" character varying(100),
        "TotalItems" integer NOT NULL,
        "AcceptedItems" integer NOT NULL,
        "RejectedItems" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        CONSTRAINT "PK_TatcBatches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "AssistantMessages" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "Sequence" integer NOT NULL,
        "Role" character varying(20) NOT NULL,
        "Content" text NOT NULL,
        "Intent" character varying(30),
        "AnswerType" character varying(30),
        "CitationsJson" text,
        "ActionsJson" text,
        "Engine" character varying(20),
        "EngineFallback" boolean NOT NULL,
        "ElapsedMs" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AssistantMessages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssistantMessages_AssistantSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "AssistantSessions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE TABLE "TatcBatchItems" (
        "Id" uuid NOT NULL,
        "BatchId" uuid NOT NULL,
        "LineNumber" integer NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "BillOfLadingId" uuid,
        "Status" character varying(20) NOT NULL,
        "ReasonCode" character varying(50),
        CONSTRAINT "PK_TatcBatchItems" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TatcBatchItems_TatcBatches_BatchId" FOREIGN KEY ("BatchId") REFERENCES "TatcBatches" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "AssistantMailboxes" ("Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "ModifiedAt", "ModifiedBy", "Notes", "Topic")
    VALUES ('501c69c3-4255-3394-8838-8a916d18bfff', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'boservice@hapag-lloyd.com', TRUE, NULL, NULL, 'Casilla por validar con Customer Service Bolivia antes de producción.', 'GENERAL');
    INSERT INTO "AssistantMailboxes" ("Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "ModifiedAt", "ModifiedBy", "Notes", "Topic")
    VALUES ('b305d4b2-1f96-e2ba-6fbf-3306a86c0f69', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'clservice@hapag-lloyd.com', TRUE, NULL, NULL, 'Casilla de Customer Service publicada en la FAQ del portal.', 'GENERAL');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLSCL', "IssuanceStatus" = 'TelexReleased', "IssuanceStatusAt" = TIMESTAMPTZ '2026-04-02T14:00:00Z', "PortOfDischargeCode" = 'CLSAI', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLVAP', "IssuanceStatus" = 'Issued', "IssuanceStatusAt" = TIMESTAMPTZ '2026-04-16T09:00:00Z', "PortOfDischargeCode" = 'CLVAP', "TransportDocumentType" = 'SWB'
    WHERE "Id" = '11111111-0007-0007-0007-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLSCL', "IssuanceStatus" = 'Surrendered', "IssuanceStatusAt" = TIMESTAMPTZ '2026-02-16T12:00:00Z', "PortOfDischargeCode" = 'CLSAI', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'BOLPB', "IssuanceStatus" = 'AuthorizedAtDestination', "IssuanceStatusAt" = TIMESTAMPTZ '2026-03-20T15:00:00Z', "PortOfDischargeCode" = 'CLARI', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'BOSRZ', "IssuanceStatus" = 'IssuedAtDestination', "IssuanceStatusAt" = TIMESTAMPTZ '2026-05-08T13:00:00Z', "PortOfDischargeCode" = 'CLIQQ', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = 'WAVE', "FinalDestinationCode" = 'NLRTM', "IssuanceStatus" = 'Pending', "IssuanceStatusAt" = NULL, "PortOfDischargeCode" = 'NLRTM', "TransportDocumentType" = 'EBL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = 'WAVE', "FinalDestinationCode" = 'CNSHA', "IssuanceStatus" = 'Issued', "IssuanceStatusAt" = TIMESTAMPTZ '2026-10-29T10:00:00Z', "PortOfDischargeCode" = 'CNSHA', "TransportDocumentType" = 'EBL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'PELIM', "IssuanceStatus" = 'Pending', "IssuanceStatusAt" = NULL, "PortOfDischargeCode" = 'PECLL', "TransportDocumentType" = 'SWB'
    WHERE "Id" = '11111111-0007-0007-0007-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLSCL', "IssuanceStatus" = 'Issued', "IssuanceStatusAt" = TIMESTAMPTZ '2026-07-16T11:00:00Z', "PortOfDischargeCode" = 'CLSAI', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLSCL', "IssuanceStatus" = 'Issued', "IssuanceStatusAt" = TIMESTAMPTZ '2026-08-21T10:00:00Z', "PortOfDischargeCode" = 'CLSAI', "TransportDocumentType" = 'SWB'
    WHERE "Id" = '11111111-0007-0007-0007-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = 'WAVE', "FinalDestinationCode" = 'CLVAP', "IssuanceStatus" = 'Transferred', "IssuanceStatusAt" = TIMESTAMPTZ '2026-09-01T16:00:00Z', "PortOfDischargeCode" = 'CLVAP', "TransportDocumentType" = 'EBL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000011';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLSCL', "IssuanceStatus" = 'TelexReleased', "IssuanceStatusAt" = TIMESTAMPTZ '2026-09-30T18:00:00Z', "PortOfDischargeCode" = 'CLSAI', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000012';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    UPDATE "BillsOfLading" SET "DifuCode" = NULL, "DifuLocationCode" = NULL, "EblPlatform" = NULL, "FinalDestinationCode" = 'CLVAP', "IssuanceStatus" = 'Issued', "IssuanceStatusAt" = TIMESTAMPTZ '2026-08-31T09:00:00Z', "PortOfDischargeCode" = 'CLVAP', "TransportDocumentType" = 'BL'
    WHERE "Id" = '11111111-0007-0007-0007-000000000013';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000014', 'HLCUSAI260601410', NULL, 'HLCUBKG2606141', 'c3d4e5f6-0003-0003-0003-000000000010', 'Importadora Demo SpA', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-10-03T00:00:00Z', TIMESTAMPTZ '2026-09-01T00:00:00Z', NULL, 'CLANF', 3100.0, 'USD', NULL, NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-09-02T12:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Antofagasta, Chile', 'San Antonio (CLSAI)', 'CLSAI', 'Shanghai (CNSHA)', 'Import', 'Shanghai Mining Supplies Co.', 'Arrived', 'BL', 'Lima Express', NULL, '2612E');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000015', 'HLCUSAI260601520', NULL, 'HLCUBKG2606152', 'c3d4e5f6-0003-0003-0003-000000000010', 'Importadora Demo SpA', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'PUQ-DIFU-0915', 'CLPUQ', TIMESTAMPTZ '2026-10-03T00:00:00Z', TIMESTAMPTZ '2026-09-01T00:00:00Z', NULL, 'CLPUQ', 2750.0, 'USD', TIMESTAMPTZ '2026-09-05T15:00:00Z', NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-09-02T12:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Punta Arenas, Chile', 'San Antonio (CLSAI)', 'CLSAI', 'Shanghai (CNSHA)', 'Import', 'Shanghai Cold Chain Ltd', 'Arrived', 'SWB', 'Lima Express', NULL, '2612E');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('077e55d6-7b2e-253b-c6c9-c5bd14640d3e', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '8', TRUE, TRUE, 'baterias, acumuladores, bateria de plomo', NULL, NULL, NULL, NULL, 'Batteries, wet, filled with acid', 'Baterías húmedas llenas de ácido', 'un2794 2794 baterias humedas llenas de acido batteries, wet, filled with acid baterias, acumuladores, bateria de plomo', 'SAMPLE', NULL, '2794');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('0a52470d-60bc-f9c2-e321-f92766b42e8c', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'petroleo diesel, gasoil, combustible', NULL, NULL, NULL, 'III', 'Diesel fuel', 'Combustible diésel', 'un1202 1202 combustible diesel diesel fuel petroleo diesel, gasoil, combustible', 'SAMPLE', NULL, '1202');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('0d4ec6d0-512d-2417-9bf6-7a963350c92f', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'baterias de litio, pilas de litio', NULL, NULL, 'Incluye baterías de polímero de ion litio.', NULL, 'Lithium ion batteries', 'Baterías de ion litio', 'un3480 3480 baterias de ion litio lithium ion batteries baterias de litio, pilas de litio', 'SAMPLE', NULL, '3480');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('105d0eac-743d-7d48-d7b7-b13227141e48', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, FALSE, 'cobre, catodos', NULL, NULL, NULL, NULL, 'Copper cathodes', 'Cátodos de cobre', 'catodos de cobre copper cathodes cobre, catodos', 'SAMPLE', NULL, NULL);
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('22362860-9851-a198-09da-7fe8490b5ede', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '5.1', TRUE, TRUE, 'nitrato de amonio', NULL, NULL, 'Con no más del 0,2 % de sustancia combustible.', 'III', 'Ammonium nitrate', 'Nitrato de amonio', 'un1942 1942 nitrato de amonio ammonium nitrate nitrato de amonio', 'SAMPLE', NULL, '1942');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('24765d35-fb69-b6b5-85f8-ded3bb725df7', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'bencina, nafta, combustible', NULL, NULL, NULL, 'II', 'Gasoline (motor spirit)', 'Gasolina', 'un1203 1203 gasolina gasoline (motor spirit) bencina, nafta, combustible', 'SAMPLE', NULL, '1203');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('304efdd2-7458-277b-1c60-2af4af1af337', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'hielo seco, co2 solido', NULL, NULL, NULL, NULL, 'Carbon dioxide, solid (dry ice)', 'Dióxido de carbono sólido (hielo seco)', 'un1845 1845 dioxido de carbono solido (hielo seco) carbon dioxide, solid (dry ice) hielo seco, co2 solido', 'SAMPLE', NULL, '1845');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('327ab997-3424-cb68-15b0-3b7b33aaf1b8', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '8', TRUE, TRUE, 'soda caustica, sosa caustica', NULL, NULL, NULL, 'II', 'Sodium hydroxide, solid', 'Hidróxido de sodio sólido', 'un1823 1823 hidroxido de sodio solido sodium hydroxide, solid soda caustica, sosa caustica', 'SAMPLE', NULL, '1823');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('336746ef-6353-0852-f1ce-9537ca17110f', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '5.1', TRUE, TRUE, 'cloro, cloro granulado, piscina', NULL, NULL, NULL, 'II', 'Calcium hypochlorite, dry', 'Hipoclorito de calcio seco', 'un1748 1748 hipoclorito de calcio seco calcium hypochlorite, dry cloro, cloro granulado, piscina', 'SAMPLE', NULL, '1748');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('36f15fa3-1970-ac8d-989d-6e23f2d81c61', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '2.1', TRUE, TRUE, 'gas propano, glp', NULL, NULL, NULL, NULL, 'Propane', 'Propano', 'un1978 1978 propano propane gas propano, glp', 'SAMPLE', NULL, '1978');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('4c7b6b59-ddd3-95f4-5279-79efa3f44e22', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, FALSE, 'ropa, textiles, vestuario', NULL, NULL, NULL, NULL, 'Clothing', 'Prendas de vestir', 'prendas de vestir clothing ropa, textiles, vestuario', 'SAMPLE', NULL, NULL);
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('50ef7190-04da-4782-2266-529843702934', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, FALSE, 'vino, bebidas alcoholicas', NULL, NULL, 'Las bebidas alcohólicas con más de 24 % de alcohol en volumen se clasifican como UN3065, clase 3.', NULL, 'Bottled wine', 'Vino embotellado', 'vino embotellado bottled wine vino, bebidas alcoholicas', 'SAMPLE', NULL, NULL);
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('5c5b3d26-bc80-92ac-c4e3-d2ff92ad99e8', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'liquido inflamable', NULL, NULL, 'Grupo de embalaje según el punto de inflamación.', NULL, 'Flammable liquid, n.o.s.', 'Líquido inflamable, n.e.p.', 'un1993 1993 liquido inflamable, n.e.p. flammable liquid, n.o.s. liquido inflamable', 'SAMPLE', NULL, '1993');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('61cf63a7-152d-af37-4217-c3a609aa67b4', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '5.1', TRUE, TRUE, 'fertilizante, abono', NULL, NULL, NULL, 'III', 'Ammonium nitrate based fertilizer', 'Abonos a base de nitrato de amonio', 'un2067 2067 abonos a base de nitrato de amonio ammonium nitrate based fertilizer fertilizante, abono', 'SAMPLE', NULL, '2067');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('68b6e81e-140d-38e3-53bf-f395f3607ae5', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'alcohol etilico, alcohol', NULL, NULL, 'Grupo de embalaje II o III según la concentración.', 'II', 'Ethanol (ethyl alcohol) or ethanol solution', 'Etanol (alcohol etílico) o solución de etanol', 'un1170 1170 etanol (alcohol etilico) o solucion de etanol ethanol (ethyl alcohol) or ethanol solution alcohol etilico, alcohol', 'SAMPLE', NULL, '1170');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('6fbed7a0-0f71-dfb5-5bfa-5580ac1cd6d1', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'baterias de litio, equipos electronicos', NULL, NULL, NULL, NULL, 'Lithium ion batteries contained in equipment or packed with equipment', 'Baterías de ion litio contenidas en un equipo o embaladas con él', 'un3481 3481 baterias de ion litio contenidas en un equipo o embaladas con el lithium ion batteries contained in equipment or packed with equipment baterias de litio, equipos electronicos', 'SAMPLE', NULL, '3481');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('7e4bfe6c-34af-523c-57a2-074bbda12a84', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'contaminante marino', NULL, NULL, NULL, 'III', 'Environmentally hazardous substance, solid, n.o.s.', 'Sustancia sólida peligrosa para el medio ambiente, n.e.p.', 'un3077 3077 sustancia solida peligrosa para el medio ambiente, n.e.p. environmentally hazardous substance, solid, n.o.s. contaminante marino', 'SAMPLE', NULL, '3077');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('8618626d-f8e5-680d-b35a-eea8b98e93a0', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '2.3', TRUE, TRUE, 'amoniaco, refrigerante', NULL, NULL, NULL, NULL, 'Ammonia, anhydrous', 'Amoníaco anhidro', 'un1005 1005 amoniaco anhidro ammonia, anhydrous amoniaco, refrigerante', 'SAMPLE', '8', '1005');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('9df6d733-49f7-4989-63a0-ff8d601424fc', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '2.1', TRUE, TRUE, 'glp, gas licuado', NULL, NULL, NULL, NULL, 'Petroleum gases, liquefied', 'Gases de petróleo licuados', 'un1075 1075 gases de petroleo licuados petroleum gases, liquefied glp, gas licuado', 'SAMPLE', NULL, '1075');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('a32273dd-ab7d-00e5-6006-7e8c91bf8797', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '8', TRUE, TRUE, 'acido sulfurico', NULL, NULL, NULL, 'II', 'Sulphuric acid', 'Ácido sulfúrico', 'un1830 1830 acido sulfurico sulphuric acid acido sulfurico', 'SAMPLE', NULL, '1830');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('ac55b5f8-ff9c-c7da-9364-3ea41d040a6b', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '4.2', TRUE, TRUE, 'carbon vegetal, carbon', NULL, NULL, 'Grupo de embalaje II o III.', NULL, 'Carbon, animal or vegetable origin', 'Carbón de origen animal o vegetal', 'un1361 1361 carbon de origen animal o vegetal carbon, animal or vegetable origin carbon vegetal, carbon', 'SAMPLE', NULL, '1361');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('bffa68f3-de6e-6f8d-a472-ca8dce2a9e1c', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, FALSE, 'muebles, mobiliario', NULL, NULL, NULL, NULL, 'Wooden furniture', 'Muebles de madera', 'muebles de madera wooden furniture muebles, mobiliario', 'SAMPLE', NULL, NULL);
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('c109a092-3cf7-5b9a-1bda-25476a9ed4a8', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'pinturas, barniz, laca, esmalte', NULL, NULL, 'Grupo de embalaje I, II o III según el punto de inflamación.', NULL, 'Paint', 'Pintura', 'un1263 1263 pintura paint pinturas, barniz, laca, esmalte', 'SAMPLE', NULL, '1263');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('c2f40342-cc39-4603-7153-f475053adf2b', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'baterias de litio, pilas de litio', NULL, NULL, NULL, NULL, 'Lithium metal batteries', 'Baterías de metal litio', 'un3090 3090 baterias de metal litio lithium metal batteries baterias de litio, pilas de litio', 'SAMPLE', NULL, '3090');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('c3941ce9-d357-2e6c-351c-388924c9a455', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, FALSE, 'fruta, manzanas, uvas, cerezas', NULL, NULL, 'Carga refrigerada no clasificada como mercancía peligrosa.', NULL, 'Fresh fruit', 'Fruta fresca', 'fruta fresca fresh fruit fruta, manzanas, uvas, cerezas', 'SAMPLE', NULL, NULL);
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('d19fd508-6e73-b57b-4130-d86abe076cc4', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'alcohol', NULL, NULL, 'Grupo de embalaje II o III según el punto de inflamación.', NULL, 'Alcohols, n.o.s.', 'Alcoholes, n.e.p.', 'un1987 1987 alcoholes, n.e.p. alcohols, n.o.s. alcohol', 'SAMPLE', NULL, '1987');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('d301d0ab-c9c3-abc7-8615-4eff72143cc1', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'automovil, auto, vehiculo', NULL, NULL, NULL, NULL, 'Vehicle, flammable liquid powered', 'Vehículo propulsado por líquido inflamable', 'un3166 3166 vehiculo propulsado por liquido inflamable vehicle, flammable liquid powered automovil, auto, vehiculo', 'SAMPLE', NULL, '3166');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('d81659f8-e496-ef4a-5d69-04669899a616', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '2.1', TRUE, TRUE, 'spray, aerosol', NULL, NULL, 'La división (2.1, 2.2 o 2.3) depende del contenido del aerosol.', NULL, 'Aerosols', 'Aerosoles', 'un1950 1950 aerosoles aerosols spray, aerosol', 'SAMPLE', NULL, '1950');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('d9bab528-d5bd-3ca1-e223-ae7685f250af', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '9', TRUE, TRUE, 'contaminante marino', NULL, NULL, NULL, 'III', 'Environmentally hazardous substance, liquid, n.o.s.', 'Sustancia líquida peligrosa para el medio ambiente, n.e.p.', 'un3082 3082 sustancia liquida peligrosa para el medio ambiente, n.e.p. environmentally hazardous substance, liquid, n.o.s. contaminante marino', 'SAMPLE', NULL, '3082');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('e1710ee1-b144-16d0-595a-d4094795db40', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '2.2', TRUE, TRUE, 'co2, gas carbonico', NULL, NULL, NULL, NULL, 'Carbon dioxide', 'Dióxido de carbono', 'un1013 1013 dioxido de carbono carbon dioxide co2, gas carbonico', 'SAMPLE', NULL, '1013');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('ec432f34-8f5a-b0ea-8990-8c6e2dbf415a', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '8', TRUE, TRUE, 'acido clorhidrico, acido muriatico', NULL, NULL, 'Grupo de embalaje II o III según la concentración.', NULL, 'Hydrochloric acid', 'Ácido clorhídrico', 'un1789 1789 acido clorhidrico hydrochloric acid acido clorhidrico, acido muriatico', 'SAMPLE', NULL, '1789');
    INSERT INTO "DangerousGoods" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "HazardClass", "IsActive", "IsClassified", "Keywords", "ModifiedAt", "ModifiedBy", "Notes", "PackingGroup", "ProperShippingNameEn", "ProperShippingNameEs", "SearchText", "Source", "SubsidiaryRisk", "UnNumber")
    VALUES ('ecb4a1d5-2dab-226d-7bdc-8244a9635258', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, '3', TRUE, TRUE, 'solvente, quitaesmalte', NULL, NULL, NULL, 'II', 'Acetone', 'Acetona', 'un1090 1090 acetona acetone solvente, quitaesmalte', 'SAMPLE', NULL, '1090');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('0cf519fe-d8a2-241f-740d-011e9f6eec9d', 'El NIT (Número de Identificación Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisión de documentos fiscales.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 4, 'f6a7b8c9-0006-0006-0006-000000000014', '¿Qué es el NIT y por qué lo necesito?', 'GENERAL');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('16a9eb6a-7ade-d4a6-e84b-1f7b78787229', 'En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 2, 'f6a7b8c9-0006-0006-0006-000000000012', '¿Qué métodos de pago están disponibles en Bolivia?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('1d1add93-0f39-af1b-5416-c59e1ae23e74', 'En Accesos de terceros, el administrador de su organización puede otorgar acceso a un BL o booking, de forma individual o masiva, con vigencia y permisos definidos, y revocarlo cuando quiera. También puede configurar terceros por defecto para los BL nuevos. Todos los cambios quedan registrados en la auditoría.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'acceso, terceros, agencia, mandato, otorgar, revocar', NULL, NULL, 26, NULL, '¿Cómo doy acceso a mi agencia de aduanas u otro tercero?', 'GENERAL');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('1d9a519c-78d6-5e51-dff3-20d2aeeea4ab', 'Agregue al carro los cargos pendientes desde el detalle del BL, la pestaña de demurrage o sus facturas, indicando el RUT de facturación. El carro agrupa los ítems por país y moneda de pago; cada grupo se paga por separado con los medios habilitados, por ejemplo Khipu, botón de pago bancario o depósito con boleta.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'carro, pago, pagar, khipu, deposito, boleta, moneda, rut de facturacion', NULL, NULL, 25, NULL, '¿Cómo pago mis servicios en el carro?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('2e485c6e-0932-aa83-fca0-44c221257b56', 'En Documentos del embarque solicite el certificado de transbordo: el portal agrega el cargo del servicio según la tarifa vigente y, una vez confirmado el pago en el carro, emite el certificado firmado, lo publica en el repositorio del BL y lo envía por correo.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'certificado, transbordo, certificado de transbordo', NULL, NULL, 23, NULL, '¿Cómo obtengo el certificado de transbordo?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('3746d3f8-f379-04cc-942e-236c3c248994', 'El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los días libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a través del módulo de Demurrage.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 5, 'f6a7b8c9-0006-0006-0006-000000000015', '¿Cómo funciona el demurrage para carga en tránsito a Bolivia?', 'DEMURRAGE');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('38e0bbdf-dcfd-ad4b-7425-a43db4786451', 'Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al +56 2 2630 1700 (Chile) / +591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 12, 'f6a7b8c9-0006-0006-0006-000000000023', '¿Cómo contacto a soporte técnico?', 'GENERAL');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('39b01132-be66-8672-471e-d76ffb03210f', 'Ingrese al módulo ''Bills of Lading'' y busque por número de BL. Verá el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 1, 'f6a7b8c9-0006-0006-0006-000000000011', '¿Cómo puedo consultar el estado de mi BL en Bolivia?', 'SHIPPING');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('4e576670-dc83-d46f-b8df-48b2cce4003e', 'Desde el detalle del BL o la sección Cambio de almacén puede solicitar el cambio para el BL completo o para un contenedor. Si su cuenta tiene derecho a un cambio gratuito (condición informada por Nexus o regla del portal), la solicitud queda completada sin costo. Si no, se aplica la tarifa vigente (KTE o KTF) y el cargo queda listo para pagarlo en el carro. Para varios BL use la solicitud masiva y consulte su avance en la misma sección.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'almacen, cambio de almacen, bodega, KTE, KTF, deposito', NULL, NULL, 20, NULL, '¿Cómo funciona el cambio de almacén?', 'SHIPPING');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('55710394-2ba1-ea8b-e512-20c30b428663', 'Si su organización es un Freight Forwarder autorizado y es consignatario del BL, debe emitir la carta de responsabilidad antes de pagar los cargos del embarque. Se genera en Documentos del embarque completando los datos del firmante y aceptando los términos vigentes; una carta vigente levanta el bloqueo de ese BL.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'carta, carta de responsabilidad, ffww, freight forwarder, bloqueo', NULL, NULL, 22, NULL, '¿Qué es la carta de responsabilidad para Freight Forwarders?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('56ae180b-f99c-6a17-1eea-6e0c979bec7f', 'Algunas cuentas de Bolivia deben pagar demoras anticipadas por contenedor antes de emitir el CLD. El portal las informa en la pestaña de demurrage del BL; al pagarlas, el monto se descuenta del MHD y deja de bloquear el CLD.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'demoras anticipadas, adelanto, demurrage, mhd', NULL, NULL, 21, NULL, '¿Qué son las demoras anticipadas?', 'DEMURRAGE');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('5a58c5dd-046f-3f66-b69b-2b680c1c146d', 'Vaya al módulo ''Cambio de Almacén'', ingrese el número de BL, el contenedor, el almacén actual y el almacén destino. La solicitud será procesada y recibirá confirmación por correo.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 3, 'f6a7b8c9-0006-0006-0006-000000000003', '¿Cómo solicito un cambio de almacén?', 'SHIPPING');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('5a6f0183-6ea9-4ca5-4022-a73b23a80b89', 'En Documentos del embarque de un BL de importación de Bolivia consulte si el CLD está disponible. Se emite firmado cuando no hay recargos, demurrage, facturas, flete Collect ni demoras anticipadas pendientes; si algo falta, el portal indica qué bloquea la emisión.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'cld, libre deuda, certificado, bolivia', NULL, NULL, 20, NULL, '¿Cómo obtengo el certificado de libre deuda (CLD)?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('5dfa074c-9654-883f-68a8-2dafe0784949', 'La tasa de IVA vigente en Bolivia es del 13%. Se aplica automáticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 3, 'f6a7b8c9-0006-0006-0006-000000000013', '¿Cuál es la tasa de IVA aplicada en Bolivia?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('66411fca-e5ab-f656-ff6d-a7a9f60ed389', 'En la pantalla de login, haga clic en ''¿Olvidó su contraseña?''. Ingrese su correo electrónico y recibirá un enlace para restablecer su contraseña.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 11, 'f6a7b8c9-0006-0006-0006-000000000022', '¿Olvidé mi contraseña, cómo la recupero?', 'GENERAL');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('715a32fa-66b0-77ea-386c-de7cd891089f', 'En Chile puede pagar con Tarjeta de Crédito, Tarjeta de Débito, Transferencia Bancaria y WebPay. Todos los pagos electrónicos se procesan en tiempo real.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 2, 'f6a7b8c9-0006-0006-0006-000000000002', '¿Qué métodos de pago están disponibles en Chile?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('96bd2015-d692-1d1d-3b1d-e217f757b716', 'Ingrese al módulo ''Bills of Lading'', escriba su número de BL en el buscador y presione buscar. Verá el detalle completo incluyendo contenedores, cargos locales y demurrage.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 1, 'f6a7b8c9-0006-0006-0006-000000000001', '¿Cómo puedo consultar el estado de mi BL?', 'SHIPPING');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('9a6bc66a-dfa0-63b8-a053-f9d11bdf40c9', 'En el carro elija Depósito o transferencia bancaria y emita la boleta. Realice el depósito o la transferencia por el monto indicado; Finanzas confirma el abono y el pago queda confirmado en el historial de pagos.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'deposito, transferencia, boleta, pago, bolivianos', NULL, NULL, 22, NULL, '¿Cómo pago con depósito o transferencia en Bolivia?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('a43ec294-e615-b557-7514-1d74754dc32b', 'Una vez confirmado el pago, vaya al detalle del pago y presione ''Generar Recibo''. El recibo se genera automáticamente en formato PDF con todos los datos fiscales.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 5, 'f6a7b8c9-0006-0006-0006-000000000005', '¿Cómo genero un recibo de pago?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('a5db2fd7-be42-da7c-64c0-510c8a1da34b', 'En Documentos del embarque puede solicitar la copia del BL valorada (con fletes y cargos) o no valorada (sin valores comerciales). La copia se publica en el repositorio del embarque y se envía al correo registrado de su organización. El shipper solo accede a la copia no valorada.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'copia, copia de bl, valorada, no valorada, documento', NULL, NULL, 21, NULL, '¿Cómo solicito una copia del BL?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('a7624e2a-707b-e991-f46a-7e54449dd785', 'En Accesos de terceros, el administrador de su organización puede otorgar acceso a un BL o booking, con vigencia y permisos definidos, y revocarlo cuando quiera. Todos los cambios quedan registrados en la auditoría.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'acceso, terceros, agencia, mandato, otorgar, revocar', NULL, NULL, 24, NULL, '¿Cómo doy acceso a mi agencia de aduanas u otro tercero?', 'GENERAL');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('cb0c73ee-3a8a-5b3b-9033-b8087818da70', 'La tasa de IVA vigente en Chile es del 19%. Se aplica automáticamente sobre los cargos locales y servicios facturables.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 4, 'f6a7b8c9-0006-0006-0006-000000000004', '¿Cuál es la tasa de IVA aplicada en Chile?', 'PAYMENTS');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('d1273909-b058-56f3-cd79-8b966b84fe8b', 'En el detalle de un BL de importación, la sección TATC muestra el estado vigente de cada contenedor según el sistema de TATC (sin emitir, pre-TATC, emitido o anulado) y los motivos pendientes, como pagos o documentos. Los clientes con alto volumen en una misma localidad pueden solicitar la generación masiva de TATC.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'tatc, retiro, contenedor, generacion masiva', NULL, NULL, 24, NULL, '¿Dónde consulto el estado del TATC?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('dfbd1162-e8fd-569d-96bf-dbd1963d5fa1', 'En el detalle de un BL de importación, la sección TATC muestra el estado vigente de cada contenedor según el sistema de TATC y los motivos pendientes. Para operaciones de alto volumen en una misma localidad puede solicitar la generación masiva de TATC.', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, 'tatc, retiro, contenedor, generacion masiva', NULL, NULL, 23, NULL, '¿Dónde consulto el estado del TATC?', 'DOCUMENTATION');
    INSERT INTO "KnowledgeArticles" ("Id", "Content", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "Keywords", "ModifiedAt", "ModifiedBy", "SortOrder", "SourceFaqId", "Title", "Topic")
    VALUES ('f569fc25-d919-0336-5d34-38d94c4bfa4f', 'Haga clic en ''Registrarse'', seleccione su país (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contraseña. Recibirá un correo de confirmación.', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, NULL, 10, 'f6a7b8c9-0006-0006-0006-000000000021', '¿Cómo registro mi empresa en el portal?', 'GENERAL');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('04ebfebf-4705-16f2-4f2e-eb85300cf792', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'cb0c73ee-3a8a-5b3b-9033-b8087818da70', 'KnowledgeArticle', '{"country":"CL","topic":"PAYMENTS","title":"\u00BFCu\u00E1l es la tasa de IVA aplicada en Chile?","content":"La tasa de IVA vigente en Chile es del 19%. Se aplica autom\u00E1ticamente sobre los cargos locales y servicios facturables.","keywords":null,"sortOrder":4,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('0c9f6b00-f9a6-debc-c14a-7e1f21d25b09', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '16a9eb6a-7ade-d4a6-e84b-1f7b78787229', 'KnowledgeArticle', '{"country":"BO","topic":"PAYMENTS","title":"\u00BFQu\u00E9 m\u00E9todos de pago est\u00E1n disponibles en Bolivia?","content":"En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.","keywords":null,"sortOrder":2,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('0ee73058-562b-0d9c-5f40-f1d916eb621d', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '5a58c5dd-046f-3f66-b69b-2b680c1c146d', 'KnowledgeArticle', '{"country":"CL","topic":"SHIPPING","title":"\u00BFC\u00F3mo solicito un cambio de almac\u00E9n?","content":"Vaya al m\u00F3dulo \u0027Cambio de Almac\u00E9n\u0027, ingrese el n\u00FAmero de BL, el contenedor, el almac\u00E9n actual y el almac\u00E9n destino. La solicitud ser\u00E1 procesada y recibir\u00E1 confirmaci\u00F3n por correo.","keywords":null,"sortOrder":3,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('1758de8e-db7b-f140-0035-1b74fa2dcc71', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '39b01132-be66-8672-471e-d76ffb03210f', 'KnowledgeArticle', '{"country":"BO","topic":"SHIPPING","title":"\u00BFC\u00F3mo puedo consultar el estado de mi BL en Bolivia?","content":"Ingrese al m\u00F3dulo \u0027Bills of Lading\u0027 y busque por n\u00FAmero de BL. Ver\u00E1 el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.","keywords":null,"sortOrder":1,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('1c9945f0-fef8-6143-5cac-5262b6360710', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '66411fca-e5ab-f656-ff6d-a7a9f60ed389', 'KnowledgeArticle', '{"country":"CL","topic":"GENERAL","title":"\u00BFOlvid\u00E9 mi contrase\u00F1a, c\u00F3mo la recupero?","content":"En la pantalla de login, haga clic en \u0027\u00BFOlvid\u00F3 su contrase\u00F1a?\u0027. Ingrese su correo electr\u00F3nico y recibir\u00E1 un enlace para restablecer su contrase\u00F1a.","keywords":null,"sortOrder":11,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('23016027-cc30-1ce9-7dce-0d4b58bb031c', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'dfbd1162-e8fd-569d-96bf-dbd1963d5fa1', 'KnowledgeArticle', '{"country":"BO","topic":"DOCUMENTATION","title":"\u00BFD\u00F3nde consulto el estado del TATC?","content":"En el detalle de un BL de importaci\u00F3n, la secci\u00F3n TATC muestra el estado vigente de cada contenedor seg\u00FAn el sistema de TATC y los motivos pendientes. Para operaciones de alto volumen en una misma localidad puede solicitar la generaci\u00F3n masiva de TATC.","keywords":"tatc, retiro, contenedor, generacion masiva","sortOrder":23,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('2e984f5f-ce4d-8337-cec7-b6598ed60abc', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '38e0bbdf-dcfd-ad4b-7425-a43db4786451', 'KnowledgeArticle', '{"country":"CL","topic":"GENERAL","title":"\u00BFC\u00F3mo contacto a soporte t\u00E9cnico?","content":"Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al \u002B56 2 2630 1700 (Chile) / \u002B591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.","keywords":null,"sortOrder":12,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('330b0267-314d-b97e-6b7e-be7e77469a3a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0019-0019-0019-000000000001', 'ShipmentPublicationRule', '{"country":"CL","finalDestinationCode":"CLANF","finalDestinationName":"Antofagasta","dischargePortCode":"CLSAI","description":"Carga con destino final Antofagasta distribuida desde San Antonio: se publica con DIFU asociado al destino final.","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('41f0ba56-f20c-64d3-e1be-03d34ada591a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '2e485c6e-0932-aa83-fca0-44c221257b56', 'KnowledgeArticle', '{"country":"CL","topic":"DOCUMENTATION","title":"\u00BFC\u00F3mo obtengo el certificado de transbordo?","content":"En Documentos del embarque solicite el certificado de transbordo: el portal agrega el cargo del servicio seg\u00FAn la tarifa vigente y, una vez confirmado el pago en el carro, emite el certificado firmado, lo publica en el repositorio del BL y lo env\u00EDa por correo.","keywords":"certificado, transbordo, certificado de transbordo","sortOrder":23,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('448be100-c31a-072d-a990-dd783e9be688', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '55710394-2ba1-ea8b-e512-20c30b428663', 'KnowledgeArticle', '{"country":"CL","topic":"DOCUMENTATION","title":"\u00BFQu\u00E9 es la carta de responsabilidad para Freight Forwarders?","content":"Si su organizaci\u00F3n es un Freight Forwarder autorizado y es consignatario del BL, debe emitir la carta de responsabilidad antes de pagar los cargos del embarque. Se genera en Documentos del embarque completando los datos del firmante y aceptando los t\u00E9rminos vigentes; una carta vigente levanta el bloqueo de ese BL.","keywords":"carta, carta de responsabilidad, ffww, freight forwarder, bloqueo","sortOrder":22,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('448e2614-c473-a342-7c89-45d7076f9c91', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0019-0019-0019-000000000002', 'ShipmentPublicationRule', '{"country":"CL","finalDestinationCode":"CLPUQ","finalDestinationName":"Punta Arenas","dischargePortCode":"CLSAI","description":"Carga con destino final Punta Arenas distribuida desde San Antonio: se publica con DIFU asociado al destino final.","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('4580513a-30ca-9b89-9170-3d00fa4974a5', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '9a6bc66a-dfa0-63b8-a053-f9d11bdf40c9', 'KnowledgeArticle', '{"country":"BO","topic":"PAYMENTS","title":"\u00BFC\u00F3mo pago con dep\u00F3sito o transferencia en Bolivia?","content":"En el carro elija Dep\u00F3sito o transferencia bancaria y emita la boleta. Realice el dep\u00F3sito o la transferencia por el monto indicado; Finanzas confirma el abono y el pago queda confirmado en el historial de pagos.","keywords":"deposito, transferencia, boleta, pago, bolivianos","sortOrder":22,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('4cf6e84a-b709-eb08-ebb4-fd0ad3591bc5', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'd1273909-b058-56f3-cd79-8b966b84fe8b', 'KnowledgeArticle', '{"country":"CL","topic":"DOCUMENTATION","title":"\u00BFD\u00F3nde consulto el estado del TATC?","content":"En el detalle de un BL de importaci\u00F3n, la secci\u00F3n TATC muestra el estado vigente de cada contenedor seg\u00FAn el sistema de TATC (sin emitir, pre-TATC, emitido o anulado) y los motivos pendientes, como pagos o documentos. Los clientes con alto volumen en una misma localidad pueden solicitar la generaci\u00F3n masiva de TATC.","keywords":"tatc, retiro, contenedor, generacion masiva","sortOrder":24,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5c8b4814-fc1f-ecfb-c955-ba36a27021dd', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '0cf519fe-d8a2-241f-740d-011e9f6eec9d', 'KnowledgeArticle', '{"country":"BO","topic":"GENERAL","title":"\u00BFQu\u00E9 es el NIT y por qu\u00E9 lo necesito?","content":"El NIT (N\u00FAmero de Identificaci\u00F3n Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisi\u00F3n de documentos fiscales.","keywords":null,"sortOrder":4,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('860c6994-8ce6-781b-0381-b1a608ddf07a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '5dfa074c-9654-883f-68a8-2dafe0784949', 'KnowledgeArticle', '{"country":"BO","topic":"PAYMENTS","title":"\u00BFCu\u00E1l es la tasa de IVA aplicada en Bolivia?","content":"La tasa de IVA vigente en Bolivia es del 13%. Se aplica autom\u00E1ticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).","keywords":null,"sortOrder":3,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('8f53822a-0dbd-8b5f-ca47-f7f1bcfd33f8', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'b305d4b2-1f96-e2ba-6fbf-3306a86c0f69', 'AssistantMailbox', '{"country":"CL","topic":"GENERAL","email":"clservice@hapag-lloyd.com","notes":"Casilla de Customer Service publicada en la FAQ del portal.","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('912a37d4-1d25-ba26-c523-601cfbb80a4e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'a5db2fd7-be42-da7c-64c0-510c8a1da34b', 'KnowledgeArticle', '{"country":"CL","topic":"DOCUMENTATION","title":"\u00BFC\u00F3mo solicito una copia del BL?","content":"En Documentos del embarque puede solicitar la copia del BL valorada (con fletes y cargos) o no valorada (sin valores comerciales). La copia se publica en el repositorio del embarque y se env\u00EDa al correo registrado de su organizaci\u00F3n. El shipper solo accede a la copia no valorada.","keywords":"copia, copia de bl, valorada, no valorada, documento","sortOrder":21,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('9bfaef86-18ff-7fb5-d963-eb8684f03928', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '5a6f0183-6ea9-4ca5-4022-a73b23a80b89', 'KnowledgeArticle', '{"country":"BO","topic":"DOCUMENTATION","title":"\u00BFC\u00F3mo obtengo el certificado de libre deuda (CLD)?","content":"En Documentos del embarque de un BL de importaci\u00F3n de Bolivia consulte si el CLD est\u00E1 disponible. Se emite firmado cuando no hay recargos, demurrage, facturas, flete Collect ni demoras anticipadas pendientes; si algo falta, el portal indica qu\u00E9 bloquea la emisi\u00F3n.","keywords":"cld, libre deuda, certificado, bolivia","sortOrder":20,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a00f8b4a-8c81-185e-bc3d-d225ff93bc4a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '96bd2015-d692-1d1d-3b1d-e217f757b716', 'KnowledgeArticle', '{"country":"CL","topic":"SHIPPING","title":"\u00BFC\u00F3mo puedo consultar el estado de mi BL?","content":"Ingrese al m\u00F3dulo \u0027Bills of Lading\u0027, escriba su n\u00FAmero de BL en el buscador y presione buscar. Ver\u00E1 el detalle completo incluyendo contenedores, cargos locales y demurrage.","keywords":null,"sortOrder":1,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('aaddddcc-3132-82dd-9038-fd2f1f73f65e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '4e576670-dc83-d46f-b8df-48b2cce4003e', 'KnowledgeArticle', '{"country":"CL","topic":"SHIPPING","title":"\u00BFC\u00F3mo funciona el cambio de almac\u00E9n?","content":"Desde el detalle del BL o la secci\u00F3n Cambio de almac\u00E9n puede solicitar el cambio para el BL completo o para un contenedor. Si su cuenta tiene derecho a un cambio gratuito (condici\u00F3n informada por Nexus o regla del portal), la solicitud queda completada sin costo. Si no, se aplica la tarifa vigente (KTE o KTF) y el cargo queda listo para pagarlo en el carro. Para varios BL use la solicitud masiva y consulte su avance en la misma secci\u00F3n.","keywords":"almacen, cambio de almacen, bodega, KTE, KTF, deposito","sortOrder":20,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('ab821fdb-1437-7a98-7607-bdbc1c31a414', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '501c69c3-4255-3394-8838-8a916d18bfff', 'AssistantMailbox', '{"country":"BO","topic":"GENERAL","email":"boservice@hapag-lloyd.com","notes":"Casilla por validar con Customer Service Bolivia antes de producci\u00F3n.","isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('b0fe0baa-0238-5953-3336-77be180946a0', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'f569fc25-d919-0336-5d34-38d94c4bfa4f', 'KnowledgeArticle', '{"country":"CL","topic":"GENERAL","title":"\u00BFC\u00F3mo registro mi empresa en el portal?","content":"Haga clic en \u0027Registrarse\u0027, seleccione su pa\u00EDs (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contrase\u00F1a. Recibir\u00E1 un correo de confirmaci\u00F3n.","keywords":null,"sortOrder":10,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('d4f3fa2e-f62e-3b96-5a26-87f96369f490', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '56ae180b-f99c-6a17-1eea-6e0c979bec7f', 'KnowledgeArticle', '{"country":"BO","topic":"DEMURRAGE","title":"\u00BFQu\u00E9 son las demoras anticipadas?","content":"Algunas cuentas de Bolivia deben pagar demoras anticipadas por contenedor antes de emitir el CLD. El portal las informa en la pesta\u00F1a de demurrage del BL; al pagarlas, el monto se descuenta del MHD y deja de bloquear el CLD.","keywords":"demoras anticipadas, adelanto, demurrage, mhd","sortOrder":21,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('dcaab0be-6d77-28fb-fc18-80d79570ad41', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '1d1add93-0f39-af1b-5416-c59e1ae23e74', 'KnowledgeArticle', '{"country":"CL","topic":"GENERAL","title":"\u00BFC\u00F3mo doy acceso a mi agencia de aduanas u otro tercero?","content":"En Accesos de terceros, el administrador de su organizaci\u00F3n puede otorgar acceso a un BL o booking, de forma individual o masiva, con vigencia y permisos definidos, y revocarlo cuando quiera. Tambi\u00E9n puede configurar terceros por defecto para los BL nuevos. Todos los cambios quedan registrados en la auditor\u00EDa.","keywords":"acceso, terceros, agencia, mandato, otorgar, revocar","sortOrder":26,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e461c099-2caf-9f58-a814-94f9e2ad1730', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '715a32fa-66b0-77ea-386c-de7cd891089f', 'KnowledgeArticle', '{"country":"CL","topic":"PAYMENTS","title":"\u00BFQu\u00E9 m\u00E9todos de pago est\u00E1n disponibles en Chile?","content":"En Chile puede pagar con Tarjeta de Cr\u00E9dito, Tarjeta de D\u00E9bito, Transferencia Bancaria y WebPay. Todos los pagos electr\u00F3nicos se procesan en tiempo real.","keywords":null,"sortOrder":2,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('ed0a9d06-e01c-dec7-ee9d-1d2e72d2cadb', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'a43ec294-e615-b557-7514-1d74754dc32b', 'KnowledgeArticle', '{"country":"CL","topic":"DOCUMENTATION","title":"\u00BFC\u00F3mo genero un recibo de pago?","content":"Una vez confirmado el pago, vaya al detalle del pago y presione \u0027Generar Recibo\u0027. El recibo se genera autom\u00E1ticamente en formato PDF con todos los datos fiscales.","keywords":null,"sortOrder":5,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('f3f5b5e0-556e-b2be-b555-bf4f22b80c12', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '1d9a519c-78d6-5e51-dff3-20d2aeeea4ab', 'KnowledgeArticle', '{"country":"CL","topic":"PAYMENTS","title":"\u00BFC\u00F3mo pago mis servicios en el carro?","content":"Agregue al carro los cargos pendientes desde el detalle del BL, la pesta\u00F1a de demurrage o sus facturas, indicando el RUT de facturaci\u00F3n. El carro agrupa los \u00EDtems por pa\u00EDs y moneda de pago; cada grupo se paga por separado con los medios habilitados, por ejemplo Khipu, bot\u00F3n de pago bancario o dep\u00F3sito con boleta.","keywords":"carro, pago, pagar, khipu, deposito, boleta, moneda, rut de facturacion","sortOrder":25,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('f5691cee-26c9-ad8d-2ade-9c7e2eec8999', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'a7624e2a-707b-e991-f46a-7e54449dd785', 'KnowledgeArticle', '{"country":"BO","topic":"GENERAL","title":"\u00BFC\u00F3mo doy acceso a mi agencia de aduanas u otro tercero?","content":"En Accesos de terceros, el administrador de su organizaci\u00F3n puede otorgar acceso a un BL o booking, con vigencia y permisos definidos, y revocarlo cuando quiera. Todos los cambios quedan registrados en la auditor\u00EDa.","keywords":"acceso, terceros, agencia, mandato, otorgar, revocar","sortOrder":24,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('f8a7cc50-78dc-f8e9-dfed-fdb8d86560b2', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '3746d3f8-f379-04cc-942e-236c3c248994', 'KnowledgeArticle', '{"country":"BO","topic":"DEMURRAGE","title":"\u00BFC\u00F3mo funciona el demurrage para carga en tr\u00E1nsito a Bolivia?","content":"El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los d\u00EDas libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a trav\u00E9s del m\u00F3dulo de Demurrage.","keywords":null,"sortOrder":5,"isActive":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "ShipmentPublicationRules" ("Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DischargePortCode", "FinalDestinationCode", "FinalDestinationName", "IsActive", "ModifiedAt", "ModifiedBy")
    VALUES ('ffffffff-0019-0019-0019-000000000001', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'Carga con destino final Antofagasta distribuida desde San Antonio: se publica con DIFU asociado al destino final.', 'CLSAI', 'CLANF', 'Antofagasta', TRUE, NULL, NULL);
    INSERT INTO "ShipmentPublicationRules" ("Id", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DischargePortCode", "FinalDestinationCode", "FinalDestinationName", "IsActive", "ModifiedAt", "ModifiedBy")
    VALUES ('ffffffff-0019-0019-0019-000000000002', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'Carga con destino final Punta Arenas distribuida desde San Antonio: se publica con DIFU asociado al destino final.', 'CLSAI', 'CLPUQ', 'Punta Arenas', TRUE, NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000017', '11111111-0007-0007-0007-000000000014', 'HLXU3046001', '40HC', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-046001', 'Discharged', NULL, NULL, 26300.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000018', '11111111-0007-0007-0007-000000000015', 'HLXU3046002', '40RF', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-046002', 'Discharged', NULL, NULL, 24800.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000026', 210000.0, '11111111-0007-0007-0007-000000000015', 'THC', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Terminal Handling Charge - 40RF (San Antonio)', TRUE, NULL, NULL, 'Pending', 39900.0, 19.0, 249900.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('35804351-fee3-10ce-ed3a-782a1346abcb', '11111111-0007-0007-0007-000000000014', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('3fd9b388-e81b-e6b6-87b4-17bb9996ac3b', '11111111-0007-0007-0007-000000000015', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_BillsOfLading_Country_FinalDestinationCode" ON "BillsOfLading" ("Country", "FinalDestinationCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE UNIQUE INDEX "IX_AssistantMailboxes_Country_Topic" ON "AssistantMailboxes" ("Country", "Topic");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_AssistantMessages_Role_CreatedAt" ON "AssistantMessages" ("Role", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE UNIQUE INDEX "IX_AssistantMessages_SessionId_Sequence" ON "AssistantMessages" ("SessionId", "Sequence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_AssistantSessions_UserId_StartedAt" ON "AssistantSessions" ("UserId", "StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_DangerousGoods_UnNumber" ON "DangerousGoods" ("UnNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_KnowledgeArticles_Country_IsActive_Topic" ON "KnowledgeArticles" ("Country", "IsActive", "Topic");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_ShipmentPublicationRules_Country_FinalDestinationCode_IsAct~" ON "ShipmentPublicationRules" ("Country", "FinalDestinationCode", "IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_TatcBatches_ClientId_CreatedAt" ON "TatcBatches" ("ClientId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    CREATE INDEX "IX_TatcBatchItems_BatchId_LineNumber" ON "TatcBatchItems" ("BatchId", "LineNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006055657_AddPortalAssistantAndPublication') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006055657_AddPortalAssistantAndPublication', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE TABLE "ServiceDefinitions" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "NameEs" character varying(150) NOT NULL,
        "NameEn" character varying(150) NOT NULL,
        "DescriptionEs" character varying(1000),
        "DescriptionEn" character varying(1000),
        "Operations" character varying(20) NOT NULL,
        "Countries" character varying(20) NOT NULL,
        "ReferenceType" character varying(20) NOT NULL,
        "RequiredBlStatuses" character varying(300),
        "AvailabilityWindow" character varying(30) NOT NULL,
        "RequiresContainers" boolean NOT NULL,
        "AllowMultiplePerBl" boolean NOT NULL,
        "InputSchemaJson" text NOT NULL,
        "BillingDataRequired" boolean NOT NULL,
        "TariffAcceptanceRequired" boolean NOT NULL,
        "PricingMode" character varying(20) NOT NULL,
        "ChargeConceptCode" character varying(50),
        "TariffCode" character varying(20),
        "LateTariffCode" character varying(20),
        "QuantityMode" character varying(20) NOT NULL,
        "MeasureFieldKey" character varying(40),
        "Milestone" character varying(30) NOT NULL,
        "MilestoneOffsetHours" integer NOT NULL,
        "DeadlineRuleCode" character varying(50),
        "TimingRule" character varying(20) NOT NULL,
        "Taxable" boolean NOT NULL,
        "ExemptionConcept" character varying(50),
        "ExcludeShipperOwnedContainers" boolean NOT NULL,
        "ApprovalTeam" character varying(30) NOT NULL,
        "FulfillmentTeam" character varying(30) NOT NULL,
        "RequiresOutputDocument" boolean NOT NULL,
        "ActionCode" character varying(60) NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ServiceDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE TABLE "ServiceRequests" (
        "Id" uuid NOT NULL,
        "RequestNumber" character varying(30) NOT NULL,
        "DefinitionId" uuid NOT NULL,
        "DefinitionCode" character varying(50) NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "RequestedByUserId" uuid,
        "RequestedByEmail" character varying(256),
        "OnBehalfOfClientId" uuid,
        "AccessGrantId" uuid,
        "BillOfLadingId" uuid NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "BookingNumber" character varying(50),
        "Country" character varying(5) NOT NULL,
        "Operation" character varying(10) NOT NULL,
        "ContainerNumbers" character varying(2000),
        "InputValuesJson" text NOT NULL,
        "BillingTaxId" character varying(20),
        "BillingName" character varying(200),
        "BillingAddress" character varying(300),
        "BillingEmail" character varying(256),
        "BillingActivity" character varying(200),
        "ChargeConceptCode" character varying(50),
        "TariffId" uuid,
        "TariffCode" character varying(20),
        "TariffSource" character varying(20),
        "TierUnit" character varying(20),
        "MeasuredUnits" integer,
        "Quantity" integer NOT NULL,
        "Timing" character varying(20),
        "MilestoneAt" timestamp with time zone,
        "MilestoneSource" character varying(30),
        "Amount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "TotalAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(5),
        "PricingDetailJson" text,
        "QuotedAt" timestamp with time zone,
        "TariffAcceptedAt" timestamp with time zone,
        "IsExempt" boolean NOT NULL,
        "ExemptionReference" character varying(200),
        "Status" character varying(30) NOT NULL,
        "StatusChangedAt" timestamp with time zone NOT NULL,
        "AssignedTeam" character varying(30),
        "SubmittedAt" timestamp with time zone,
        "ApprovedAt" timestamp with time zone,
        "RejectedAt" timestamp with time zone,
        "PaidAt" timestamp with time zone,
        "PaymentId" uuid,
        "CompletedAt" timestamp with time zone,
        "CancelledAt" timestamp with time zone,
        "ResolutionNotes" character varying(1000),
        "TimelineSequence" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ServiceRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceRequests_ServiceDefinitions_DefinitionId" FOREIGN KEY ("DefinitionId") REFERENCES "ServiceDefinitions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE TABLE "ServiceRequestAttachments" (
        "Id" uuid NOT NULL,
        "ServiceRequestId" uuid NOT NULL,
        "FieldKey" character varying(40) NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "StorageKey" character varying(300) NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "UploadedByUserId" uuid,
        "UploadedBy" character varying(256) NOT NULL,
        CONSTRAINT "PK_ServiceRequestAttachments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceRequestAttachments_ServiceRequests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES "ServiceRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE TABLE "ServiceRequestCharges" (
        "Id" uuid NOT NULL,
        "ServiceRequestId" uuid NOT NULL,
        "LocalChargeId" uuid NOT NULL,
        "Generated" boolean NOT NULL,
        CONSTRAINT "PK_ServiceRequestCharges" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceRequestCharges_ServiceRequests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES "ServiceRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE TABLE "ServiceRequestEvents" (
        "Id" uuid NOT NULL,
        "ServiceRequestId" uuid NOT NULL,
        "Sequence" integer NOT NULL,
        "FromStatus" character varying(30),
        "ToStatus" character varying(30) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "ActorUserId" uuid,
        "ActorName" character varying(256) NOT NULL,
        "ActorKind" character varying(20) NOT NULL,
        "Notes" character varying(1000),
        CONSTRAINT "PK_ServiceRequestEvents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ServiceRequestEvents_ServiceRequests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES "ServiceRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000022', '11111111-0007-0007-0007-000000000005', 'HLXU5566779', '20DV', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, TRUE, NULL, NULL, NULL, NULL, 'SL-015679', 'Discharged', NULL, NULL, 14900.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000016', 'HLCUSAI260901610', NULL, 'HLCUBKG2609161', 'c3d4e5f6-0003-0003-0003-000000000010', 'Lima Foods SAC', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-10-10T00:00:00Z', TIMESTAMPTZ '2026-09-28T00:00:00Z', NULL, 'PELIM', 2100.0, 'USD', NULL, NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-09-28T06:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Lima, Peru', 'Callao (PECLL)', 'PECLL', 'San Antonio (CLSAI)', 'Export', 'Importadora Demo SpA', 'Departed', 'BL', 'Cartagena Express', NULL, '2609S');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000017', 'HLCUARI260901720', NULL, 'HLCUBKG2609172', 'c3d4e5f6-0003-0003-0003-000000000020', 'Andes Foods SAC', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-10-07T00:00:00Z', TIMESTAMPTZ '2026-09-30T00:00:00Z', NULL, 'PELIM', 1300.0, 'USD', NULL, NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-09-30T08:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Lima, Peru', 'Callao (PECLL)', 'PECLL', 'Arica (CLARI)', 'Export', 'Comercial Altiplano SRL', 'Departed', 'SWB', 'Antofagasta Express', NULL, '2609S');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('04860673-2222-59b2-d04c-6a971962465b', 'Service', 'BL_HOUSE_TRANSMISSION', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 260, TRUE, NULL, NULL, 'Transmisión de BL hijo', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('14a10063-2e24-3dad-0f58-d91f07276e71', 'Service', 'MATRIX_LATE', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 270, TRUE, NULL, NULL, 'Matriz fuera de plazo', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('2ba2ee40-e9b4-20e6-5f43-3597e9b1e3e4', 'Service', 'XOM', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 240, TRUE, NULL, NULL, 'Administración de contenedor (XOM)', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('349644e9-cc16-f243-5929-b308f01814d7', 'Service', 'SEAL_MANAGEMENT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 210, TRUE, NULL, NULL, 'Gestión de sellos', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('7cb784b5-5363-d794-f9b9-ba72edd30a69', 'Service', 'DROP_OFF', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 230, TRUE, NULL, NULL, 'Drop Off (devolución en SCL)', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('95cfe440-ad17-116d-e382-d8d4925a8450', 'LocalCharge', 'VALUATION', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 290, TRUE, NULL, NULL, 'Valorización', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('da68ecd8-b854-1daa-fea9-3c7e06e83e30', 'Service', 'EARLY_ARRIVAL', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 220, TRUE, NULL, NULL, 'Early (ingreso anticipado)', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('f27c3d60-1e67-07f7-b8f6-204b0f483bd5', 'LocalCharge', 'OPENING', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 280, TRUE, NULL, NULL, 'Apertura', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('fc9e7715-ccf9-3b6a-4c06-744aea94758d', 'Service', 'BL_CORRECTION', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 250, TRUE, NULL, NULL, 'Corrección o aclaración de BL', FALSE, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ExchangeRateRecords" ("Id", "Approved", "CapturedAt", "ConvertedAmount", "EffectiveDate", "FromCurrency", "Rate", "Source", "SourceAmount", "ToCurrency", "TransactionId", "TransactionType")
    VALUES ('ff504a00-3987-a4bb-a953-ee9fd8f4765c', TRUE, TIMESTAMPTZ '2026-10-02T14:58:00Z', 114000.0, DATE '2026-10-02', 'USD', 950.0, 'DUMMY', 120.0, 'CLP', '55555555-000b-000b-000b-000000000014', 'Payment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000027', 12000.0, '11111111-0007-0007-0007-000000000006', 'SEAL_MANAGEMENT', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gestión de sellos (SRV-20261005-5E1A0002)', TRUE, NULL, NULL, 'Pending', 2280.0, 19.0, 14280.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000028', 45000.0, '11111111-0007-0007-0007-000000000002', 'BL_CORRECTION', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Corrección o aclaración de BL (SRV-20261001-5E1A0003)', TRUE, NULL, NULL, 'Paid', 8550.0, 19.0, 53550.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('0c84def6-4017-1cc8-5cb3-bc6949b15e84', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '23d06884-598b-bc4f-1cef-a28759ac8f70', 'ServiceDefinition', '{"code":"EARLY_ARRIVAL","nameEs":"Early","nameEn":"Early arrival","descriptionEs":"Ingreso anticipado de unidades antes de la apertura del stacking; tarifa por tramos de d\u00EDas; se presta tras el pago (M3-08).","descriptionEn":"Container delivery before the stacking opens; tiered by days early; provided after payment (M3-08).","operations":"EXPORT","countries":"CL","referenceType":"Booking","requiredBlStatuses":null,"availabilityWindow":"BeforeDeparture","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022days\u0022,\u0022labelEs\u0022:\u0022D\\u00EDas de anticipaci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Days early\u0022,\u0022type\u0022:\u0022number\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:1,\u0022max\u0022:30,\u0022integer\u0022:true,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"EARLY_ARRIVAL","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":"days","milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"CustomerService","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":30,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('0fb183cc-89fb-a728-538a-341c8a1ba5cc', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '5d3115fa-f98b-ad3c-43e1-390bdb3261c3', 'ServiceDefinition', '{"code":"VALUATION","nameEs":"Valorizaci\u00F3n","nameEn":"Valuation","descriptionEs":"Homologaci\u00F3n de CL-IMP-06 con el modelo est\u00E1ndar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.","descriptionEn":"CL-IMP-06 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.","operations":"IMPORT","countries":"CL","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":false,"inputSchemaJson":"[]","billingDataRequired":false,"tariffAcceptanceRequired":false,"pricingMode":"SourceCharge","chargeConceptCode":"VALUATION","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-mandatory.pay","displayOrder":110,"isActive":false}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('25242d34-dba2-fbd8-3cc7-a482b769d040', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '85efb04f-05d5-2318-3bbb-1149a9593e99', 'Tariff', '{"conceptCode":"MATRIX_LATE","code":null,"country":"CL","currency":"USD","containerType":null,"description":"Matriz fuera de plazo, d\u00EDas desde el plazo de presentaci\u00F3n (M3-14)","amount":0,"tierUnit":"CalendarDays","tierMode":"Flat","tiers":[{"fromUnit":0,"toUnit":1,"amount":50},{"fromUnit":2,"toUnit":5,"amount":100},{"fromUnit":6,"toUnit":null,"amount":200}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('2db0b575-0067-0cba-ac87-53cb2963e9e5', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '09121ab9-02ec-4885-dda1-1cc481c6ddd3', 'Tariff', '{"conceptCode":"BL_HOUSE_TRANSMISSION","code":"PLAZO","country":"CL","currency":"USD","containerType":null,"description":"Transmisi\u00F3n de BL hijo dentro de plazo (M3-13)","amount":35,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('42237288-ed5e-c85a-2efc-d27408e207f2', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '3f963e93-b130-ae94-6ee6-8426587d9197', 'Tariff', '{"conceptCode":"BL_CORRECTION","code":null,"country":"BO","currency":"BOB","containerType":null,"description":"Correcci\u00F3n o aclaraci\u00F3n de BL Bolivia (M3-12)","amount":300,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('431c7c6a-11ee-5094-96c8-8fc4cf1857e1', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '4c67c42c-973f-bad8-3b14-d35c9df6cc01', 'ServiceDefinition', '{"code":"GATE_IN_RETURN","nameEs":"Gate In por devoluci\u00F3n de unidades","nameEn":"Gate In for returned export units","descriptionEs":"El cliente selecciona el booking y ve sus unidades; el cargo es el Gate In registrado en el sistema de origen, con las exenciones de Nexus (M3-15, CL-EXP-07).","descriptionEn":"The customer selects the booking and sees its units; the charge is the Gate In registered in the source system, with Nexus exemptions (M3-15).","operations":"EXPORT","countries":"CL","referenceType":"Booking","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":true,"allowMultiplePerBl":false,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":false,"tariffAcceptanceRequired":false,"pricingMode":"SourceCharge","chargeConceptCode":"GATE_IN","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-mandatory.pay","displayOrder":90,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('46f9d240-dab8-66bd-d19d-aa9e21346450', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'b023bef2-0e70-2988-f28c-2da684ab098e', 'Tariff', '{"conceptCode":"BL_HOUSE_TRANSMISSION","code":"FUERA_PLAZO","country":"CL","currency":"USD","containerType":null,"description":"Transmisi\u00F3n de BL hijo fuera de plazo, horas desde el plazo (M3-13)","amount":0,"tierUnit":"Hours","tierMode":"Flat","tiers":[{"fromUnit":0,"toUnit":24,"amount":60},{"fromUnit":25,"toUnit":72,"amount":120},{"fromUnit":73,"toUnit":null,"amount":250}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('495338b3-b5bd-0ece-e4f1-2a53619a8bb1', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ea3a5a01-c4ee-76ca-9d51-f9e542075cc7', 'Tariff', '{"conceptCode":"XOM","code":null,"country":"BO","currency":"BOB","containerType":"40HC","description":"Administraci\u00F3n de contenedor XOM, 40\u0027 HC (M3-10)","amount":520,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('4e72a36d-56b5-10c6-c568-24c15dd2a17e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '8991b39b-f6dc-fa22-f2cf-cdaae3287f4b', 'ServiceDefinition', '{"code":"DROP_OFF_SCL","nameEs":"Drop Off SCL","nameEn":"Drop Off SCL","descriptionEs":"Devoluci\u00F3n de contenedores en Santiago: el cliente elige las unidades, acepta la tarifa y el equipo ED aprueba o rechaza (M3-09, CL-IMP-10).","descriptionEn":"Container return in Santiago: the customer selects the units, accepts the tariff and the ED team approves or rejects (M3-09).","operations":"IMPORT","countries":"CL","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"AfterArrival","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022returnDate\u0022,\u0022labelEs\u0022:\u0022Fecha de devoluci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Return date\u0022,\u0022type\u0022:\u0022date\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022depot\u0022,\u0022labelEs\u0022:\u0022Dep\\u00F3sito de devoluci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Return depot\u0022,\u0022type\u0022:\u0022select\u0022,\u0022required\u0022:true,\u0022options\u0022:[{\u0022value\u0022:\u0022SCL_PUDAHUEL\u0022,\u0022labelEs\u0022:\u0022Dep\\u00F3sito Pudahuel\u0022,\u0022labelEn\u0022:\u0022Pudahuel depot\u0022},{\u0022value\u0022:\u0022SCL_QUILICURA\u0022,\u0022labelEs\u0022:\u0022Dep\\u00F3sito Quilicura\u0022,\u0022labelEn\u0022:\u0022Quilicura depot\u0022},{\u0022value\u0022:\u0022SCL_SAN_BERNARDO\u0022,\u0022labelEs\u0022:\u0022Dep\\u00F3sito San Bernardo\u0022,\u0022labelEn\u0022:\u0022San Bernardo depot\u0022}],\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":true,"pricingMode":"Tariff","chargeConceptCode":"DROP_OFF","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerContainer","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"ED","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"drop-off.request","displayOrder":40,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('534111a5-d9fe-9b27-3302-c3faf5ac50c8', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '8f9cd335-f4f6-df32-36b6-94794d2906dc', 'Tariff', '{"conceptCode":"DROP_OFF","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Drop Off SCL por contenedor (M3-09)","amount":180000,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5b857144-a78e-010d-dee9-a7cd0dd2cbf4', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '08f6a148-a021-357f-11e0-9e263e3bc70e', 'Tariff', '{"conceptCode":"SEAL_MANAGEMENT","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Gesti\u00F3n de sellos por contenedor (M3-07)","amount":12000,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('619584fa-1d71-2fc2-9b1b-74cab244ee62', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '7eb0b617-bc72-a0a1-a533-93df4e8d1d6e', 'ServiceDefinition', '{"code":"LATE_ARRIVAL","nameEs":"Late Arrival","nameEn":"Late Arrival","descriptionEs":"Ingreso de unidades despu\u00E9s del cierre de recepci\u00F3n; tarifa por tramos de horas de atraso; se presta tras el pago (M3-08, CL-EXP-10).","descriptionEn":"Container delivery after the receiving cut-off; tiered by hours late; provided after payment (M3-08).","operations":"EXPORT","countries":"CL","referenceType":"Booking","requiredBlStatuses":null,"availabilityWindow":"BeforeDeparture","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022hours\u0022,\u0022labelEs\u0022:\u0022Horas de atraso respecto del cierre\u0022,\u0022labelEn\u0022:\u0022Hours after the cut-off\u0022,\u0022type\u0022:\u0022number\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:1,\u0022max\u0022:240,\u0022integer\u0022:true,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"LATE_ARRIVAL","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":"hours","milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"CustomerService","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":20,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('70c99437-5242-1670-7f8d-cc32be458aef', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '1f4a4958-c841-daef-756a-b040b2014e4a', 'ServiceDefinition', '{"code":"XOM","nameEs":"Administraci\u00F3n de contenedor (XOM)","nameEn":"Container administration (XOM)","descriptionEs":"Cargo por unidad en bolivianos para la operaci\u00F3n de Bolivia; no se cobra con una excepci\u00F3n vigente en Nexus (XOM) ni a las unidades del embarcador (SOC) (M3-10, BO-EXP-02, BO-IMP-04).","descriptionEn":"Per-unit charge in bolivianos for the Bolivia operation; waived by a Nexus exception (XOM) and for shipper-owned units (M3-10).","operations":"IMPORT,EXPORT","countries":"BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"XOM","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerContainer","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":false,"exemptionConcept":"XOM","excludeShipperOwnedContainers":true,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":50,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('76574e01-7eec-b1c6-fd96-d86cae36ffac', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '62db1148-8639-1e56-909e-ec8eaaa7d035', 'ServiceDefinition', '{"code":"OPENING","nameEs":"Apertura","nameEn":"Opening","descriptionEs":"Homologaci\u00F3n de CL-IMP-05 con el modelo est\u00E1ndar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.","descriptionEn":"CL-IMP-05 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.","operations":"IMPORT","countries":"CL","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":false,"inputSchemaJson":"[]","billingDataRequired":false,"tariffAcceptanceRequired":false,"pricingMode":"SourceCharge","chargeConceptCode":"OPENING","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-mandatory.pay","displayOrder":100,"isActive":false}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('7ddf473a-2e3b-af3d-0a5c-e2f912d005cf', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '4f0226d1-eabb-3e9f-6355-41589b2ceb4a', 'Tariff', '{"conceptCode":"BL_CORRECTION","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Correcci\u00F3n o aclaraci\u00F3n de BL (M3-12)","amount":45000,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('84c4c7c9-2237-1f1c-fc66-3d44058448b1', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ed9325ae-cb8d-6b70-cfc6-3bfaeaff51e2', 'Tariff', '{"conceptCode":"BL_HOUSE_TRANSMISSION","code":"PLAZO","country":"BO","currency":"USD","containerType":null,"description":"Transmisi\u00F3n de BL hijo dentro de plazo (M3-13)","amount":35,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('959121c1-5b6a-a81b-f74c-33d4af71595b', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7', 'ServiceDefinition', '{"code":"MATRIX_LATE","nameEs":"Matriz fuera de plazo","nameEn":"Late matrix submission","descriptionEs":"Cobro por presentar la matriz despu\u00E9s del plazo (48 horas antes del zarpe mientras Nexus no informe los plazos documentales, M2-10); tarifa por tramos de d\u00EDas desde el plazo (M3-14).","descriptionEn":"Charge for submitting the matrix after the deadline (48 hours before departure until Nexus reports document deadlines, M2-10); tiered by days since the deadline (M3-14).","operations":"EXPORT","countries":"CL,BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":false,"inputSchemaJson":"[{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"MATRIX_LATE","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"VesselDeparture","milestoneOffsetHours":-48,"deadlineRuleCode":null,"timingRule":"LateOnly","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":80,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a1fbcd01-ec97-6f0b-e0dd-da2db2f77f15', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '6e03d8d0-8670-c1cf-46a4-5248d899b57e', 'ServiceDefinition', '{"code":"BL_HOUSE_TRANSMISSION","nameEs":"Transmisi\u00F3n de BL hijo","nameEn":"House BL transmission","descriptionEs":"Transmisi\u00F3n de BL hijo de exportaci\u00F3n dentro o fuera de plazo: el plazo es el aduanero del BL o de su manifiesto (BL_EMPTY_OUT) o, sin \u00E9l, el zarpe m\u00E1s 72 horas; fuera de plazo se cobra por tramos de horas (M3-13).","descriptionEn":"Export house BL transmission in time or late: the deadline is the customs one of the BL or its manifest (BL_EMPTY_OUT) or, without it, departure plus 72 hours; late transmissions are tiered by hours (M3-13).","operations":"EXPORT","countries":"CL,BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"AfterDeparture","requiresContainers":false,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022houseBlNumbers\u0022,\u0022labelEs\u0022:\u0022N\\u00FAmeros de BL hijo\u0022,\u0022labelEn\u0022:\u0022House BL numbers\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:500,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"BL_HOUSE_TRANSMISSION","tariffCode":"PLAZO","lateTariffCode":"FUERA_PLAZO","quantityMode":"PerRequest","measureFieldKey":null,"milestone":"CustomsDeadline","milestoneOffsetHours":72,"deadlineRuleCode":"BL_EMPTY_OUT","timingRule":"InTimeAndLate","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"CustomerService","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":70,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a6924dd1-4d90-21a1-5a01-037e1f908345', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '37f4f7a0-8da4-c832-bb22-7d2009de5dad', 'ServiceDefinition', '{"code":"BL_CORRECTION","nameEs":"Correcci\u00F3n o aclaraci\u00F3n de BL","nameEn":"BL correction or clarification","descriptionEs":"Solicitud y pago de correcciones o aclaraciones del BL, con seguimiento de su estado; Customer Service la atiende tras el pago (M3-12).","descriptionEn":"Request and payment of BL corrections or clarifications, with status tracking; handled by Customer Service after payment (M3-12).","operations":"IMPORT,EXPORT","countries":"CL,BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022requestType\u0022,\u0022labelEs\u0022:\u0022Tipo de solicitud\u0022,\u0022labelEn\u0022:\u0022Request type\u0022,\u0022type\u0022:\u0022select\u0022,\u0022required\u0022:true,\u0022options\u0022:[{\u0022value\u0022:\u0022CORRECTION\u0022,\u0022labelEs\u0022:\u0022Correcci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Correction\u0022},{\u0022value\u0022:\u0022CLARIFICATION\u0022,\u0022labelEs\u0022:\u0022Aclaraci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Clarification\u0022}],\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022section\u0022,\u0022labelEs\u0022:\u0022Secci\\u00F3n del BL\u0022,\u0022labelEn\u0022:\u0022BL section\u0022,\u0022type\u0022:\u0022select\u0022,\u0022required\u0022:true,\u0022options\u0022:[{\u0022value\u0022:\u0022SHIPPER\u0022,\u0022labelEs\u0022:\u0022Embarcador\u0022,\u0022labelEn\u0022:\u0022Shipper\u0022},{\u0022value\u0022:\u0022CONSIGNEE\u0022,\u0022labelEs\u0022:\u0022Consignatario\u0022,\u0022labelEn\u0022:\u0022Consignee\u0022},{\u0022value\u0022:\u0022NOTIFY\u0022,\u0022labelEs\u0022:\u0022Notificar a\u0022,\u0022labelEn\u0022:\u0022Notify party\u0022},{\u0022value\u0022:\u0022CARGO\u0022,\u0022labelEs\u0022:\u0022Descripci\\u00F3n de la carga\u0022,\u0022labelEn\u0022:\u0022Cargo description\u0022},{\u0022value\u0022:\u0022CONTAINERS\u0022,\u0022labelEs\u0022:\u0022Contenedores y sellos\u0022,\u0022labelEn\u0022:\u0022Containers and seals\u0022},{\u0022value\u0022:\u0022FREIGHT\u0022,\u0022labelEs\u0022:\u0022Flete\u0022,\u0022labelEn\u0022:\u0022Freight\u0022},{\u0022value\u0022:\u0022OTHER\u0022,\u0022labelEs\u0022:\u0022Otra\u0022,\u0022labelEn\u0022:\u0022Other\u0022}],\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022description\u0022,\u0022labelEs\u0022:\u0022Detalle de la correcci\\u00F3n o aclaraci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Correction or clarification details\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:2000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022supportingDocument\u0022,\u0022labelEs\u0022:\u0022Documento de respaldo\u0022,\u0022labelEn\u0022:\u0022Supporting document\u0022,\u0022type\u0022:\u0022file\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"BL_CORRECTION","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"CustomerService","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":60,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('c5544fec-42c2-acc4-7ada-c458f513372e', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'f69d95b8-610f-9e67-bb63-836e8e6ba3d2', 'Tariff', '{"conceptCode":"MATRIX_LATE","code":null,"country":"BO","currency":"USD","containerType":null,"description":"Matriz fuera de plazo, d\u00EDas desde el plazo de presentaci\u00F3n (M3-14)","amount":0,"tierUnit":"CalendarDays","tierMode":"Flat","tiers":[{"fromUnit":0,"toUnit":1,"amount":50},{"fromUnit":2,"toUnit":5,"amount":100},{"fromUnit":6,"toUnit":null,"amount":200}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('d880e37a-b4e7-4968-d22c-7722998ad7d2', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'f383edf7-a9c4-fff5-2c56-78c53ce50bc1', 'Tariff', '{"conceptCode":"XOM","code":null,"country":"BO","currency":"BOB","containerType":null,"description":"Administraci\u00F3n de contenedor XOM por unidad (M3-10)","amount":350,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e2b446b6-0654-1934-f54a-a38e2ebd598a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '14c8c000-da8f-ba14-3e54-52df0949ac66', 'Tariff', '{"conceptCode":"BL_HOUSE_TRANSMISSION","code":"FUERA_PLAZO","country":"BO","currency":"USD","containerType":null,"description":"Transmisi\u00F3n de BL hijo fuera de plazo, horas desde el plazo (M3-13)","amount":0,"tierUnit":"Hours","tierMode":"Flat","tiers":[{"fromUnit":0,"toUnit":24,"amount":60},{"fromUnit":25,"toUnit":72,"amount":120},{"fromUnit":73,"toUnit":null,"amount":250}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e7efc002-82db-179e-b1f1-ab3f8d91e501', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '7e645de7-824d-4876-2e7e-d433d76e1f92', 'Tariff', '{"conceptCode":"EARLY_ARRIVAL","code":null,"country":"CL","currency":"USD","containerType":null,"description":"Early por d\u00EDas de anticipaci\u00F3n (M3-08)","amount":0,"tierUnit":"CalendarDays","tierMode":"Flat","tiers":[{"fromUnit":1,"toUnit":2,"amount":80},{"fromUnit":3,"toUnit":5,"amount":150},{"fromUnit":6,"toUnit":null,"amount":250}],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e8703b3e-1773-0ee3-ef72-9f591e2ae550', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'b75305ad-33dc-5c3e-6831-1b8aedfc9756', 'ServiceDefinition', '{"code":"SEAL_MANAGEMENT","nameEs":"Gesti\u00F3n de sellos","nameEn":"Seal management","descriptionEs":"Ingreso de los datos de sellos del booking; se presta tras el pago (M3-07, CL-EXP-09).","descriptionEn":"Seal data entry for the booking; provided after payment (M3-07).","operations":"EXPORT","countries":"CL","referenceType":"Booking","requiredBlStatuses":null,"availabilityWindow":"BeforeDeparture","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022sealNumbers\u0022,\u0022labelEs\u0022:\u0022N\\u00FAmeros de sello\u0022,\u0022labelEn\u0022:\u0022Seal numbers\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:500,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":false,"pricingMode":"Tariff","chargeConceptCode":"SEAL_MANAGEMENT","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerContainer","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"CustomerService","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":10,"isActive":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000014', NULL, 168940.0, NULL, NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-02T15:00:00Z', 'KHIPU_WEBHOOK', 'CL', TIMESTAMPTZ '2026-10-02T14:58:00Z', 'd4e5f6a7-0004-0004-0004-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20261002-6F5E4D3C', NULL, NULL, NULL, NULL, NULL, 'Cart', 'Importadora Demo SpA', '76123456-7', TIMESTAMPTZ '2026-10-02T15:00:00Z', 'KHIPU', 'KHIPU', 'PAY-20261002-6F5E4D3C', 'Cart', 'Khipu', 'DUMMY-KHIPU-PAY-20261002-6F5E4D3C', 'KHP-TXN-8813377', 'RCP-20261002-7A8B9C0D', NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-10-02T15:00:00Z', 8550.0, 177490.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('61285041-197b-46ea-7ea3-57f2c557eba3', 'service-requests.process', 'service-requests.process');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('1f4a4958-c841-daef-756a-b040b2014e4a', 'local-charges-on-demand.pay', TRUE, 'None', 'Always', TRUE, 'XOM', 'XOM', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Per-unit charge in bolivianos for the Bolivia operation; waived by a Nexus exception (XOM) and for shipper-owned units (M3-10).', 'Cargo por unidad en bolivianos para la operación de Bolivia; no se cobra con una excepción vigente en Nexus (XOM) ni a las unidades del embarcador (SOC) (M3-10, BO-EXP-02, BO-IMP-04).', 50, TRUE, 'XOM', 'None', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Container administration (XOM)', 'Administración de contenedor (XOM)', 'IMPORT,EXPORT', 'Tariff', 'PerContainer', 'BL', NULL, TRUE, FALSE, FALSE, NULL, FALSE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('23d06884-598b-bc4f-1cef-a28759ac8f70', 'local-charges-on-demand.pay', TRUE, 'None', 'BeforeDeparture', TRUE, 'EARLY_ARRIVAL', 'EARLY_ARRIVAL', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Container delivery before the stacking opens; tiered by days early; provided after payment (M3-08).', 'Ingreso anticipado de unidades antes de la apertura del stacking; tarifa por tramos de días; se presta tras el pago (M3-08).', 30, FALSE, NULL, 'CustomerService', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"days","labelEs":"D\u00EDas de anticipaci\u00F3n","labelEn":"Days early","type":"number","required":true,"options":null,"min":1,"max":30,"integer":true,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, 'days', 'None', 0, NULL, NULL, 'Early arrival', 'Early', 'EXPORT', 'Tariff', 'PerRequest', 'Booking', NULL, TRUE, FALSE, FALSE, NULL, FALSE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('37f4f7a0-8da4-c832-bb22-7d2009de5dad', 'local-charges-on-demand.pay', TRUE, 'None', 'Always', TRUE, 'BL_CORRECTION', 'BL_CORRECTION', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Request and payment of BL corrections or clarifications, with status tracking; handled by Customer Service after payment (M3-12).', 'Solicitud y pago de correcciones o aclaraciones del BL, con seguimiento de su estado; Customer Service la atiende tras el pago (M3-12).', 60, FALSE, NULL, 'CustomerService', '[{"key":"requestType","labelEs":"Tipo de solicitud","labelEn":"Request type","type":"select","required":true,"options":[{"value":"CORRECTION","labelEs":"Correcci\u00F3n","labelEn":"Correction"},{"value":"CLARIFICATION","labelEs":"Aclaraci\u00F3n","labelEn":"Clarification"}],"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"section","labelEs":"Secci\u00F3n del BL","labelEn":"BL section","type":"select","required":true,"options":[{"value":"SHIPPER","labelEs":"Embarcador","labelEn":"Shipper"},{"value":"CONSIGNEE","labelEs":"Consignatario","labelEn":"Consignee"},{"value":"NOTIFY","labelEs":"Notificar a","labelEn":"Notify party"},{"value":"CARGO","labelEs":"Descripci\u00F3n de la carga","labelEn":"Cargo description"},{"value":"CONTAINERS","labelEs":"Contenedores y sellos","labelEn":"Containers and seals"},{"value":"FREIGHT","labelEs":"Flete","labelEn":"Freight"},{"value":"OTHER","labelEs":"Otra","labelEn":"Other"}],"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"description","labelEs":"Detalle de la correcci\u00F3n o aclaraci\u00F3n","labelEn":"Correction or clarification details","type":"textarea","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":2000,"helpEs":null,"helpEn":null},{"key":"supportingDocument","labelEs":"Documento de respaldo","labelEn":"Supporting document","type":"file","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'BL correction or clarification', 'Corrección o aclaración de BL', 'IMPORT,EXPORT', 'Tariff', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('4c67c42c-973f-bad8-3b14-d35c9df6cc01', 'local-charges-mandatory.pay', FALSE, 'None', 'Always', FALSE, 'GATE_IN', 'GATE_IN_RETURN', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'The customer selects the booking and sees its units; the charge is the Gate In registered in the source system, with Nexus exemptions (M3-15).', 'El cliente selecciona el booking y ve sus unidades; el cargo es el Gate In registrado en el sistema de origen, con las exenciones de Nexus (M3-15, CL-EXP-07).', 90, FALSE, NULL, 'None', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Gate In for returned export units', 'Gate In por devolución de unidades', 'EXPORT', 'SourceCharge', 'PerRequest', 'Booking', NULL, TRUE, FALSE, FALSE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('5d3115fa-f98b-ad3c-43e1-390bdb3261c3', 'local-charges-mandatory.pay', FALSE, 'None', 'Always', FALSE, 'VALUATION', 'VALUATION', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'CL-IMP-06 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.', 'Homologación de CL-IMP-06 con el modelo estándar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.', 110, FALSE, NULL, 'None', '[]', FALSE, NULL, NULL, 'None', 0, NULL, NULL, 'Valuation', 'Valorización', 'IMPORT', 'SourceCharge', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('62db1148-8639-1e56-909e-ec8eaaa7d035', 'local-charges-mandatory.pay', FALSE, 'None', 'Always', FALSE, 'OPENING', 'OPENING', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'CL-IMP-05 mapped to the standard model: source-system charge with Nexus rules. Inactive: currently paid as a local charge.', 'Homologación de CL-IMP-05 con el modelo estándar: cargo del sistema de origen con reglas de Nexus. Inactiva: hoy se paga como recargo local.', 100, FALSE, NULL, 'None', '[]', FALSE, NULL, NULL, 'None', 0, NULL, NULL, 'Opening', 'Apertura', 'IMPORT', 'SourceCharge', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('6e03d8d0-8670-c1cf-46a4-5248d899b57e', 'local-charges-on-demand.pay', TRUE, 'None', 'AfterDeparture', TRUE, 'BL_HOUSE_TRANSMISSION', 'BL_HOUSE_TRANSMISSION', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BL_EMPTY_OUT', NULL, NULL, 'Export house BL transmission in time or late: the deadline is the customs one of the BL or its manifest (BL_EMPTY_OUT) or, without it, departure plus 72 hours; late transmissions are tiered by hours (M3-13).', 'Transmisión de BL hijo de exportación dentro o fuera de plazo: el plazo es el aduanero del BL o de su manifiesto (BL_EMPTY_OUT) o, sin él, el zarpe más 72 horas; fuera de plazo se cobra por tramos de horas (M3-13).', 70, FALSE, NULL, 'CustomerService', '[{"key":"houseBlNumbers","labelEs":"N\u00FAmeros de BL hijo","labelEn":"House BL numbers","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":500,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, 'FUERA_PLAZO', NULL, 'CustomsDeadline', 72, NULL, NULL, 'House BL transmission', 'Transmisión de BL hijo', 'EXPORT', 'Tariff', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, 'PLAZO', FALSE, 'InTimeAndLate');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('7eb0b617-bc72-a0a1-a533-93df4e8d1d6e', 'local-charges-on-demand.pay', TRUE, 'None', 'BeforeDeparture', TRUE, 'LATE_ARRIVAL', 'LATE_ARRIVAL', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Container delivery after the receiving cut-off; tiered by hours late; provided after payment (M3-08).', 'Ingreso de unidades después del cierre de recepción; tarifa por tramos de horas de atraso; se presta tras el pago (M3-08, CL-EXP-10).', 20, FALSE, NULL, 'CustomerService', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"hours","labelEs":"Horas de atraso respecto del cierre","labelEn":"Hours after the cut-off","type":"number","required":true,"options":null,"min":1,"max":240,"integer":true,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, 'hours', 'None', 0, NULL, NULL, 'Late Arrival', 'Late Arrival', 'EXPORT', 'Tariff', 'PerRequest', 'Booking', NULL, TRUE, FALSE, FALSE, NULL, FALSE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('8991b39b-f6dc-fa22-f2cf-cdaae3287f4b', 'drop-off.request', TRUE, 'ED', 'AfterArrival', TRUE, 'DROP_OFF', 'DROP_OFF_SCL', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Container return in Santiago: the customer selects the units, accepts the tariff and the ED team approves or rejects (M3-09).', 'Devolución de contenedores en Santiago: el cliente elige las unidades, acepta la tarifa y el equipo ED aprueba o rechaza (M3-09, CL-IMP-10).', 40, FALSE, NULL, 'None', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"returnDate","labelEs":"Fecha de devoluci\u00F3n","labelEn":"Return date","type":"date","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"depot","labelEs":"Dep\u00F3sito de devoluci\u00F3n","labelEn":"Return depot","type":"select","required":true,"options":[{"value":"SCL_PUDAHUEL","labelEs":"Dep\u00F3sito Pudahuel","labelEn":"Pudahuel depot"},{"value":"SCL_QUILICURA","labelEs":"Dep\u00F3sito Quilicura","labelEn":"Quilicura depot"},{"value":"SCL_SAN_BERNARDO","labelEs":"Dep\u00F3sito San Bernardo","labelEn":"San Bernardo depot"}],"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Drop Off SCL', 'Drop Off SCL', 'IMPORT', 'Tariff', 'PerContainer', 'BL', NULL, TRUE, FALSE, TRUE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('b75305ad-33dc-5c3e-6831-1b8aedfc9756', 'local-charges-on-demand.pay', TRUE, 'None', 'BeforeDeparture', TRUE, 'SEAL_MANAGEMENT', 'SEAL_MANAGEMENT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Seal data entry for the booking; provided after payment (M3-07).', 'Ingreso de los datos de sellos del booking; se presta tras el pago (M3-07, CL-EXP-09).', 10, FALSE, NULL, 'CustomerService', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"sealNumbers","labelEs":"N\u00FAmeros de sello","labelEn":"Seal numbers","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":500,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Seal management', 'Gestión de sellos', 'EXPORT', 'Tariff', 'PerContainer', 'Booking', NULL, TRUE, FALSE, FALSE, NULL, TRUE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7', 'local-charges-on-demand.pay', FALSE, 'None', 'Always', TRUE, 'MATRIX_LATE', 'MATRIX_LATE', 'CL,BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Charge for submitting the matrix after the deadline (48 hours before departure until Nexus reports document deadlines, M2-10); tiered by days since the deadline (M3-14).', 'Cobro por presentar la matriz después del plazo (48 horas antes del zarpe mientras Nexus no informe los plazos documentales, M2-10); tarifa por tramos de días desde el plazo (M3-14).', 80, FALSE, NULL, 'None', '[{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'VesselDeparture', -48, NULL, NULL, 'Late matrix submission', 'Matriz fuera de plazo', 'EXPORT', 'Tariff', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, NULL, FALSE, 'LateOnly');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('08f6a148-a021-357f-11e0-9e263e3bc70e', 12000.0, NULL, 'SEAL_MANAGEMENT', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gestión de sellos por contenedor (M3-07)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('09121ab9-02ec-4885-dda1-1cc481c6ddd3', 35.0, 'PLAZO', 'BL_HOUSE_TRANSMISSION', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Transmisión de BL hijo dentro de plazo (M3-13)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('14c8c000-da8f-ba14-3e54-52df0949ac66', 0.0, 'FUERA_PLAZO', 'BL_HOUSE_TRANSMISSION', NULL, 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Transmisión de BL hijo fuera de plazo, horas desde el plazo (M3-13)', TRUE, NULL, NULL, 'Flat', 'Hours', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('3f963e93-b130-ae94-6ee6-8426587d9197', 300.0, NULL, 'BL_CORRECTION', NULL, 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Corrección o aclaración de BL Bolivia (M3-12)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('4f0226d1-eabb-3e9f-6355-41589b2ceb4a', 45000.0, NULL, 'BL_CORRECTION', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Corrección o aclaración de BL (M3-12)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('7e645de7-824d-4876-2e7e-d433d76e1f92', 0.0, NULL, 'EARLY_ARRIVAL', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Early por días de anticipación (M3-08)', TRUE, NULL, NULL, 'Flat', 'CalendarDays', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('85efb04f-05d5-2318-3bbb-1149a9593e99', 0.0, NULL, 'MATRIX_LATE', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Matriz fuera de plazo, días desde el plazo de presentación (M3-14)', TRUE, NULL, NULL, 'Flat', 'CalendarDays', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('8f9cd335-f4f6-df32-36b6-94794d2906dc', 180000.0, NULL, 'DROP_OFF', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Drop Off SCL por contenedor (M3-09)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('b023bef2-0e70-2988-f28c-2da684ab098e', 0.0, 'FUERA_PLAZO', 'BL_HOUSE_TRANSMISSION', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Transmisión de BL hijo fuera de plazo, horas desde el plazo (M3-13)', TRUE, NULL, NULL, 'Flat', 'Hours', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('ea3a5a01-c4ee-76ca-9d51-f9e542075cc7', 520.0, NULL, 'XOM', '40HC', 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Administración de contenedor XOM, 40'' HC (M3-10)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('ed9325ae-cb8d-6b70-cfc6-3bfaeaff51e2', 35.0, 'PLAZO', 'BL_HOUSE_TRANSMISSION', NULL, 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Transmisión de BL hijo dentro de plazo (M3-13)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('f383edf7-a9c4-fff5-2c56-78c53ce50bc1', 350.0, NULL, 'XOM', NULL, 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'BOB', NULL, NULL, 'Administración de contenedor XOM por unidad (M3-10)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('f69d95b8-610f-9e67-bb63-836e8e6ba3d2', 0.0, NULL, 'MATRIX_LATE', NULL, 'BO', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Matriz fuera de plazo, días desde el plazo de presentación (M3-14)', TRUE, NULL, NULL, 'Flat', 'CalendarDays', DATE '2026-10-01', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "WarehouseChangeBatches" ("Id", "ClientId", "CompletedAt", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "FailedItems", "ModifiedAt", "ModifiedBy", "ProcessedItems", "RequestedByUserId", "StartedAt", "Status", "SucceededItems", "TotalItems")
    VALUES ('99999999-0020-0020-0020-000000000001', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-03T09:00:06Z', TIMESTAMPTZ '2026-10-03T09:00:00Z', 'demo@importadorademo.cl', NULL, NULL, 0, NULL, NULL, 1, 'd4e5f6a7-0004-0004-0004-000000000010', TIMESTAMPTZ '2026-10-03T09:00:05Z', 'Completed', 1, 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "WarehouseChanges" ("Id", "Amount", "BatchId", "BillOfLadingId", "CompletedAt", "ContainerNumber", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "EntitlementReference", "EntitlementSource", "FromWarehouse", "IsFree", "ModifiedAt", "ModifiedBy", "RequestedByClientId", "RequestedByUserId", "Status", "TariffCode", "TariffSource", "ToWarehouse")
    VALUES ('99999999-000f-000f-000f-000000000003', 9940.0, NULL, '11111111-0007-0007-0007-000000000009', TIMESTAMPTZ '2026-10-02T15:00:00Z', NULL, 'CL', TIMESTAMPTZ '2026-10-02T14:30:00Z', 'demo@importadorademo.cl', 'CLP', NULL, NULL, NULL, NULL, 'STI San Antonio - Patio B', FALSE, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'Completed', 'KTE', 'PORTAL', 'Bodega Lo Espejo');
    INSERT INTO "WarehouseChanges" ("Id", "Amount", "BatchId", "BillOfLadingId", "CompletedAt", "ContainerNumber", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "EntitlementReference", "EntitlementSource", "FromWarehouse", "IsFree", "ModifiedAt", "ModifiedBy", "RequestedByClientId", "RequestedByUserId", "Status", "TariffCode", "TariffSource", "ToWarehouse")
    VALUES ('99999999-000f-000f-000f-000000000004', 0.0, '99999999-0020-0020-0020-000000000001', '11111111-0007-0007-0007-000000000010', TIMESTAMPTZ '2026-10-03T09:00:06Z', NULL, 'CL', TIMESTAMPTZ '2026-10-03T09:00:06Z', 'SYSTEM', 'CLP', NULL, NULL, 'RULE:eeeeeeee-0015-0015-0015-000000000001', 'PORTAL', 'N/D', TRUE, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'Completed', NULL, NULL, 'Bodega Central Santiago');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000019', '11111111-0007-0007-0007-000000000016', 'HLXU2609161', '40RF', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-260916', 'OnBoard', NULL, NULL, 25400.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000020', '11111111-0007-0007-0007-000000000016', 'HLXU2609162', '20DV', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-260917', 'OnBoard', NULL, NULL, 16100.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000021', '11111111-0007-0007-0007-000000000017', 'HLXU2609171', '20DV', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-260918', 'OnBoard', NULL, NULL, 15800.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000029', 120.0, '11111111-0007-0007-0007-000000000016', 'BL_HOUSE_TRANSMISSION', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'USD', NULL, NULL, 'Transmisión de BL hijo (SRV-20261002-5E1A0004)', FALSE, NULL, NULL, 'Paid', 0.0, 0.0, 120.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('2d66e7bf-d1f8-ee1d-5010-79747e017882', NULL, 45000.0, '11111111-0007-0007-0007-000000000002', 'Importadora Demo SpA', '76123456-7', 'HLCUVAL250200456', 'HLCUBKG2502004', 'BL_CORRECTION', 'CLP', 'Corrección o aclaración de BL (SRV-20261001-5E1A0003)', NULL, 'LocalCharge', NULL, 53550.0, 'CLP', '55555555-000b-000b-000b-000000000014', TIMESTAMPTZ '2026-10-02T15:00:00Z', '33333333-0009-0009-0009-000000000028', 8550.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('30ada185-078c-2fbf-3a02-2812eb08a5a0', NULL, 114000.0, '11111111-0007-0007-0007-000000000016', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260901610', 'HLCUBKG2609161', 'BL_HOUSE_TRANSMISSION', 'CLP', 'Transmisión de BL hijo (SRV-20261002-5E1A0004)', 950.0, 'LocalCharge', NULL, 120.0, 'USD', '55555555-000b-000b-000b-000000000014', TIMESTAMPTZ '2026-10-02T15:00:00Z', '33333333-0009-0009-0009-000000000029', 0.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('a832ba20-d7cd-be95-bd38-7c02c2b76c51', NULL, 9940.0, '11111111-0007-0007-0007-000000000009', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260400910', 'HLCUBKG2604091', 'WAREHOUSE_CHANGE', 'CLP', 'Cambio de almacén STI San Antonio - Patio B → Bodega Lo Espejo', NULL, 'WarehouseChange', NULL, 9940.0, 'CLP', '55555555-000b-000b-000b-000000000014', TIMESTAMPTZ '2026-10-02T15:00:00Z', '99999999-000f-000f-000f-000000000003', 0.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('4b5dd8b9-21c3-ce5e-3bba-5c3c17ec632f', TIMESTAMPTZ '2026-10-02T14:58:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, '55555555-000b-000b-000b-000000000014', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('9bfbb3d4-412d-6600-9482-ef7f1e64220f', TIMESTAMPTZ '2026-10-02T15:00:00Z', 'KHIPU_WEBHOOK', NULL, 'Processing', '55555555-000b-000b-000b-000000000014', NULL, 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('a0b9ed1f-e4ac-fe81-28be-77676bf235aa', TIMESTAMPTZ '2026-10-02T14:58:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000014', 'Initiated in Khipu', 'Processing');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000001', NULL, 180000.0, NULL, 'ED', '11111111-0007-0007-0007-000000000001', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUVAL250100123', 'HLCUBKG2501001', NULL, 'DROP_OFF', NULL, 'HLXU1234567', 'CL', TIMESTAMPTZ '2026-10-05T13:55:00Z', 'demo@importadorademo.cl', 'CLP', 'DROP_OFF_SCL', '8991b39b-f6dc-fa22-f2cf-cdaae3287f4b', NULL, NULL, NULL, '{"containers":["HLXU1234567"],"returnDate":"2026-10-09","depot":"SCL_PUDAHUEL"}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '{"pricingMode":"Tariff","chargeConceptCode":"DROP_OFF","requiresPayment":true,"amount":180000,"taxAmount":34200,"totalAmount":214200,"currency":"CLP","taxRate":19,"quantity":1,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":"8f9cd335-f4f6-df32-36b6-94794d2906dc","tariffCode":null,"tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":"HLXU1234567","containerType":"40HC","amount":180000,"tariffCode":null,"breakdown":[]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-05T14:00:00Z"}', 1, TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL, 'SRV-20261005-5E1A0001', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'PendingApproval', TIMESTAMPTZ '2026-10-05T14:00:00Z', TIMESTAMPTZ '2026-10-05T14:00:00Z', TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL, '8f9cd335-f4f6-df32-36b6-94794d2906dc', 'PORTAL', 34200.0, NULL, 3, 'NotApplicable', 214200.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000002', NULL, 12000.0, NULL, NULL, '11111111-0007-0007-0007-000000000006', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260300610', 'HLCUBKG2603061', NULL, 'SEAL_MANAGEMENT', NULL, 'HLXU2023001', 'CL', TIMESTAMPTZ '2026-10-05T14:57:00Z', 'demo@importadorademo.cl', 'CLP', 'SEAL_MANAGEMENT', 'b75305ad-33dc-5c3e-6831-1b8aedfc9756', NULL, NULL, NULL, '{"containers":["HLXU2023001"],"sealNumbers":"HLS-889120"}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'EXPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '{"pricingMode":"Tariff","chargeConceptCode":"SEAL_MANAGEMENT","requiresPayment":true,"amount":12000,"taxAmount":2280,"totalAmount":14280,"currency":"CLP","taxRate":19,"quantity":1,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":"08f6a148-a021-357f-11e0-9e263e3bc70e","tariffCode":null,"tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":"HLXU2023001","containerType":"40RF","amount":12000,"tariffCode":null,"breakdown":[]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-05T15:00:00Z"}', 1, TIMESTAMPTZ '2026-10-05T15:00:00Z', NULL, 'SRV-20261005-5E1A0002', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'PendingPayment', TIMESTAMPTZ '2026-10-05T15:00:00Z', TIMESTAMPTZ '2026-10-05T15:00:00Z', NULL, NULL, '08f6a148-a021-357f-11e0-9e263e3bc70e', 'PORTAL', 2280.0, NULL, 3, 'NotApplicable', 14280.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000003', NULL, 45000.0, NULL, 'CustomerService', '11111111-0007-0007-0007-000000000002', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUVAL250200456', 'HLCUBKG2502004', NULL, 'BL_CORRECTION', NULL, NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:50:00Z', 'demo@importadorademo.cl', 'CLP', 'BL_CORRECTION', '37f4f7a0-8da4-c832-bb22-7d2009de5dad', NULL, NULL, NULL, '{"requestType":"CORRECTION","section":"CONSIGNEE","description":"Corregir la dirección del consignatario: Av. Apoquindo 4500, Las Condes, Santiago."}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-02T15:00:00Z', '55555555-000b-000b-000b-000000000014', '{"pricingMode":"Tariff","chargeConceptCode":"BL_CORRECTION","requiresPayment":true,"amount":45000,"taxAmount":8550,"totalAmount":53550,"currency":"CLP","taxRate":19,"quantity":1,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":"4f0226d1-eabb-3e9f-6355-41589b2ceb4a","tariffCode":null,"tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":null,"containerType":null,"amount":45000,"tariffCode":null,"breakdown":[]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-01T13:00:00Z"}', 1, TIMESTAMPTZ '2026-10-01T13:00:00Z', NULL, 'SRV-20261001-5E1A0003', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'InProgress', TIMESTAMPTZ '2026-10-02T15:00:00Z', TIMESTAMPTZ '2026-10-01T13:00:00Z', NULL, NULL, '4f0226d1-eabb-3e9f-6355-41589b2ceb4a', 'PORTAL', 8550.0, NULL, 5, 'NotApplicable', 53550.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000004', NULL, 120.0, NULL, NULL, '11111111-0007-0007-0007-000000000016', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260901610', 'HLCUBKG2609161', NULL, 'BL_HOUSE_TRANSMISSION', TIMESTAMPTZ '2026-10-03T10:00:00Z', NULL, 'CL', TIMESTAMPTZ '2026-10-02T11:56:00Z', 'demo@importadorademo.cl', 'USD', 'BL_HOUSE_TRANSMISSION', '6e03d8d0-8670-c1cf-46a4-5248d899b57e', NULL, NULL, NULL, '{"houseBlNumbers":"HLCUSAI26090161A, HLCUSAI26090161B"}', FALSE, 36, TIMESTAMPTZ '2026-10-01T00:00:00Z', 'ETD', NULL, NULL, NULL, 'EXPORT', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-02T15:00:00Z', '55555555-000b-000b-000b-000000000014', '{"pricingMode":"Tariff","chargeConceptCode":"BL_HOUSE_TRANSMISSION","requiresPayment":true,"amount":120,"taxAmount":0,"totalAmount":120,"currency":"USD","taxRate":0,"quantity":1,"tierUnit":"Hours","measuredUnits":36,"timing":"Late","milestoneAt":"2026-10-01T00:00:00Z","milestoneSource":"ETD","tariffId":"b023bef2-0e70-2988-f28c-2da684ab098e","tariffCode":"FUERA_PLAZO","tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":null,"containerType":null,"amount":120,"tariffCode":"FUERA_PLAZO","breakdown":[{"fromUnit":25,"toUnit":72,"units":1,"unitAmount":120,"amount":120}]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-02T12:00:00Z"}', 1, TIMESTAMPTZ '2026-10-02T12:00:00Z', NULL, 'SRV-20261002-5E1A0004', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'BL hijo transmitido y aceptado por Aduana.', 'Completed', TIMESTAMPTZ '2026-10-03T10:00:00Z', TIMESTAMPTZ '2026-10-02T12:00:00Z', NULL, 'FUERA_PLAZO', 'b023bef2-0e70-2988-f28c-2da684ab098e', 'PORTAL', 0.0, 'Hours', 6, 'Late', 120.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000005', NULL, 180000.0, NULL, NULL, '11111111-0007-0007-0007-000000000009', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260400910', 'HLCUBKG2604091', NULL, 'DROP_OFF', NULL, 'HLXU3034001', 'CL', TIMESTAMPTZ '2026-10-03T09:54:00Z', 'demo@importadorademo.cl', 'CLP', 'DROP_OFF_SCL', '8991b39b-f6dc-fa22-f2cf-cdaae3287f4b', NULL, NULL, NULL, '{"containers":["HLXU3034001"],"returnDate":"2026-10-06","depot":"SCL_QUILICURA"}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '{"pricingMode":"Tariff","chargeConceptCode":"DROP_OFF","requiresPayment":true,"amount":180000,"taxAmount":34200,"totalAmount":214200,"currency":"CLP","taxRate":19,"quantity":1,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":"8f9cd335-f4f6-df32-36b6-94794d2906dc","tariffCode":null,"tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":"HLXU3034001","containerType":"40HC","amount":180000,"tariffCode":null,"breakdown":[]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-03T10:00:00Z"}', 1, TIMESTAMPTZ '2026-10-03T10:00:00Z', TIMESTAMPTZ '2026-10-03T16:00:00Z', 'SRV-20261003-5E1A0005', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'El depósito Quilicura no recibe unidades 40HC esta semana; solicite la devolución en Pudahuel.', 'Rejected', TIMESTAMPTZ '2026-10-03T16:00:00Z', TIMESTAMPTZ '2026-10-03T10:00:00Z', TIMESTAMPTZ '2026-10-03T10:00:00Z', NULL, '8f9cd335-f4f6-df32-36b6-94794d2906dc', 'PORTAL', 34200.0, NULL, 4, 'NotApplicable', 214200.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000006', NULL, 0.0, NULL, NULL, '11111111-0007-0007-0007-000000000006', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260300610', 'HLCUBKG2603061', TIMESTAMPTZ '2026-10-04T11:20:00Z', NULL, NULL, 'HLXU2023001', 'CL', TIMESTAMPTZ '2026-10-04T11:00:00Z', 'demo@importadorademo.cl', NULL, 'LATE_ARRIVAL', '7eb0b617-bc72-a0a1-a533-93df4e8d1d6e', NULL, NULL, NULL, '{"containers":["HLXU2023001"],"hours":30}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'EXPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, NULL, 1, NULL, NULL, 'SRV-20261004-5E1A0006', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'Cancelled', TIMESTAMPTZ '2026-10-04T11:20:00Z', NULL, NULL, NULL, NULL, NULL, 0.0, NULL, 2, NULL, 0.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000007', NULL, 0.0, NULL, NULL, '11111111-0007-0007-0007-000000000005', 'Comercio exterior', 'Av. Arce 2631, La Paz', 'facturacion@altiplano.bo', 'Comercial Altiplano SRL', '1023456017', 'HLCUIQQ260200078', 'HLCUBKG2602078', NULL, 'XOM', TIMESTAMPTZ '2026-10-04T15:00:00Z', 'HLXU5566779', 'BO', TIMESTAMPTZ '2026-10-04T14:58:00Z', 'demo@altiplano.bo', NULL, 'XOM', '1f4a4958-c841-daef-756a-b040b2014e4a', NULL, NULL, 'SOC:HLXU5566779', '{"containers":["HLXU5566779"]}', TRUE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000020', NULL, NULL, '{"pricingMode":"Tariff","chargeConceptCode":"XOM","requiresPayment":false,"amount":0,"taxAmount":0,"totalAmount":0,"currency":null,"taxRate":0,"quantity":0,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":null,"tariffCode":null,"tariffSource":null,"isExempt":true,"exemptionReference":"SOC:HLXU5566779","excludedContainers":["HLXU5566779"],"lines":[],"sourceCharges":[],"timeZone":"America/La_Paz","quotedAt":"2026-10-04T15:00:00Z"}', 0, TIMESTAMPTZ '2026-10-04T15:00:00Z', NULL, 'SRV-20261004-5E1A0007', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, 'Completed', TIMESTAMPTZ '2026-10-04T15:00:00Z', TIMESTAMPTZ '2026-10-04T15:00:00Z', NULL, NULL, NULL, NULL, 0.0, NULL, 3, 'NotApplicable', 0.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000008', NULL, 0.0, NULL, NULL, '11111111-0007-0007-0007-000000000016', 'Importación y distribución', 'Av. Apoquindo 4500, Las Condes, Santiago', 'facturacion@importadorademo.cl', 'Importadora Demo SpA', '76123456-7', 'HLCUSAI260901610', 'HLCUBKG2609161', NULL, NULL, NULL, NULL, 'CL', TIMESTAMPTZ '2026-10-05T18:00:00Z', 'demo@importadorademo.cl', NULL, 'MATRIX_LATE', 'cf38fc5e-d7af-910b-aba2-ea8ddde3e9d7', NULL, NULL, NULL, '{"observations":"Matriz enviada por correo el 29-09."}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'EXPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, NULL, 1, NULL, NULL, 'SRV-20261005-5E1A0008', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'Draft', TIMESTAMPTZ '2026-10-05T18:00:00Z', NULL, NULL, NULL, NULL, NULL, 0.0, NULL, 1, NULL, 0.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('075936e7-41be-53b0-1290-d3970873bf0f', '11111111-0007-0007-0007-000000000017', 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('ef31b036-2222-2f88-9a2f-85178328013c', '11111111-0007-0007-0007-000000000016', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('1115e1cb-e5e3-af7b-ce32-0b3cd2bf6644', 200.0, 6, '85efb04f-05d5-2318-3bbb-1149a9593e99', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('36e03912-6741-6aaa-b1df-6030fc077295', 80.0, 1, '7e645de7-824d-4876-2e7e-d433d76e1f92', 2);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('3a54e555-290b-7035-3a46-d48779e9e782', 60.0, 0, '14c8c000-da8f-ba14-3e54-52df0949ac66', 24);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('59813e09-2340-3be1-6176-a8c729aad23d', 60.0, 0, 'b023bef2-0e70-2988-f28c-2da684ab098e', 24);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('5e0f0096-0f79-5f68-c614-61a6cf3cb064', 250.0, 73, 'b023bef2-0e70-2988-f28c-2da684ab098e', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('63dcf8d2-9418-c776-3498-1f4c7ea28c6e', 120.0, 25, 'b023bef2-0e70-2988-f28c-2da684ab098e', 72);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('7922f54d-8324-e8a7-61ff-16a15e0f6ccc', 50.0, 0, 'f69d95b8-610f-9e67-bb63-836e8e6ba3d2', 1);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('82204082-2cb6-314d-5720-7d64f6f6d063', 100.0, 2, '85efb04f-05d5-2318-3bbb-1149a9593e99', 5);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('86950eae-1c4a-4602-f867-0be816597eea', 100.0, 2, 'f69d95b8-610f-9e67-bb63-836e8e6ba3d2', 5);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('8fb701ca-e302-16aa-bb3f-9d088b224703', 250.0, 6, '7e645de7-824d-4876-2e7e-d433d76e1f92', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('9989fc8b-7c73-ac18-3ddc-456af0138636', 120.0, 25, '14c8c000-da8f-ba14-3e54-52df0949ac66', 72);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('a606e47d-f958-2e06-61cb-5669ff5e2329', 200.0, 6, 'f69d95b8-610f-9e67-bb63-836e8e6ba3d2', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('c854d92c-53f0-62af-469a-54367d0ac7bf', 250.0, 73, '14c8c000-da8f-ba14-3e54-52df0949ac66', NULL);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('d9fe224f-0c5b-3653-adcf-e097be8b1085', 50.0, 0, '85efb04f-05d5-2318-3bbb-1149a9593e99', 1);
    INSERT INTO "TariffTiers" ("Id", "Amount", "FromUnit", "TariffId", "ToUnit")
    VALUES ('e1a3888e-a1ce-926b-2ccb-0118f80da5e1', 150.0, 3, '7e645de7-824d-4876-2e7e-d433d76e1f92', 5);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "WarehouseChangeBatchItems" ("Id", "BatchId", "BillOfLadingId", "BlNumber", "ContainerNumber", "ErrorCode", "ErrorMessage", "FromWarehouse", "LineNumber", "ProcessedAt", "Status", "TariffCode", "ToWarehouse", "WarehouseChangeId")
    VALUES ('9e413a2f-a1b6-9608-e41a-10e3ceebf9f8', '99999999-0020-0020-0020-000000000001', '11111111-0007-0007-0007-000000000010', 'HLCUSAI260401020', NULL, NULL, NULL, NULL, 1, TIMESTAMPTZ '2026-10-03T09:00:06Z', 'Succeeded', NULL, 'Bodega Central Santiago', '99999999-000f-000f-000f-000000000004');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ServiceRequestCharges" ("Id", "Generated", "LocalChargeId", "ServiceRequestId")
    VALUES ('52e277a6-8cb1-cc63-5049-2fedff932df2', TRUE, '33333333-0009-0009-0009-000000000028', 'ffffffff-0021-0021-0021-000000000003');
    INSERT INTO "ServiceRequestCharges" ("Id", "Generated", "LocalChargeId", "ServiceRequestId")
    VALUES ('8ae9ad55-dc43-4036-1737-82dd6d6dbb1a', TRUE, '33333333-0009-0009-0009-000000000029', 'ffffffff-0021-0021-0021-000000000004');
    INSERT INTO "ServiceRequestCharges" ("Id", "Generated", "LocalChargeId", "ServiceRequestId")
    VALUES ('b1d45b30-156b-9dba-dc89-95fce3304f08', TRUE, '33333333-0009-0009-0009-000000000027', 'ffffffff-0021-0021-0021-000000000002');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('0c46ef99-c9b2-b2da-4dd1-2a64950e2c88', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-05T15:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000002', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('0eca138e-a6fe-d34f-d7ce-0b313d25b1e8', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-03T09:54:00Z', 1, 'ffffffff-0021-0021-0021-000000000005', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('12312f19-f1e7-a19b-087d-db4cedffac15', 'System', 'SYSTEM', NULL, 'Submitted', 'Total 14280 CLP.', TIMESTAMPTZ '2026-10-05T15:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000002', 'PendingPayment');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('1cdf9dfc-60fa-4097-b2aa-4f5f6182dafc', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', 'Se reprogramó el ingreso de la unidad.', TIMESTAMPTZ '2026-10-04T11:20:00Z', 2, 'ffffffff-0021-0021-0021-000000000006', 'Cancelled');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('1ceb83e1-e697-1a75-3ae8-511c892693dc', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-05T14:57:00Z', 1, 'ffffffff-0021-0021-0021-000000000002', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('1fe56423-ce52-019c-88b2-a2e4c87dde8d', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-05T14:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000001', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('22d850c0-4f1f-76d9-e0f8-59b4fa87dd8e', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-02T12:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000004', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('233e7636-4120-570f-a96d-992cd1d85e44', 'System', 'SYSTEM', NULL, 'Paid', NULL, TIMESTAMPTZ '2026-10-02T15:00:00Z', 5, 'ffffffff-0021-0021-0021-000000000004', 'InProgress');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('2949375f-ff9f-e06a-b38a-69272b88ab4d', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-04T11:00:00Z', 1, 'ffffffff-0021-0021-0021-000000000006', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('2cb530f0-29e1-b3a6-14d0-43149ea77b16', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-03T10:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000005', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('3cadc7b7-cb67-e64b-d863-ef36787e6e4c', 'System', 'PAYMENT PAY-20261002-6F5E4D3C', NULL, 'PendingPayment', 'Comprobante RCP-20261002-7A8B9C0D.', TIMESTAMPTZ '2026-10-02T15:00:00Z', 4, 'ffffffff-0021-0021-0021-000000000004', 'Paid');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('46f143ca-839c-ab02-0a69-f2027e5795c0', 'System', 'SYSTEM', NULL, 'Submitted', 'Sin cobro: SOC:HLXU5566779.', TIMESTAMPTZ '2026-10-04T15:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000007', 'Completed');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('4c71ac76-58f3-656e-b492-6cdcf494a2a4', 'System', 'SYSTEM', NULL, 'Paid', NULL, TIMESTAMPTZ '2026-10-02T15:00:00Z', 5, 'ffffffff-0021-0021-0021-000000000003', 'InProgress');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('4d4b11bb-2ce8-5b4b-c2b5-89809ec47b3c', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-05T18:00:00Z', 1, 'ffffffff-0021-0021-0021-000000000008', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('8bc9ea6d-6047-ed03-8715-865e0038dbcd', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-01T13:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000003', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('8e7eaa46-1c0c-6d0e-b3ae-bb3d92296391', 'System', 'PAYMENT PAY-20261002-6F5E4D3C', NULL, 'PendingPayment', 'Comprobante RCP-20261002-7A8B9C0D.', TIMESTAMPTZ '2026-10-02T15:00:00Z', 4, 'ffffffff-0021-0021-0021-000000000003', 'Paid');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('9fd48303-f1f1-ab06-8ee0-8d0b60faba6a', 'Internal', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'PendingApproval', 'El depósito Quilicura no recibe unidades 40HC esta semana; solicite la devolución en Pudahuel.', TIMESTAMPTZ '2026-10-03T16:00:00Z', 4, 'ffffffff-0021-0021-0021-000000000005', 'Rejected');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('ad14be4a-ebbe-be59-6e22-a876f398fb72', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Draft', NULL, TIMESTAMPTZ '2026-10-04T15:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000007', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('ba24d9bc-4ee8-8753-b4b4-0b6aebd0d574', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-05T13:55:00Z', 1, 'ffffffff-0021-0021-0021-000000000001', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('c6a6da09-505c-a4d8-78f9-700398403b24', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-01T12:50:00Z', 1, 'ffffffff-0021-0021-0021-000000000003', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('cca46ba1-145e-2be1-d0e0-8af2dd5a0e97', 'System', 'SYSTEM', NULL, 'Submitted', 'Total 53550 CLP.', TIMESTAMPTZ '2026-10-01T13:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000003', 'PendingPayment');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('dec466f0-24bf-71ac-7ec9-bcc8f5a9a315', 'System', 'SYSTEM', NULL, 'Submitted', 'Derivada al equipo ED.', TIMESTAMPTZ '2026-10-05T14:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000001', 'PendingApproval');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('e49d521c-ba5c-2e4e-666a-136461165ef3', 'System', 'SYSTEM', NULL, 'Submitted', 'Derivada al equipo ED.', TIMESTAMPTZ '2026-10-03T10:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000005', 'PendingApproval');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('e67dd7d2-1b76-a227-fe0d-cd0a4904e01c', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, TIMESTAMPTZ '2026-10-02T11:56:00Z', 1, 'ffffffff-0021-0021-0021-000000000004', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('ed4f28fa-e309-d42f-6b2b-ef4b2c1db0c0', 'Internal', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'InProgress', 'BL hijo transmitido y aceptado por Aduana.', TIMESTAMPTZ '2026-10-03T10:00:00Z', 6, 'ffffffff-0021-0021-0021-000000000004', 'Completed');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('edbd4f6a-c3e0-4180-4fc6-24cb71a0e524', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, NULL, TIMESTAMPTZ '2026-10-04T14:58:00Z', 1, 'ffffffff-0021-0021-0021-000000000007', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('f20e8fcb-2085-f157-43aa-eb5751bdf13a', 'System', 'SYSTEM', NULL, 'Submitted', 'Total 120 USD.', TIMESTAMPTZ '2026-10-02T12:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000004', 'PendingPayment');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE UNIQUE INDEX "IX_ServiceDefinitions_Code" ON "ServiceDefinitions" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceDefinitions_IsActive_DisplayOrder" ON "ServiceDefinitions" ("IsActive", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequestAttachments_ServiceRequestId_FieldKey" ON "ServiceRequestAttachments" ("ServiceRequestId", "FieldKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequestCharges_LocalChargeId" ON "ServiceRequestCharges" ("LocalChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE UNIQUE INDEX "IX_ServiceRequestCharges_ServiceRequestId_LocalChargeId" ON "ServiceRequestCharges" ("ServiceRequestId", "LocalChargeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequestEvents_ServiceRequestId_OccurredAt_Sequence" ON "ServiceRequestEvents" ("ServiceRequestId", "OccurredAt", "Sequence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequests_BillOfLadingId_DefinitionId" ON "ServiceRequests" ("BillOfLadingId", "DefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequests_DefinitionId" ON "ServiceRequests" ("DefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequests_OrganizationId_CreatedAt" ON "ServiceRequests" ("OrganizationId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE UNIQUE INDEX "IX_ServiceRequests_RequestNumber" ON "ServiceRequests" ("RequestNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    CREATE INDEX "IX_ServiceRequests_Status_AssignedTeam" ON "ServiceRequests" ("Status", "AssignedTeam");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006121708_AddOnDemandServices') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006121708_AddOnDemandServices', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    ALTER TABLE "CustomerInvoices" ADD "SupersededByInvoiceId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    ALTER TABLE "CustomerInvoices" ADD "SupersedesInvoiceId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE TABLE "ChargeSettlements" (
        "Id" uuid NOT NULL,
        "Kind" character varying(20) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "PaymentId" uuid NOT NULL,
        "PaymentDetailId" uuid NOT NULL,
        "PaymentNumber" character varying(50) NOT NULL,
        "ReceiptNumber" character varying(50),
        "PayerOrganizationId" uuid NOT NULL,
        "PayerTaxId" character varying(20),
        "PayerName" character varying(200),
        "OnBehalfOfOrganizationId" uuid,
        "BillingTaxId" character varying(20),
        "BillingName" character varying(200),
        "BillOfLadingId" uuid,
        "BlNumber" character varying(50),
        "BookingNumber" character varying(50),
        "Country" character varying(5) NOT NULL,
        "ItemType" character varying(30) NOT NULL,
        "SourceId" uuid NOT NULL,
        "ConceptCode" character varying(50) NOT NULL,
        "Description" character varying(500),
        "Amount" numeric(18,2) NOT NULL,
        "Currency" character varying(5) NOT NULL,
        "PaidAmount" numeric(18,2) NOT NULL,
        "PaidCurrency" character varying(5) NOT NULL,
        "SettledAt" timestamp with time zone NOT NULL,
        "ReceiptDocumentId" uuid,
        "MatchedInvoiceId" uuid,
        "MatchedAt" timestamp with time zone,
        "MatchedBy" character varying(256),
        "MatchNote" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ChargeSettlements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE TABLE "CreditImputationRules" (
        "Id" uuid NOT NULL,
        "Country" character varying(5) NOT NULL,
        "ConceptCode" character varying(50) NOT NULL,
        "NexusCreditConcept" character varying(30) NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "Notes" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_CreditImputationRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE TABLE "DepositProofs" (
        "Id" uuid NOT NULL,
        "PaymentId" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "FileName" character varying(255) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "StorageKey" character varying(300),
        "ContentHash" character varying(64),
        "BankName" character varying(100),
        "BankReference" character varying(60),
        "DepositDate" date,
        "DepositAmount" numeric(18,2),
        "Notes" character varying(500),
        "UploadedAt" timestamp with time zone NOT NULL,
        "UploadedByUserId" uuid,
        "UploadedBy" character varying(256) NOT NULL,
        "ReviewedAt" timestamp with time zone,
        "ReviewedByUserId" uuid,
        "ReviewedBy" character varying(256),
        "ReviewNotes" character varying(500),
        "RejectionReason" character varying(500),
        CONSTRAINT "PK_DepositProofs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DepositProofs_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE TABLE "InvoiceReissues" (
        "Id" uuid NOT NULL,
        "ServiceRequestId" uuid NOT NULL,
        "OriginalInvoiceId" uuid NOT NULL,
        "OriginalSiiNumber" character varying(30),
        "OriginalSourceNumber" character varying(50) NOT NULL,
        "OriginalTaxId" character varying(20) NOT NULL,
        "OriginalLegalName" character varying(200) NOT NULL,
        "VatLossAmount" numeric(18,2) NOT NULL,
        "FeeAmount" numeric(18,2) NOT NULL,
        "FeeTaxAmount" numeric(18,2) NOT NULL,
        "Currency" character varying(5),
        "ExchangeRate" numeric(18,6),
        "AcceptorEmail" character varying(256) NOT NULL,
        "AcceptanceStatus" character varying(20),
        "AcceptanceTokenHash" character varying(64),
        "AcceptanceRequestedAt" timestamp with time zone,
        "AcceptanceExpiresAt" timestamp with time zone,
        "AcceptedAt" timestamp with time zone,
        "AcceptedByName" character varying(200),
        "AcceptedByTaxId" character varying(20),
        "AcceptedFromAddress" character varying(64),
        "DeclinedAt" timestamp with time zone,
        "DeclineReason" character varying(500),
        "NewInvoiceId" uuid,
        "IssuedAt" timestamp with time zone,
        CONSTRAINT "PK_InvoiceReissues" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_InvoiceReissues_ServiceRequests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES "ServiceRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000018', 'HLCUSAI260701810', NULL, 'HLCUBKG2607181', 'c3d4e5f6-0003-0003-0003-000000000010', 'Lima Foods SAC', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-10-12T00:00:00Z', TIMESTAMPTZ '2026-10-03T00:00:00Z', NULL, 'PELIM', 1900.0, 'USD', TIMESTAMPTZ '2026-09-29T15:00:00Z', NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-10-03T06:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Lima, Peru', 'Callao (PECLL)', 'PECLL', 'San Antonio (CLSAI)', 'Export', 'Importadora Demo SpA', 'Departed', 'BL', 'Valparaiso Express', NULL, '2610N');
    INSERT INTO "BillsOfLading" ("Id", "BLNumber", "BLType", "BookingNumber", "ClientId", "Consignee", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DifuCode", "DifuLocationCode", "ETA", "ETD", "EblPlatform", "FinalDestinationCode", "FreightAmount", "FreightCurrency", "FreightPaidAt", "FreightTerms", "Incoterm", "IsSeaWaybill", "IsToOrder", "IssuanceStatus", "IssuanceStatusAt", "ModifiedAt", "ModifiedBy", "NotifyParty", "OperatorVoyage", "ParentBLId", "PlaceOfDelivery", "PortOfDischarge", "PortOfDischargeCode", "PortOfLoading", "ShipmentType", "Shipper", "Status", "TransportDocumentType", "Vessel", "VesselImo", "Voyage")
    VALUES ('11111111-0007-0007-0007-000000000019', 'HLCUVAP260601930', NULL, 'HLCUBKG2606193', 'c3d4e5f6-0003-0003-0003-000000000060', 'Distribuidora Andes Crédito SpA', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-09-28T00:00:00Z', TIMESTAMPTZ '2026-09-05T00:00:00Z', NULL, 'CLVAP', 2400.0, 'USD', TIMESTAMPTZ '2026-09-05T12:00:00Z', NULL, NULL, FALSE, FALSE, 'Issued', TIMESTAMPTZ '2026-09-06T10:00:00Z', NULL, NULL, NULL, NULL, NULL, 'Santiago, Chile', 'Valparaiso (CLVAP)', 'CLVAP', 'Santos (BRSSZ)', 'Import', 'Santos Coffee Exporters Ltda', 'Arrived', 'BL', 'Santos Express', NULL, '2610N');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('51c3d02a-634f-2135-64c7-0c59a15165fb', 'Service', 'VAT_LOSS', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 320, TRUE, NULL, NULL, 'Pérdida de IVA', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('923fac6f-9222-dbf9-90ea-4f267bafce41', 'Service', 'REINVOICING', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 310, TRUE, NULL, NULL, 'Refacturación IAO', FALSE, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ChargeSettlements" ("Id", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "Currency", "Description", "ItemType", "Kind", "MatchNote", "MatchedAt", "MatchedBy", "MatchedInvoiceId", "OnBehalfOfOrganizationId", "PaidAmount", "PaidCurrency", "PayerName", "PayerOrganizationId", "PayerTaxId", "PaymentDetailId", "PaymentId", "PaymentNumber", "ReceiptDocumentId", "ReceiptNumber", "SettledAt", "SourceId", "Status")
    VALUES ('1f978f39-3168-a413-0edc-19582df4a4e7', 71400.0, '11111111-0007-0007-0007-000000000018', 'Agencia Marítima del Pacífico Ltda', '96555444-3', 'HLCUSAI260701810', 'HLCUBKG2607181', 'GATE_OUT', 'CL', TIMESTAMPTZ '2026-10-01T14:00:00Z', 'CLP', 'Gate Out - 40HC (San Antonio)', 'LocalCharge', 'Advance', NULL, TIMESTAMPTZ '2026-10-04T12:00:00Z', 'INVOICE_REFRESH', 'ffffffff-0017-0017-0017-000000000014', NULL, 71400.0, 'CLP', 'Agencia Marítima del Pacífico Ltda', 'c3d4e5f6-0003-0003-0003-000000000030', '96555444-3', '0745ba3a-34ac-c833-5db6-bf116971a424', '55555555-000b-000b-000b-000000000017', 'PAY-20261001-D4E5F6A7', 'ffffffff-0018-0018-0018-000000000005', 'RCP-20261001-E8F9A0B1', TIMESTAMPTZ '2026-10-01T14:00:00Z', '33333333-0009-0009-0009-000000000034', 'Matched');
    INSERT INTO "ChargeSettlements" ("Id", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "Currency", "Description", "ItemType", "Kind", "MatchNote", "MatchedAt", "MatchedBy", "MatchedInvoiceId", "OnBehalfOfOrganizationId", "PaidAmount", "PaidCurrency", "PayerName", "PayerOrganizationId", "PayerTaxId", "PaymentDetailId", "PaymentId", "PaymentNumber", "ReceiptDocumentId", "ReceiptNumber", "SettledAt", "SourceId", "Status")
    VALUES ('3302d5bd-8631-c56a-7b09-37d0629b2c35', 53550.0, '11111111-0007-0007-0007-000000000019', 'Distribuidora Andes Crédito SpA', '76000002-2', 'HLCUVAP260601930', 'HLCUBKG2606193', 'BL_FEE', 'CL', TIMESTAMPTZ '2026-10-02T13:00:00Z', 'CLP', 'BL Documentation Fee (import)', 'LocalCharge', 'Advance', NULL, NULL, NULL, NULL, NULL, 53550.0, 'CLP', 'Distribuidora Andes Crédito SpA', 'c3d4e5f6-0003-0003-0003-000000000060', '76000002-2', 'db2bf9c7-ceb5-f5a2-e768-8501f7664a50', '55555555-000b-000b-000b-000000000015', 'PAY-20261002-A1C2E3F4', NULL, 'RCP-20261002-B5D6E7F8', TIMESTAMPTZ '2026-10-02T13:00:00Z', '33333333-0009-0009-0009-000000000030', 'Open');
    INSERT INTO "ChargeSettlements" ("Id", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "Currency", "Description", "ItemType", "Kind", "MatchNote", "MatchedAt", "MatchedBy", "MatchedInvoiceId", "OnBehalfOfOrganizationId", "PaidAmount", "PaidCurrency", "PayerName", "PayerOrganizationId", "PayerTaxId", "PaymentDetailId", "PaymentId", "PaymentNumber", "ReceiptDocumentId", "ReceiptNumber", "SettledAt", "SourceId", "Status")
    VALUES ('8b700f9d-0925-04e9-55c9-7eefbc8e15fc', 29750.0, '11111111-0007-0007-0007-000000000019', 'Distribuidora Andes Crédito SpA', '76000002-2', 'HLCUVAP260601930', 'HLCUBKG2606193', 'ISPS', 'CL', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'CLP', 'ISPS', 'LocalCharge', 'CreditImputation', NULL, NULL, NULL, NULL, NULL, 29750.0, 'CLP', 'Distribuidora Andes Crédito SpA', 'c3d4e5f6-0003-0003-0003-000000000060', '76000002-2', 'aab31ddd-35ff-069a-d97b-e746504c1cf4', '55555555-000b-000b-000b-000000000016', 'CRI-20261003-C9D8E7F6', NULL, NULL, TIMESTAMPTZ '2026-10-03T10:30:00Z', '33333333-0009-0009-0009-000000000031', 'Open');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ConfigurationSettings" ("Id", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Key", "ModifiedAt", "ModifiedBy", "Scope", "Value")
    VALUES ('11f1dde1-1820-e7fe-1e07-4aa557ecd21c', NULL, TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'Statement.AgingBuckets', NULL, NULL, 'Global', '30,60,90');
    INSERT INTO "ConfigurationSettings" ("Id", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Key", "ModifiedAt", "ModifiedBy", "Scope", "Value")
    VALUES ('faff8f22-0d64-9389-8e15-d248dd2301b5', NULL, TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, 'Statement.DueSoonDays', NULL, NULL, 'Global', '7');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "CreditImputationRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy", "NexusCreditConcept", "Notes")
    VALUES ('0016cb8a-acd8-7248-7612-82d73aa4d31f', 'GATE_OUT', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'LOCAL_CHARGES', 'Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10).');
    INSERT INTO "CreditImputationRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy", "NexusCreditConcept", "Notes")
    VALUES ('3a4b4010-b87c-a88e-d52a-b2d36a834765', 'THC', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'LOCAL_CHARGES', 'Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10).');
    INSERT INTO "CreditImputationRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy", "NexusCreditConcept", "Notes")
    VALUES ('5b866cc3-d88e-fc09-72a4-460533ba92bd', 'BL_FEE', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'LOCAL_CHARGES', 'Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10).');
    INSERT INTO "CreditImputationRules" ("Id", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsEnabled", "ModifiedAt", "ModifiedBy", "NexusCreditConcept", "Notes")
    VALUES ('893a8a68-2509-dfec-e30f-9131e821fc5e', 'ISPS', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, TRUE, NULL, NULL, 'LOCAL_CHARGES', 'Propuesta conservadora (recargos locales de Chile) pendiente de confirmación de Finanzas (M5-10).');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    UPDATE "CustomerInvoices" SET "SupersededByInvoiceId" = NULL, "SupersedesInvoiceId" = NULL
    WHERE "Id" = 'ffffffff-0017-0017-0017-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SupersededByInvoiceId", "SupersedesInvoiceId", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000011', '11111111-0007-0007-0007-000000000011', 'HLCUVAP260401130', 'HLCUBKG2604113', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-07-01', TRUE, DATE '2026-06-01', 'Distribuidora Andes Crédito SpA', NULL, NULL, 80000.0, 'c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, '100120', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-003410', 'Pending', NULL, NULL, TIMESTAMPTZ '2026-10-05T11:00:00Z', 15200.0, '76000002-2', 95200.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SupersededByInvoiceId", "SupersedesInvoiceId", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000012', '11111111-0007-0007-0007-000000000011', 'HLCUVAP260401130', 'HLCUBKG2604113', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-08-03', TRUE, DATE '2026-07-20', 'Distribuidora Andes Crédito SpA', NULL, NULL, 150000.0, 'c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, '100190', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-003702', 'Pending', NULL, NULL, TIMESTAMPTZ '2026-10-05T11:00:00Z', 28500.0, '76000002-2', 178500.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SupersededByInvoiceId", "SupersedesInvoiceId", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000013', '11111111-0007-0007-0007-000000000019', 'HLCUVAP260601930', 'HLCUBKG2606193', NULL, 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-10-10', TRUE, DATE '2026-09-10', 'Distribuidora Andes Crédito SpA', NULL, NULL, 45000.0, 'c3d4e5f6-0003-0003-0003-000000000060', NULL, NULL, '100310', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004720', 'Pending', NULL, NULL, TIMESTAMPTZ '2026-10-05T11:00:00Z', 8550.0, '76000002-2', 53550.0);
    INSERT INTO "CustomerInvoices" ("Id", "BillOfLadingId", "BlNumber", "BookingNumber", "ConceptCode", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "DocumentType", "DueDate", "IsPayable", "IssueDate", "LegalName", "ModifiedAt", "ModifiedBy", "NetAmount", "OrganizationId", "PaidAt", "PaymentId", "SiiNumber", "SiiStatus", "Source", "SourceNumber", "Status", "SupersededByInvoiceId", "SupersedesInvoiceId", "SyncedAt", "TaxAmount", "TaxId", "TotalAmount")
    VALUES ('ffffffff-0017-0017-0017-000000000014', '11111111-0007-0007-0007-000000000018', 'HLCUSAI260701810', 'HLCUBKG2607181', 'GATE_OUT', 'CL', TIMESTAMPTZ '2026-10-05T11:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Invoice', DATE '2026-11-03', FALSE, DATE '2026-10-04', 'Agencia Marítima del Pacífico Ltda', NULL, NULL, 60000.0, 'c3d4e5f6-0003-0003-0003-000000000030', TIMESTAMPTZ '2026-10-01T14:00:00Z', '55555555-000b-000b-000b-000000000017', '100318', 'ACCEPTED', 'DUMMY', 'HL-CL-2026-004790', 'Paid', NULL, NULL, TIMESTAMPTZ '2026-10-05T11:00:00Z', 11400.0, '96555444-3', 71400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "DepositProofs" ("Id", "BankName", "BankReference", "ContentHash", "ContentType", "DepositAmount", "DepositDate", "FileName", "Notes", "PaymentId", "RejectionReason", "ReviewNotes", "ReviewedAt", "ReviewedBy", "ReviewedByUserId", "SizeBytes", "Status", "StorageKey", "UploadedAt", "UploadedBy", "UploadedByUserId")
    VALUES ('ffffffff-0024-0024-0024-000000000001', 'Banco de Chile', NULL, NULL, 'image/jpeg', 53550.0, DATE '2026-10-03', 'comprobante-deposito-borroso.jpg', NULL, '55555555-000b-000b-000b-000000000011', 'La imagen no permite leer el número de operación ni el monto abonado.', NULL, TIMESTAMPTZ '2026-10-04T12:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 0, 'Rejected', NULL, TIMESTAMPTZ '2026-10-03T18:00:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010');
    INSERT INTO "DepositProofs" ("Id", "BankName", "BankReference", "ContentHash", "ContentType", "DepositAmount", "DepositDate", "FileName", "Notes", "PaymentId", "RejectionReason", "ReviewNotes", "ReviewedAt", "ReviewedBy", "ReviewedByUserId", "SizeBytes", "Status", "StorageKey", "UploadedAt", "UploadedBy", "UploadedByUserId")
    VALUES ('ffffffff-0024-0024-0024-000000000002', 'Banco de Chile', 'OP-55821473', NULL, 'application/pdf', 53550.0, DATE '2026-10-03', 'comprobante-deposito-BDP-20261003-5C7D9E1F.pdf', 'Depósito en efectivo, sucursal Las Condes.', '55555555-000b-000b-000b-000000000011', NULL, NULL, NULL, NULL, NULL, 0, 'Submitted', NULL, TIMESTAMPTZ '2026-10-04T15:10:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000035', 25000.0, '11111111-0007-0007-0007-000000000001', 'REINVOICING', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Refacturación de la factura 100198 (SRV-20261005-5E1A0009)', TRUE, NULL, NULL, 'Pending', 4750.0, 19.0, 29750.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000036', 22800.0, '11111111-0007-0007-0007-000000000001', 'VAT_LOSS', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Pérdida de IVA de la factura 100198 (SRV-20261005-5E1A0009)', FALSE, NULL, NULL, 'Pending', 0.0, 0.0, 22800.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('218d2183-e252-8852-de39-cd0104d6a085', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'c7f2abc0-29ae-1d0b-d957-ea4707a567b0', 'Tariff', '{"conceptCode":"REINVOICING","code":null,"country":"CL","currency":"CLP","containerType":null,"description":"Refacturaci\u00F3n IAO por factura (M3-11)","amount":25000,"tierUnit":"None","tierMode":"Flat","tiers":[],"validFrom":"2026-10-01","validTo":null,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('2d9f6544-3c95-8cf5-f6dd-e808cedd59ce', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '5b866cc3-d88e-fc09-72a4-460533ba92bd', 'CreditImputationRule', '{"country":"CL","conceptCode":"BL_FEE","nexusCreditConcept":"LOCAL_CHARGES","isEnabled":true,"notes":"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\u00F3n de Finanzas (M5-10)."}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('89c5b237-f1a2-5a8e-335a-e2855cd4e5bb', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0022-0022-0022-000000000001', 'ServiceDefinition', '{"code":"IAO_REINVOICING","nameEs":"Refacturaci\u00F3n IAO y p\u00E9rdida de IVA","nameEn":"IAO re-invoicing and VAT loss","descriptionEs":"Refacturaci\u00F3n de una factura emitida a una nueva raz\u00F3n social: el cliente registra los nuevos datos de facturaci\u00F3n y adjunta la aprobaci\u00F3n de la nueva raz\u00F3n social; se cobran juntos la refacturaci\u00F3n y la p\u00E9rdida de IVA, y la factura se emite solo con la aceptaci\u00F3n del cobro por la nueva raz\u00F3n social (M3-11, CL-EXP-11, CL-IMP-09). Se solicita desde la factura.","descriptionEn":"Re-invoicing of an issued invoice to a new legal entity: the customer enters the new billing data and attaches the new entity\u0027s approval; the re-invoicing fee and the VAT loss are paid together, and the invoice is issued only after the new legal entity accepts the charge (M3-11). Requested from the invoice.","operations":"IMPORT,EXPORT","countries":"CL","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022newCompanyApproval\u0022,\u0022labelEs\u0022:\u0022Aprobaci\\u00F3n de la nueva raz\\u00F3n social\u0022,\u0022labelEn\u0022:\u0022Approval of the new legal entity\u0022,\u0022type\u0022:\u0022file\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022reason\u0022,\u0022labelEs\u0022:\u0022Motivo de la refacturaci\\u00F3n\u0022,\u0022labelEn\u0022:\u0022Reason for the re-invoicing\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":true,"tariffAcceptanceRequired":true,"pricingMode":"Tariff","chargeConceptCode":"REINVOICING","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":true,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"local-charges-on-demand.pay","displayOrder":120,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('afa9137e-bf2b-5690-fe33-0cb261464598', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '0016cb8a-acd8-7248-7612-82d73aa4d31f', 'CreditImputationRule', '{"country":"CL","conceptCode":"GATE_OUT","nexusCreditConcept":"LOCAL_CHARGES","isEnabled":true,"notes":"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\u00F3n de Finanzas (M5-10)."}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('b7fe99ef-e1c5-2eba-49b5-12812d0cc83a', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '893a8a68-2509-dfec-e30f-9131e821fc5e', 'CreditImputationRule', '{"country":"CL","conceptCode":"ISPS","nexusCreditConcept":"LOCAL_CHARGES","isEnabled":true,"notes":"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\u00F3n de Finanzas (M5-10)."}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e09a7df1-2e24-226b-f05d-131b4282f10b', 'Created', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, '3a4b4010-b87c-a88e-d52a-b2d36a834765', 'CreditImputationRule', '{"country":"CL","conceptCode":"THC","nexusCreditConcept":"LOCAL_CHARGES","isEnabled":true,"notes":"Propuesta conservadora (recargos locales de Chile) pendiente de confirmaci\u00F3n de Finanzas (M5-10)."}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('ffffffff-0022-0022-0022-000000000001', 'local-charges-on-demand.pay', TRUE, 'None', 'Always', TRUE, 'REINVOICING', 'IAO_REINVOICING', 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Re-invoicing of an issued invoice to a new legal entity: the customer enters the new billing data and attaches the new entity''s approval; the re-invoicing fee and the VAT loss are paid together, and the invoice is issued only after the new legal entity accepts the charge (M3-11). Requested from the invoice.', 'Refacturación de una factura emitida a una nueva razón social: el cliente registra los nuevos datos de facturación y adjunta la aprobación de la nueva razón social; se cobran juntos la refacturación y la pérdida de IVA, y la factura se emite solo con la aceptación del cobro por la nueva razón social (M3-11, CL-EXP-11, CL-IMP-09). Se solicita desde la factura.', 120, FALSE, NULL, 'None', '[{"key":"newCompanyApproval","labelEs":"Aprobaci\u00F3n de la nueva raz\u00F3n social","labelEn":"Approval of the new legal entity","type":"file","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"reason","labelEs":"Motivo de la refacturaci\u00F3n","labelEn":"Reason for the re-invoicing","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'IAO re-invoicing and VAT loss', 'Refacturación IAO y pérdida de IVA', 'IMPORT,EXPORT', 'Tariff', 'PerRequest', 'BL', NULL, FALSE, FALSE, TRUE, NULL, TRUE, 'None');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "Tariffs" ("Id", "Amount", "Code", "ConceptCode", "ContainerType", "Country", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsActive", "ModifiedAt", "ModifiedBy", "TierMode", "TierUnit", "ValidFrom", "ValidTo")
    VALUES ('c7f2abc0-29ae-1d0b-d957-ea4707a567b0', 25000.0, NULL, 'REINVOICING', NULL, 'CL', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Refacturación IAO por factura (M3-11)', TRUE, NULL, NULL, 'Flat', 'None', DATE '2026-10-01', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000023', '11111111-0007-0007-0007-000000000018', 'HLXU3071801', '40HC', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-071801', 'OnBoard', NULL, NULL, 24800.0);
    INSERT INTO "BLContainers" ("Id", "BillOfLadingId", "ContainerNumber", "ContainerType", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "GrossWeight", "IsShipperOwned", "IsoTypeCode", "ModifiedAt", "ModifiedBy", "PackageCount", "SealNumber", "Status", "Tare", "Vgm", "Weight")
    VALUES ('22222222-0008-0008-0008-000000000024', '11111111-0007-0007-0007-000000000019', 'HLXU3061901', '20DV', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL, 'SL-061901', 'Discharged', NULL, NULL, 17600.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000030', 45000.0, '11111111-0007-0007-0007-000000000019', 'BL_FEE', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'BL Documentation Fee (import)', TRUE, NULL, NULL, 'Paid', 8550.0, 19.0, 53550.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000031', 25000.0, '11111111-0007-0007-0007-000000000019', 'ISPS', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'ISPS', TRUE, NULL, NULL, 'CreditImputed', 4750.0, 19.0, 29750.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000032', 185000.0, '11111111-0007-0007-0007-000000000019', 'THC', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Terminal Handling Charge - 20DV (Valparaíso)', TRUE, NULL, NULL, 'Pending', 35150.0, 19.0, 220150.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000033', 60000.0, '11111111-0007-0007-0007-000000000019', 'GATE_OUT', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate Out - 20DV (Valparaíso)', TRUE, NULL, NULL, 'Pending', 11400.0, 19.0, 71400.0);
    INSERT INTO "LocalCharges" ("Id", "Amount", "BillOfLadingId", "ChargeType", "CreatedAt", "CreatedBy", "Currency", "DeletedAt", "DeletedBy", "Description", "IsTaxable", "ModifiedAt", "ModifiedBy", "Status", "TaxAmount", "TaxRate", "TotalAmount")
    VALUES ('33333333-0009-0009-0009-000000000034', 60000.0, '11111111-0007-0007-0007-000000000018', 'GATE_OUT', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', 'CLP', NULL, NULL, 'Gate Out - 40HC (San Antonio)', TRUE, NULL, NULL, 'Paid', 11400.0, 19.0, 71400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000015', NULL, 45000.0, '11111111-0007-0007-0007-000000000019', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000060', TIMESTAMPTZ '2026-10-02T13:00:00Z', 'BANCOCHILE_WEBHOOK', 'CL', TIMESTAMPTZ '2026-10-02T12:58:00Z', 'd4e5f6a7-0004-0004-0004-000000000060', 'd4e5f6a7-0004-0004-0004-000000000060', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20261002-A1C2E3F4', NULL, NULL, NULL, NULL, NULL, 'Account', 'Distribuidora Andes Crédito SpA', '76000002-2', TIMESTAMPTZ '2026-10-02T13:00:00Z', 'BANK_BUTTON_BCH', 'BANK_BUTTON_BCH', 'PAY-20261002-A1C2E3F4', 'Account', 'BancoChile', 'DUMMY-BANCOCHILE-PAY-20261002-A1C2E3F4', 'BCH-TXN-55100877', 'RCP-20261002-B5D6E7F8', NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-10-02T13:00:00Z', 8550.0, 53550.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000016', NULL, 25000.0, '11111111-0007-0007-0007-000000000019', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000060', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'credito@distribuidoraandes.cl', 'CL', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'd4e5f6a7-0004-0004-0004-000000000060', 'd4e5f6a7-0004-0004-0004-000000000060', 'CLP', NULL, NULL, NULL, NULL, 'CRI-20261003-C9D8E7F6', NULL, NULL, NULL, NULL, NULL, 'CreditLine', 'Distribuidora Andes Crédito SpA', '76000002-2', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'CREDIT_LINE', 'CREDIT_LINE', 'CRI-20261003-C9D8E7F6', 'CreditLine', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-10-03T10:30:00Z', 4750.0, 29750.0);
    INSERT INTO "Payments" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "CancellationReason", "CancelledAt", "CancelledBy", "CancelledByRole", "CancelledByUserId", "ClientId", "ConfirmedAt", "ConfirmedBy", "Country", "CreatedAt", "CreatedBy", "CreatedByUserId", "Currency", "DeletedAt", "DeletedBy", "DepositProofUrl", "ExchangeRate", "ExternalReference", "FailureReason", "IdempotencyKey", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Origin", "PayerName", "PayerTaxId", "PaymentDate", "PaymentMethod", "PaymentMethodCode", "PaymentNumber", "PaymentType", "ProviderKey", "ProviderReference", "ProviderTransactionId", "ReceiptNumber", "RedirectUrl", "RequestFingerprint", "SlipIssuedAt", "SlipNumber", "Status", "StatusChangedAt", "TaxAmount", "TotalAmount")
    VALUES ('55555555-000b-000b-000b-000000000017', NULL, 60000.0, '11111111-0007-0007-0007-000000000018', NULL, NULL, NULL, NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000030', TIMESTAMPTZ '2026-10-01T14:00:00Z', 'KHIPU_WEBHOOK', 'CL', TIMESTAMPTZ '2026-10-01T13:57:00Z', 'd4e5f6a7-0004-0004-0004-000000000030', 'd4e5f6a7-0004-0004-0004-000000000030', 'CLP', NULL, NULL, NULL, NULL, 'PAY-20261001-D4E5F6A7', NULL, NULL, NULL, NULL, NULL, 'Cart', 'Agencia Marítima del Pacífico Ltda', '96555444-3', TIMESTAMPTZ '2026-10-01T14:00:00Z', 'KHIPU', 'KHIPU', 'PAY-20261001-D4E5F6A7', 'Cart', 'Khipu', 'DUMMY-KHIPU-PAY-20261001-D4E5F6A7', 'KHP-TXN-8813901', 'RCP-20261001-E8F9A0B1', NULL, NULL, NULL, NULL, 'Confirmed', TIMESTAMPTZ '2026-10-01T14:00:00Z', 11400.0, 71400.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000009', NULL, 47800.0, NULL, NULL, '11111111-0007-0007-0007-000000000001', 'Comercio al por mayor', 'Av. Libertad 1405, Viña del Mar', 'facturacion@comercialaustral.cl', 'Comercial Austral SpA', '77888999-1', 'HLCUVAL250100123', 'HLCUBKG2501001', NULL, 'REINVOICING', NULL, NULL, 'CL', TIMESTAMPTZ '2026-10-05T15:40:00Z', 'demo@importadorademo.cl', 'CLP', 'IAO_REINVOICING', 'ffffffff-0022-0022-0022-000000000001', NULL, NULL, NULL, '{"invoiceNumber":"100198","reason":"La mercancía fue vendida a Comercial Austral SpA antes del retiro; la factura debe emitirse a su nombre."}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000010', NULL, NULL, '{"pricingMode":"Tariff","chargeConceptCode":"REINVOICING","requiresPayment":true,"amount":47800,"taxAmount":4750,"totalAmount":52550,"currency":"CLP","taxRate":19,"quantity":1,"tierUnit":null,"measuredUnits":null,"timing":"NotApplicable","milestoneAt":null,"milestoneSource":null,"tariffId":"c7f2abc0-29ae-1d0b-d957-ea4707a567b0","tariffCode":null,"tariffSource":"PORTAL","isExempt":false,"exemptionReference":null,"excludedContainers":[],"lines":[{"containerNumber":null,"containerType":null,"amount":25000,"tariffCode":null,"breakdown":[]},{"containerNumber":null,"containerType":null,"amount":22800,"tariffCode":"VAT_LOSS","breakdown":[]}],"sourceCharges":[],"timeZone":"America/Santiago","quotedAt":"2026-10-05T16:00:00Z"}', 1, TIMESTAMPTZ '2026-10-05T16:00:00Z', NULL, 'SRV-20261005-5E1A0009', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'PendingPayment', TIMESTAMPTZ '2026-10-05T16:00:00Z', TIMESTAMPTZ '2026-10-05T16:00:00Z', TIMESTAMPTZ '2026-10-05T16:00:00Z', NULL, 'c7f2abc0-29ae-1d0b-d957-ea4707a567b0', 'PORTAL', 4750.0, NULL, 4, 'NotApplicable', 52550.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000005', NULL, '11111111-0007-0007-0007-000000000018', 'HLCUSAI260701810', 'HLCUBKG2607181', 'HLXU3071801', NULL, 'application/pdf', 'CL', TIMESTAMPTZ '2026-10-01T14:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'RGO-20261001-1A2B3C4D', 'GateOutAdvanceReceipt', 'recibo-anticipo-gate-out-RGO-20261001-1A2B3C4D.pdf', 'GateOutAdvanceReceipt:0745ba3a-34ac-c833-5db6-bf116971a424', TIMESTAMPTZ '2026-10-01T14:00:00Z', NULL, NULL, 'c3d4e5f6-0003-0003-0003-000000000030', NULL, NULL, NULL, 'Seed', '0745ba3a-34ac-c833-5db6-bf116971a424', '55555555-000b-000b-000b-000000000017', NULL, TIMESTAMPTZ '2036-10-01T14:00:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Recibo de pago anticipado - Gate Out","subtitle":"Pago recibido antes de la emisi\u00F3n de la factura","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"RGO-20261001-1A2B3C4D","issuedAt":"2026-10-01T14:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260701810"},{"label":"Booking","value":"HLCUBKG2607181"},{"label":"Operaci\u00F3n","value":"Exportaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Valparaiso Express / 2610N"},{"label":"Ruta","value":"San Antonio (CLSAI) - Callao (PECLL)"}],"sections":[{"heading":"Pagador","fields":[{"label":"Raz\u00F3n social","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"},{"label":"RUT / NIT","value":"96555444-3"}],"table":null,"paragraphs":null},{"heading":"Pago","fields":[{"label":"Concepto","value":"Gate Out - 40HC (San Antonio)"},{"label":"Monto del cargo","value":"71.400,00 CLP"},{"label":"Monto pagado","value":"71.400,00 CLP"},{"label":"Tipo de cambio","value":null},{"label":"RUT de facturaci\u00F3n","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda (96555444-3)"},{"label":"Pago","value":"PAY-20261001-D4E5F6A7"},{"label":"Comprobante de pago","value":"RCP-20261001-E8F9A0B1"},{"label":"Medio de pago","value":"KHIPU"},{"label":"Fecha de pago","value":"01-10-2026 11:00 (America/Santiago)"}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3071801","40HC","SL-071801","24.800,00","OnBoard"]],"numericColumns":[3]},"paragraphs":null},{"heading":"Vinculaci\u00F3n con la factura","fields":null,"table":null,"paragraphs":["La factura del Gate Out se emite despu\u00E9s del zarpe de la nave y se vincula a este recibo.","El cargo pagado con este recibo no vuelve a cobrarse al cliente."]}],"verificationCode":"CBA5-6DA7-95A4-8C6B","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', NULL, NULL, NULL, 'CBA5-6DA7-95A4-8C6B');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('1b202914-40c5-8096-4927-353dddf8200a', '11111111-0007-0007-0007-000000000019', 'c3d4e5f6-0003-0003-0003-000000000060', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Consignee', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('5055f8f3-4141-5a05-5c15-fa5d8e322379', '11111111-0007-0007-0007-000000000018', 'c3d4e5f6-0003-0003-0003-000000000030', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'CustomsAgency', 'Seed');
    INSERT INTO "ShipmentRoles" ("Id", "BillOfLadingId", "ClientId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Role", "Source")
    VALUES ('7d6585a0-7050-e8d6-4d34-3c0389d1eef6', '11111111-0007-0007-0007-000000000018', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, NULL, 'Shipper', 'Seed');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "InvoiceReissues" ("Id", "AcceptanceExpiresAt", "AcceptanceRequestedAt", "AcceptanceStatus", "AcceptanceTokenHash", "AcceptedAt", "AcceptedByName", "AcceptedByTaxId", "AcceptedFromAddress", "AcceptorEmail", "Currency", "DeclineReason", "DeclinedAt", "ExchangeRate", "FeeAmount", "FeeTaxAmount", "IssuedAt", "NewInvoiceId", "OriginalInvoiceId", "OriginalLegalName", "OriginalSiiNumber", "OriginalSourceNumber", "OriginalTaxId", "ServiceRequestId", "VatLossAmount")
    VALUES ('ffffffff-0023-0023-0023-000000000001', TIMESTAMPTZ '2026-10-31T03:00:00Z', TIMESTAMPTZ '2026-10-05T16:00:00Z', 'Pending', '3b238dd0c85c32dc19e44d7fe5588e575b17b19b569c99505e23329dafdb2bfe', NULL, NULL, NULL, NULL, 'facturacion@comercialaustral.cl', 'CLP', NULL, NULL, NULL, 25000.0, 4750.0, NULL, NULL, 'ffffffff-0017-0017-0017-000000000002', 'Importadora Demo SpA', '100198', 'HL-CL-2026-003987', '76123456-7', 'ffffffff-0021-0021-0021-000000000009', 22800.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('0745ba3a-34ac-c833-5db6-bf116971a424', NULL, 60000.0, '11111111-0007-0007-0007-000000000018', 'Agencia Marítima del Pacífico Ltda', '96555444-3', 'HLCUSAI260701810', 'HLCUBKG2607181', 'GATE_OUT', 'CLP', 'Gate Out - 40HC (San Antonio)', NULL, 'LocalCharge', NULL, 71400.0, 'CLP', '55555555-000b-000b-000b-000000000017', TIMESTAMPTZ '2026-10-01T14:00:00Z', '33333333-0009-0009-0009-000000000034', 11400.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('aab31ddd-35ff-069a-d97b-e746504c1cf4', NULL, 25000.0, '11111111-0007-0007-0007-000000000019', 'Distribuidora Andes Crédito SpA', '76000002-2', 'HLCUVAP260601930', 'HLCUBKG2606193', 'ISPS', 'CLP', 'ISPS', NULL, 'LocalCharge', NULL, 29750.0, 'CLP', '55555555-000b-000b-000b-000000000016', TIMESTAMPTZ '2026-10-03T10:30:00Z', '33333333-0009-0009-0009-000000000031', 4750.0);
    INSERT INTO "PaymentDetails" ("Id", "AccessGrantId", "Amount", "BillOfLadingId", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "ConceptType", "Currency", "Description", "ExchangeRate", "ItemType", "OnBehalfOfClientId", "OriginalAmount", "OriginalCurrency", "PaymentId", "ReleasedAt", "SourceId", "TaxAmount")
    VALUES ('db2bf9c7-ceb5-f5a2-e768-8501f7664a50', NULL, 45000.0, '11111111-0007-0007-0007-000000000019', 'Distribuidora Andes Crédito SpA', '76000002-2', 'HLCUVAP260601930', 'HLCUBKG2606193', 'BL_FEE', 'CLP', 'BL Documentation Fee (import)', NULL, 'LocalCharge', NULL, 53550.0, 'CLP', '55555555-000b-000b-000b-000000000015', TIMESTAMPTZ '2026-10-02T13:00:00Z', '33333333-0009-0009-0009-000000000030', 8550.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('0e2ffe61-7b7a-16d8-e1f5-2abef222143b', TIMESTAMPTZ '2026-10-02T12:58:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000015', 'Initiated in BancoChile', 'Processing');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('1af9cc34-5b6c-90f4-9ca9-19f7778bdc57', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'credito@distribuidoraandes.cl', 'd4e5f6a7-0004-0004-0004-000000000060', 'Pending', '55555555-000b-000b-000b-000000000016', 'Imputed to the credit line (M5-10)', 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('3104f716-436d-9983-05a7-5c405ba0da9c', TIMESTAMPTZ '2026-10-02T13:00:00Z', 'BANCOCHILE_WEBHOOK', NULL, 'Processing', '55555555-000b-000b-000b-000000000015', NULL, 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('37ca6d78-0244-c1a5-6957-90caa071b341', TIMESTAMPTZ '2026-10-01T13:57:00Z', 'agente@maritimpacifico.cl', 'd4e5f6a7-0004-0004-0004-000000000030', NULL, '55555555-000b-000b-000b-000000000017', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('50ab39f7-389c-6890-6be1-b75d01f3f8db', TIMESTAMPTZ '2026-10-03T10:30:00Z', 'credito@distribuidoraandes.cl', 'd4e5f6a7-0004-0004-0004-000000000060', NULL, '55555555-000b-000b-000b-000000000016', NULL, 'Pending');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('9528ce39-c13b-df80-2b2d-4c89e9bed794', TIMESTAMPTZ '2026-10-01T14:00:00Z', 'KHIPU_WEBHOOK', NULL, 'Processing', '55555555-000b-000b-000b-000000000017', NULL, 'Confirmed');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('a6a9cbb7-07b5-921b-cd4e-2115cf99b383', TIMESTAMPTZ '2026-10-01T13:57:00Z', 'SYSTEM', NULL, 'Pending', '55555555-000b-000b-000b-000000000017', 'Initiated in Khipu', 'Processing');
    INSERT INTO "PaymentStatusChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "FromStatus", "PaymentId", "Reason", "ToStatus")
    VALUES ('a6d6c0cc-1fac-a63b-8645-66bc1026ac62', TIMESTAMPTZ '2026-10-02T12:58:00Z', 'credito@distribuidoraandes.cl', 'd4e5f6a7-0004-0004-0004-000000000060', NULL, '55555555-000b-000b-000b-000000000015', NULL, 'Pending');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ServiceRequestAttachments" ("Id", "ContentType", "FieldKey", "FileName", "ServiceRequestId", "SizeBytes", "StorageKey", "UploadedAt", "UploadedBy", "UploadedByUserId")
    VALUES ('754b238a-70ca-d8d1-1d67-0d0da9be0fb5', 'application/pdf', 'newCompanyApproval', 'aprobacion-comercial-austral.pdf', 'ffffffff-0021-0021-0021-000000000009', 48213, 'seed/service-requests/aprobacion-comercial-austral.pdf', TIMESTAMPTZ '2026-10-05T15:50:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ServiceRequestCharges" ("Id", "Generated", "LocalChargeId", "ServiceRequestId")
    VALUES ('4809a14d-d8fc-9a8a-b0b6-14a1930f0b38', TRUE, '33333333-0009-0009-0009-000000000036', 'ffffffff-0021-0021-0021-000000000009');
    INSERT INTO "ServiceRequestCharges" ("Id", "Generated", "LocalChargeId", "ServiceRequestId")
    VALUES ('d4adb761-1298-f523-3999-af07756bdf9c', TRUE, '33333333-0009-0009-0009-000000000035', 'ffffffff-0021-0021-0021-000000000009');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('2037c661-6f62-04e0-5e7a-cc8bc21a8e33', 'System', 'SYSTEM', NULL, 'Submitted', 'Total 52550 CLP. La factura se emite con el pago y la aceptación de la nueva razón social.', TIMESTAMPTZ '2026-10-05T16:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000009', 'PendingPayment');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('837acaa2-8b35-7e99-4adf-08cb72018d26', 'System', 'SYSTEM', NULL, 'PendingPayment', 'Enlace de aceptación enviado a facturacion@comercialaustral.cl.', TIMESTAMPTZ '2026-10-05T16:00:00Z', 4, 'ffffffff-0021-0021-0021-000000000009', 'PendingPayment');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('85d9e28f-8b9f-a6ab-6658-bf2b79378585', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Draft', NULL, TIMESTAMPTZ '2026-10-05T16:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000009', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('8bf5b0b1-b37c-7ad0-ae50-f7427180845c', 'Client', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'Refacturación de la factura 100198.', TIMESTAMPTZ '2026-10-05T15:40:00Z', 1, 'ffffffff-0021-0021-0021-000000000009', 'Draft');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('c016e13b-8056-1abc-4c0a-b0294e037fbd', 'System', NULL, 'Issued', TIMESTAMPTZ '2026-10-01T14:00:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000030', NULL, 'ffffffff-0018-0018-0018-000000000005', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_CustomerInvoices_SupersededByInvoiceId" ON "CustomerInvoices" ("SupersededByInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_ChargeSettlements_BillOfLadingId_ConceptCode_Status" ON "ChargeSettlements" ("BillOfLadingId", "ConceptCode", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_ChargeSettlements_ItemType_SourceId" ON "ChargeSettlements" ("ItemType", "SourceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_ChargeSettlements_MatchedInvoiceId" ON "ChargeSettlements" ("MatchedInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_ChargeSettlements_PayerOrganizationId" ON "ChargeSettlements" ("PayerOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE UNIQUE INDEX "IX_ChargeSettlements_PaymentDetailId" ON "ChargeSettlements" ("PaymentDetailId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE UNIQUE INDEX "IX_CreditImputationRules_Country_ConceptCode" ON "CreditImputationRules" ("Country", "ConceptCode") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_DepositProofs_PaymentId_UploadedAt" ON "DepositProofs" ("PaymentId", "UploadedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_DepositProofs_Status_UploadedAt" ON "DepositProofs" ("Status", "UploadedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE UNIQUE INDEX "IX_InvoiceReissues_AcceptanceTokenHash" ON "InvoiceReissues" ("AcceptanceTokenHash") WHERE "AcceptanceTokenHash" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE INDEX "IX_InvoiceReissues_OriginalInvoiceId" ON "InvoiceReissues" ("OriginalInvoiceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    CREATE UNIQUE INDEX "IX_InvoiceReissues_ServiceRequestId" ON "InvoiceReissues" ("ServiceRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006141619_AddFinanceStatementAndReinvoicing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006141619_AddFinanceStatementAndReinvoicing', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "ActionResolvedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "ActionTargetId" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "ActionType" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "BlNumber" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "EmailSent" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "EntityId" character varying(64);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "EntityReference" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "EntityType" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    ALTER TABLE "Notifications" ADD "Module" character varying(30);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "Announcements" (
        "Id" uuid NOT NULL,
        "TitleEs" character varying(200) NOT NULL,
        "TitleEn" character varying(200) NOT NULL,
        "BodyEs" character varying(4000) NOT NULL,
        "BodyEn" character varying(4000) NOT NULL,
        "Countries" character varying(20) NOT NULL,
        "Operation" character varying(10) NOT NULL,
        "Severity" character varying(20) NOT NULL,
        "ValidFrom" timestamp with time zone NOT NULL,
        "ValidTo" timestamp with time zone,
        "Status" character varying(20) NOT NULL,
        "PublishedAt" timestamp with time zone,
        "PublishedBy" character varying(256),
        "UnpublishedAt" timestamp with time zone,
        "UnpublishedBy" character varying(256),
        "NotifyOnPublish" boolean NOT NULL,
        "NotifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_Announcements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "CarrierPreRegistrations" (
        "Id" uuid NOT NULL,
        "CarrierOrganizationId" uuid NOT NULL,
        "CarrierUserId" uuid NOT NULL,
        "RequestedByOrganizationId" uuid NOT NULL,
        "RequestedByUserId" uuid,
        "RequestedBy" character varying(256) NOT NULL,
        "LegalName" character varying(200) NOT NULL,
        "TaxId" character varying(20) NOT NULL,
        "Email" character varying(256) NOT NULL,
        "Country" character varying(5) NOT NULL,
        "DurationDays" integer,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "InvitationSentAt" timestamp with time zone,
        "ActivatedAt" timestamp with time zone,
        CONSTRAINT "PK_CarrierPreRegistrations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "ContactListChanges" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ReportType" character varying(40) NOT NULL,
        "PreviousEmails" character varying(6000),
        "NewEmails" character varying(6000) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "SourceReference" character varying(100),
        "ErrorCode" character varying(100),
        "ChangedByUserId" uuid,
        "ChangedBy" character varying(256) NOT NULL,
        "ChangedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ContactListChanges" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "CounterRecords" (
        "Id" uuid NOT NULL,
        "BillOfLadingId" uuid NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "Country" character varying(5) NOT NULL,
        "ExchangeDate" date,
        "HblReceived" boolean NOT NULL,
        "HblReceivedAt" date,
        "Deconsolidated" boolean NOT NULL,
        "DeconsolidatedAt" date,
        "Notes" character varying(500),
        "SyncStatus" character varying(20) NOT NULL,
        "SyncedAt" timestamp with time zone,
        "SyncError" character varying(100),
        "SourceReference" character varying(100),
        "RecordedByUserId" uuid,
        "RecordedBy" character varying(256) NOT NULL,
        "RecordedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_CounterRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CounterRecords_BillsOfLading_BillOfLadingId" FOREIGN KEY ("BillOfLadingId") REFERENCES "BillsOfLading" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "GuideDefinitions" (
        "Id" uuid NOT NULL,
        "Code" character varying(60) NOT NULL,
        "NameEs" character varying(150) NOT NULL,
        "NameEn" character varying(150) NOT NULL,
        "DescriptionEs" character varying(500),
        "DescriptionEn" character varying(500),
        "Route" character varying(200) NOT NULL,
        "Audience" character varying(20) NOT NULL,
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "Version" integer NOT NULL,
        "StepsJson" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_GuideDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "ImpersonationSessions" (
        "Id" uuid NOT NULL,
        "ActorUserId" uuid NOT NULL,
        "ActorEmail" character varying(256) NOT NULL,
        "SubjectUserId" uuid NOT NULL,
        "SubjectEmail" character varying(256) NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "OrganizationName" character varying(200) NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone,
        "EndReason" character varying(20),
        "DurationSeconds" integer,
        "RequestCount" integer NOT NULL,
        "BlockedCount" integer NOT NULL,
        "SourceAddress" character varying(64),
        CONSTRAINT "PK_ImpersonationSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "NotificationPreferences" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "NotificationType" character varying(40) NOT NULL,
        "EmailEnabled" boolean NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotificationPreferences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "OrganizationParentLinks" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ParentOrganizationId" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "VisibilityEnabled" boolean NOT NULL,
        "VisibilityChangedAt" timestamp with time zone,
        "VisibilityChangedBy" character varying(256),
        "RequestedAt" timestamp with time zone NOT NULL,
        "RequestedByUserId" uuid,
        "RequestedBy" character varying(256) NOT NULL,
        "Notes" character varying(500),
        "DecidedAt" timestamp with time zone,
        "DecidedBy" character varying(256),
        "DecisionNotes" character varying(500),
        "EndedAt" timestamp with time zone,
        "EndedBy" character varying(256),
        CONSTRAINT "PK_OrganizationParentLinks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE TABLE "UserGuideStates" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "GuideCode" character varying(60) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "GuideVersion" integer NOT NULL,
        "LastStep" integer,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserGuideStates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('05e01777-b203-ab12-9d17-52be09a3061e', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, NULL, '{"preRegistrationId":"ffffffff-0025-0025-0025-000000000001","created":true,"source":"seed"}', 'CarrierPreCreated', 'c3d4e5f6-0003-0003-0003-000000000091', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-05T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('0be019c6-b464-f45c-6eee-4541493e9060', 'cccccccc-0012-0012-0012-000000000003', 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', '11111111-0007-0007-0007-000000000002', 'HLCUVAL250200456', 'HLCUBKG2502004', '{"grantType":"Individual","preCreatedCarrier":true,"status":"PendingActivation","durationDays":90,"source":"seed"}', 'GrantCreated', 'c3d4e5f6-0003-0003-0003-000000000091', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-05T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('25a03bf1-6107-d007-e1bc-24be1b53f3be', NULL, 'c3d4e5f6-0003-0003-0003-000000000001', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', NULL, NULL, NULL, '{"parentLinkId":"ffffffff-0026-0026-0026-000000000001","source":"seed"}', 'ParentLinkApproved', 'c3d4e5f6-0003-0003-0003-000000000090', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('5cb9eac8-2fcd-a6f8-82a7-fb5125b6dc84', NULL, 'c3d4e5f6-0003-0003-0003-000000000010', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, NULL, '{"parentLinkId":"ffffffff-0026-0026-0026-000000000001","visibilityEnabled":true,"source":"seed"}', 'ParentLinkRequested', 'c3d4e5f6-0003-0003-0003-000000000090', 'c3d4e5f6-0003-0003-0003-000000000010', TIMESTAMPTZ '2026-10-05T12:00:00Z', NULL);
    INSERT INTO "AccessAuditEntries" ("Id", "AccessGrantId", "ActorClientId", "ActorEmail", "ActorUserId", "BillOfLadingId", "BlNumber", "BookingNumber", "Details", "EventType", "GranteeClientId", "GrantorClientId", "OccurredAt", "VisibilityWideningId")
    VALUES ('6f216ae0-209c-c092-4099-0043c21b3aa8', NULL, 'c3d4e5f6-0003-0003-0003-000000000020', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, NULL, NULL, '{"parentLinkId":"ffffffff-0026-0026-0026-000000000002","visibilityEnabled":true,"source":"seed"}', 'ParentLinkRequested', 'c3d4e5f6-0003-0003-0003-000000000090', 'c3d4e5f6-0003-0003-0003-000000000020', TIMESTAMPTZ '2026-10-05T15:00:00Z', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "Announcements" ("Id", "BodyEn", "BodyEs", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "NotifiedAt", "NotifyOnPublish", "Operation", "PublishedAt", "PublishedBy", "Severity", "Status", "TitleEn", "TitleEs", "UnpublishedAt", "UnpublishedBy", "ValidFrom", "ValidTo")
    VALUES ('ffffffff-0027-0027-0027-000000000001', 'From October 1st the San Antonio empty depot receives returns Monday to Saturday from 08:00 to 20:00. Schedule the Gate In in advance to avoid delays.', 'Desde el 1 de octubre el depósito de vacíos de San Antonio recibe devoluciones de lunes a sábado de 08:00 a 20:00. Programe el Gate In con anticipación para evitar demoras.', 'CL', TIMESTAMPTZ '2026-10-01T11:30:00Z', 'admin@hapag-lloyd.cl', NULL, NULL, NULL, NULL, NULL, FALSE, 'Import', TIMESTAMPTZ '2026-10-01T12:00:00Z', 'admin@hapag-lloyd.cl', 'Important', 'Published', 'New opening hours at the San Antonio empty depot', 'Nuevo horario del depósito de vacíos en San Antonio', NULL, NULL, TIMESTAMPTZ '2026-10-01T03:00:00Z', TIMESTAMPTZ '2027-01-01T03:00:00Z');
    INSERT INTO "Announcements" ("Id", "BodyEn", "BodyEs", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "NotifiedAt", "NotifyOnPublish", "Operation", "PublishedAt", "PublishedBy", "Severity", "Status", "TitleEn", "TitleEs", "UnpublishedAt", "UnpublishedBy", "ValidFrom", "ValidTo")
    VALUES ('ffffffff-0027-0027-0027-000000000002', 'Bolivian export BLs are exchanged at the Arica Counter presenting the original BL and the release letter. The exchange status is shown in the shipment detail.', 'El canje de los BL de exportación de Bolivia se realiza en el Counter de Arica presentando el BL original y la carta de liberación. El estado del canje queda visible en el detalle del embarque.', 'BO', TIMESTAMPTZ '2026-10-02T12:45:00Z', 'admin@hapag-lloyd.cl', NULL, NULL, NULL, NULL, NULL, FALSE, 'Export', TIMESTAMPTZ '2026-10-02T13:00:00Z', 'admin@hapag-lloyd.cl', 'Info', 'Published', 'Bolivian export BL exchange in Arica', 'Canje de BL de exportación boliviana en Arica', NULL, NULL, TIMESTAMPTZ '2026-10-02T04:00:00Z', NULL);
    INSERT INTO "Announcements" ("Id", "BodyEn", "BodyEs", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "NotifiedAt", "NotifyOnPublish", "Operation", "PublishedAt", "PublishedBy", "Severity", "Status", "TitleEn", "TitleEs", "UnpublishedAt", "UnpublishedBy", "ValidFrom", "ValidTo")
    VALUES ('ffffffff-0027-0027-0027-000000000003', 'The portal will be unavailable on October 20th between 23:00 and 23:59 (Chile time) for scheduled maintenance.', 'El portal no estará disponible el 20 de octubre entre las 23:00 y las 23:59 (hora de Chile) por mantención programada.', 'CL,BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', NULL, NULL, NULL, NULL, NULL, TRUE, 'Both', NULL, NULL, 'Important', 'Draft', 'Scheduled portal maintenance', 'Mantención programada del portal', NULL, NULL, TIMESTAMPTZ '2026-10-15T03:00:00Z', TIMESTAMPTZ '2026-10-21T03:00:00Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "AuditLogs" ("Id", "Action", "EntityId", "EntityName", "NewValues", "OldValues", "Timestamp", "UserId")
    VALUES ('044990a0-297c-6041-7f72-96fd33ff3f5f', 'Ended', 'ffffffff-0030-0030-0030-000000000001', 'ImpersonationSession', '{"actorUserId":"d4e5f6a7-0004-0004-0004-000000000001","actorEmail":"admin@hapag-lloyd.cl","subjectUserId":"d4e5f6a7-0004-0004-0004-000000000010","subjectEmail":"demo@importadorademo.cl","organizationId":"c3d4e5f6-0003-0003-0003-000000000010","organizationName":"Importadora Demo SpA","details":{"reason":"Manual","durationSeconds":720,"requestCount":6,"blockedCount":1}}', NULL, TIMESTAMPTZ '2026-10-04T15:12:00Z', 'd4e5f6a7-0004-0004-0004-000000000001');
    INSERT INTO "AuditLogs" ("Id", "Action", "EntityId", "EntityName", "NewValues", "OldValues", "Timestamp", "UserId")
    VALUES ('8dd50263-2800-0557-93b9-07a71d223c9b', 'BlockedWrite', 'ffffffff-0030-0030-0030-000000000001', 'ImpersonationSession', '{"actorUserId":"d4e5f6a7-0004-0004-0004-000000000001","actorEmail":"admin@hapag-lloyd.cl","subjectUserId":"d4e5f6a7-0004-0004-0004-000000000010","subjectEmail":"demo@importadorademo.cl","organizationId":"c3d4e5f6-0003-0003-0003-000000000010","organizationName":"Importadora Demo SpA","details":{"method":"POST","path":"/api/v1/cart/items","statusCode":403}}', NULL, TIMESTAMPTZ '2026-10-04T15:07:00Z', 'd4e5f6a7-0004-0004-0004-000000000001');
    INSERT INTO "AuditLogs" ("Id", "Action", "EntityId", "EntityName", "NewValues", "OldValues", "Timestamp", "UserId")
    VALUES ('b92706ba-8ccb-e7ef-266a-5ecab8db678a', 'Started', 'ffffffff-0030-0030-0030-000000000001', 'ImpersonationSession', '{"actorUserId":"d4e5f6a7-0004-0004-0004-000000000001","actorEmail":"admin@hapag-lloyd.cl","subjectUserId":"d4e5f6a7-0004-0004-0004-000000000010","subjectEmail":"demo@importadorademo.cl","organizationId":"c3d4e5f6-0003-0003-0003-000000000010","organizationName":"Importadora Demo SpA","details":{"reason":"Ticket CS-2026-1004","readOnly":true,"allowedActions":[]}}', NULL, TIMESTAMPTZ '2026-10-04T15:00:00Z', 'd4e5f6a7-0004-0004-0004-000000000001');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "CarrierPreRegistrations" ("Id", "ActivatedAt", "CarrierOrganizationId", "CarrierUserId", "Country", "CreatedAt", "DurationDays", "Email", "InvitationSentAt", "LegalName", "RequestedBy", "RequestedByOrganizationId", "RequestedByUserId", "Status", "TaxId")
    VALUES ('ffffffff-0025-0025-0025-000000000001', NULL, 'c3d4e5f6-0003-0003-0003-000000000091', 'd4e5f6a7-0004-0004-0004-000000000091', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 90, 'contacto@transportescordillera.cl', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'Transportes Cordillera Ltda.', 'demo@importadorademo.cl', 'c3d4e5f6-0003-0003-0003-000000000010', 'd4e5f6a7-0004-0004-0004-000000000010', 'Pending', '77.123.321-5');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000090', NULL, NULL, TIMESTAMPTZ '2026-10-05T12:00:00Z', NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, 'holding@grupodemo.cl', TRUE, TRUE, 'MC100090', NULL, NULL, 'Grupo Demo Holding S.A.', 'CL,BO', 'Customer', '+56 2 2400 1000', 'Approved', NULL, NULL, '96.700.100-1', 'RUT', NULL, NULL);
    INSERT INTO "Clients" ("Id", "Address", "AgentCode", "ApprovedAt", "ArCheckedAt", "ArCheckedBy", "ArReference", "City", "ClientType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "IsActive", "IsEmailConfirmed", "MatchCode", "ModifiedAt", "ModifiedBy", "Name", "OperatingCountries", "OrganizationType", "Phone", "RegistrationStatus", "RejectedAt", "ReviewNotes", "TaxId", "TaxIdType", "ValidatedAt", "ValidatedBy")
    VALUES ('c3d4e5f6-0003-0003-0003-000000000091', NULL, NULL, NULL, NULL, NULL, NULL, NULL, 'Client', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, 'contacto@transportescordillera.cl', TRUE, FALSE, NULL, NULL, NULL, 'Transportes Cordillera Ltda.', 'CL', 'Carrier', NULL, 'PreCreated', NULL, 'Pre-creado por Importadora Demo SpA (M1-09).', '77.123.321-5', 'RUT', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "ContactListChanges" ("Id", "ChangedAt", "ChangedBy", "ChangedByUserId", "ErrorCode", "NewEmails", "OrganizationId", "PreviousEmails", "ReportType", "SourceReference", "Status")
    VALUES ('ffffffff-0031-0031-0031-000000000001', TIMESTAMPTZ '2026-09-15T13:00:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', NULL, 'finanzas@importadorademo.cl', 'c3d4e5f6-0003-0003-0003-000000000010', 'contabilidad@importadorademo.cl', 'INVOICES', 'P0060-SEED0001', 'Propagated');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "CounterRecords" ("Id", "BillOfLadingId", "BlNumber", "Country", "CreatedAt", "CreatedBy", "Deconsolidated", "DeconsolidatedAt", "DeletedAt", "DeletedBy", "ExchangeDate", "HblReceived", "HblReceivedAt", "ModifiedAt", "ModifiedBy", "Notes", "RecordedAt", "RecordedBy", "RecordedByUserId", "SourceReference", "SyncError", "SyncStatus", "SyncedAt")
    VALUES ('ffffffff-0029-0029-0029-000000000001', '11111111-0007-0007-0007-000000000008', 'HLCUARI260300830', 'BO', TIMESTAMPTZ '2026-10-03T14:00:00Z', 'admin@hapag-lloyd.cl', FALSE, NULL, NULL, NULL, DATE '2026-10-02', TRUE, DATE '2026-10-03', NULL, NULL, NULL, TIMESTAMPTZ '2026-10-03T14:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'CNT-BO-000830', NULL, 'Synced', TIMESTAMPTZ '2026-10-03T14:00:00Z');
    INSERT INTO "CounterRecords" ("Id", "BillOfLadingId", "BlNumber", "Country", "CreatedAt", "CreatedBy", "Deconsolidated", "DeconsolidatedAt", "DeletedAt", "DeletedBy", "ExchangeDate", "HblReceived", "HblReceivedAt", "ModifiedAt", "ModifiedBy", "Notes", "RecordedAt", "RecordedBy", "RecordedByUserId", "SourceReference", "SyncError", "SyncStatus", "SyncedAt")
    VALUES ('ffffffff-0029-0029-0029-000000000002', '11111111-0007-0007-0007-000000000004', 'HLCUARI260100045', 'BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', TRUE, DATE '2026-10-05', NULL, NULL, DATE '2026-10-04', TRUE, DATE '2026-10-04', NULL, NULL, 'Desconsolidado en el depósito de Arica.', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', NULL, 'Integration.Unavailable', 'Failed', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "GuideDefinitions" ("Id", "Audience", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Route", "StepsJson", "Version")
    VALUES ('ffffffff-0028-0028-0028-000000000001', 'Client', 'cart-checkout', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, 'Walks through the unified cart: added charges, billing tax ID, currency, payment method and confirmation.', 'Recorre el carro unificado: cargos agregados, RUT de facturación, moneda, medio de pago y confirmación.', 10, TRUE, NULL, NULL, 'How to pay from the cart', 'Cómo pagar desde el carro', '/cart', '[{"order":1,"route":"/cart","elementKey":"cart.items","titleEs":"Revise los cargos","titleEn":"Review the charges","textEs":"Aqu\u00ED est\u00E1n los cargos que agreg\u00F3 desde sus embarques, agrupados por moneda. Puede quitar los que no pagar\u00E1 ahora.","textEn":"These are the charges you added from your shipments, grouped by currency. You can remove the ones you will not pay now."},{"order":2,"route":"/cart","elementKey":"cart.billing-tax-id","titleEs":"RUT de facturaci\u00F3n","titleEn":"Billing tax ID","textEs":"Indique a qu\u00E9 RUT se emitir\u00E1 la factura de cada cargo. Por defecto es el de su organizaci\u00F3n.","textEn":"Choose the tax ID to be invoiced for each charge. By default it is your organization\u0027s."},{"order":3,"route":"/cart","elementKey":"cart.currency","titleEs":"Moneda de pago","titleEn":"Payment currency","textEs":"Elija la moneda en que pagar\u00E1; si es distinta de la del cargo se usa el tipo de cambio del d\u00EDa.","textEn":"Choose the payment currency; if it differs from the charge currency the day\u0027s exchange rate applies."},{"order":4,"route":"/cart","elementKey":"cart.payment-method","titleEs":"Medio de pago","titleEn":"Payment method","textEs":"Seleccione el medio de pago habilitado para su pa\u00EDs y moneda.","textEn":"Select a payment method enabled for your country and currency."},{"order":5,"route":"/cart","elementKey":"cart.checkout","titleEs":"Pague","titleEn":"Pay","textEs":"Confirme el pago. Recibir\u00E1 el comprobante en la bandeja de notificaciones y en el historial de pagos.","textEn":"Confirm the payment. You will receive the receipt in the notification inbox and in the payment history."}]', 1);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "ImpersonationSessions" ("Id", "ActorEmail", "ActorUserId", "BlockedCount", "DurationSeconds", "EndReason", "EndedAt", "ExpiresAt", "OrganizationId", "OrganizationName", "Reason", "RequestCount", "SourceAddress", "StartedAt", "Status", "SubjectEmail", "SubjectUserId")
    VALUES ('ffffffff-0030-0030-0030-000000000001', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 1, 720, 'Manual', TIMESTAMPTZ '2026-10-04T15:12:00Z', TIMESTAMPTZ '2026-10-04T15:30:00Z', 'c3d4e5f6-0003-0003-0003-000000000010', 'Importadora Demo SpA', 'Ticket CS-2026-1004: el cliente no ve el flete del BL HLCUVAL250200456.', 6, '10.0.0.15', TIMESTAMPTZ '2026-10-04T15:00:00Z', 'Ended', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('0f35358b-a740-0f03-384d-771138ac802d', 'Created', TIMESTAMPTZ '2026-10-03T14:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'ffffffff-0029-0029-0029-000000000001', 'CounterRecord', '{"blNumber":"HLCUARI260300830","country":"BO","exchangeDate":"2026-10-02","hblReceived":true,"hblReceivedAt":"2026-10-03","deconsolidated":false,"deconsolidatedAt":null,"notes":null,"syncStatus":"Pending","sourceReference":null}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('293debcc-96de-bc6a-7028-c71f703ef980', 'Created', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0028-0028-0028-000000000001', 'Guide', '{"code":"cart-checkout","nameEs":"C\u00F3mo pagar desde el carro","nameEn":"How to pay from the cart","route":"/cart","audience":"Client","isActive":true,"displayOrder":10,"version":1,"steps":[{"order":1,"route":"/cart","elementKey":"cart.items","titleEs":"Revise los cargos","titleEn":"Review the charges","textEs":"Aqu\u00ED est\u00E1n los cargos que agreg\u00F3 desde sus embarques, agrupados por moneda. Puede quitar los que no pagar\u00E1 ahora.","textEn":"These are the charges you added from your shipments, grouped by currency. You can remove the ones you will not pay now."},{"order":2,"route":"/cart","elementKey":"cart.billing-tax-id","titleEs":"RUT de facturaci\u00F3n","titleEn":"Billing tax ID","textEs":"Indique a qu\u00E9 RUT se emitir\u00E1 la factura de cada cargo. Por defecto es el de su organizaci\u00F3n.","textEn":"Choose the tax ID to be invoiced for each charge. By default it is your organization\u0027s."},{"order":3,"route":"/cart","elementKey":"cart.currency","titleEs":"Moneda de pago","titleEn":"Payment currency","textEs":"Elija la moneda en que pagar\u00E1; si es distinta de la del cargo se usa el tipo de cambio del d\u00EDa.","textEn":"Choose the payment currency; if it differs from the charge currency the day\u0027s exchange rate applies."},{"order":4,"route":"/cart","elementKey":"cart.payment-method","titleEs":"Medio de pago","titleEn":"Payment method","textEs":"Seleccione el medio de pago habilitado para su pa\u00EDs y moneda.","textEn":"Select a payment method enabled for your country and currency."},{"order":5,"route":"/cart","elementKey":"cart.checkout","titleEs":"Pague","titleEn":"Pay","textEs":"Confirme el pago. Recibir\u00E1 el comprobante en la bandeja de notificaciones y en el historial de pagos.","textEn":"Confirm the payment. You will receive the receipt in the notification inbox and in the payment history."}]}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('3e4a98bd-4067-59ac-66e0-6e5cb4137108', 'Created', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'ffffffff-0027-0027-0027-000000000003', 'Announcement', '{"titleEs":"Mantenci\u00F3n programada del portal","titleEn":"Scheduled portal maintenance","bodyEs":"El portal no estar\u00E1 disponible el 20 de octubre entre las 23:00 y las 23:59 (hora de Chile) por mantenci\u00F3n programada.","bodyEn":"The portal will be unavailable on October 20th between 23:00 and 23:59 (Chile time) for scheduled maintenance.","countries":"CL,BO","operation":"Both","severity":"Important","validFrom":"2026-10-15T03:00:00Z","validTo":"2026-10-21T03:00:00Z","status":"Draft","notifyOnPublish":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a32f168f-b981-c833-76f9-e98191ff9d3f', 'Created', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'ffffffff-0029-0029-0029-000000000002', 'CounterRecord', '{"blNumber":"HLCUARI260100045","country":"BO","exchangeDate":"2026-10-04","hblReceived":true,"hblReceivedAt":"2026-10-04","deconsolidated":true,"deconsolidatedAt":"2026-10-05","notes":"Desconsolidado en el dep\u00F3sito de Arica.","syncStatus":"Pending","sourceReference":null}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('b0e7ddaa-2c3c-3d6f-e8fc-a47e3dc3fe71', 'Created', TIMESTAMPTZ '2026-10-01T11:30:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'ffffffff-0027-0027-0027-000000000001', 'Announcement', '{"titleEs":"Nuevo horario del dep\u00F3sito de vac\u00EDos en San Antonio","titleEn":"New opening hours at the San Antonio empty depot","bodyEs":"Desde el 1 de octubre el dep\u00F3sito de vac\u00EDos de San Antonio recibe devoluciones de lunes a s\u00E1bado de 08:00 a 20:00. Programe el Gate In con anticipaci\u00F3n para evitar demoras.","bodyEn":"From October 1st the San Antonio empty depot receives returns Monday to Saturday from 08:00 to 20:00. Schedule the Gate In in advance to avoid delays.","countries":"CL","operation":"Import","severity":"Important","validFrom":"2026-10-01T03:00:00Z","validTo":"2027-01-01T03:00:00Z","status":"Published","notifyOnPublish":false}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('e5e93f5e-8734-6c2f-751d-09b56c091735', 'Created', TIMESTAMPTZ '2026-10-02T12:45:00Z', 'admin@hapag-lloyd.cl', 'd4e5f6a7-0004-0004-0004-000000000001', 'ffffffff-0027-0027-0027-000000000002', 'Announcement', '{"titleEs":"Canje de BL de exportaci\u00F3n boliviana en Arica","titleEn":"Bolivian export BL exchange in Arica","bodyEs":"El canje de los BL de exportaci\u00F3n de Bolivia se realiza en el Counter de Arica presentando el BL original y la carta de liberaci\u00F3n. El estado del canje queda visible en el detalle del embarque.","bodyEn":"Bolivian export BLs are exchanged at the Arica Counter presenting the original BL and the release letter. The exchange status is shown in the shipment detail.","countries":"BO","operation":"Export","severity":"Info","validFrom":"2026-10-02T04:00:00Z","validTo":null,"status":"Published","notifyOnPublish":false}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "NotificationPreferences" ("Id", "EmailEnabled", "NotificationType", "UpdatedAt", "UserId")
    VALUES ('4728100f-24b0-d70d-8491-616cbedbcff8', FALSE, 'DocumentIssued', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'd4e5f6a7-0004-0004-0004-000000000010');
    INSERT INTO "NotificationPreferences" ("Id", "EmailEnabled", "NotificationType", "UpdatedAt", "UserId")
    VALUES ('85b8a6da-44d9-7264-e037-62ac5754f081', TRUE, 'AnnouncementPublished', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'd4e5f6a7-0004-0004-0004-000000000010');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "Notifications" ("Id", "ActionResolvedAt", "ActionTargetId", "ActionType", "BlNumber", "Body", "CreatedAt", "CreatedBy", "DedupKey", "DeletedAt", "DeletedBy", "EmailSent", "EntityId", "EntityReference", "EntityType", "ModifiedAt", "ModifiedBy", "Module", "ReadAt", "RoleCode", "Title", "Type", "UserId")
    VALUES ('46a9dd3c-e522-7448-02ab-7696e37612eb', NULL, NULL, NULL, NULL, 'Importadora Demo SpA quedó asociada a su organización como filial. Ya puede ver sus BL en el listado de embarques, identificados por organización.', TIMESTAMPTZ '2026-10-05T14:00:00Z', 'SYSTEM', NULL, NULL, NULL, FALSE, 'ffffffff-0026-0026-0026-000000000001', 'Importadora Demo SpA', 'ParentLink', NULL, NULL, 'Organization', NULL, NULL, 'Filial asociada: Importadora Demo SpA', 'ParentLinkApproved', 'd4e5f6a7-0004-0004-0004-000000000090');
    INSERT INTO "Notifications" ("Id", "ActionResolvedAt", "ActionTargetId", "ActionType", "BlNumber", "Body", "CreatedAt", "CreatedBy", "DedupKey", "DeletedAt", "DeletedBy", "EmailSent", "EntityId", "EntityReference", "EntityType", "ModifiedAt", "ModifiedBy", "Module", "ReadAt", "RoleCode", "Title", "Type", "UserId")
    VALUES ('be0c6821-74a8-fb28-c072-2e6dd368194f', NULL, 'ffffffff-0026-0026-0026-000000000002', 'ReviewParentLink', NULL, 'Comercial Altiplano SRL pide asociarse a Grupo Demo Holding S.A. como su empresa matriz. Revise la solicitud en el área de administración.', TIMESTAMPTZ '2026-10-05T15:00:00Z', 'SYSTEM', 'parent-link-requested:ffffffff-0026-0026-0026-000000000002', NULL, NULL, FALSE, 'ffffffff-0026-0026-0026-000000000002', 'Comercial Altiplano SRL', 'ParentLink', NULL, NULL, 'Administration', NULL, 'Administrador', 'Solicitud de empresa matriz: Comercial Altiplano SRL', 'ParentLinkRequested', NULL);
    INSERT INTO "Notifications" ("Id", "ActionResolvedAt", "ActionTargetId", "ActionType", "BlNumber", "Body", "CreatedAt", "CreatedBy", "DedupKey", "DeletedAt", "DeletedBy", "EmailSent", "EntityId", "EntityReference", "EntityType", "ModifiedAt", "ModifiedBy", "Module", "ReadAt", "RoleCode", "Title", "Type", "UserId")
    VALUES ('d5e36d15-3543-4d73-b0fc-af18dbec0030', NULL, 'd4e5f6a7-0004-0004-0004-000000000012', 'ApproveJoinRequest', NULL, 'Sergio Solicitante (solicitud@importadorademo.cl) solicita vincularse a Importadora Demo SpA.', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', 'join-request:d4e5f6a7-0004-0004-0004-000000000012:d4e5f6a7-0004-0004-0004-000000000010', NULL, NULL, FALSE, 'd4e5f6a7-0004-0004-0004-000000000012', 'Sergio Solicitante', 'JoinRequest', NULL, NULL, 'Organization', NULL, NULL, 'Nueva solicitud de vinculación', 'JoinRequestReceived', 'd4e5f6a7-0004-0004-0004-000000000010');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "OrganizationParentLinks" ("Id", "DecidedAt", "DecidedBy", "DecisionNotes", "EndedAt", "EndedBy", "Notes", "OrganizationId", "ParentOrganizationId", "RequestedAt", "RequestedBy", "RequestedByUserId", "Status", "VisibilityChangedAt", "VisibilityChangedBy", "VisibilityEnabled")
    VALUES ('ffffffff-0026-0026-0026-000000000001', TIMESTAMPTZ '2026-10-05T14:00:00Z', 'admin@hapag-lloyd.cl', 'Escritura de constitución del grupo verificada.', NULL, NULL, 'Importadora Demo es filial del grupo.', 'c3d4e5f6-0003-0003-0003-000000000010', 'c3d4e5f6-0003-0003-0003-000000000090', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'demo@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000010', 'Active', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'demo@importadorademo.cl', TRUE);
    INSERT INTO "OrganizationParentLinks" ("Id", "DecidedAt", "DecidedBy", "DecisionNotes", "EndedAt", "EndedBy", "Notes", "OrganizationId", "ParentOrganizationId", "RequestedAt", "RequestedBy", "RequestedByUserId", "Status", "VisibilityChangedAt", "VisibilityChangedBy", "VisibilityEnabled")
    VALUES ('ffffffff-0026-0026-0026-000000000002', NULL, NULL, NULL, NULL, NULL, 'Filial boliviana del grupo.', 'c3d4e5f6-0003-0003-0003-000000000020', 'c3d4e5f6-0003-0003-0003-000000000090', TIMESTAMPTZ '2026-10-05T15:00:00Z', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Pending', TIMESTAMPTZ '2026-10-05T15:00:00Z', 'demo@altiplano.bo', TRUE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('19a63320-87df-605a-721c-5740ad587879', 'counter.manage', 'counter.manage');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('4d5a28c9-edc7-c3ae-e185-e86340b1787a', 'announcements.manage', 'announcements.manage');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('83a1a2ee-e56f-cd4e-d5f5-47e46234fa75', 'admin-area.access', 'admin-area.access');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('91d410af-6706-1491-a2c4-b06a18c8ec72', 'transactions-report.view', 'transactions-report.view');
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('fb1e2d86-80da-a74a-d175-d17668f16149', 'impersonation.use', 'impersonation.use');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "AccessGrants" ("Id", "ActionCodes", "BillOfLadingId", "BookingNumber", "CeilingActionCodes", "CreatedAt", "CreatedBy", "DefaultGranteeId", "DeletedAt", "DeletedBy", "DurationDays", "EndReason", "EndedAt", "EndedByUserId", "GrantType", "GrantedByUserId", "GranteeClientId", "GrantorClientId", "GrantorRole", "IntendedRole", "IsMandate", "ModifiedAt", "ModifiedBy", "ParentGrantId", "Status", "TermsAcceptedAt", "TermsAcceptedByUserId", "TermsVersion", "ValidFrom", "ValidTo", "ValidityType")
    VALUES ('cccccccc-0012-0012-0012-000000000003', NULL, '11111111-0007-0007-0007-000000000002', 'HLCUBKG2502004', 'access-audit.view,access-validity.set,access.grant,access.revoke,account-statement.view,bl-copy-unvalued.request,bl-copy-valued.request,bl-issuance.view,data-visibility.extend,drop-off.request,early-booking-access.grant,freight-certificate.generate,freight.pay,import-demurrage.pay,import-depot.view,invoices-billed.view,local-charges-mandatory.pay,local-charges-on-demand.pay,no-debt-certificate.download,open-access.enable,release-letter.generate,release-requirements.view,shipment.view,tatc.download,third-party-query.notify,tracking.view,transshipment-certificate.generate,warehouse-change.request', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 90, NULL, NULL, NULL, 'Individual', 'd4e5f6a7-0004-0004-0004-000000000010', 'c3d4e5f6-0003-0003-0003-000000000091', 'c3d4e5f6-0003-0003-0003-000000000010', 'Customer', NULL, FALSE, NULL, NULL, NULL, 'PendingActivation', NULL, NULL, NULL, TIMESTAMPTZ '2026-10-05T12:00:00Z', NULL, 'Duration');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('243b1584-1695-7ee6-9356-12ca13425aaf', '83a1a2ee-e56f-cd4e-d5f5-47e46234fa75', '3bb3e862-e9d1-77dd-9c69-b1c097511556');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('8d7f6b98-d232-0652-9708-e433e7f80ba1', '91d410af-6706-1491-a2c4-b06a18c8ec72', '3bb3e862-e9d1-77dd-9c69-b1c097511556');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('ce8e714c-3cbb-8ef0-770c-dd6d3a6377c8', '83a1a2ee-e56f-cd4e-d5f5-47e46234fa75', 'e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a');
    INSERT INTO "RolePermissions" ("Id", "PermissionId", "RoleId")
    VALUES ('fd71bfe7-2fba-41ab-5548-ccdf4539f4df', '19a63320-87df-605a-721c-5740ad587879', 'e5fafd0d-29c5-637b-6ab4-d7abc98e1c8a');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000090', 'c3d4e5f6-0003-0003-0003-000000000090', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'holding@grupodemo.cl', NULL, NULL, 'Hilda', TRUE, FALSE, NULL, 'Holding', NULL, NULL, 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', NULL, NULL, NULL, NULL, NULL, 'Client', 'holding@grupodemo.cl');
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000091', 'c3d4e5f6-0003-0003-0003-000000000091', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'contacto@transportescordillera.cl', NULL, NULL, 'Tomás', TRUE, FALSE, NULL, 'Cordillera', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'demo@importadorademo.cl', 'Active', NULL, NULL, '$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca', 'CARRIER-DEMO-INVITE-2026-10', TIMESTAMPTZ '2026-12-31T23:59:00Z', NULL, NULL, NULL, 'Client', 'contacto@transportescordillera.cl');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('3cc0937d-04de-51c0-4c38-f3a240ebcd8a', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000091');
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('6e26ae2d-56bd-69c9-28fa-cc0c9c248a42', 'e200b49e-343b-36a4-fbcc-e10fa786728c', 'OrgAdmin', 'd4e5f6a7-0004-0004-0004-000000000090');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_Notifications_ActionType_ActionTargetId" ON "Notifications" ("ActionType", "ActionTargetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_Announcements_Status_ValidFrom" ON "Announcements" ("Status", "ValidFrom");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_CarrierPreRegistrations_CarrierOrganizationId_RequestedByOr~" ON "CarrierPreRegistrations" ("CarrierOrganizationId", "RequestedByOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_CarrierPreRegistrations_RequestedByOrganizationId" ON "CarrierPreRegistrations" ("RequestedByOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_ContactListChanges_OrganizationId_ChangedAt" ON "ContactListChanges" ("OrganizationId", "ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_CounterRecords_BillOfLadingId" ON "CounterRecords" ("BillOfLadingId") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_CounterRecords_BlNumber" ON "CounterRecords" ("BlNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_CounterRecords_SyncStatus" ON "CounterRecords" ("SyncStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_GuideDefinitions_Code" ON "GuideDefinitions" ("Code") WHERE "DeletedAt" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_ImpersonationSessions_ActorUserId_Status" ON "ImpersonationSessions" ("ActorUserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_ImpersonationSessions_OrganizationId_StartedAt" ON "ImpersonationSessions" ("OrganizationId", "StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_ImpersonationSessions_StartedAt" ON "ImpersonationSessions" ("StartedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_NotificationPreferences_UserId_NotificationType" ON "NotificationPreferences" ("UserId", "NotificationType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_OrganizationParentLinks_OrganizationId" ON "OrganizationParentLinks" ("OrganizationId") WHERE "Status" IN ('Pending', 'Active');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE INDEX "IX_OrganizationParentLinks_ParentOrganizationId_Status" ON "OrganizationParentLinks" ("ParentOrganizationId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    CREATE UNIQUE INDEX "IX_UserGuideStates_UserId_GuideCode" ON "UserGuideStates" ("UserId", "GuideCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006155728_AddAdministrationAndNotificationInbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006155728_AddAdministrationAndNotificationInbox', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE TABLE "ApiClients" (
        "Id" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "TechnicalUserId" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "Scopes" character varying(200) NOT NULL,
        "RateLimitPerMinute" integer NOT NULL,
        "SignatoryName" character varying(200),
        "SignatoryTaxId" character varying(30),
        "SignatoryPosition" character varying(100),
        "SignatoryEmail" character varying(256),
        "TechnicalContactEmail" character varying(256),
        "Notes" character varying(1000),
        "RevokedAt" timestamp with time zone,
        "RevokedBy" character varying(256),
        "RevocationReason" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(256),
        "DeletedAt" timestamp with time zone,
        "DeletedBy" character varying(256),
        CONSTRAINT "PK_ApiClients" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApiClients_Clients_OrganizationId" FOREIGN KEY ("OrganizationId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ApiClients_Users_TechnicalUserId" FOREIGN KEY ("TechnicalUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE TABLE "AssistantDocumentDeliveries" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "MessageId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "UserEmail" character varying(256),
        "OrganizationId" uuid,
        "OnBehalfOfOrganizationId" uuid,
        "BillOfLadingId" uuid NOT NULL,
        "BlNumber" character varying(50) NOT NULL,
        "DocumentKind" character varying(30) NOT NULL,
        "DocumentType" character varying(50),
        "DocumentId" uuid NOT NULL,
        "DocumentNumber" character varying(50) NOT NULL,
        "DeliveredAt" timestamp with time zone NOT NULL,
        "DownloadedAt" timestamp with time zone,
        "DownloadCount" integer NOT NULL,
        CONSTRAINT "PK_AssistantDocumentDeliveries" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AssistantDocumentDeliveries_AssistantSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "AssistantSessions" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE TABLE "ReleaseLetterRequests" (
        "Id" uuid NOT NULL,
        "ServiceRequestId" uuid NOT NULL,
        "LegalEntityType" character varying(20) NOT NULL,
        "CarrierOrganizationId" uuid,
        "TatcAvailableAtSubmission" boolean NOT NULL,
        "TatcStatusAtSubmission" character varying(20),
        "TatcErrorAtSubmission" character varying(100),
        "TatcCheckedAtSubmission" timestamp with time zone NOT NULL,
        "TatcSnapshotAtSubmission" text,
        "TatcAvailableAtApproval" boolean,
        "TatcStatusAtApproval" character varying(20),
        "TatcErrorAtApproval" character varying(100),
        "TatcCheckedAtApproval" timestamp with time zone,
        "TatcSnapshotAtApproval" text,
        "DocumentId" uuid,
        CONSTRAINT "PK_ReleaseLetterRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ReleaseLetterRequests_ServiceRequests_ServiceRequestId" FOREIGN KEY ("ServiceRequestId") REFERENCES "ServiceRequests" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE TABLE "ApiClientKeys" (
        "Id" uuid NOT NULL,
        "ApiClientId" uuid NOT NULL,
        "Prefix" character varying(20) NOT NULL,
        "KeyHash" character varying(64) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(256) NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "RevokedAt" timestamp with time zone,
        "RevokedBy" character varying(256),
        "LastUsedAt" timestamp with time zone,
        CONSTRAINT "PK_ApiClientKeys" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApiClientKeys_ApiClients_ApiClientId" FOREIGN KEY ("ApiClientId") REFERENCES "ApiClients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE TABLE "ApiClientRequests" (
        "Id" uuid NOT NULL,
        "ApiClientId" uuid NOT NULL,
        "ApiClientKeyId" uuid,
        "OrganizationId" uuid NOT NULL,
        "TechnicalUserId" uuid NOT NULL,
        "Operation" character varying(40) NOT NULL,
        "Method" character varying(10) NOT NULL,
        "Path" character varying(300) NOT NULL,
        "IdempotencyKey" character varying(100),
        "RequestHash" character varying(64),
        "Outcome" character varying(20) NOT NULL,
        "StatusCode" integer,
        "ErrorCode" character varying(100),
        "ResponseJson" text,
        "TargetType" character varying(40),
        "TargetId" uuid,
        "TargetReference" character varying(100),
        "BlNumber" character varying(50),
        "SourceAddress" character varying(64),
        "ReceivedAt" timestamp with time zone NOT NULL,
        "CompletedAt" timestamp with time zone,
        "DurationMs" integer,
        CONSTRAINT "PK_ApiClientRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ApiClientRequests_ApiClients_ApiClientId" FOREIGN KEY ("ApiClientId") REFERENCES "ApiClients" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('7bd6dee1-8b2d-df8c-222b-3ccd1227bf8c', 'Service', 'FREIGHT_CERTIFICATE', 'BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, 330, TRUE, NULL, NULL, 'Certificado de flete', FALSE, FALSE);
    INSERT INTO "ChargeConcepts" ("Id", "Category", "Code", "Countries", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayOrder", "IsActive", "ModifiedAt", "ModifiedBy", "Name", "NexusExemptible", "NexusTariff")
    VALUES ('b8406cc8-7a30-756b-b1d6-9509cfe805f4', 'Service', 'RELEASE_LETTER', 'BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, 340, TRUE, NULL, NULL, 'Carta de liberación y desconsolidado', FALSE, FALSE);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('5b6d64e3-30e7-2094-9668-48c5b0f4624f', 'Created', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0022-0022-0022-000000000003', 'ServiceDefinition', '{"code":"RELEASE_LETTER","nameEs":"Carta de liberaci\u00F3n y desconsolidado","nameEn":"Release and deconsolidation letter","descriptionEs":"Carta de liberaci\u00F3n y desconsolidado de las unidades seleccionadas, importaci\u00F3n de Bolivia (M6-08, BO-IMP-11): datos del consignatario seg\u00FAn el tipo de sociedad y del transportista; el TATC de las unidades se registra al enviar y al aprobar; la aprueba Customer Service y la carta se emite al aprobarse. Sin cobro. Se solicita desde los documentos del embarque.","descriptionEn":"Release and deconsolidation letter for the selected units, Bolivia imports (M6-08): consignee data by legal entity type and carrier data; the units\u0027 TATC is recorded on submission and approval; Customer Service approves it and the letter is issued on approval. No charge. Requested from the shipment documents.","operations":"IMPORT","countries":"BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":true,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022containers\u0022,\u0022labelEs\u0022:\u0022Contenedores\u0022,\u0022labelEn\u0022:\u0022Containers\u0022,\u0022type\u0022:\u0022containers\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022legalEntityType\u0022,\u0022labelEs\u0022:\u0022Tipo de sociedad\u0022,\u0022labelEn\u0022:\u0022Legal entity type\u0022,\u0022type\u0022:\u0022select\u0022,\u0022required\u0022:true,\u0022options\u0022:[{\u0022value\u0022:\u0022COMPANY\u0022,\u0022labelEs\u0022:\u0022Empresa\u0022,\u0022labelEn\u0022:\u0022Company\u0022},{\u0022value\u0022:\u0022NATURAL_PERSON\u0022,\u0022labelEs\u0022:\u0022Persona natural\u0022,\u0022labelEn\u0022:\u0022Natural person\u0022}],\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022consigneeName\u0022,\u0022labelEs\u0022:\u0022Consignatario (raz\\u00F3n social o nombre)\u0022,\u0022labelEn\u0022:\u0022Consignee (legal name or name)\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022consigneeTaxId\u0022,\u0022labelEs\u0022:\u0022NIT o documento de identidad\u0022,\u0022labelEn\u0022:\u0022Tax ID or ID document\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:30,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022consigneeAddress\u0022,\u0022labelEs\u0022:\u0022Domicilio\u0022,\u0022labelEn\u0022:\u0022Address\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:300,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022legalRepresentativeName\u0022,\u0022labelEs\u0022:\u0022Representante legal\u0022,\u0022labelEn\u0022:\u0022Legal representative\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022legalRepresentativeId\u0022,\u0022labelEs\u0022:\u0022Documento del representante\u0022,\u0022labelEn\u0022:\u0022Representative\\u0027s ID\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:30,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022carrierName\u0022,\u0022labelEs\u0022:\u0022Transportista\u0022,\u0022labelEn\u0022:\u0022Carrier\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022carrierTaxId\u0022,\u0022labelEs\u0022:\u0022NIT / RUT del transportista\u0022,\u0022labelEn\u0022:\u0022Carrier tax ID\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:30,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022driverName\u0022,\u0022labelEs\u0022:\u0022Conductor\u0022,\u0022labelEn\u0022:\u0022Driver\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022driverId\u0022,\u0022labelEs\u0022:\u0022Documento del conductor\u0022,\u0022labelEn\u0022:\u0022Driver\\u0027s ID\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:30,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022truckPlate\u0022,\u0022labelEs\u0022:\u0022Patente\u0022,\u0022labelEn\u0022:\u0022Truck plate\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:20,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022observations\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":false,"tariffAcceptanceRequired":false,"pricingMode":"None","chargeConceptCode":"RELEASE_LETTER","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"CustomerService","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"release-letter.generate","displayOrder":140,"isActive":true}', NULL);
    INSERT INTO "MaintainerChangeLogs" ("Id", "Action", "ChangedAt", "ChangedBy", "ChangedByUserId", "EntityId", "Maintainer", "NewValue", "PreviousValue")
    VALUES ('a586d1a7-d19d-2034-2f92-6cb6c8f53a5c', 'Created', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, 'ffffffff-0022-0022-0022-000000000002', 'ServiceDefinition', '{"code":"FREIGHT_CERTIFICATE","nameEs":"Certificado de flete","nameEn":"Freight certificate","descriptionEs":"Certificado del flete del BL para importaci\u00F3n de Bolivia (M6-02, BO-IMP-08): el cliente ingresa los datos del consignatario y la finalidad; el portal emite el PDF firmado y lo env\u00EDa a su correo. Primera entrega de Fase 2 sin pago ni carro. Se solicita desde los documentos del embarque.","descriptionEn":"Freight certificate of the BL for Bolivia imports (M6-02): the customer enters the consignee data and the purpose; the portal issues the signed PDF and e-mails it. First Phase 2 delivery without payment or cart. Requested from the shipment documents.","operations":"IMPORT","countries":"BO","referenceType":"BL","requiredBlStatuses":null,"availabilityWindow":"Always","requiresContainers":false,"allowMultiplePerBl":true,"inputSchemaJson":"[{\u0022key\u0022:\u0022consigneeName\u0022,\u0022labelEs\u0022:\u0022Consignatario\u0022,\u0022labelEn\u0022:\u0022Consignee\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022consigneeTaxId\u0022,\u0022labelEs\u0022:\u0022NIT del consignatario\u0022,\u0022labelEn\u0022:\u0022Consignee tax ID\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:true,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:30,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022purpose\u0022,\u0022labelEs\u0022:\u0022Finalidad\u0022,\u0022labelEn\u0022:\u0022Purpose\u0022,\u0022type\u0022:\u0022select\u0022,\u0022required\u0022:true,\u0022options\u0022:[{\u0022value\u0022:\u0022CUSTOMS\u0022,\u0022labelEs\u0022:\u0022Tr\\u00E1mite aduanero\u0022,\u0022labelEn\u0022:\u0022Customs clearance\u0022},{\u0022value\u0022:\u0022INSURANCE\u0022,\u0022labelEs\u0022:\u0022Seguro de la carga\u0022,\u0022labelEn\u0022:\u0022Cargo insurance\u0022},{\u0022value\u0022:\u0022BANK\u0022,\u0022labelEs\u0022:\u0022Tr\\u00E1mite bancario\u0022,\u0022labelEn\u0022:\u0022Banking\u0022},{\u0022value\u0022:\u0022OTHER\u0022,\u0022labelEs\u0022:\u0022Otra\u0022,\u0022labelEn\u0022:\u0022Other\u0022}],\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:null,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022recipient\u0022,\u0022labelEs\u0022:\u0022Dirigido a\u0022,\u0022labelEn\u0022:\u0022Addressed to\u0022,\u0022type\u0022:\u0022text\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:200,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null},{\u0022key\u0022:\u0022notes\u0022,\u0022labelEs\u0022:\u0022Observaciones\u0022,\u0022labelEn\u0022:\u0022Remarks\u0022,\u0022type\u0022:\u0022textarea\u0022,\u0022required\u0022:false,\u0022options\u0022:null,\u0022min\u0022:null,\u0022max\u0022:null,\u0022integer\u0022:false,\u0022maxLength\u0022:1000,\u0022helpEs\u0022:null,\u0022helpEn\u0022:null}]","billingDataRequired":false,"tariffAcceptanceRequired":false,"pricingMode":"None","chargeConceptCode":"FREIGHT_CERTIFICATE","tariffCode":null,"lateTariffCode":null,"quantityMode":"PerRequest","measureFieldKey":null,"milestone":"None","milestoneOffsetHours":0,"deadlineRuleCode":null,"timingRule":"None","taxable":false,"exemptionConcept":null,"excludeShipperOwnedContainers":false,"approvalTeam":"None","fulfillmentTeam":"None","requiresOutputDocument":false,"actionCode":"freight-certificate.generate","displayOrder":130,"isActive":true}', NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "Permissions" ("Id", "Code", "Description")
    VALUES ('4e1a9a66-9f8f-e94f-f54e-45976fd5f9ec', 'api-clients.manage', 'api-clients.manage');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('ffffffff-0022-0022-0022-000000000002', 'freight-certificate.generate', TRUE, 'None', 'Always', FALSE, 'FREIGHT_CERTIFICATE', 'FREIGHT_CERTIFICATE', 'BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Freight certificate of the BL for Bolivia imports (M6-02): the customer enters the consignee data and the purpose; the portal issues the signed PDF and e-mails it. First Phase 2 delivery without payment or cart. Requested from the shipment documents.', 'Certificado del flete del BL para importación de Bolivia (M6-02, BO-IMP-08): el cliente ingresa los datos del consignatario y la finalidad; el portal emite el PDF firmado y lo envía a su correo. Primera entrega de Fase 2 sin pago ni carro. Se solicita desde los documentos del embarque.', 130, FALSE, NULL, 'None', '[{"key":"consigneeName","labelEs":"Consignatario","labelEn":"Consignee","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"consigneeTaxId","labelEs":"NIT del consignatario","labelEn":"Consignee tax ID","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":30,"helpEs":null,"helpEn":null},{"key":"purpose","labelEs":"Finalidad","labelEn":"Purpose","type":"select","required":true,"options":[{"value":"CUSTOMS","labelEs":"Tr\u00E1mite aduanero","labelEn":"Customs clearance"},{"value":"INSURANCE","labelEs":"Seguro de la carga","labelEn":"Cargo insurance"},{"value":"BANK","labelEs":"Tr\u00E1mite bancario","labelEn":"Banking"},{"value":"OTHER","labelEs":"Otra","labelEn":"Other"}],"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"recipient","labelEs":"Dirigido a","labelEn":"Addressed to","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"notes","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Freight certificate', 'Certificado de flete', 'IMPORT', 'None', 'PerRequest', 'BL', NULL, FALSE, FALSE, FALSE, NULL, FALSE, 'None');
    INSERT INTO "ServiceDefinitions" ("Id", "ActionCode", "AllowMultiplePerBl", "ApprovalTeam", "AvailabilityWindow", "BillingDataRequired", "ChargeConceptCode", "Code", "Countries", "CreatedAt", "CreatedBy", "DeadlineRuleCode", "DeletedAt", "DeletedBy", "DescriptionEn", "DescriptionEs", "DisplayOrder", "ExcludeShipperOwnedContainers", "ExemptionConcept", "FulfillmentTeam", "InputSchemaJson", "IsActive", "LateTariffCode", "MeasureFieldKey", "Milestone", "MilestoneOffsetHours", "ModifiedAt", "ModifiedBy", "NameEn", "NameEs", "Operations", "PricingMode", "QuantityMode", "ReferenceType", "RequiredBlStatuses", "RequiresContainers", "RequiresOutputDocument", "TariffAcceptanceRequired", "TariffCode", "Taxable", "TimingRule")
    VALUES ('ffffffff-0022-0022-0022-000000000003', 'release-letter.generate', TRUE, 'CustomerService', 'Always', FALSE, 'RELEASE_LETTER', 'RELEASE_LETTER', 'BO', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'Release and deconsolidation letter for the selected units, Bolivia imports (M6-08): consignee data by legal entity type and carrier data; the units'' TATC is recorded on submission and approval; Customer Service approves it and the letter is issued on approval. No charge. Requested from the shipment documents.', 'Carta de liberación y desconsolidado de las unidades seleccionadas, importación de Bolivia (M6-08, BO-IMP-11): datos del consignatario según el tipo de sociedad y del transportista; el TATC de las unidades se registra al enviar y al aprobar; la aprueba Customer Service y la carta se emite al aprobarse. Sin cobro. Se solicita desde los documentos del embarque.', 140, FALSE, NULL, 'None', '[{"key":"containers","labelEs":"Contenedores","labelEn":"Containers","type":"containers","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"legalEntityType","labelEs":"Tipo de sociedad","labelEn":"Legal entity type","type":"select","required":true,"options":[{"value":"COMPANY","labelEs":"Empresa","labelEn":"Company"},{"value":"NATURAL_PERSON","labelEs":"Persona natural","labelEn":"Natural person"}],"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null},{"key":"consigneeName","labelEs":"Consignatario (raz\u00F3n social o nombre)","labelEn":"Consignee (legal name or name)","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"consigneeTaxId","labelEs":"NIT o documento de identidad","labelEn":"Tax ID or ID document","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":30,"helpEs":null,"helpEn":null},{"key":"consigneeAddress","labelEs":"Domicilio","labelEn":"Address","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":300,"helpEs":null,"helpEn":null},{"key":"legalRepresentativeName","labelEs":"Representante legal","labelEn":"Legal representative","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"legalRepresentativeId","labelEs":"Documento del representante","labelEn":"Representative\u0027s ID","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":30,"helpEs":null,"helpEn":null},{"key":"carrierName","labelEs":"Transportista","labelEn":"Carrier","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"carrierTaxId","labelEs":"NIT / RUT del transportista","labelEn":"Carrier tax ID","type":"text","required":true,"options":null,"min":null,"max":null,"integer":false,"maxLength":30,"helpEs":null,"helpEn":null},{"key":"driverName","labelEs":"Conductor","labelEn":"Driver","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":200,"helpEs":null,"helpEn":null},{"key":"driverId","labelEs":"Documento del conductor","labelEn":"Driver\u0027s ID","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":30,"helpEs":null,"helpEn":null},{"key":"truckPlate","labelEs":"Patente","labelEn":"Truck plate","type":"text","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":20,"helpEs":null,"helpEn":null},{"key":"observations","labelEs":"Observaciones","labelEn":"Remarks","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":1000,"helpEs":null,"helpEn":null}]', TRUE, NULL, NULL, 'None', 0, NULL, NULL, 'Release and deconsolidation letter', 'Carta de liberación y desconsolidado', 'IMPORT', 'None', 'PerRequest', 'BL', NULL, TRUE, FALSE, FALSE, NULL, FALSE, 'None');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ShipmentDocuments" ("Id", "AccessGrantId", "BillOfLadingId", "BlNumber", "BookingNumber", "ContainerNumbers", "ContentHash", "ContentType", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DeliveredAt", "DocumentNumber", "DocumentType", "FileName", "GenerationKey", "IssuedAt", "IssuedByEmail", "IssuedByUserId", "IssuedForOrganizationId", "ModifiedAt", "ModifiedBy", "OnBehalfOfOrganizationId", "Origin", "PaymentDetailId", "PaymentId", "RecipientEmails", "RetainUntil", "SignatureId", "SignatureLevel", "SignatureProvider", "SignedAt", "SizeBytes", "Status", "StorageKey", "TemplateJson", "TermsAcceptedAt", "TermsVersion", "ValidUntil", "VerificationCode")
    VALUES ('ffffffff-0018-0018-0018-000000000006', NULL, '11111111-0007-0007-0007-000000000005', 'HLCUIQQ260200078', 'HLCUBKG2602078', 'HLXU5566778,HLXU5566779', NULL, 'application/pdf', 'BO', TIMESTAMPTZ '2026-10-05T14:00:00Z', 'demo@altiplano.bo', NULL, NULL, TIMESTAMPTZ '2026-10-05T14:00:00Z', 'CFL-20261005-3B4C5D6E', 'FreightCertificate', 'certificado-flete-CFL-20261005-3B4C5D6E.pdf', 'service-request:ffffffff-0021-0021-0021-000000000010', TIMESTAMPTZ '2026-10-05T14:00:00Z', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'c3d4e5f6-0003-0003-0003-000000000020', NULL, NULL, NULL, 'Seed', NULL, NULL, 'demo@altiplano.bo', TIMESTAMPTZ '2036-10-05T14:00:00Z', NULL, NULL, NULL, NULL, 0, 'Issued', NULL, '{"title":"Certificado de flete","subtitle":"Importaci\u00F3n - Bolivia","issuer":"Hapag-Lloyd Bolivia S.R.L.","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Bolivia","documentNumber":"CFL-20261005-3B4C5D6E","issuedAt":"2026-10-05T14:00:00Z","timeZoneId":"America/La_Paz","references":[{"label":"BL","value":"HLCUIQQ260200078"},{"label":"Booking","value":"HLCUBKG2602078"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"BO"},{"label":"Nave / viaje","value":"Guayaquil Express / 007W"},{"label":"Ruta","value":"Mumbai (INBOM) - Iquique (CLIQQ)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Mumbai Spices \u0026 Commodities Pvt Ltd"},{"label":"Consignee","value":"Comercial Altiplano SRL"},{"label":"Notify","value":null}],"table":null,"paragraphs":null},{"heading":"Solicitud","fields":[{"label":"Solicitud","value":"SRV-20261005-5E1A0010"},{"label":"Solicitada por","value":"Comercial Altiplano SRL (1023456017)"},{"label":"Consignatario","value":"Comercial Altiplano SRL (1023456017)"},{"label":"Finalidad","value":"Tr\u00E1mite aduanero"},{"label":"Dirigido a","value":"Aduana Nacional de Bolivia"},{"label":"Observaciones","value":"Para la declaraci\u00F3n de importaci\u00F3n (DIM)."}],"table":null,"paragraphs":null},{"heading":"Flete","fields":[{"label":"Condici\u00F3n del flete","value":null},{"label":"Monto del flete","value":"1.950,00 USD"},{"label":"Puerto de carga","value":"Mumbai (INBOM)"},{"label":"Puerto de descarga","value":"Iquique (CLIQQ)"},{"label":"Lugar de entrega","value":"Santa Cruz, Bolivia"}],"table":null,"paragraphs":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU5566778","40HC","SL-015678","21.300,00","OnBoard"],["HLXU5566779","20DV","SL-015679","14.900,00","Discharged"]],"numericColumns":[3]},"paragraphs":null},{"heading":"Mercanc\u00EDa","fields":null,"table":null,"paragraphs":["Sin detalle de mercanc\u00EDa registrado."]},{"heading":"Certificaci\u00F3n","fields":null,"table":null,"paragraphs":["Hapag-Lloyd Bolivia S.R.L. certifica que el flete mar\u00EDtimo de la carga amparada en el BL HLCUIQQ260200078, transportada en la nave Guayaquil Express viaje 007W desde Mumbai (INBOM) hasta Santa Cruz, Bolivia, asciende a 1.950,00 USD, seg\u00FAn el registro del embarque a la fecha de emisi\u00F3n.","Se emite a solicitud del interesado para la finalidad declarada."]}],"verificationCode":"7900-1AF6-3B9B-DCF4","signatureNote":"Documento firmado electr\u00F3nicamente. La validez de la firma se acredita seg\u00FAn el mecanismo publicado por Hapag-Lloyd.","footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado."}', NULL, NULL, NULL, '7900-1AF6-3B9B-DCF4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "Users" ("Id", "ClientId", "Country", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayId", "Email", "EmailConfirmationToken", "EmailConfirmationTokenExpiry", "FirstName", "IsActive", "IsEmailConfirmed", "LastLoginAt", "LastName", "MembershipDecidedAt", "MembershipDecidedBy", "MembershipStatus", "ModifiedAt", "ModifiedBy", "PasswordHash", "PasswordResetToken", "PasswordResetTokenExpiry", "Phone", "RefreshToken", "RefreshTokenExpiryTime", "UserType", "Username")
    VALUES ('d4e5f6a7-0004-0004-0004-000000000092', 'c3d4e5f6-0003-0003-0003-000000000010', 'CL', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'SYSTEM', NULL, NULL, NULL, 'ws-ffffffff003300330033000000000001@clients.ws.invalid', NULL, NULL, 'Web Service', TRUE, TRUE, NULL, 'ERP Importadora Demo', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', 'Active', NULL, NULL, '$2a$12$O1N2kndfeGWg41xxfI/dcOwtqqctzfoR6cdyUT./IfSvcU4NaDUGe', NULL, NULL, NULL, NULL, NULL, 'Technical', 'ws-ffffffff003300330033000000000001@clients.ws.invalid');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ApiClients" ("Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "ModifiedAt", "ModifiedBy", "Name", "Notes", "OrganizationId", "RateLimitPerMinute", "RevocationReason", "RevokedAt", "RevokedBy", "Scopes", "SignatoryEmail", "SignatoryName", "SignatoryPosition", "SignatoryTaxId", "Status", "TechnicalContactEmail", "TechnicalUserId")
    VALUES ('ffffffff-0033-0033-0033-000000000001', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', NULL, NULL, NULL, NULL, 'ERP Importadora Demo', 'Cliente de demostración del canal Web Service (Ola J).', 'c3d4e5f6-0003-0003-0003-000000000010', 60, NULL, NULL, NULL, 'responsibility-letter,warehouse-change', 'demo@importadorademo.cl', 'Daniela Demo', 'Gerente de Comercio Exterior', '15.678.901-2', 'Active', 'ti@importadorademo.cl', 'd4e5f6a7-0004-0004-0004-000000000092');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000010', NULL, 0.0, NULL, NULL, '11111111-0007-0007-0007-000000000005', NULL, NULL, NULL, NULL, NULL, 'HLCUIQQ260200078', 'HLCUBKG2602078', NULL, NULL, TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL, 'BO', TIMESTAMPTZ '2026-10-05T14:00:00Z', 'demo@altiplano.bo', NULL, 'FREIGHT_CERTIFICATE', 'ffffffff-0022-0022-0022-000000000002', NULL, NULL, NULL, '{"consigneeName":"Comercial Altiplano SRL","consigneeTaxId":"1023456017","purpose":"CUSTOMS","recipient":"Aduana Nacional de Bolivia","notes":"Para la declaración de importación (DIM)."}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000020', NULL, NULL, NULL, 1, NULL, NULL, 'SRV-20261005-5E1A0010', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Certificado CFL-20261005-3B4C5D6E emitido.', 'Completed', TIMESTAMPTZ '2026-10-05T14:00:00Z', TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL, NULL, NULL, NULL, 0.0, NULL, 3, NULL, 0.0);
    INSERT INTO "ServiceRequests" ("Id", "AccessGrantId", "Amount", "ApprovedAt", "AssignedTeam", "BillOfLadingId", "BillingActivity", "BillingAddress", "BillingEmail", "BillingName", "BillingTaxId", "BlNumber", "BookingNumber", "CancelledAt", "ChargeConceptCode", "CompletedAt", "ContainerNumbers", "Country", "CreatedAt", "CreatedBy", "Currency", "DefinitionCode", "DefinitionId", "DeletedAt", "DeletedBy", "ExemptionReference", "InputValuesJson", "IsExempt", "MeasuredUnits", "MilestoneAt", "MilestoneSource", "ModifiedAt", "ModifiedBy", "OnBehalfOfClientId", "Operation", "OrganizationId", "PaidAt", "PaymentId", "PricingDetailJson", "Quantity", "QuotedAt", "RejectedAt", "RequestNumber", "RequestedByEmail", "RequestedByUserId", "ResolutionNotes", "Status", "StatusChangedAt", "SubmittedAt", "TariffAcceptedAt", "TariffCode", "TariffId", "TariffSource", "TaxAmount", "TierUnit", "TimelineSequence", "Timing", "TotalAmount")
    VALUES ('ffffffff-0021-0021-0021-000000000011', NULL, 0.0, NULL, 'CustomerService', '11111111-0007-0007-0007-000000000004', NULL, NULL, NULL, NULL, NULL, 'HLCUARI260100045', 'HLCUBKG2601045', NULL, NULL, NULL, 'HLXU8899001', 'BO', TIMESTAMPTZ '2026-10-05T15:00:00Z', 'demo@altiplano.bo', NULL, 'RELEASE_LETTER', 'ffffffff-0022-0022-0022-000000000003', NULL, NULL, NULL, '{"containers":["HLXU8899001"],"legalEntityType":"COMPANY","consigneeName":"Comercial Altiplano SRL","consigneeTaxId":"1023456017","consigneeAddress":"Av. Arce 2631, La Paz","legalRepresentativeName":"Marcela Quispe","legalRepresentativeId":"4876512 LP","carrierName":"Transportes Illimani SRL","carrierTaxId":"4455667018","driverName":"Juan Mamani","driverId":"6123987 LP","truckPlate":"2345-KTR"}', FALSE, NULL, NULL, NULL, NULL, NULL, NULL, 'IMPORT', 'c3d4e5f6-0003-0003-0003-000000000020', NULL, NULL, NULL, 1, NULL, NULL, 'SRV-20261005-5E1A0011', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, 'PendingApproval', TIMESTAMPTZ '2026-10-05T15:00:00Z', TIMESTAMPTZ '2026-10-05T15:00:00Z', NULL, NULL, NULL, NULL, 0.0, NULL, 3, NULL, 0.0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ShipmentDocumentEvents" ("Id", "Channel", "Details", "EventType", "OccurredAt", "OnBehalfOfOrganizationId", "OrganizationId", "Recipient", "ShipmentDocumentId", "UserEmail", "UserId")
    VALUES ('5f032085-9241-c2f6-7fd5-5f1258c1dc9c', 'Portal', NULL, 'Issued', TIMESTAMPTZ '2026-10-05T14:00:00Z', NULL, 'c3d4e5f6-0003-0003-0003-000000000020', NULL, 'ffffffff-0018-0018-0018-000000000006', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "UserRoles" ("Id", "RoleId", "RoleName", "UserId")
    VALUES ('8b85820f-23d8-1ed6-e46f-1001c7af915c', '227e87c5-a8d9-51c6-e0e9-373dc6d2c60b', 'OrgOperator', 'd4e5f6a7-0004-0004-0004-000000000092');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ApiClientKeys" ("Id", "ApiClientId", "CreatedAt", "CreatedBy", "ExpiresAt", "KeyHash", "LastUsedAt", "Prefix", "RevokedAt", "RevokedBy")
    VALUES ('ffffffff-0033-0033-0033-000000000002', 'ffffffff-0033-0033-0033-000000000001', TIMESTAMPTZ '2026-10-05T12:00:00Z', 'admin@hapag-lloyd.cl', NULL, 'b23557bc92386afaa0dacb9c9c48adc6554223090784f268cbe3fba4bd93e6ba', NULL, '4pnrf9yxigh5', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ReleaseLetterRequests" ("Id", "CarrierOrganizationId", "DocumentId", "LegalEntityType", "ServiceRequestId", "TatcAvailableAtApproval", "TatcAvailableAtSubmission", "TatcCheckedAtApproval", "TatcCheckedAtSubmission", "TatcErrorAtApproval", "TatcErrorAtSubmission", "TatcSnapshotAtApproval", "TatcSnapshotAtSubmission", "TatcStatusAtApproval", "TatcStatusAtSubmission")
    VALUES ('ffffffff-0032-0032-0032-000000000001', NULL, NULL, 'COMPANY', 'ffffffff-0021-0021-0021-000000000011', NULL, TRUE, NULL, TIMESTAMPTZ '2026-10-05T15:00:00Z', NULL, NULL, NULL, '[{"containerNumber":"HLXU8899001","status":"NotIssued","sourceStatus":"NOT_ISSUED","tatcNumber":null,"pendingReasons":["PAYMENT_PENDING","MHD_PENDING"]}]', NULL, 'NotIssued');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('0d5309ac-17ef-dae7-dbe7-375b1e62c38f', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, NULL, TIMESTAMPTZ '2026-10-05T15:00:00Z', 1, 'ffffffff-0021-0021-0021-000000000011', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('2adb63cd-684f-cca7-e35d-e9e73e299a1e', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Draft', NULL, TIMESTAMPTZ '2026-10-05T15:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000011', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('7fbd7dc7-3378-6e62-9ba9-0911de05fe7c', 'System', 'SYSTEM', NULL, 'Submitted', 'Certificado CFL-20261005-3B4C5D6E emitido, sin cobro (primera entrega de Fase 2, M6-02).', TIMESTAMPTZ '2026-10-05T14:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000010', 'Completed');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('ab6f4921-65c2-6608-23e0-d46e8d0ee864', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', 'Draft', NULL, TIMESTAMPTZ '2026-10-05T14:00:00Z', 2, 'ffffffff-0021-0021-0021-000000000010', 'Submitted');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('af9b468e-db5e-890c-63af-2c8716f674d1', 'Client', 'demo@altiplano.bo', 'd4e5f6a7-0004-0004-0004-000000000020', NULL, NULL, TIMESTAMPTZ '2026-10-05T14:00:00Z', 1, 'ffffffff-0021-0021-0021-000000000010', 'Draft');
    INSERT INTO "ServiceRequestEvents" ("Id", "ActorKind", "ActorName", "ActorUserId", "FromStatus", "Notes", "OccurredAt", "Sequence", "ServiceRequestId", "ToStatus")
    VALUES ('bb6e05ed-c743-6c57-d75e-db85983752ae', 'System', 'SYSTEM', NULL, 'Submitted', 'Derivada al equipo CustomerService. Al enviar: TATC NotIssued: HLXU8899001 NotIssued.', TIMESTAMPTZ '2026-10-05T15:00:00Z', 3, 'ffffffff-0021-0021-0021-000000000011', 'PendingApproval');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_ApiClientKeys_ApiClientId" ON "ApiClientKeys" ("ApiClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE UNIQUE INDEX "IX_ApiClientKeys_Prefix" ON "ApiClientKeys" ("Prefix");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE UNIQUE INDEX "IX_ApiClientRequests_ApiClientId_IdempotencyKey" ON "ApiClientRequests" ("ApiClientId", "IdempotencyKey") WHERE "IdempotencyKey" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_ApiClientRequests_ApiClientId_ReceivedAt" ON "ApiClientRequests" ("ApiClientId", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_ApiClientRequests_OrganizationId_ReceivedAt" ON "ApiClientRequests" ("OrganizationId", "ReceivedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_ApiClients_OrganizationId" ON "ApiClients" ("OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE UNIQUE INDEX "IX_ApiClients_TechnicalUserId" ON "ApiClients" ("TechnicalUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_AssistantDocumentDeliveries_DocumentId_DeliveredAt" ON "AssistantDocumentDeliveries" ("DocumentId", "DeliveredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_AssistantDocumentDeliveries_SessionId_DeliveredAt" ON "AssistantDocumentDeliveries" ("SessionId", "DeliveredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE INDEX "IX_AssistantDocumentDeliveries_UserId_DeliveredAt" ON "AssistantDocumentDeliveries" ("UserId", "DeliveredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    CREATE UNIQUE INDEX "IX_ReleaseLetterRequests_ServiceRequestId" ON "ReleaseLetterRequests" ("ServiceRequestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261006180701_AddDocumentRequestsAndWebServiceChannel') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261006180701_AddDocumentRequestsAndWebServiceChannel', '9.0.4');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    ALTER TABLE "Payments" ADD "ProviderCheckedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    ALTER TABLE "Payments" ADD "RedirectForm" character varying(8000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000008';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000009';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000011';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000012';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000013';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000014';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000015';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000016';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "Payments" SET "ProviderCheckedAt" = NULL, "RedirectForm" = NULL
    WHERE "Id" = '55555555-000b-000b-000b-000000000017';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Certificado de transbordo","subtitle":"Operaci\u00F3n de exportaci\u00F3n","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CTB-20261004-5A1B2C3D","issuedAt":"2026-10-04T13:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260300610"},{"label":"Booking","value":"HLCUBKG2603061"},{"label":"Operaci\u00F3n","value":"Exportaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Valparaiso Express / 2610S"},{"label":"Ruta","value":"San Antonio (CLSAI) - Rotterdam (NLRTM)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Importadora Demo SpA"},{"label":"Consignee","value":"Fruit Import BV"},{"label":"Notify","value":null}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Cliente","fields":[{"label":"Raz\u00F3n social","value":"Importadora Demo SpA"},{"label":"RUT / NIT","value":"76123456-7"},{"label":"Pago","value":null}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU2023001","40RF","SL-020301","26.800,00","GateIn"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Certificaci\u00F3n","fields":null,"table":null,"paragraphs":["Hapag-Lloyd Chile SpA certifica que la carga amparada en el BL HLCUSAI260300610, transportada en la nave Valparaiso Express viaje 2610S, con origen en San Antonio (CLSAI) y destino Rotterdam, Netherlands, fue objeto de transbordo en el puerto de Rotterdam (NLRTM) en las unidades individualizadas en este documento.","Se emite a solicitud del interesado para los fines que estime convenientes."],"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"EF67-5D1A-FCD1-C29B","signatureNote":"Documento firmado electr\u00F3nicamente. La validez de la firma se acredita seg\u00FAn el mecanismo publicado por Hapag-Lloyd.","footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Copia de BL - no valorada","subtitle":"Copia informativa, no negociable","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CBL-20261003-7E8F9A0B","issuedAt":"2026-10-03T16:30:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUVAL250100123"},{"label":"Booking","value":"HLCUBKG2501001"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Hamburg Express / 025E"},{"label":"Ruta","value":"Shanghai (CNSHA) - San Antonio (CLSAI)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Shanghai Electronics Co. Ltd"},{"label":"Consignee","value":"Importadora Demo SpA"},{"label":"Notify","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Transporte","fields":[{"label":"Nave / viaje","value":"Hamburg Express / 025E"},{"label":"Puerto de carga","value":"Shanghai (CNSHA)"},{"label":"Puerto de descarga","value":"San Antonio (CLSAI)"},{"label":"Lugar de entrega","value":"Santiago, Chile"},{"label":"ETD","value":"01-03-2026"},{"label":"ETA","value":"05-04-2026"},{"label":"Tipo de BL","value":null},{"label":"Incoterm","value":null}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU1234567","40HC","SL-001234","24.500,00","Discharged"],["HLXU7654321","20DV","SL-005678","18.200,00","Discharged"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Mercanc\u00EDa","fields":null,"table":null,"paragraphs":["Sin detalle de mercanc\u00EDa registrado."],"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Valores comerciales","fields":null,"table":null,"paragraphs":["Copia no valorada: no incluye el flete ni los cargos del embarque."],"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Solicitud","fields":[{"label":"Solicitada por","value":"Importadora Demo SpA (demo@importadorademo.cl)"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"90FF-9F4C-557C-3C9D","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Comprobante de flete Collect","subtitle":null,"issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CCO-20261003-1C2D3E4F","issuedAt":"2026-10-03T15:05:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260501240"},{"label":"Booking","value":"HLCUBKG2605124"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Cartagena Express / 2611E"},{"label":"Ruta","value":"Yokohama (JPYOK) - San Antonio (CLSAI)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Yokohama Machinery Co."},{"label":"Consignee","value":"Importadora Demo SpA"},{"label":"Notify","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Pago del flete","fields":[{"label":"Condici\u00F3n del flete","value":"Collect"},{"label":"Monto pagado","value":"4.800,00 USD"},{"label":"Pagador","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"},{"label":"RUT / NIT del pagador","value":"96555444-3"},{"label":"Pago","value":null},{"label":"Comprobante de pago","value":null},{"label":"Fecha de pago","value":"03-10-2026 12:00 (America/Santiago)"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3045001","40HC","SL-045001","25.100,00","Discharged"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"B3D6-27E7-7995-FF24","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Carta de responsabilidad","subtitle":"T\u00E9rminos CARTA-RESP-2026-10","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"CRE-20261002-9F8E7D6C","issuedAt":"2026-10-02T14:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUVAP260501350"},{"label":"Booking","value":"HLCUBKG2605135"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Callao Express / 2611N"},{"label":"Ruta","value":"Shanghai (CNSHA) - Valparaiso (CLVAP)"}],"sections":[{"heading":"Organizaci\u00F3n responsable","fields":[{"label":"Raz\u00F3n social","value":"Global Forwarding Chile SpA"},{"label":"RUT / NIT","value":"76000003-3"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Firmante","fields":[{"label":"Nombre","value":"Felipe Forwarder"},{"label":"Documento de identidad","value":"12.345.678-5"},{"label":"Cargo","value":"Gerente de Operaciones"},{"label":"Correo de contacto","value":"ffww@globalforwarding.cl"},{"label":"Tel\u00E9fono","value":null}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3045002","20DV","SL-045002","17.900,00","Discharged"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Declaraci\u00F3n","fields":[{"label":"Mercanc\u00EDa","value":"Muebles de madera"},{"label":"Observaciones","value":null}],"table":null,"paragraphs":["El firmante, en representaci\u00F3n de la organizaci\u00F3n indicada, declara que los datos ingresados son ver\u00EDdicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulaci\u00F3n, liberando a Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.","T\u00E9rminos aceptados en el portal el 02-10-2026 11:00 (America/Santiago) (versi\u00F3n CARTA-RESP-2026-10)."],"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"94E7-43DA-1F4E-8AA7","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Recibo de pago anticipado - Gate Out","subtitle":"Pago recibido antes de la emisi\u00F3n de la factura","issuer":"Hapag-Lloyd Chile SpA","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Chile","documentNumber":"RGO-20261001-1A2B3C4D","issuedAt":"2026-10-01T14:00:00Z","timeZoneId":"America/Santiago","references":[{"label":"BL","value":"HLCUSAI260701810"},{"label":"Booking","value":"HLCUBKG2607181"},{"label":"Operaci\u00F3n","value":"Exportaci\u00F3n"},{"label":"Pa\u00EDs","value":"CL"},{"label":"Nave / viaje","value":"Valparaiso Express / 2610N"},{"label":"Ruta","value":"San Antonio (CLSAI) - Callao (PECLL)"}],"sections":[{"heading":"Pagador","fields":[{"label":"Raz\u00F3n social","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda"},{"label":"RUT / NIT","value":"96555444-3"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Pago","fields":[{"label":"Concepto","value":"Gate Out - 40HC (San Antonio)"},{"label":"Monto del cargo","value":"71.400,00 CLP"},{"label":"Monto pagado","value":"71.400,00 CLP"},{"label":"Tipo de cambio","value":null},{"label":"RUT de facturaci\u00F3n","value":"Agencia Mar\u00EDtima del Pac\u00EDfico Ltda (96555444-3)"},{"label":"Pago","value":"PAY-20261001-D4E5F6A7"},{"label":"Comprobante de pago","value":"RCP-20261001-E8F9A0B1"},{"label":"Medio de pago","value":"KHIPU"},{"label":"Fecha de pago","value":"01-10-2026 11:00 (America/Santiago)"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU3071801","40HC","SL-071801","24.800,00","OnBoard"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Vinculaci\u00F3n con la factura","fields":null,"table":null,"paragraphs":["La factura del Gate Out se emite despu\u00E9s del zarpe de la nave y se vincula a este recibo.","El cargo pagado con este recibo no vuelve a cobrarse al cliente."],"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"CBA5-6DA7-95A4-8C6B","signatureNote":null,"footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    UPDATE "ShipmentDocuments" SET "TemplateJson" = '{"title":"Certificado de flete","subtitle":"Importaci\u00F3n - Bolivia","issuer":"Hapag-Lloyd Bolivia S.R.L.","issuerDetail":"Agente de Hapag-Lloyd AG - Operaci\u00F3n Bolivia","documentNumber":"CFL-20261005-3B4C5D6E","issuedAt":"2026-10-05T14:00:00Z","timeZoneId":"America/La_Paz","references":[{"label":"BL","value":"HLCUIQQ260200078"},{"label":"Booking","value":"HLCUBKG2602078"},{"label":"Operaci\u00F3n","value":"Importaci\u00F3n"},{"label":"Pa\u00EDs","value":"BO"},{"label":"Nave / viaje","value":"Guayaquil Express / 007W"},{"label":"Ruta","value":"Mumbai (INBOM) - Iquique (CLIQQ)"}],"sections":[{"heading":"Partes","fields":[{"label":"Shipper","value":"Mumbai Spices \u0026 Commodities Pvt Ltd"},{"label":"Consignee","value":"Comercial Altiplano SRL"},{"label":"Notify","value":null}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Solicitud","fields":[{"label":"Solicitud","value":"SRV-20261005-5E1A0010"},{"label":"Solicitada por","value":"Comercial Altiplano SRL (1023456017)"},{"label":"Consignatario","value":"Comercial Altiplano SRL (1023456017)"},{"label":"Finalidad","value":"Tr\u00E1mite aduanero"},{"label":"Dirigido a","value":"Aduana Nacional de Bolivia"},{"label":"Observaciones","value":"Para la declaraci\u00F3n de importaci\u00F3n (DIM)."}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Flete","fields":[{"label":"Condici\u00F3n del flete","value":null},{"label":"Monto del flete","value":"1.950,00 USD"},{"label":"Puerto de carga","value":"Mumbai (INBOM)"},{"label":"Puerto de descarga","value":"Iquique (CLIQQ)"},{"label":"Lugar de entrega","value":"Santa Cruz, Bolivia"}],"table":null,"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Unidades","fields":null,"table":{"headers":["Contenedor","Tipo","Sello","Peso (kg)","Estado"],"rows":[["HLXU5566778","40HC","SL-015678","21.300,00","OnBoard"],["HLXU5566779","20DV","SL-015679","14.900,00","Discharged"]],"numericColumns":[3],"columnWidths":null},"paragraphs":null,"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Mercanc\u00EDa","fields":null,"table":null,"paragraphs":["Sin detalle de mercanc\u00EDa registrado."],"totals":null,"steps":null,"note":null,"noteTone":null},{"heading":"Certificaci\u00F3n","fields":null,"table":null,"paragraphs":["Hapag-Lloyd Bolivia S.R.L. certifica que el flete mar\u00EDtimo de la carga amparada en el BL HLCUIQQ260200078, transportada en la nave Guayaquil Express viaje 007W desde Mumbai (INBOM) hasta Santa Cruz, Bolivia, asciende a 1.950,00 USD, seg\u00FAn el registro del embarque a la fecha de emisi\u00F3n.","Se emite a solicitud del interesado para la finalidad declarada."],"totals":null,"steps":null,"note":null,"noteTone":null}],"verificationCode":"7900-1AF6-3B9B-DCF4","signatureNote":"Documento firmado electr\u00F3nicamente. La validez de la firma se acredita seg\u00FAn el mecanismo publicado por Hapag-Lloyd.","footer":"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\u00F3n con el c\u00F3digo indicado.","qrPayload":null,"statusLabel":null,"statusTone":null,"highlight":null}'
    WHERE "Id" = 'ffffffff-0018-0018-0018-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261007135717_AddPaymentGatewayRedirectFormAndCheck') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261007135717_AddPaymentGatewayRedirectFormAndCheck', '9.0.4');
    END IF;
END $EF$;
COMMIT;

