-- =============================================================================================================
-- Portal Hapag-Lloyd 2.0: deja un único usuario administrador "demo@hapaglloyd.cl" y elimina todos los demás.
--
-- Cuándo ejecutarlo: DESPUÉS de desplegar la versión del PR #44, para que el esquema esté al día y se eliminen
-- también los usuarios de demostración que agregan las migraciones nuevas.
-- Cómo: en Railway, abra la base PostgreSQL > pestaña "Data" (o "Query"), pegue TODO este archivo y ejecútelo.
--
-- Todo va en una transacción: si algo falla, no se aplica nada. Antes de ejecutarlo, respalde la base
-- (Railway: Backups, o pg_dump).
--
-- Qué hace:
--   1. Crea (o actualiza, si ya existe) demo@hapaglloyd.cl como interno con el rol Administrador, que recibe
--      todos los permisos, en la organización interna de Hapag-Lloyd.
--   2. Elimina los datos personales de los demás usuarios (carro, notificaciones, preferencias, sesiones del
--      asistente, estado de las guías) y los clientes de API cuyo usuario técnico se elimina.
--   3. Elimina a los demás usuarios. Sus roles se borran en cascada. Los registros históricos (pagos,
--      solicitudes, auditoría) se conservan: guardan el usuario como texto o como id sin llave foránea.
--
-- La contraseña no está en este archivo: solo su hash BCrypt (costo 12). Se entregó aparte.
-- =============================================================================================================

BEGIN;

-- Vista previa: usuarios antes del cambio.
SELECT "Email", "UserType", "IsActive" FROM "Users" ORDER BY "Email";

DO $$
DECLARE
    demo_email   CONSTANT text := 'demo@hapaglloyd.cl';
    demo_hash    CONSTANT text := '$2a$12$6.GmxVGs5VabWL1xy8gzZ.GrKftqiOf2iLKsLK9qIJqEgZc0BaCmm';
    internal_org uuid;
    admin_role   uuid;
    demo_id      uuid;
    removed      uuid[];
    t            record;
BEGIN
    -- Organización interna de Hapag-Lloyd (la del administrador del sistema).
    SELECT "Id" INTO internal_org FROM "Clients"
     WHERE "OrganizationType" = 'Internal' AND "DeletedAt" IS NULL
     ORDER BY "CreatedAt" LIMIT 1;
    IF internal_org IS NULL THEN
        RAISE EXCEPTION 'No se encontró la organización interna de Hapag-Lloyd (Clients.OrganizationType = Internal).';
    END IF;

    SELECT "Id" INTO admin_role FROM "Roles" WHERE "Code" = 'Administrador' AND "DeletedAt" IS NULL LIMIT 1;
    IF admin_role IS NULL THEN
        RAISE EXCEPTION 'No se encontró el rol Administrador (Roles.Code = Administrador).';
    END IF;

    -- 1. Usuario demo: se crea o se actualiza.
    SELECT "Id" INTO demo_id FROM "Users" WHERE lower("Email") = demo_email LIMIT 1;
    IF demo_id IS NULL THEN
        demo_id := gen_random_uuid();
        INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "UserType", "Country", "IsActive",
                             "ClientId", "IsEmailConfirmed", "MembershipStatus", "FirstName", "LastName",
                             "CreatedAt", "CreatedBy")
        VALUES (demo_id, demo_email, demo_email, demo_hash, 'Admin', 'CL', true,
                internal_org, true, 'Active', 'Usuario', 'Demo',
                now(), 'script:usuario-demo-unico');
    ELSE
        UPDATE "Users"
           SET "Username" = demo_email, "PasswordHash" = demo_hash, "UserType" = 'Admin', "IsActive" = true,
               "ClientId" = internal_org, "IsEmailConfirmed" = true, "MembershipStatus" = 'Active',
               "DeletedAt" = NULL, "DeletedBy" = NULL,
               "PasswordResetToken" = NULL, "PasswordResetTokenExpiry" = NULL,
               "RefreshToken" = NULL, "RefreshTokenExpiryTime" = NULL,
               "ModifiedAt" = now(), "ModifiedBy" = 'script:usuario-demo-unico'
         WHERE "Id" = demo_id;
    END IF;

    -- Rol: Administrador (todos los permisos) más el nombre heredado "Admin" que también usa el portal.
    DELETE FROM "UserRoles" WHERE "UserId" = demo_id;
    INSERT INTO "UserRoles" ("Id", "RoleName", "UserId", "RoleId")
    VALUES (gen_random_uuid(), 'Admin', demo_id, admin_role);

    -- 2. Usuarios a eliminar: todos menos el demo.
    SELECT array_agg("Id") INTO removed FROM "Users" WHERE "Id" <> demo_id;
    IF removed IS NULL THEN
        RAISE NOTICE 'No hay otros usuarios que eliminar.';
        RETURN;
    END IF;

    -- Datos personales de esos usuarios (solo si la tabla existe en esta versión del esquema).
    FOR t IN SELECT * FROM (VALUES
            ('CartItems', 'AddedByUserId'),
            ('Carts', 'UserId'),
            ('Notifications', 'UserId'),
            ('NotificationPreferences', 'UserId'),
            ('UserGuideStates', 'UserId'),
            ('AssistantDocumentDeliveries', 'UserId'),
            ('AssistantSessions', 'UserId')) AS x(tbl, col)
    LOOP
        IF to_regclass(format('public.%I', t.tbl)) IS NOT NULL THEN
            EXECUTE format('DELETE FROM %I WHERE %I = ANY($1)', t.tbl, t.col) USING removed;
        END IF;
    END LOOP;

    -- Los ítems de carros de esos usuarios que haya agregado otra persona también se van con el carro.
    IF to_regclass('public."CartItems"') IS NOT NULL THEN
        DELETE FROM "CartItems" WHERE "CartId" NOT IN (SELECT "Id" FROM "Carts");
    END IF;

    -- Clientes de API cuyo usuario técnico se elimina (las llaves foráneas lo exigen): primero su registro de
    -- solicitudes y sus llaves, luego el cliente.
    IF to_regclass('public."ApiClients"') IS NOT NULL THEN
        IF to_regclass('public."ApiClientRequests"') IS NOT NULL THEN
            DELETE FROM "ApiClientRequests"
             WHERE "TechnicalUserId" = ANY(removed)
                OR "ApiClientId" IN (SELECT "Id" FROM "ApiClients" WHERE "TechnicalUserId" = ANY(removed));
        END IF;
        IF to_regclass('public."ApiClientKeys"') IS NOT NULL THEN
            DELETE FROM "ApiClientKeys"
             WHERE "ApiClientId" IN (SELECT "Id" FROM "ApiClients" WHERE "TechnicalUserId" = ANY(removed));
        END IF;
        DELETE FROM "ApiClients" WHERE "TechnicalUserId" = ANY(removed);
    END IF;

    -- 3. Usuarios (UserRoles se elimina en cascada).
    DELETE FROM "Users" WHERE "Id" = ANY(removed);

    RAISE NOTICE 'Usuarios eliminados: %', array_length(removed, 1);
END $$;

-- Resultado: debe quedar solo demo@hapaglloyd.cl con el rol Administrador.
SELECT u."Email", u."UserType", u."IsActive", ur."RoleName", r."Code" AS "Rol"
  FROM "Users" u
  LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
  LEFT JOIN "Roles" r ON r."Id" = ur."RoleId";

COMMIT;
