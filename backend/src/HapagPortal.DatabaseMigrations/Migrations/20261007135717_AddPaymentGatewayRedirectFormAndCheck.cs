using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HapagPortal.DatabaseMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGatewayRedirectFormAndCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderCheckedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedirectForm",
                table: "Payments",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000001"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000002"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000003"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000004"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000005"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000006"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000007"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000008"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000009"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000010"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000011"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000012"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000013"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000014"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000015"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000016"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-000b-000b-000b-000000000017"),
                columns: new[] { "ProviderCheckedAt", "RedirectForm" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000001"),
                column: "TemplateJson",
                value: "{\"title\":\"Certificado de transbordo\",\"subtitle\":\"Operaci\\u00F3n de exportaci\\u00F3n\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CTB-20261004-5A1B2C3D\",\"issuedAt\":\"2026-10-04T13:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260300610\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2603061\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610S\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Rotterdam (NLRTM)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Consignee\",\"value\":\"Fruit Import BV\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Cliente\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76123456-7\"},{\"label\":\"Pago\",\"value\":null}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU2023001\",\"40RF\",\"SL-020301\",\"26.800,00\",\"GateIn\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Chile SpA certifica que la carga amparada en el BL HLCUSAI260300610, transportada en la nave Valparaiso Express viaje 2610S, con origen en San Antonio (CLSAI) y destino Rotterdam, Netherlands, fue objeto de transbordo en el puerto de Rotterdam (NLRTM) en las unidades individualizadas en este documento.\",\"Se emite a solicitud del interesado para los fines que estime convenientes.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"EF67-5D1A-FCD1-C29B\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000002"),
                column: "TemplateJson",
                value: "{\"title\":\"Copia de BL - no valorada\",\"subtitle\":\"Copia informativa, no negociable\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CBL-20261003-7E8F9A0B\",\"issuedAt\":\"2026-10-03T16:30:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAL250100123\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2501001\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Shanghai Electronics Co. Ltd\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Transporte\",\"fields\":[{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Puerto de carga\",\"value\":\"Shanghai (CNSHA)\"},{\"label\":\"Puerto de descarga\",\"value\":\"San Antonio (CLSAI)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santiago, Chile\"},{\"label\":\"ETD\",\"value\":\"01-03-2026\"},{\"label\":\"ETA\",\"value\":\"05-04-2026\"},{\"label\":\"Tipo de BL\",\"value\":null},{\"label\":\"Incoterm\",\"value\":null}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU1234567\",\"40HC\",\"SL-001234\",\"24.500,00\",\"Discharged\"],[\"HLXU7654321\",\"20DV\",\"SL-005678\",\"18.200,00\",\"Discharged\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Valores comerciales\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Copia no valorada: no incluye el flete ni los cargos del embarque.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitada por\",\"value\":\"Importadora Demo SpA (demo@importadorademo.cl)\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"90FF-9F4C-557C-3C9D\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000003"),
                column: "TemplateJson",
                value: "{\"title\":\"Comprobante de flete Collect\",\"subtitle\":null,\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CCO-20261003-1C2D3E4F\",\"issuedAt\":\"2026-10-03T15:05:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260501240\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605124\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Cartagena Express / 2611E\"},{\"label\":\"Ruta\",\"value\":\"Yokohama (JPYOK) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Yokohama Machinery Co.\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Pago del flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":\"Collect\"},{\"label\":\"Monto pagado\",\"value\":\"4.800,00 USD\"},{\"label\":\"Pagador\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT del pagador\",\"value\":\"96555444-3\"},{\"label\":\"Pago\",\"value\":null},{\"label\":\"Comprobante de pago\",\"value\":null},{\"label\":\"Fecha de pago\",\"value\":\"03-10-2026 12:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045001\",\"40HC\",\"SL-045001\",\"25.100,00\",\"Discharged\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"B3D6-27E7-7995-FF24\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000004"),
                column: "TemplateJson",
                value: "{\"title\":\"Carta de responsabilidad\",\"subtitle\":\"T\\u00E9rminos CARTA-RESP-2026-10\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CRE-20261002-9F8E7D6C\",\"issuedAt\":\"2026-10-02T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAP260501350\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605135\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Callao Express / 2611N\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - Valparaiso (CLVAP)\"}],\"sections\":[{\"heading\":\"Organizaci\\u00F3n responsable\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Global Forwarding Chile SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76000003-3\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Firmante\",\"fields\":[{\"label\":\"Nombre\",\"value\":\"Felipe Forwarder\"},{\"label\":\"Documento de identidad\",\"value\":\"12.345.678-5\"},{\"label\":\"Cargo\",\"value\":\"Gerente de Operaciones\"},{\"label\":\"Correo de contacto\",\"value\":\"ffww@globalforwarding.cl\"},{\"label\":\"Tel\\u00E9fono\",\"value\":null}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045002\",\"20DV\",\"SL-045002\",\"17.900,00\",\"Discharged\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Declaraci\\u00F3n\",\"fields\":[{\"label\":\"Mercanc\\u00EDa\",\"value\":\"Muebles de madera\"},{\"label\":\"Observaciones\",\"value\":null}],\"table\":null,\"paragraphs\":[\"El firmante, en representaci\\u00F3n de la organizaci\\u00F3n indicada, declara que los datos ingresados son ver\\u00EDdicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulaci\\u00F3n, liberando a Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.\",\"T\\u00E9rminos aceptados en el portal el 02-10-2026 11:00 (America/Santiago) (versi\\u00F3n CARTA-RESP-2026-10).\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"94E7-43DA-1F4E-8AA7\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000005"),
                column: "TemplateJson",
                value: "{\"title\":\"Recibo de pago anticipado - Gate Out\",\"subtitle\":\"Pago recibido antes de la emisi\\u00F3n de la factura\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"RGO-20261001-1A2B3C4D\",\"issuedAt\":\"2026-10-01T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260701810\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2607181\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610N\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Callao (PECLL)\"}],\"sections\":[{\"heading\":\"Pagador\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT\",\"value\":\"96555444-3\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Pago\",\"fields\":[{\"label\":\"Concepto\",\"value\":\"Gate Out - 40HC (San Antonio)\"},{\"label\":\"Monto del cargo\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Monto pagado\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Tipo de cambio\",\"value\":null},{\"label\":\"RUT de facturaci\\u00F3n\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda (96555444-3)\"},{\"label\":\"Pago\",\"value\":\"PAY-20261001-D4E5F6A7\"},{\"label\":\"Comprobante de pago\",\"value\":\"RCP-20261001-E8F9A0B1\"},{\"label\":\"Medio de pago\",\"value\":\"KHIPU\"},{\"label\":\"Fecha de pago\",\"value\":\"01-10-2026 11:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3071801\",\"40HC\",\"SL-071801\",\"24.800,00\",\"OnBoard\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Vinculaci\\u00F3n con la factura\",\"fields\":null,\"table\":null,\"paragraphs\":[\"La factura del Gate Out se emite despu\\u00E9s del zarpe de la nave y se vincula a este recibo.\",\"El cargo pagado con este recibo no vuelve a cobrarse al cliente.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"CBA5-6DA7-95A4-8C6B\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000006"),
                column: "TemplateJson",
                value: "{\"title\":\"Certificado de flete\",\"subtitle\":\"Importaci\\u00F3n - Bolivia\",\"issuer\":\"Hapag-Lloyd Bolivia S.R.L.\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Bolivia\",\"documentNumber\":\"CFL-20261005-3B4C5D6E\",\"issuedAt\":\"2026-10-05T14:00:00Z\",\"timeZoneId\":\"America/La_Paz\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUIQQ260200078\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2602078\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"BO\"},{\"label\":\"Nave / viaje\",\"value\":\"Guayaquil Express / 007W\"},{\"label\":\"Ruta\",\"value\":\"Mumbai (INBOM) - Iquique (CLIQQ)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Mumbai Spices \\u0026 Commodities Pvt Ltd\"},{\"label\":\"Consignee\",\"value\":\"Comercial Altiplano SRL\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitud\",\"value\":\"SRV-20261005-5E1A0010\"},{\"label\":\"Solicitada por\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Consignatario\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Finalidad\",\"value\":\"Tr\\u00E1mite aduanero\"},{\"label\":\"Dirigido a\",\"value\":\"Aduana Nacional de Bolivia\"},{\"label\":\"Observaciones\",\"value\":\"Para la declaraci\\u00F3n de importaci\\u00F3n (DIM).\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":null},{\"label\":\"Monto del flete\",\"value\":\"1.950,00 USD\"},{\"label\":\"Puerto de carga\",\"value\":\"Mumbai (INBOM)\"},{\"label\":\"Puerto de descarga\",\"value\":\"Iquique (CLIQQ)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santa Cruz, Bolivia\"}],\"table\":null,\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU5566778\",\"40HC\",\"SL-015678\",\"21.300,00\",\"OnBoard\"],[\"HLXU5566779\",\"20DV\",\"SL-015679\",\"14.900,00\",\"Discharged\"]],\"numericColumns\":[3],\"columnWidths\":null},\"paragraphs\":null,\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Bolivia S.R.L. certifica que el flete mar\\u00EDtimo de la carga amparada en el BL HLCUIQQ260200078, transportada en la nave Guayaquil Express viaje 007W desde Mumbai (INBOM) hasta Santa Cruz, Bolivia, asciende a 1.950,00 USD, seg\\u00FAn el registro del embarque a la fecha de emisi\\u00F3n.\",\"Se emite a solicitud del interesado para la finalidad declarada.\"],\"totals\":null,\"steps\":null,\"note\":null,\"noteTone\":null}],\"verificationCode\":\"7900-1AF6-3B9B-DCF4\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\",\"qrPayload\":null,\"statusLabel\":null,\"statusTone\":null,\"highlight\":null}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderCheckedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RedirectForm",
                table: "Payments");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000001"),
                column: "TemplateJson",
                value: "{\"title\":\"Certificado de transbordo\",\"subtitle\":\"Operaci\\u00F3n de exportaci\\u00F3n\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CTB-20261004-5A1B2C3D\",\"issuedAt\":\"2026-10-04T13:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260300610\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2603061\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610S\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Rotterdam (NLRTM)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Consignee\",\"value\":\"Fruit Import BV\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Cliente\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76123456-7\"},{\"label\":\"Pago\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU2023001\",\"40RF\",\"SL-020301\",\"26.800,00\",\"GateIn\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Chile SpA certifica que la carga amparada en el BL HLCUSAI260300610, transportada en la nave Valparaiso Express viaje 2610S, con origen en San Antonio (CLSAI) y destino Rotterdam, Netherlands, fue objeto de transbordo en el puerto de Rotterdam (NLRTM) en las unidades individualizadas en este documento.\",\"Se emite a solicitud del interesado para los fines que estime convenientes.\"]}],\"verificationCode\":\"EF67-5D1A-FCD1-C29B\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000002"),
                column: "TemplateJson",
                value: "{\"title\":\"Copia de BL - no valorada\",\"subtitle\":\"Copia informativa, no negociable\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CBL-20261003-7E8F9A0B\",\"issuedAt\":\"2026-10-03T16:30:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAL250100123\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2501001\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Shanghai Electronics Co. Ltd\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Transporte\",\"fields\":[{\"label\":\"Nave / viaje\",\"value\":\"Hamburg Express / 025E\"},{\"label\":\"Puerto de carga\",\"value\":\"Shanghai (CNSHA)\"},{\"label\":\"Puerto de descarga\",\"value\":\"San Antonio (CLSAI)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santiago, Chile\"},{\"label\":\"ETD\",\"value\":\"01-03-2026\"},{\"label\":\"ETA\",\"value\":\"05-04-2026\"},{\"label\":\"Tipo de BL\",\"value\":null},{\"label\":\"Incoterm\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU1234567\",\"40HC\",\"SL-001234\",\"24.500,00\",\"Discharged\"],[\"HLXU7654321\",\"20DV\",\"SL-005678\",\"18.200,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"]},{\"heading\":\"Valores comerciales\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Copia no valorada: no incluye el flete ni los cargos del embarque.\"]},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitada por\",\"value\":\"Importadora Demo SpA (demo@importadorademo.cl)\"}],\"table\":null,\"paragraphs\":null}],\"verificationCode\":\"90FF-9F4C-557C-3C9D\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000003"),
                column: "TemplateJson",
                value: "{\"title\":\"Comprobante de flete Collect\",\"subtitle\":null,\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CCO-20261003-1C2D3E4F\",\"issuedAt\":\"2026-10-03T15:05:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260501240\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605124\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Cartagena Express / 2611E\"},{\"label\":\"Ruta\",\"value\":\"Yokohama (JPYOK) - San Antonio (CLSAI)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Yokohama Machinery Co.\"},{\"label\":\"Consignee\",\"value\":\"Importadora Demo SpA\"},{\"label\":\"Notify\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Pago del flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":\"Collect\"},{\"label\":\"Monto pagado\",\"value\":\"4.800,00 USD\"},{\"label\":\"Pagador\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT del pagador\",\"value\":\"96555444-3\"},{\"label\":\"Pago\",\"value\":null},{\"label\":\"Comprobante de pago\",\"value\":null},{\"label\":\"Fecha de pago\",\"value\":\"03-10-2026 12:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045001\",\"40HC\",\"SL-045001\",\"25.100,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null}],\"verificationCode\":\"B3D6-27E7-7995-FF24\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000004"),
                column: "TemplateJson",
                value: "{\"title\":\"Carta de responsabilidad\",\"subtitle\":\"T\\u00E9rminos CARTA-RESP-2026-10\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"CRE-20261002-9F8E7D6C\",\"issuedAt\":\"2026-10-02T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUVAP260501350\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2605135\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Callao Express / 2611N\"},{\"label\":\"Ruta\",\"value\":\"Shanghai (CNSHA) - Valparaiso (CLVAP)\"}],\"sections\":[{\"heading\":\"Organizaci\\u00F3n responsable\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Global Forwarding Chile SpA\"},{\"label\":\"RUT / NIT\",\"value\":\"76000003-3\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Firmante\",\"fields\":[{\"label\":\"Nombre\",\"value\":\"Felipe Forwarder\"},{\"label\":\"Documento de identidad\",\"value\":\"12.345.678-5\"},{\"label\":\"Cargo\",\"value\":\"Gerente de Operaciones\"},{\"label\":\"Correo de contacto\",\"value\":\"ffww@globalforwarding.cl\"},{\"label\":\"Tel\\u00E9fono\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3045002\",\"20DV\",\"SL-045002\",\"17.900,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Declaraci\\u00F3n\",\"fields\":[{\"label\":\"Mercanc\\u00EDa\",\"value\":\"Muebles de madera\"},{\"label\":\"Observaciones\",\"value\":null}],\"table\":null,\"paragraphs\":[\"El firmante, en representaci\\u00F3n de la organizaci\\u00F3n indicada, declara que los datos ingresados son ver\\u00EDdicos y asume ante Hapag-Lloyd la responsabilidad por la carga amparada en el BL individualizado, incluidos los cargos, demoras y perjuicios que se originen por su retiro y manipulaci\\u00F3n, liberando a Hapag-Lloyd de toda responsabilidad frente al consignatario final y a terceros.\",\"T\\u00E9rminos aceptados en el portal el 02-10-2026 11:00 (America/Santiago) (versi\\u00F3n CARTA-RESP-2026-10).\"]}],\"verificationCode\":\"94E7-43DA-1F4E-8AA7\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000005"),
                column: "TemplateJson",
                value: "{\"title\":\"Recibo de pago anticipado - Gate Out\",\"subtitle\":\"Pago recibido antes de la emisi\\u00F3n de la factura\",\"issuer\":\"Hapag-Lloyd Chile SpA\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Chile\",\"documentNumber\":\"RGO-20261001-1A2B3C4D\",\"issuedAt\":\"2026-10-01T14:00:00Z\",\"timeZoneId\":\"America/Santiago\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUSAI260701810\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2607181\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Exportaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"CL\"},{\"label\":\"Nave / viaje\",\"value\":\"Valparaiso Express / 2610N\"},{\"label\":\"Ruta\",\"value\":\"San Antonio (CLSAI) - Callao (PECLL)\"}],\"sections\":[{\"heading\":\"Pagador\",\"fields\":[{\"label\":\"Raz\\u00F3n social\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda\"},{\"label\":\"RUT / NIT\",\"value\":\"96555444-3\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Pago\",\"fields\":[{\"label\":\"Concepto\",\"value\":\"Gate Out - 40HC (San Antonio)\"},{\"label\":\"Monto del cargo\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Monto pagado\",\"value\":\"71.400,00 CLP\"},{\"label\":\"Tipo de cambio\",\"value\":null},{\"label\":\"RUT de facturaci\\u00F3n\",\"value\":\"Agencia Mar\\u00EDtima del Pac\\u00EDfico Ltda (96555444-3)\"},{\"label\":\"Pago\",\"value\":\"PAY-20261001-D4E5F6A7\"},{\"label\":\"Comprobante de pago\",\"value\":\"RCP-20261001-E8F9A0B1\"},{\"label\":\"Medio de pago\",\"value\":\"KHIPU\"},{\"label\":\"Fecha de pago\",\"value\":\"01-10-2026 11:00 (America/Santiago)\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU3071801\",\"40HC\",\"SL-071801\",\"24.800,00\",\"OnBoard\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Vinculaci\\u00F3n con la factura\",\"fields\":null,\"table\":null,\"paragraphs\":[\"La factura del Gate Out se emite despu\\u00E9s del zarpe de la nave y se vincula a este recibo.\",\"El cargo pagado con este recibo no vuelve a cobrarse al cliente.\"]}],\"verificationCode\":\"CBA5-6DA7-95A4-8C6B\",\"signatureNote\":null,\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");

            migrationBuilder.UpdateData(
                table: "ShipmentDocuments",
                keyColumn: "Id",
                keyValue: new Guid("ffffffff-0018-0018-0018-000000000006"),
                column: "TemplateJson",
                value: "{\"title\":\"Certificado de flete\",\"subtitle\":\"Importaci\\u00F3n - Bolivia\",\"issuer\":\"Hapag-Lloyd Bolivia S.R.L.\",\"issuerDetail\":\"Agente de Hapag-Lloyd AG - Operaci\\u00F3n Bolivia\",\"documentNumber\":\"CFL-20261005-3B4C5D6E\",\"issuedAt\":\"2026-10-05T14:00:00Z\",\"timeZoneId\":\"America/La_Paz\",\"references\":[{\"label\":\"BL\",\"value\":\"HLCUIQQ260200078\"},{\"label\":\"Booking\",\"value\":\"HLCUBKG2602078\"},{\"label\":\"Operaci\\u00F3n\",\"value\":\"Importaci\\u00F3n\"},{\"label\":\"Pa\\u00EDs\",\"value\":\"BO\"},{\"label\":\"Nave / viaje\",\"value\":\"Guayaquil Express / 007W\"},{\"label\":\"Ruta\",\"value\":\"Mumbai (INBOM) - Iquique (CLIQQ)\"}],\"sections\":[{\"heading\":\"Partes\",\"fields\":[{\"label\":\"Shipper\",\"value\":\"Mumbai Spices \\u0026 Commodities Pvt Ltd\"},{\"label\":\"Consignee\",\"value\":\"Comercial Altiplano SRL\"},{\"label\":\"Notify\",\"value\":null}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Solicitud\",\"fields\":[{\"label\":\"Solicitud\",\"value\":\"SRV-20261005-5E1A0010\"},{\"label\":\"Solicitada por\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Consignatario\",\"value\":\"Comercial Altiplano SRL (1023456017)\"},{\"label\":\"Finalidad\",\"value\":\"Tr\\u00E1mite aduanero\"},{\"label\":\"Dirigido a\",\"value\":\"Aduana Nacional de Bolivia\"},{\"label\":\"Observaciones\",\"value\":\"Para la declaraci\\u00F3n de importaci\\u00F3n (DIM).\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Flete\",\"fields\":[{\"label\":\"Condici\\u00F3n del flete\",\"value\":null},{\"label\":\"Monto del flete\",\"value\":\"1.950,00 USD\"},{\"label\":\"Puerto de carga\",\"value\":\"Mumbai (INBOM)\"},{\"label\":\"Puerto de descarga\",\"value\":\"Iquique (CLIQQ)\"},{\"label\":\"Lugar de entrega\",\"value\":\"Santa Cruz, Bolivia\"}],\"table\":null,\"paragraphs\":null},{\"heading\":\"Unidades\",\"fields\":null,\"table\":{\"headers\":[\"Contenedor\",\"Tipo\",\"Sello\",\"Peso (kg)\",\"Estado\"],\"rows\":[[\"HLXU5566778\",\"40HC\",\"SL-015678\",\"21.300,00\",\"OnBoard\"],[\"HLXU5566779\",\"20DV\",\"SL-015679\",\"14.900,00\",\"Discharged\"]],\"numericColumns\":[3]},\"paragraphs\":null},{\"heading\":\"Mercanc\\u00EDa\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Sin detalle de mercanc\\u00EDa registrado.\"]},{\"heading\":\"Certificaci\\u00F3n\",\"fields\":null,\"table\":null,\"paragraphs\":[\"Hapag-Lloyd Bolivia S.R.L. certifica que el flete mar\\u00EDtimo de la carga amparada en el BL HLCUIQQ260200078, transportada en la nave Guayaquil Express viaje 007W desde Mumbai (INBOM) hasta Santa Cruz, Bolivia, asciende a 1.950,00 USD, seg\\u00FAn el registro del embarque a la fecha de emisi\\u00F3n.\",\"Se emite a solicitud del interesado para la finalidad declarada.\"]}],\"verificationCode\":\"7900-1AF6-3B9B-DCF4\",\"signatureNote\":\"Documento firmado electr\\u00F3nicamente. La validez de la firma se acredita seg\\u00FAn el mecanismo publicado por Hapag-Lloyd.\",\"footer\":\"Documento emitido por el Portal de Clientes de Hapag-Lloyd. Verifique su emisi\\u00F3n con el c\\u00F3digo indicado.\"}");
        }
    }
}
