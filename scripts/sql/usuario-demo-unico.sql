-- Deja un único usuario administrador: demo@hapaglloyd.cl. Se ejecuta una vez, a mano, en la consola SQL de Railway.
-- Respalde la base antes. Si algo falla, la transacción no aplica nada.

BEGIN;

-- Clientes de API: dependen de usuarios técnicos que se eliminan.
DELETE FROM "ApiClientRequests";
DELETE FROM "ApiClientKeys";
DELETE FROM "ApiClients";

-- Todos los usuarios (sus roles se eliminan en cascada).
DELETE FROM "Users";

-- Usuario demo: interno, en la organización interna de Hapag-Lloyd.
INSERT INTO "Users" ("Id", "Username", "Email", "PasswordHash", "UserType", "Country", "IsActive", "ClientId",
                     "IsEmailConfirmed", "MembershipStatus", "FirstName", "LastName", "CreatedAt", "CreatedBy")
SELECT gen_random_uuid(), 'demo@hapaglloyd.cl', 'demo@hapaglloyd.cl',
       '$2a$12$6.GmxVGs5VabWL1xy8gzZ.GrKftqiOf2iLKsLK9qIJqEgZc0BaCmm',
       'Admin', 'CL', true, "Id", true, 'Active', 'Usuario', 'Demo', now(), 'script'
  FROM "Clients" WHERE "OrganizationType" = 'Internal' LIMIT 1;

-- Rol Administrador: todos los permisos.
INSERT INTO "UserRoles" ("Id", "RoleName", "UserId", "RoleId")
SELECT gen_random_uuid(), 'Admin', u."Id", r."Id"
  FROM "Users" u, "Roles" r
 WHERE u."Email" = 'demo@hapaglloyd.cl' AND r."Code" = 'Administrador';

COMMIT;
