using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HapagPortal.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<BillOfLading> BillsOfLading => Set<BillOfLading>();
    public DbSet<BLContainer> BLContainers => Set<BLContainer>();
    public DbSet<LocalCharge> LocalCharges => Set<LocalCharge>();
    public DbSet<DemurrageCharge> DemurrageCharges => Set<DemurrageCharge>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentDetail> PaymentDetails => Set<PaymentDetail>();
    public DbSet<CreditClient> CreditClients => Set<CreditClient>();
    public DbSet<DemurrageExemption> DemurrageExemptions => Set<DemurrageExemption>();
    public DbSet<WarehouseChange> WarehouseChanges => Set<WarehouseChange>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        SeedCurrencies(modelBuilder);
        SeedTaxConfigurations(modelBuilder);
        SeedAdminClientAndUser(modelBuilder);
        SeedFAQs(modelBuilder);
        SeedDemoData(modelBuilder);
    }

    private static void SeedCurrencies(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Currency>().HasData(
            new Currency
            {
                Id = SeedDataIds.CurrencyCLP,
                Code = "CLP",
                Name = "Peso Chileno",
                Symbol = "$",
                ExchangeRateToUSD = 950m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new Currency
            {
                Id = SeedDataIds.CurrencyBOB,
                Code = "BOB",
                Name = "Boliviano",
                Symbol = "Bs",
                ExchangeRateToUSD = 6.91m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new Currency
            {
                Id = SeedDataIds.CurrencyUSD,
                Code = "USD",
                Name = "US Dollar",
                Symbol = "$",
                ExchangeRateToUSD = 1m,
                LastUpdated = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedTaxConfigurations(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<TaxConfiguration>().HasData(
            new TaxConfiguration
            {
                Id = SeedDataIds.TaxIvaCL,
                Country = CountryCodes.Chile,
                ServiceType = "General",
                TaxName = "IVA",
                TaxRate = 19m,
                IsActive = true,
                EffectiveFrom = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new TaxConfiguration
            {
                Id = SeedDataIds.TaxIvaBO,
                Country = CountryCodes.Bolivia,
                ServiceType = "General",
                TaxName = "IVA",
                TaxRate = 13m,
                IsActive = true,
                EffectiveFrom = now,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedAdminClientAndUser(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12
        const string adminPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.AdminClient,
            Name = "Hapag-Lloyd Administrador",
            TaxId = "99999999-K",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "admin@hapag-lloyd.cl",
            Phone = "+56 2 2630 1700",
            ClientType = "Internal",
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.AdminUser,
            Username = "admin@hapag-lloyd.cl",
            Email = "admin@hapag-lloyd.cl",
            PasswordHash = adminPasswordHash,
            UserType = "Admin",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.AdminClient,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.AdminUserRole,
            UserId = SeedDataIds.AdminUser,
            RoleName = "Admin"
        });
    }

    private static void SeedFAQs(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<FAQ>().HasData(
            // Chile FAQs
            new FAQ
            {
                Id = SeedDataIds.FaqCL01,
                Question = "¿Cómo puedo consultar el estado de mi BL?",
                Answer = "Ingrese al módulo 'Bills of Lading', escriba su número de BL en el buscador y presione buscar. Verá el detalle completo incluyendo contenedores, cargos locales y demurrage.",
                Category = "SHIPPING",
                Country = CountryCodes.Chile,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL02,
                Question = "¿Qué métodos de pago están disponibles en Chile?",
                Answer = "En Chile puede pagar con Tarjeta de Crédito, Tarjeta de Débito, Transferencia Bancaria y WebPay. Todos los pagos electrónicos se procesan en tiempo real.",
                Category = "PAYMENTS",
                Country = CountryCodes.Chile,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL03,
                Question = "¿Cómo solicito un cambio de almacén?",
                Answer = "Vaya al módulo 'Cambio de Almacén', ingrese el número de BL, el contenedor, el almacén actual y el almacén destino. La solicitud será procesada y recibirá confirmación por correo.",
                Category = "SHIPPING",
                Country = CountryCodes.Chile,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL04,
                Question = "¿Cuál es la tasa de IVA aplicada en Chile?",
                Answer = "La tasa de IVA vigente en Chile es del 19%. Se aplica automáticamente sobre los cargos locales y servicios facturables.",
                Category = "PAYMENTS",
                Country = CountryCodes.Chile,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqCL05,
                Question = "¿Cómo genero un recibo de pago?",
                Answer = "Una vez confirmado el pago, vaya al detalle del pago y presione 'Generar Recibo'. El recibo se genera automáticamente en formato PDF con todos los datos fiscales.",
                Category = "DOCUMENTATION",
                Country = CountryCodes.Chile,
                SortOrder = 5,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Bolivia FAQs
            new FAQ
            {
                Id = SeedDataIds.FaqBO01,
                Question = "¿Cómo puedo consultar el estado de mi BL en Bolivia?",
                Answer = "Ingrese al módulo 'Bills of Lading' y busque por número de BL. Verá el estado de su carga incluyendo el puerto de ingreso (Arica, Iquique o Antofagasta) y los cargos asociados.",
                Category = "SHIPPING",
                Country = CountryCodes.Bolivia,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO02,
                Question = "¿Qué métodos de pago están disponibles en Bolivia?",
                Answer = "En Bolivia puede pagar mediante Transferencia Bancaria, Efectivo y Cheque. Los pagos en efectivo deben realizarse en oficinas autorizadas.",
                Category = "PAYMENTS",
                Country = CountryCodes.Bolivia,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO03,
                Question = "¿Cuál es la tasa de IVA aplicada en Bolivia?",
                Answer = "La tasa de IVA vigente en Bolivia es del 13%. Se aplica automáticamente sobre los cargos locales y servicios facturables. Los montos se manejan en Bolivianos (BOB).",
                Category = "PAYMENTS",
                Country = CountryCodes.Bolivia,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO04,
                Question = "¿Qué es el NIT y por qué lo necesito?",
                Answer = "El NIT (Número de Identificación Tributaria) es el identificador fiscal en Bolivia. Es obligatorio para el registro en el portal y para la emisión de documentos fiscales.",
                Category = "GENERAL",
                Country = CountryCodes.Bolivia,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqBO05,
                Question = "¿Cómo funciona el demurrage para carga en tránsito a Bolivia?",
                Answer = "El demurrage se calcula desde la fecha de descarga en el puerto chileno. Los días libres y tarifas diarias dependen del tipo de contenedor y acuerdos comerciales. Puede solicitar exenciones a través del módulo de Demurrage.",
                Category = "DEMURRAGE",
                Country = CountryCodes.Bolivia,
                SortOrder = 5,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // General FAQs (both countries)
            new FAQ
            {
                Id = SeedDataIds.FaqGen01,
                Question = "¿Cómo registro mi empresa en el portal?",
                Answer = "Haga clic en 'Registrarse', seleccione su país (Chile o Bolivia), ingrese los datos de su empresa (RUT/NIT, nombre, correo) y cree una contraseña. Recibirá un correo de confirmación.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 10,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqGen02,
                Question = "¿Olvidé mi contraseña, cómo la recupero?",
                Answer = "En la pantalla de login, haga clic en '¿Olvidó su contraseña?'. Ingrese su correo electrónico y recibirá un enlace para restablecer su contraseña.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 11,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new FAQ
            {
                Id = SeedDataIds.FaqGen03,
                Question = "¿Cómo contacto a soporte técnico?",
                Answer = "Puede contactarnos al correo clservice@hapag-lloyd.com o llamar al +56 2 2630 1700 (Chile) / +591 2 211 0700 (Bolivia) en horario de oficina de lunes a viernes.",
                Category = "GENERAL",
                Country = CountryCodes.Chile,
                SortOrder = 12,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });
    }

    private static void SeedDemoData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // BCrypt hash of "Admin123!" with work factor 12 (reused for all demo users)
        const string demoPasswordHash = "$2a$12$cSxUVEI1bQ2CYNR.7dqfz.FTbfVBKY0xLO6/rDbvCvQ54ildbUKca";

        // ──────────────────────────────────────────────────────────────
        // CLIENTS & USERS
        // ──────────────────────────────────────────────────────────────

        // Demo client — Chilean importer
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.DemoClientCL,
            Name = "Importadora Demo SpA",
            TaxId = "76.123.456-7",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "demo@importadorademo.cl",
            Phone = "+56 2 2345 6789",
            Address = "Av. Providencia 1234, Of. 501",
            City = "Santiago",
            ClientType = "Client",
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.DemoUserCL,
            Username = "demo@importadorademo.cl",
            Email = "demo@importadorademo.cl",
            PasswordHash = demoPasswordHash,
            UserType = "Client",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.DemoUserRoleCL,
            UserId = SeedDataIds.DemoUserCL,
            RoleName = "User"
        });

        // Demo client — Bolivian importer
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.DemoClientBO,
            Name = "Comercial Altiplano SRL",
            TaxId = "1023456017",
            TaxIdType = "NIT",
            Country = CountryCodes.Bolivia,
            Email = "demo@altiplano.bo",
            Phone = "+591 2 211 5678",
            Address = "Calle Comercio 456",
            City = "La Paz",
            ClientType = "Client",
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.DemoUserBO,
            Username = "demo@altiplano.bo",
            Email = "demo@altiplano.bo",
            PasswordHash = demoPasswordHash,
            UserType = "Client",
            Country = CountryCodes.Bolivia,
            IsActive = true,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.DemoUserRoleBO,
            UserId = SeedDataIds.DemoUserBO,
            RoleName = "User"
        });

        // Agent client — Chilean freight agent
        modelBuilder.Entity<Client>().HasData(new Client
        {
            Id = SeedDataIds.AgentClientCL,
            Name = "Agencia Marítima del Pacífico Ltda",
            TaxId = "96.555.444-3",
            TaxIdType = "RUT",
            Country = CountryCodes.Chile,
            Email = "agente@maritimpacifico.cl",
            Phone = "+56 32 225 1000",
            Address = "Blanco 1199, Of. 301",
            City = "Valparaíso",
            ClientType = "Agent",
            AgentCode = "AGT-CL-001",
            IsActive = true,
            IsEmailConfirmed = true,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SeedDataIds.AgentUserCL,
            Username = "agente@maritimpacifico.cl",
            Email = "agente@maritimpacifico.cl",
            PasswordHash = demoPasswordHash,
            UserType = "Agent",
            Country = CountryCodes.Chile,
            IsActive = true,
            ClientId = SeedDataIds.AgentClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            Id = SeedDataIds.AgentUserRoleCL,
            UserId = SeedDataIds.AgentUserCL,
            RoleName = "User"
        });

        // ──────────────────────────────────────────────────────────────
        // BILLS OF LADING (5 total: 3 CL, 2 BO)
        // ──────────────────────────────────────────────────────────────

        // BL 1 — Chile, Arrived, 2 containers
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL01,
            BLNumber = "HLCUVAL250100123",
            ShipmentType = "Import",
            Vessel = "Hamburg Express",
            Voyage = "025E",
            PortOfLoading = "Shanghai (CNSHA)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            ETD = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Importadora Demo SpA",
            Shipper = "Shanghai Electronics Co. Ltd",
            NotifyParty = "Agencia Marítima del Pacífico Ltda",
            FreightAmount = 3500m,
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 2 — Chile, InTransit, 2 containers
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL02,
            BLNumber = "HLCUVAL250200456",
            ShipmentType = "Import",
            Vessel = "Berlin Express",
            Voyage = "031W",
            PortOfLoading = "Busan (KRPUS)",
            PortOfDischarge = "Valparaiso (CLVAP)",
            PlaceOfDelivery = "Valparaiso, Chile",
            ETD = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Importadora Demo SpA",
            Shipper = "Korea Auto Parts Inc.",
            FreightAmount = 5200m,
            FreightCurrency = "USD",
            Status = "InTransit",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.DemoClientCL,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 3 — Chile, Delivered, 1 container (admin client)
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL03,
            BLNumber = "HLCUVAL250300789",
            ShipmentType = "Import",
            Vessel = "Colombo Express",
            Voyage = "018E",
            PortOfLoading = "Rotterdam (NLRTM)",
            PortOfDischarge = "San Antonio (CLSAI)",
            PlaceOfDelivery = "Santiago, Chile",
            ETD = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Hapag-Lloyd Administrador",
            Shipper = "European Machinery GmbH",
            FreightAmount = 8750m,
            FreightCurrency = "USD",
            Status = "Delivered",
            Country = CountryCodes.Chile,
            ClientId = SeedDataIds.AdminClient,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 4 — Bolivia, Arrived via Arica, 1 container
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL04,
            BLNumber = "HLCUARI260100045",
            ShipmentType = "Import",
            Vessel = "Antofagasta Express",
            Voyage = "012E",
            PortOfLoading = "Ningbo (CNNGB)",
            PortOfDischarge = "Arica (CLARI)",
            PlaceOfDelivery = "La Paz, Bolivia",
            ETD = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Comercial Altiplano SRL",
            Shipper = "Ningbo Textiles Export Co.",
            FreightAmount = 2800m,
            FreightCurrency = "USD",
            Status = "Arrived",
            Country = CountryCodes.Bolivia,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // BL 5 — Bolivia, InTransit via Iquique, 1 container
        modelBuilder.Entity<BillOfLading>().HasData(new BillOfLading
        {
            Id = SeedDataIds.BL05,
            BLNumber = "HLCUIQQ260200078",
            ShipmentType = "Import",
            Vessel = "Guayaquil Express",
            Voyage = "007W",
            PortOfLoading = "Mumbai (INBOM)",
            PortOfDischarge = "Iquique (CLIQQ)",
            PlaceOfDelivery = "Santa Cruz, Bolivia",
            ETD = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            ETA = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            Consignee = "Comercial Altiplano SRL",
            Shipper = "Mumbai Spices & Commodities Pvt Ltd",
            FreightAmount = 1950m,
            FreightCurrency = "USD",
            Status = "InTransit",
            Country = CountryCodes.Bolivia,
            ClientId = SeedDataIds.DemoClientBO,
            CreatedAt = now,
            CreatedBy = "SYSTEM"
        });

        // ──────────────────────────────────────────────────────────────
        // CONTAINERS (7 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<BLContainer>().HasData(
            new BLContainer { Id = SeedDataIds.Container01, ContainerNumber = "HLXU1234567", ContainerType = "40HC", SealNumber = "SL-001234", Weight = 24500m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container02, ContainerNumber = "HLXU7654321", ContainerType = "20DV", SealNumber = "SL-005678", Weight = 18200m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container03, ContainerNumber = "HLXU9876543", ContainerType = "40HC", SealNumber = "SL-009012", Weight = 22100m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container04, ContainerNumber = "HLXU1112233", ContainerType = "40RF", SealNumber = "SL-003456", Weight = 19800m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container05, ContainerNumber = "HLXU4455667", ContainerType = "40OT", SealNumber = "SL-007890", Weight = 31500m, Status = "Delivered", BillOfLadingId = SeedDataIds.BL03, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container06, ContainerNumber = "HLXU8899001", ContainerType = "20DV", SealNumber = "SL-011234", Weight = 15600m, Status = "Discharged", BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" },
            new BLContainer { Id = SeedDataIds.Container07, ContainerNumber = "HLXU5566778", ContainerType = "40HC", SealNumber = "SL-015678", Weight = 21300m, Status = "OnBoard", BillOfLadingId = SeedDataIds.BL05, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // LOCAL CHARGES (8 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<LocalCharge>().HasData(
            // BL 1 charges (CL, Pending)
            new LocalCharge { Id = SeedDataIds.LocalCharge01, ChargeType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge02, ChargeType = "BL_FEE", Description = "BL Documentation Fee", Amount = 45000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 8550m, TotalAmount = 53550m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge03, ChargeType = "ISPS", Description = "ISPS Security Surcharge", Amount = 25000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 4750m, TotalAmount = 29750m, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 2 charges (CL, Pending)
            new LocalCharge { Id = SeedDataIds.LocalCharge04, ChargeType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 35150m, TotalAmount = 220150m, BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge05, ChargeType = "THC_RF", Description = "Terminal Handling Charge - 40RF Reefer", Amount = 295000m, Currency = "CLP", Status = "Pending", IsTaxable = true, TaxRate = 19m, TaxAmount = 56050m, TotalAmount = 351050m, BillOfLadingId = SeedDataIds.BL02, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 3 charges (CL, Paid)
            new LocalCharge { Id = SeedDataIds.LocalCharge06, ChargeType = "THC", Description = "Terminal Handling Charge - 40OT", Amount = 210000m, Currency = "CLP", Status = "Paid", IsTaxable = true, TaxRate = 19m, TaxAmount = 39900m, TotalAmount = 249900m, BillOfLadingId = SeedDataIds.BL03, CreatedAt = now, CreatedBy = "SYSTEM" },
            // BL 4 charges (BO, Pending — BOB currency, 13% IVA)
            new LocalCharge { Id = SeedDataIds.LocalCharge07, ChargeType = "THC", Description = "Terminal Handling Charge - 20DV (Arica)", Amount = 1280m, Currency = "BOB", Status = "Pending", IsTaxable = true, TaxRate = 13m, TaxAmount = 166.40m, TotalAmount = 1446.40m, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" },
            new LocalCharge { Id = SeedDataIds.LocalCharge08, ChargeType = "TRANSIT_FEE", Description = "Bolivia Transit Documentation Fee", Amount = 690m, Currency = "BOB", Status = "Pending", IsTaxable = true, TaxRate = 13m, TaxAmount = 89.70m, TotalAmount = 779.70m, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // DEMURRAGE CHARGES (3 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<DemurrageCharge>().HasData(
            new DemurrageCharge { Id = SeedDataIds.Demurrage01, ContainerNumber = "HLXU1234567", FreeDays = 7, DemurrageDays = 5, DailyRate = 45000m, TotalAmount = 225000m, Currency = "CLP", StartDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new DemurrageCharge { Id = SeedDataIds.Demurrage02, ContainerNumber = "HLXU7654321", FreeDays = 7, DemurrageDays = 3, DailyRate = 35000m, TotalAmount = 105000m, Currency = "CLP", StartDate = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL01, CreatedAt = now, CreatedBy = "SYSTEM" },
            new DemurrageCharge { Id = SeedDataIds.Demurrage03, ContainerNumber = "HLXU8899001", FreeDays = 10, DemurrageDays = 8, DailyRate = 310m, TotalAmount = 2480m, Currency = "BOB", StartDate = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), Status = "Pending", IsExempt = false, BillOfLadingId = SeedDataIds.BL04, CreatedAt = now, CreatedBy = "SYSTEM" });

        // ──────────────────────────────────────────────────────────────
        // PAYMENTS (8 total: all types, all statuses, all methods)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<Payment>().HasData(
            // Payment 1 — BL3 local charges, confirmed (WebPay) — Admin client
            new Payment
            {
                Id = SeedDataIds.Payment01,
                PaymentNumber = "PAY-2026-00001",
                PaymentType = "LocalCharges",
                PaymentMethod = "WebPay",
                Amount = 210000m,
                TaxAmount = 39900m,
                TotalAmount = 249900m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 2, 20, 14, 30, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 2, 20, 14, 31, 0, DateTimeKind.Utc),
                ConfirmedBy = "WEBPAY_AUTO",
                ExternalReference = "WBP-TXN-20260220-001",
                ReceiptNumber = "REC-2026-00001",
                ClientId = SeedDataIds.AdminClient,
                BillOfLadingId = SeedDataIds.BL03,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 2 — BL1 local charges, pending (bank transfer) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment02,
                PaymentNumber = "PAY-2026-00002",
                PaymentType = "LocalCharges",
                PaymentMethod = "BankTransfer",
                Amount = 230000m,
                TaxAmount = 43700m,
                TotalAmount = 273700m,
                Currency = "CLP",
                Status = "Pending",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc),
                DepositProofUrl = "/uploads/deposit-proof-002.pdf",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 3 — BL4 Bolivia charges, pending (Khipu/QR) — Demo BO
            new Payment
            {
                Id = SeedDataIds.Payment03,
                PaymentNumber = "PAY-2026-00003",
                PaymentType = "LocalCharges",
                PaymentMethod = "Khipu",
                Amount = 1970m,
                TaxAmount = 256.10m,
                TotalAmount = 2226.10m,
                Currency = "BOB",
                ExchangeRate = 6.91m,
                Status = "Pending",
                Country = CountryCodes.Bolivia,
                PaymentDate = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                DepositProofUrl = "/uploads/deposit-proof-003.pdf",
                ClientId = SeedDataIds.DemoClientBO,
                BillOfLadingId = SeedDataIds.BL04,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 4 — BL1 freight, confirmed (CreditCard) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment04,
                PaymentNumber = "PAY-2026-00004",
                PaymentType = "Freight",
                PaymentMethod = "CreditCard",
                Amount = 2850000m,
                TaxAmount = 541500m,
                TotalAmount = 3391500m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 3, 15, 11, 0, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 3, 15, 11, 2, 0, DateTimeKind.Utc),
                ConfirmedBy = "GATEWAY_AUTO",
                ExternalReference = "CC-TXN-20260315-004",
                ReceiptNumber = "REC-2026-00004",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 5 — BL2 demurrage, failed (WebPay) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment05,
                PaymentNumber = "PAY-2026-00005",
                PaymentType = "Demurrage",
                PaymentMethod = "WebPay",
                Amount = 330000m,
                TaxAmount = 62700m,
                TotalAmount = 392700m,
                Currency = "CLP",
                Status = "Failed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 5, 16, 0, 0, DateTimeKind.Utc),
                ExternalReference = "WBP-TXN-20260405-FAIL",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL02,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 6 — BL5 freight, cancelled — Demo BO
            new Payment
            {
                Id = SeedDataIds.Payment06,
                PaymentNumber = "PAY-2026-00006",
                PaymentType = "Freight",
                PaymentMethod = "BankTransfer",
                Amount = 4500m,
                TaxAmount = 585m,
                TotalAmount = 5085m,
                Currency = "BOB",
                ExchangeRate = 6.91m,
                Status = "Cancelled",
                Country = CountryCodes.Bolivia,
                PaymentDate = new DateTime(2026, 3, 28, 8, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.DemoClientBO,
                BillOfLadingId = SeedDataIds.BL05,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 7 — BL2 local charges, processing (WebPay) — Demo CL
            new Payment
            {
                Id = SeedDataIds.Payment07,
                PaymentNumber = "PAY-2026-00007",
                PaymentType = "LocalCharges",
                PaymentMethod = "WebPay",
                Amount = 480000m,
                TaxAmount = 91200m,
                TotalAmount = 571200m,
                Currency = "CLP",
                Status = "Processing",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 4, 12, 9, 30, 0, DateTimeKind.Utc),
                ExternalReference = "WBP-TXN-20260412-007",
                ClientId = SeedDataIds.DemoClientCL,
                BillOfLadingId = SeedDataIds.BL02,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Payment 8 — BL3 freight, confirmed (BankTransfer) — Agent CL
            new Payment
            {
                Id = SeedDataIds.Payment08,
                PaymentNumber = "PAY-2026-00008",
                PaymentType = "Freight",
                PaymentMethod = "BankTransfer",
                Amount = 1750000m,
                TaxAmount = 332500m,
                TotalAmount = 2082500m,
                Currency = "CLP",
                Status = "Confirmed",
                Country = CountryCodes.Chile,
                PaymentDate = new DateTime(2026, 2, 18, 15, 0, 0, DateTimeKind.Utc),
                ConfirmedAt = new DateTime(2026, 2, 19, 10, 0, 0, DateTimeKind.Utc),
                ConfirmedBy = "admin@hapag-lloyd.cl",
                ReceiptNumber = "REC-2026-00008",
                ClientId = SeedDataIds.AgentClientCL,
                BillOfLadingId = SeedDataIds.BL03,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // PAYMENT DETAILS (10 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<PaymentDetail>().HasData(
            // Payment 1 detail (BL3 THC)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail01, ConceptType = "THC", Description = "Terminal Handling Charge - 40OT", Amount = 210000m, Currency = "CLP", TaxAmount = 39900m, PaymentId = SeedDataIds.Payment01 },
            // Payment 2 details (BL1 THC + BL_FEE)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail02, ConceptType = "THC", Description = "Terminal Handling Charge - 40HC", Amount = 185000m, Currency = "CLP", TaxAmount = 35150m, PaymentId = SeedDataIds.Payment02 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail03, ConceptType = "BL_FEE", Description = "BL Documentation Fee", Amount = 45000m, Currency = "CLP", TaxAmount = 8550m, PaymentId = SeedDataIds.Payment02 },
            // Payment 3 details (BL4 THC + Transit Fee)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail04, ConceptType = "THC", Description = "Terminal Handling Charge - 20DV (Arica)", Amount = 1280m, Currency = "BOB", TaxAmount = 166.40m, PaymentId = SeedDataIds.Payment03 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail05, ConceptType = "TRANSIT_FEE", Description = "Bolivia Transit Documentation Fee", Amount = 690m, Currency = "BOB", TaxAmount = 89.70m, PaymentId = SeedDataIds.Payment03 },
            // Payment 4 detail (BL1 Freight)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail06, ConceptType = "Freight", Description = "Ocean Freight - Shanghai to Valparaiso", Amount = 2850000m, Currency = "CLP", TaxAmount = 541500m, PaymentId = SeedDataIds.Payment04 },
            // Payment 5 detail (BL2 Demurrage)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail07, ConceptType = "Demurrage", Description = "Demurrage charges - 8 days", Amount = 330000m, Currency = "CLP", TaxAmount = 62700m, PaymentId = SeedDataIds.Payment05 },
            // Payment 6 detail (BL5 Freight BO)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail08, ConceptType = "Freight", Description = "Ocean Freight - Santos to Arica", Amount = 4500m, Currency = "BOB", TaxAmount = 585m, PaymentId = SeedDataIds.Payment06 },
            // Payment 7 details (BL2 Local Charges)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail09, ConceptType = "THC", Description = "Terminal Handling Charge - 20DV", Amount = 185000m, Currency = "CLP", TaxAmount = 35150m, PaymentId = SeedDataIds.Payment07 },
            new PaymentDetail { Id = SeedDataIds.PaymentDetail10, ConceptType = "THC_RF", Description = "Reefer THC Surcharge", Amount = 295000m, Currency = "CLP", TaxAmount = 56050m, PaymentId = SeedDataIds.Payment07 },
            // Payment 8 detail (BL3 Freight)
            new PaymentDetail { Id = SeedDataIds.PaymentDetail11, ConceptType = "Freight", Description = "Flete marítimo BL HLCUVAL250300789", Amount = 1750000m, Currency = "CLP", TaxAmount = 332500m, PaymentId = SeedDataIds.Payment08 });

        // ──────────────────────────────────────────────────────────────
        // CREDIT CLIENTS (4 total: various statuses)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<CreditClient>().HasData(
            // Agent CL — Approved, high limit
            new CreditClient
            {
                Id = SeedDataIds.CreditClient01,
                Country = CountryCodes.Chile,
                CreditLimit = 50000000m,
                CreditStatus = "Approved",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.AgentClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Demo CL — PendingApproval
            new CreditClient
            {
                Id = SeedDataIds.CreditClient02,
                Country = CountryCodes.Chile,
                CreditLimit = 15000000m,
                CreditStatus = "PendingApproval",
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Demo BO — Approved, BOB
            new CreditClient
            {
                Id = SeedDataIds.CreditClient03,
                Country = CountryCodes.Bolivia,
                CreditLimit = 350000m,
                CreditStatus = "Approved",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.DemoClientBO,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            // Admin — Suspended
            new CreditClient
            {
                Id = SeedDataIds.CreditClient04,
                Country = CountryCodes.Chile,
                CreditLimit = 100000000m,
                CreditStatus = "Suspended",
                ApprovedBy = "admin@hapag-lloyd.cl",
                ApprovedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                ClientId = SeedDataIds.AdminClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // DEMURRAGE EXEMPTIONS (2 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<DemurrageExemption>().HasData(
            new DemurrageExemption
            {
                Id = SeedDataIds.DemurrageExemption01,
                ClientName = "Agencia Marítima del Pacífico Ltda",
                TaxId = "96.555.444-3",
                Country = CountryCodes.Chile,
                Reason = "Acuerdo comercial preferencial — cliente con volumen superior a 500 TEU/año",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new DemurrageExemption
            {
                Id = SeedDataIds.DemurrageExemption02,
                ClientName = "Comercial Altiplano SRL",
                TaxId = "1023456017",
                Country = CountryCodes.Bolivia,
                Reason = "Exención por carga en tránsito internacional — convenio bilateral CL-BO",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // WAREHOUSE CHANGES (2 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<WarehouseChange>().HasData(
            new WarehouseChange
            {
                Id = SeedDataIds.WarehouseChange01,
                FromWarehouse = "STI San Antonio - Patio A",
                ToWarehouse = "Bodega Central Santiago",
                Amount = 120000m,
                Currency = "CLP",
                Status = "Approved",
                Country = CountryCodes.Chile,
                BillOfLadingId = SeedDataIds.BL01,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new WarehouseChange
            {
                Id = SeedDataIds.WarehouseChange02,
                FromWarehouse = "TPA Arica - Zona Franca",
                ToWarehouse = "Almacén Aduana La Paz",
                Amount = 850m,
                Currency = "BOB",
                Status = "Pending",
                Country = CountryCodes.Bolivia,
                BillOfLadingId = SeedDataIds.BL04,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // SERVICE ORDERS (3 total)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<ServiceOrder>().HasData(
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder01,
                OrderNumber = "SO-2026-00001",
                OrderType = "Inspection",
                Status = "Completed",
                Description = "Inspección fitosanitaria contenedor HLXU4455667 — maquinaria industrial procedente de Europa",
                Country = CountryCodes.Chile,
                RequestedAt = new DateTime(2026, 2, 16, 8, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2026, 2, 17, 15, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL03,
                ClientId = SeedDataIds.AdminClient,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder02,
                OrderNumber = "SO-2026-00002",
                OrderType = "WarehouseRelease",
                Status = "InProgress",
                Description = "Solicitud de retiro contenedores HLXU1234567 y HLXU7654321 — electrónicos importados",
                Country = CountryCodes.Chile,
                RequestedAt = new DateTime(2026, 4, 8, 10, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL01,
                ClientId = SeedDataIds.DemoClientCL,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            },
            new ServiceOrder
            {
                Id = SeedDataIds.ServiceOrder03,
                OrderNumber = "SO-2026-00003",
                OrderType = "TransitDocumentation",
                Status = "Pending",
                Description = "Documentación de tránsito internacional Arica → La Paz para contenedor HLXU8899001",
                Country = CountryCodes.Bolivia,
                RequestedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                BillOfLadingId = SeedDataIds.BL04,
                ClientId = SeedDataIds.DemoClientBO,
                CreatedAt = now,
                CreatedBy = "SYSTEM"
            });

        // ──────────────────────────────────────────────────────────────
        // AUDIT LOGS (5 total — sample activity trail)
        // ──────────────────────────────────────────────────────────────

        modelBuilder.Entity<AuditLog>().HasData(
            new AuditLog { Id = SeedDataIds.AuditLog01, EntityName = "Payment", EntityId = SeedDataIds.Payment01.ToString(), Action = "Created", NewValues = "{\"PaymentNumber\":\"PAY-2026-00001\",\"Status\":\"Pending\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 2, 20, 14, 30, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog02, EntityName = "Payment", EntityId = SeedDataIds.Payment01.ToString(), Action = "Updated", OldValues = "{\"Status\":\"Pending\"}", NewValues = "{\"Status\":\"Confirmed\"}", UserId = "WEBPAY_AUTO", Timestamp = new DateTime(2026, 2, 20, 14, 31, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog03, EntityName = "ServiceOrder", EntityId = SeedDataIds.ServiceOrder01.ToString(), Action = "Created", NewValues = "{\"OrderNumber\":\"SO-2026-00001\",\"OrderType\":\"Inspection\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 2, 16, 8, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog04, EntityName = "WarehouseChange", EntityId = SeedDataIds.WarehouseChange01.ToString(), Action = "Created", NewValues = "{\"FromWarehouse\":\"STI San Antonio - Patio A\",\"ToWarehouse\":\"Bodega Central Santiago\"}", UserId = SeedDataIds.DemoUserCL.ToString(), Timestamp = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc) },
            new AuditLog { Id = SeedDataIds.AuditLog05, EntityName = "CreditClient", EntityId = SeedDataIds.CreditClient01.ToString(), Action = "Updated", OldValues = "{\"CreditStatus\":\"PendingApproval\"}", NewValues = "{\"CreditStatus\":\"Approved\"}", UserId = SeedDataIds.AdminUser.ToString(), Timestamp = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc) });
    }
}
