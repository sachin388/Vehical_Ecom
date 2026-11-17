# Vehicle E-Commerce Platform - Database Scripts & Seed Data

This directory contains standalone, production-grade Transact-SQL (T-SQL) database scripts for all microservices across the solution. Every service script provisions its corresponding database, enforces primary and foreign key constraints, creates performance-optimized indexes, and seeds **exactly 1,000 realistic records per table** using 50+ diverse global and domestic vehicle makes, models, variants, specifications, accessories, geographic dealers, pricing breakdowns, orders, payment transactions, and notifications.

---

## Service & Database Matrix

| Service | Target Database | Tables Provisioned (1,000 Records Each) | Script |
| :--- | :--- | :--- | :--- |
| **AuthService** | `VehicleEcommerceAuthDb` | `Roles`, `Users`, `UserRoles`, `RefreshTokens` | [01_AuthService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/01_AuthService.sql) |
| **Items (Catalog)** | `VehicleEcommerceDev` | `VehicleCategories`, `VehicleMakes`, `VehicleModels`, `VehicleVariants`, `VehicleSpecifications`, `Items` | [02_ItemsService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/02_ItemsService.sql) |
| **ItemsPrice** | `VehicleEcommerce_ItemPriceDb` | `ItemPrices`, `PriceDiscounts` | [03_ItemsPriceService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/03_ItemsPriceService.sql) |
| **ItemsPriceBreakup** | `PriceBreakupDb` | `PriceBreakups`, `PriceBreakupLines` | [04_ItemsPriceBreakupService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/04_ItemsPriceBreakupService.sql) |
| **Accessory** | `AccessoryDb` | `Accessories`, `AccessoryCompatibilities` | [05_AccessoryService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/05_AccessoryService.sql) |
| **SalesOrder** | `SalesOrderDb` | `SalesOrders`, `SalesOrderLines`, `SalesOrderLineAccessories`, `SalesOrderHistory`, `OrderCancellationSagas`, `ProcessedIntegrationEvents`, `OutboxMessages` | [06_SalesOrderService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/06_SalesOrderService.sql) |
| **Dealer** | `DealerDb` | `Dealers`, `DealerBranches`, `DealerInventories`, `DealerOrderAssignments` | [07_DealerService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/07_DealerService.sql) |
| **Payment** | `PaymentDb` | `Payments`, `PaymentTransactions`, `PaymentRefunds` | [08_PaymentService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/08_PaymentService.sql) |
| **NotificationSystem** | `NotificationDb` | `NotificationTemplates`, `Notifications`, `NotificationPreferences` | [09_NotificationService.sql](file:///d:/WorkSpace/Vehical_Ecom/Vehical_Ecom/DatabaseScripts/09_NotificationService.sql) |

**Total Database Tables Seeded**: **33 Tables**  
**Total Test Records Generated**: **33,000 Records** (1,000 per table)

---

## Realistic Vehicle Domain Data

The seed scripts use realistic automotive industry values:
- **Vehicle Makes (50+)**: Toyota, Honda, Ford, BMW, Mercedes-Benz, Audi, Porsche, Tesla, Hyundai, Kia, Volkswagen, Volvo, Lexus, Chevrolet, Nissan, Mazda, Subaru, Land Rover, Jaguar, Jeep, Ferrari, Lamborghini, Maserati, Alfa Romeo, Genesis, Dodge, Ram, Suzuki, Tata Motors, Mahindra, Aston Martin, Bentley, Rolls-Royce, McLaren, Bugatti, Lucid, Rivian, Polestar, BYD, Skoda, Renault, Peugeot, Mini, Acura, Infiniti, Cadillac, etc.
- **Vehicle Models**: Camry, Civic, Mustang, 3 Series, C-Class, A4, 911 Carrera, Model Y, Tucson, Sportage, Golf, XC60, RX 350, Silverado, Altima, CX-5, Outback, Defender, F-Pace, Wrangler, 296 GTB, Huracan, Grecale, Giulia, GV70, Nexon EV, XUV700, DB12, Air Sapphire, R1S, Seal, Octavia RS, etc.
- **Variants & Powertrains**: Dual Motor AWD Electric, Performance Tri-Motor Plaid, 2.5L Dynamic Force Hybrid e-CVT, 3.0L TwinPower Turbo Inline-6, 4.0L V8 Biturbo, 2.2L mHawk Diesel 4WD, etc.
- **Specifications**: Acceleration 0-100 km/h, Peak Horsepower, Torque, Range (WLTP/EPA), Battery Capacity (kWh), Charging Speeds, Drivetrain Specs.
- **Accessories**: 3D All-Weather Liners, 4K UHD Front/Rear Dashcams, Ceramic Pro 9H Coating, MagSafe Fast Chargers, Heavy-Duty Aero Roof Racks, Illuminated Door Sills, Nappa Leather Seats, Brembo Brake Caliper Kits.
- **Pricing & Taxes**: Realistic base prices (₹9.5 Lakh to ₹1.34 Cr), GST 28%, RTO State Tax, 3-Year Motor Insurance, FASTag kits, handling fees, and TCS 1%.
- **Dealers & Geographies**: Dealers across 10 major automotive hubs (Mumbai, Bengaluru, New Delhi, Hyderabad, Chennai, Pune, Ahmedabad, Kolkata, Gurgaon, Noida) with realistic GPS coordinates, contact details, branch inventories, and order assignments.
- **Order Lifecycle & Payments**: Full saga tracking (`Draft`, `PendingPayment`, `Confirmed`, `Allocated`, `Processing`, `Completed`, `Cancelled`), outbox messages, gateway references, and refund records.

---

## How to Execute the Scripts

### Option 1: Automated PowerShell Runner (Recommended)
From PowerShell inside `d:\WorkSpace\Vehical_Ecom\Vehical_Ecom\DatabaseScripts`:
```powershell
.\Run-AllDatabaseScripts.ps1 -Server "localhost"
```

For SQL Server Authentication:
```powershell
.\Run-AllDatabaseScripts.ps1 -Server "your_server" -Username "sa" -Password "YourPassword123!"
```

### Option 2: Execute Individual Scripts via `sqlcmd`
```cmd
sqlcmd -S localhost -i 01_AuthService.sql -b
sqlcmd -S localhost -i 02_ItemsService.sql -b
sqlcmd -S localhost -i 03_ItemsPriceService.sql -b
sqlcmd -S localhost -i 04_ItemsPriceBreakupService.sql -b
sqlcmd -S localhost -i 05_AccessoryService.sql -b
sqlcmd -S localhost -i 06_SalesOrderService.sql -b
sqlcmd -S localhost -i 07_DealerService.sql -b
sqlcmd -S localhost -i 08_PaymentService.sql -b
sqlcmd -S localhost -i 09_NotificationService.sql -b
```

### Option 3: SQL Server Management Studio (SSMS) or Azure Data Studio
Open each `.sql` file in SSMS or Azure Data Studio and click **Execute** (`F5`).
