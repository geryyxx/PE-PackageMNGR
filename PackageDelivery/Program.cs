using Microsoft.EntityFrameworkCore;
using PackageDelivery.Data;
using PackageDelivery.Enums;
using PackageDelivery.Models;
using PackageDelivery.Services;
using System.Globalization;

namespace PackageDelivery;

internal class Program
{
    static void Main(string[] args)
    {
        using AppDbContext db = new AppDbContext();

        db.Database.Migrate();


        CustomerService customerService = new CustomerService(db);
        PackageService packageService = new PackageService(db);
        ReportService reportService = new ReportService(db);

        bool running = true;

        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("Csomagküldő szolgáltatás kezelő rendszere");
            Console.WriteLine();
            Console.WriteLine("1. Ügyfelek kezelése");
            Console.WriteLine("2. Új csomag feladása");
            Console.WriteLine("3. Csomag feldolgozása");
            Console.WriteLine("4. Csomag nyomkövetése");
            Console.WriteLine("5. Csomagok keresése");
            Console.WriteLine("6. Riportok");
            Console.WriteLine("0. Kilépés");
            Console.WriteLine();

            Console.Write("Választás: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ManageCustomers(customerService);
                    break;

                case "2":
                    StartPackageCreation(customerService, packageService);
                    break;

                case "3":
                    ProcessPackage(packageService);
                    break;

                case "4":
                    ShowPackageTracking(packageService);
                    break;

                case "5":
                    SearchPackages(customerService, packageService);
                    break;

                case "6":
                    ShowReports(reportService);
                    break;

                case "0":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Érvénytelen választás.");
                    break;
            }
        }
    }
    static void ManageCustomers(CustomerService customerService)
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("--- Ügyfelek kezelése ---");
            Console.WriteLine("1. Új ügyfél felvétele");
            Console.WriteLine("2. Ügyfelek listázása");
            Console.WriteLine("3. Ügyfél részletes adatai");
            Console.WriteLine("4. Ügyfél adatainak módosítása");
            Console.WriteLine("5. Ügyfél által feladott csomagok");
            Console.WriteLine("6. Ügyfélnek címzett csomagok");
            Console.WriteLine("0. Vissza a főmenübe");
            Console.WriteLine();

            Console.Write("Választás: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Név: ");
                    string name = Console.ReadLine() ?? "";

                    Console.Write("E-mail-cím: ");
                    string email = Console.ReadLine() ?? "";

                    Console.Write("Telefonszám: ");
                    string phone = Console.ReadLine() ?? "";

                    try
                    {
                        var customer = customerService.CreateCustomer(
                            name, email, phone);

                        Console.WriteLine(
                            $"Ügyfél sikeresen létrehozva! ID: {customer.Id}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Hiba: {ex.Message}");
                    }
                    break;

                case "2":
                    var customers = customerService.GetAllCustomers();

                    if (customers.Count == 0)
                    {
                        Console.WriteLine("Még nincs rögzített ügyfél.");
                    }
                    else
                    {
                        foreach (var customer in customers)
                        {
                            Console.WriteLine(
                                $"{customer.Id}. {customer.Name} | " +
                                $"{customer.Email} | {customer.PhoneNumber}");
                        }
                    }
                    break;

                case "3":
                    Console.Write("Ügyfél azonosítója: ");

                    if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
                    {
                        Console.WriteLine("Érvénytelen azonosító.");
                        break;
                    }

                    var selectedCustomer = customerService.GetCustomerById(id);

                    if (selectedCustomer == null)
                    {
                        Console.WriteLine("Nem található ilyen ügyfél.");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Azonosító: {selectedCustomer.Id}");
                        Console.WriteLine($"Név: {selectedCustomer.Name}");
                        Console.WriteLine($"E-mail: {selectedCustomer.Email}");
                        Console.WriteLine($"Telefonszám: {selectedCustomer.PhoneNumber}");
                    }
                    break;

                case "4":
                    Console.Write("Módosítandó ügyfél azonosítója: ");

                    if (!int.TryParse(Console.ReadLine(), out int customerId) || customerId <= 0)
                    {
                        Console.WriteLine("Érvénytelen azonosító.");
                        break;
                    }

                    var existingCustomer = customerService.GetCustomerById(customerId);

                    if (existingCustomer == null)
                    {
                        Console.WriteLine("Nem található ilyen ügyfél.");
                        break;
                    }

                    Console.WriteLine("Az üresen hagyott adatok nem változnak.");
                    Console.WriteLine();

                    Console.Write($"Név ({existingCustomer.Name}): ");
                    string newName = Console.ReadLine() ?? "";

                    Console.Write($"E-mail ({existingCustomer.Email}): ");
                    string newEmail = Console.ReadLine() ?? "";

                    Console.Write($"Telefonszám ({existingCustomer.PhoneNumber}): ");
                    string newPhone = Console.ReadLine() ?? "";

                    try
                    {
                        var updatedCustomer = customerService.UpdateCustomer(
                            customerId, newName, newEmail, newPhone);

                        Console.WriteLine("Az ügyfél adatai sikeresen módosítva!");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"Hiba: {ex.Message}");
                    }
                    break;

                case "5":
                case "6":
                    {
                        Console.Write("Ügyfél azonosítója: ");

                        if (!int.TryParse(Console.ReadLine(), out int packageCustomerId) || packageCustomerId <= 0)
                        {
                            Console.WriteLine("Érvénytelen azonosító.");
                            break;
                        }

                        var customer = customerService.GetCustomerById(packageCustomerId);

                        if (customer == null)
                        {
                            Console.WriteLine("Nem található ilyen ügyfél.");
                            break;
                        }

                        var packages = choice == "5"
                            ? customerService.GetSentPackages(packageCustomerId)
                            : customerService.GetReceivedPackages(packageCustomerId);

                        Console.WriteLine();
                        Console.WriteLine($"Ügyfél: {customer.Name}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine("Nem található csomag.");
                        }
                        else
                        {
                            foreach (var package in packages)
                            {
                                Console.WriteLine(
                                    $"Azonosító: {package.TrackingNumber} | " +
                                    $"Tömeg: {package.WeightKg} kg | " +
                                    $"Állapot: {package.Status} | " +
                                    $"Feladás: {package.ShippedAt:yyyy.MM.dd HH:mm}");
                            }
                        }

                        break;
                    }

                case "0":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Érvénytelen választás.");
                    break;
            }
        }
    }

    static void StartPackageCreation(
    CustomerService customerService,
    PackageService packageService)
    {
        Console.WriteLine();
        Console.WriteLine("--- Új csomag feladása ---");

        var customers = customerService.GetAllCustomers();

        if (customers.Count < 2)
        {
            Console.WriteLine(
                "A csomagfeladáshoz legalább két ügyfél szükséges.");
            return;
        }

        Console.WriteLine("Elérhető ügyfelek:");

        foreach (var customer in customers)
        {
            Console.WriteLine($"{customer.Id}. {customer.Name}");
        }

        Console.WriteLine();

        Console.Write("Feladó azonosítója: ");

        if (!int.TryParse(Console.ReadLine(), out int senderId)
            || senderId <= 0)
        {
            Console.WriteLine("Érvénytelen feladóazonosító.");
            return;
        }

        Console.Write("Címzett azonosítója: ");

        if (!int.TryParse(Console.ReadLine(), out int recipientId)
            || recipientId <= 0)
        {
            Console.WriteLine("Érvénytelen címzettazonosító.");
            return;
        }

        try
        {
            var (sender, recipient) =
                packageService.GetSenderAndRecipient(senderId, recipientId);

            Console.WriteLine();
            Console.WriteLine("Sikeres ügyfélválasztás!");
            Console.WriteLine($"Feladó: {sender.Name}");
            Console.WriteLine($"Címzett: {recipient.Name}");

            Console.WriteLine();
            Console.WriteLine("--- Kézbesítési cím ---");

            Console.Write("Irányítószám: ");
            string postalCode = (Console.ReadLine() ?? "").Trim();

            Console.Write("Település: ");
            string city = (Console.ReadLine() ?? "").Trim();

            Console.Write("Közterület neve: ");
            string streetName = (Console.ReadLine() ?? "").Trim();

            Console.Write("Közterület jellege: ");
            string streetType = (Console.ReadLine() ?? "").Trim();

            Console.Write("Házszám: ");
            string houseNumber = (Console.ReadLine() ?? "").Trim();

            Console.Write("További címadat (opcionális): ");
            string additionalInfo = (Console.ReadLine() ?? "").Trim();

            packageService.ValidateDeliveryAddress(
                postalCode,
                city,
                streetName,
                streetType,
                houseNumber);

            Console.WriteLine();
            Console.WriteLine("Kézbesítési cím sikeresen ellenőrizve!");

            Console.WriteLine($"Cím: {postalCode} {city}, " +
                $"{streetName} {streetType} {houseNumber}");

            if (!string.IsNullOrWhiteSpace(additionalInfo))
            {
                Console.WriteLine($"További címadat: {additionalInfo}");
            }

            Console.WriteLine();
            Console.WriteLine("--- Csomag adatai ---");
            Console.WriteLine("1. Normál csomag");
            Console.WriteLine("2. Törékeny csomag");
            Console.WriteLine("3. Hűtött csomag");

            Console.Write("Csomagtípus: ");
            string? selectedType = Console.ReadLine();

            Package? newPackage = selectedType switch
            {
                "1" => new NormalPackage(),
                "2" => new FragilePackage(),
                "3" => new RefrigeratedPackage(),
                _ => null
            };

            if (newPackage == null)
            {
                Console.WriteLine("Érvénytelen csomagtípus.");
                return;
            }

            Console.Write("Tömeg (kg): ");

            if (!double.TryParse(Console.ReadLine(), out double weight))
            {
                Console.WriteLine("Érvénytelen tömeg.");
                return;
            }

            newPackage.WeightKg = weight;

            packageService.ValidateWeight(newPackage);

            Console.WriteLine();
            Console.WriteLine("A csomag típusa és tömege megfelelő!");
            Console.WriteLine($"Típus: {newPackage.GetType().Name}");
            Console.WriteLine($"Tömeg: {newPackage.WeightKg} kg");

            switch (newPackage)
            {
                case FragilePackage fragilePackage:
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Törékeny csomag ---");
                        Console.WriteLine("1. Low");
                        Console.WriteLine("2. Medium");
                        Console.WriteLine("3. High");

                        Console.Write("Törékenységi szint: ");
                        string? levelChoice = Console.ReadLine();

                        FragilityLevel? level = levelChoice switch
                        {
                            "1" => FragilityLevel.Low,
                            "2" => FragilityLevel.Medium,
                            "3" => FragilityLevel.High,
                            _ => null
                        };

                        if (level == null)
                        {
                            Console.WriteLine("Érvénytelen törékenységi szint.");
                            return;
                        }

                        fragilePackage.Fragility = level.Value;

                        Console.Write("Kezelési utasítás: ");
                        fragilePackage.HandlingInstructions =
                            (Console.ReadLine() ?? "").Trim();

                        packageService.ValidateSpecialData(fragilePackage);

                        Console.WriteLine();
                        Console.WriteLine("Törékeny csomag adatai megfelelőek!");
                        Console.WriteLine($"Törékenység: {fragilePackage.Fragility}");
                        Console.WriteLine($"Utasítás: {fragilePackage.HandlingInstructions}");

                        break;
                    }

                case RefrigeratedPackage refrigeratedPackage:
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Hűtött csomag ---");

                        Console.Write("Minimum hőmérséklet (°C): ");

                        if (!double.TryParse(Console.ReadLine(), out double minTemp))
                        {
                            Console.WriteLine("Érvénytelen minimum hőmérséklet.");
                            return;
                        }

                        Console.Write("Maximum hőmérséklet (°C): ");

                        if (!double.TryParse(Console.ReadLine(), out double maxTemp))
                        {
                            Console.WriteLine("Érvénytelen maximum hőmérséklet.");
                            return;
                        }

                        refrigeratedPackage.MinTemperatureCelsius = minTemp;
                        refrigeratedPackage.MaxTemperatureCelsius = maxTemp;

                        packageService.ValidateSpecialData(refrigeratedPackage);

                        Console.WriteLine();
                        Console.WriteLine("Hűtött csomag adatai megfelelőek!");
                        Console.WriteLine($"Minimum: {minTemp} °C");
                        Console.WriteLine($"Maximum: {maxTemp} °C");

                        break;
                    }

                case NormalPackage:
                    {
                        Console.WriteLine("A normál csomag nem igényel speciális adatokat.");
                        break;
                    }
            }

            Package savedPackage = packageService.CreatePackage(
                newPackage,
                senderId,
                recipientId,
                postalCode,
                city,
                streetName,
                streetType,
                houseNumber,
                additionalInfo);

            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine("CSOMAG SIKERESEN FELADVA!");
            Console.WriteLine("=================================");
            Console.WriteLine($"Azonosító: {savedPackage.Id}");
            Console.WriteLine($"Nyomkövetési szám: {savedPackage.TrackingNumber}");
            Console.WriteLine($"Feladó: {sender.Name}");
            Console.WriteLine($"Címzett: {recipient.Name}");
            Console.WriteLine($"Tömeg: {savedPackage.WeightKg} kg");
            Console.WriteLine($"Állapot: {savedPackage.Status}");
            Console.WriteLine($"Feladás ideje: {savedPackage.ShippedAt:yyyy.MM.dd HH:mm}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Hiba: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Hiba: {ex.Message}");
        }
    }

    static void ShowPackageTracking(PackageService packageService)
    {
        Console.WriteLine();
        Console.WriteLine("--- Csomag nyomkövetése ---");

        Console.Write("Nyomkövetési azonosító: ");
        string trackingNumber = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            Console.WriteLine("A nyomkövetési azonosító kötelező.");
            return;
        }

        Package? package =
            packageService.GetPackageByTrackingNumber(trackingNumber);

        if (package == null)
        {
            Console.WriteLine("Nem található ilyen csomag.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=== Csomag adatai ===");

        Console.WriteLine($"Azonosító: {package.TrackingNumber}");

        string packageType = package switch
        {
            NormalPackage => "Normál",
            FragilePackage => "Törékeny",
            RefrigeratedPackage => "Hűtött",
            _ => "Ismeretlen"
        };

        Console.WriteLine($"Típus: {packageType}");
        Console.WriteLine($"Feladó: {package.Sender.Name}");
        Console.WriteLine($"Címzett: {package.Recipient.Name}");

        Console.WriteLine(
            $"Kézbesítési cím: {package.PostalCode} {package.City}, " +
            $"{package.StreetName} {package.StreetType} {package.HouseNumber}");

        if (!string.IsNullOrWhiteSpace(package.AdditionalAddressInfo))
        {
            Console.WriteLine(
                $"További címadat: {package.AdditionalAddressInfo}");
        }

        Console.WriteLine($"Tömeg: {package.WeightKg} kg");
        Console.WriteLine(
            $"Feladás: {package.ShippedAt:yyyy.MM.dd HH:mm}");

        Console.WriteLine($"Aktuális állapot: {package.Status}");

        // Csomagtípushoz tartozó speciális adatok
        switch (package)
        {
            case FragilePackage fragile:
                Console.WriteLine($"Törékenység: {fragile.Fragility}");
                Console.WriteLine(
                    $"Kezelési utasítás: {fragile.HandlingInstructions}");
                break;

            case RefrigeratedPackage refrigerated:
                Console.WriteLine(
                    $"Minimum hőmérséklet: {refrigerated.MinTemperatureCelsius} °C");
                Console.WriteLine(
                    $"Maximum hőmérséklet: {refrigerated.MaxTemperatureCelsius} °C");
                break;
        }

        // Nyomkövetési történet
        Console.WriteLine();
        Console.WriteLine("=== Nyomkövetési történet ===");

        var history = packageService.GetTrackingHistory(package.Id);

        if (history.Count == 0)
        {
            Console.WriteLine("Nincs nyomkövetési esemény.");
            return;
        }

        foreach (var trackingEvent in history)
        {
            Console.Write(
                $"{trackingEvent.OccurredAt:yyyy.MM.dd HH:mm} | " +
                $"{trackingEvent.Status}");

            if (trackingEvent.FailureReason.HasValue)
            {
                Console.Write($" | Ok: {trackingEvent.FailureReason.Value}");
            }

            if (!string.IsNullOrWhiteSpace(trackingEvent.Comment))
            {
                Console.Write($" | {trackingEvent.Comment}");
            }

            Console.WriteLine();
        }
    }

    static void ProcessPackage(PackageService packageService)
    {
        Console.WriteLine();
        Console.WriteLine("--- Csomag feldolgozása ---");

        Console.Write("Nyomkövetési azonosító: ");
        string trackingNumber = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            Console.WriteLine("A nyomkövetési azonosító kötelező.");
            return;
        }

        Package? package =
            packageService.GetPackageByTrackingNumber(trackingNumber);

        if (package == null)
        {
            Console.WriteLine("Nem található ilyen csomag.");
            return;
        }

        int failureCount = package.Status == PackageStatus.DeliveryFailed
            ? packageService.GetDeliveryFailureCount(package.Id)
            : 0;

        Console.WriteLine();
        Console.WriteLine($"Csomag: {package.TrackingNumber}");
        Console.WriteLine($"Aktuális állapot: {package.Status}");
        Console.WriteLine();

        switch (package.Status)
        {
            case PackageStatus.Registered:
                Console.WriteLine("1. Átvétel rögzítése");
                break;

            case PackageStatus.Accepted:
                Console.WriteLine("1. Elosztóközpontba érkezés rögzítése");
                break;

            case PackageStatus.AtDepot:
                Console.WriteLine("1. Kiszállítás megkezdése");
                break;

            case PackageStatus.OutForDelivery:
                Console.WriteLine("1. Sikeres kézbesítés rögzítése");
                Console.WriteLine("2. Sikertelen kézbesítés rögzítése");
                break;

            case PackageStatus.DeliveryFailed:
                {
                    Console.WriteLine(
                        $"Sikertelen kézbesítések száma: {failureCount}/3");

                    if (failureCount < 3)
                    {
                        Console.WriteLine("1. Új kézbesítési kísérlet");
                        Console.WriteLine("2. Visszaküldés indítása");
                    }
                    else
                    {
                        Console.WriteLine("1. Visszaküldés indítása");
                    }

                    break;
                }

            case PackageStatus.Returning:
                Console.WriteLine("1. Visszaküldés befejezése");
                break;

            default:
                Console.WriteLine(
                    "Ehhez az állapothoz még nincs feldolgozási művelet.");
                return;
        }

        Console.WriteLine("0. Vissza");
        Console.Write("Választás: ");

        string? choice = Console.ReadLine();

        if (choice == "0")
            return;

        bool allowSecondOption =
            package.Status == PackageStatus.OutForDelivery ||
            (package.Status == PackageStatus.DeliveryFailed &&
            failureCount < 3);

        if (choice != "1" && !(choice == "2" && allowSecondOption))
        {
            Console.WriteLine("Érvénytelen választás.");
            return;
        }

        try
        {
            Package updatedPackage;

            if (package.Status == PackageStatus.DeliveryFailed)
            {
                if (failureCount >= 3 || choice == "2")
                {
                    updatedPackage =
                        packageService.StartReturn(trackingNumber);
                }
                else
                {
                    updatedPackage =
                        packageService.RetryDelivery(trackingNumber);
                }
            }
            else if (package.Status == PackageStatus.Returning)
            {
                updatedPackage =
                    packageService.CompleteReturn(trackingNumber);
            }
            else if (package.Status == PackageStatus.OutForDelivery && choice == "2")
            {
                Console.WriteLine();
                Console.WriteLine("--- Sikertelen kézbesítés oka ---");
                Console.WriteLine("1. A címzett nem volt elérhető");
                Console.WriteLine("2. A cím nem volt megtalálható");
                Console.WriteLine("3. A címzett megtagadta az átvételt");

                Console.Write("Választás: ");
                string? reasonChoice = Console.ReadLine();

                DeliveryFailureReason? reason = reasonChoice switch
                {
                    "1" => DeliveryFailureReason.RecipientNotAvailable,
                    "2" => DeliveryFailureReason.AddressNotFound,
                    "3" => DeliveryFailureReason.RecipientRefused,
                    _ => null
                };

                if (reason == null)
                {
                    Console.WriteLine("Érvénytelen sikertelenségi ok.");
                    return;
                }

                updatedPackage = packageService.FailDelivery(
                    trackingNumber, reason.Value);
            }
            else if (package.Status == PackageStatus.OutForDelivery)
            {
                updatedPackage = packageService.CompleteDelivery(trackingNumber);
            }
            else
            {
                updatedPackage = packageService.AdvancePackage(trackingNumber);
            }

            Console.WriteLine();
            Console.WriteLine("Állapot sikeresen módosítva!");
            Console.WriteLine($"Új állapot: {updatedPackage.Status}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Hiba: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Hiba: {ex.Message}");
        }
    }


    static void SearchPackages(CustomerService customerService, PackageService packageService)
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("--- Csomagok keresése ---");
            Console.WriteLine("1. Keresés nyomkövetési azonosító alapján");
            Console.WriteLine("2. Keresés feladó alapján");
            Console.WriteLine("3. Keresés címzett alapján");
            Console.WriteLine("4. Keresés állapot alapján");
            Console.WriteLine("5. Keresés csomagtípus alapján");
            Console.WriteLine("6. Keresés feladási időszak alapján");
            Console.WriteLine("0. Vissza a főmenübe");
            Console.WriteLine();

            Console.Write("Választás: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    {
                        Console.Write("Nyomkövetési azonosító: ");
                        string trackingNumber = (Console.ReadLine() ?? "").Trim();

                        if (string.IsNullOrWhiteSpace(trackingNumber))
                        {
                            Console.WriteLine("A nyomkövetési azonosító kötelező.");
                            break;
                        }

                        Package? package =
                            packageService.GetPackageByTrackingNumber(trackingNumber);

                        if (package == null)
                        {
                            Console.WriteLine("Nem található ilyen csomag.");
                            break;
                        }

                        Console.WriteLine();
                        Console.WriteLine("--- Találat ---");

                        PrintPackageSearchResults(new List<Package> { package });

                        break;
                    }


                case "2":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Keresés feladó alapján ---");

                        var customers = customerService.GetAllCustomers();

                        if (!customers.Any())
                        {
                            Console.WriteLine("Nincs rögzített ügyfél.");
                            break;
                        }

                        Console.WriteLine("Választható ügyfelek:");

                        foreach (Customer customer in customers)
                        {
                            Console.WriteLine($"{customer.Id}. {customer.Name}");
                        }

                        Console.WriteLine();

                        Customer? sender = ReadExistingCustomer(
                            customerService,
                            "Feladó ügyfélazonosítója");

                        if (sender == null)
                            break;

                        List<Package> packages =
                            packageService.GetPackagesBySender(sender.Id);

                        Console.WriteLine();
                        Console.WriteLine($"Feladó: {sender.Name}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine("Az ügyfél még nem adott fel csomagot.");
                            break;
                        }
                        PrintPackageSearchResults(packages);

                        break;
                    }

                case "3":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Keresés címzett alapján ---");

                        var customers = customerService.GetAllCustomers();

                        if (!customers.Any())
                        {
                            Console.WriteLine("Nincs rögzített ügyfél.");
                            break;
                        }

                        Console.WriteLine("Választható ügyfelek:");

                        foreach (Customer customer in customers)
                        {
                            Console.WriteLine($"{customer.Id}. {customer.Name}");
                        }

                        Console.WriteLine();

                        Customer? recipient = ReadExistingCustomer(
                            customerService,
                            "Címzett ügyfélazonosítója");

                        if (recipient == null)
                            break;

                        List<Package> packages =
                            packageService.GetPackagesByRecipient(recipient.Id);

                        Console.WriteLine();
                        Console.WriteLine($"Címzett: {recipient.Name}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine(
                                "Az ügyfélnek még nem érkezett csomagja.");
                            break;
                        }

                        PrintPackageSearchResults(packages);

                        break;
                    }

                case "4":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Keresés állapot alapján ---");

                        PackageStatus[] statuses =
                            Enum.GetValues<PackageStatus>();

                        Console.WriteLine("Választható állapotok:");

                        for (int i = 0; i < statuses.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {statuses[i]}");
                        }

                        Console.WriteLine();
                        Console.Write("Állapot sorszáma: ");

                        if (!int.TryParse(Console.ReadLine(), out int statusNumber)
                            || statusNumber < 1
                            || statusNumber > statuses.Length)
                        {
                            Console.WriteLine("Érvénytelen állapot.");
                            break;
                        }

                        PackageStatus selectedStatus = statuses[statusNumber - 1];

                        List<Package> packages =
                            packageService.GetPackagesByStatus(selectedStatus);

                        Console.WriteLine();
                        Console.WriteLine($"Kiválasztott állapot: {selectedStatus}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine("Nincs ilyen állapotú csomag.");
                            break;
                        }

                        PrintPackageSearchResults(packages);

                        break;
                    }

                case "5":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Keresés csomagtípus alapján ---");

                        Console.WriteLine("1. Normál csomag");
                        Console.WriteLine("2. Törékeny csomag");
                        Console.WriteLine("3. Hűtött csomag");

                        Console.WriteLine();
                        Console.Write("Csomagtípus sorszáma: ");

                        if (!int.TryParse(Console.ReadLine(), out int typeNumber)
                            || typeNumber < 1
                            || typeNumber > 3)
                        {
                            Console.WriteLine("Érvénytelen csomagtípus.");
                            break;
                        }

                        string typeName = typeNumber switch
                        {
                            1 => "Normál",
                            2 => "Törékeny",
                            3 => "Hűtött",
                            _ => "Ismeretlen"
                        };

                        List<Package> packages =
                            packageService.GetPackagesByType(typeNumber);

                        Console.WriteLine();
                        Console.WriteLine($"Kiválasztott típus: {typeName}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine("Nincs ilyen típusú csomag.");
                            break;
                        }

                        PrintPackageSearchResults(packages);

                        break;
                    }

                case "6":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Keresés feladási időszak alapján ---");

                        var period = ReadValidDatePeriod();

                        if (period == null)
                            break;

                        DateTime startDate = period.Value.Start;
                        DateTime endDate = period.Value.End;

                        List<Package> packages =
                            packageService.GetPackagesByPeriod(startDate, endDate);

                        Console.WriteLine();
                        Console.WriteLine(
                            $"Keresett időszak: {startDate:yyyy.MM.dd} - {endDate:yyyy.MM.dd}");

                        if (packages.Count == 0)
                        {
                            Console.WriteLine("Nincs csomag a megadott időszakban.");
                            break;
                        }

                        PrintPackageSearchResults(packages);

                        break;
                    }

                case "0":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Érvénytelen választás.");
                    break;
            }
        }
    }

    static void PrintPackageSearchResults(List<Package> packages, bool showSpecialData = false)
    {
        Console.WriteLine($"Találatok száma: {packages.Count}");
        Console.WriteLine();

        foreach (Package package in packages)
        {
            string packageType = package switch
            {
                NormalPackage => "Normál",
                FragilePackage => "Törékeny",
                RefrigeratedPackage => "Hűtött",
                _ => "Ismeretlen"
            };

            Console.WriteLine($"Azonosító: {package.TrackingNumber}");
            Console.WriteLine($"Típus: {packageType}");
            Console.WriteLine($"Feladó: {package.Sender.Name}");
            Console.WriteLine($"Címzett: {package.Recipient.Name}");
            Console.WriteLine($"Tömeg: {package.WeightKg} kg");
            Console.WriteLine($"Állapot: {package.Status}");
            Console.WriteLine($"Feladás: {package.ShippedAt:yyyy.MM.dd HH:mm:ss}");

            if (showSpecialData)
            {
                switch (package)
                {
                    case FragilePackage fragile:
                        Console.WriteLine(
                            $"Törékenység szintje: {fragile.Fragility}");

                        Console.WriteLine(
                            $"Kezelési utasítás: {fragile.HandlingInstructions}");
                        break;

                    case RefrigeratedPackage refrigerated:
                        Console.WriteLine(
                            $"Minimum hőmérséklet: {refrigerated.MinTemperatureCelsius:F2} °C");

                        Console.WriteLine(
                            $"Maximum hőmérséklet: {refrigerated.MaxTemperatureCelsius:F2} °C");
                        break;
                }
            }

            Console.WriteLine("------------------------------");
        }
    }

    static void ShowReports(ReportService reportService)
    {
        bool running = true;

        while (running)
        {
            Console.WriteLine();
            Console.WriteLine("--- Riportok és kimutatások ---");
            Console.WriteLine("1. Csomagok száma állapotonként");
            Console.WriteLine("2. Csomagtípus-statisztika");
            Console.WriteLine("3. Öt legaktívabb feladó");
            Console.WriteLine("4. Sikertelen kézbesítések okai");
            Console.WriteLine("5. Időszakos feladási kimutatás");
            Console.WriteLine("6. Speciális kezelést igénylő aktív csomagok");
            Console.WriteLine("0. Vissza a főmenübe");
            Console.WriteLine();

            Console.Write("Választás: ");
            string? choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Csomagok száma állapotonként ---");

                        var results =
                            reportService.GetPackageCountsByStatus();

                        if (results.Count == 0)
                        {
                            Console.WriteLine("Nincs rögzített csomag.");
                            break;
                        }

                        foreach (var item in results)
                        {
                            Console.WriteLine(
                                $"{item.Status} | {item.Count} db");
                        }

                        break;
                    }

                case "2":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Csomagtípus-statisztika ---");

                        var results = reportService.GetPackageTypeStatistics();

                        foreach (var item in results)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"Csomagtípus: {item.Type}");
                            Console.WriteLine($"Csomagok száma: {item.Count} db");
                            Console.WriteLine($"Összesített tömeg: {item.TotalWeight:F2} kg");
                            Console.WriteLine($"Átlagos tömeg: {item.AverageWeight:F2} kg");
                            Console.WriteLine("------------------------------");
                        }

                        break;
                    }

                case "3":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Öt legaktívabb feladó ---");

                        var results = reportService.GetTopSenders();

                        if (results.Count == 0)
                        {
                            Console.WriteLine("Nincs még feladott csomag.");
                            break;
                        }

                        int place = 1;

                        foreach (var item in results)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"{place}. helyezett");
                            Console.WriteLine($"Feladó: {item.SenderName}");
                            Console.WriteLine($"Feladott csomagok: {item.PackageCount} db");
                            Console.WriteLine($"Összesített tömeg: {item.TotalWeight:F2} kg");
                            Console.WriteLine("------------------------------");

                            place++;
                        }

                        break;
                    }

                case "4":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Sikertelen kézbesítések okai ---");

                        var results = reportService.GetDeliveryFailureStatistics();

                        int totalFailures = 0;

                        foreach (var item in results)
                        {
                            string reasonName = item.Reason switch
                            {
                                DeliveryFailureReason.RecipientNotAvailable
                                    => "A címzett nem volt elérhető",

                                DeliveryFailureReason.AddressNotFound
                                    => "A cím nem volt megtalálható",

                                DeliveryFailureReason.RecipientRefused
                                    => "A címzett megtagadta az átvételt",

                                _ => "Ismeretlen ok"
                            };

                            Console.WriteLine();
                            Console.WriteLine($"Ok: {reasonName}");
                            Console.WriteLine($"Előfordulások száma: {item.Count} db");
                            Console.WriteLine("------------------------------");

                            totalFailures += item.Count;
                        }

                        Console.WriteLine();
                        Console.WriteLine(
                            $"Összes sikertelen kézbesítés: {totalFailures} db");

                        break;
                    }

                case "5":
                    {
                        Console.WriteLine();
                        Console.WriteLine("--- Időszaki feladási kimutatás ---");


                        var period = ReadValidDatePeriod();

                        if (period == null)
                            break;

                        DateTime startDate = period.Value.Start;
                        DateTime endDate = period.Value.End;

                        var report = reportService.GetShipmentPeriodReport(
                            startDate, endDate);

                        Console.WriteLine();
                        Console.WriteLine(
                            $"Időszak: {startDate:yyyy.MM.dd} - {endDate:yyyy.MM.dd}");

                        Console.WriteLine($"Csomagok száma: {report.Count} db");

                        Console.WriteLine(
                            $"Összesített tömeg: {report.TotalWeight:F2} kg");

                        Console.WriteLine(
                            $"Átlagos tömeg: {report.AverageWeight:F2} kg");

                        if (report.Count == 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "Nincs csomag a megadott időszakban.");
                            break;
                        }

                        Console.WriteLine();
                        Console.WriteLine("--- Időszakban feladott csomagok ---");

                        PrintPackageSearchResults(report.Packages);

                        break;
                    }

                case "6":
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "--- Speciális kezelést igénylő aktív csomagok ---");

                        List<Package> packages =
                            reportService.GetActiveSpecialPackages();

                        if (packages.Count == 0)
                        {
                            Console.WriteLine(
                                "Nincs aktív, speciális kezelést igénylő csomag.");
                            break;
                        }

                        PrintPackageSearchResults(packages, true);

                        break;
                    }

                case "0":
                    running = false;
                    break;

                default:
                    Console.WriteLine("Érvénytelen választás.");
                    break;
            }
        }
    }


    static DateTime? ReadValidDate(string label)
    {
        while (true)
        {
            Console.Write($"{label} (éééé.hh.nn, 0 = vissza): ");
            string input = (Console.ReadLine() ?? "").Trim();

            if (input == "0")
                return null;

            if (DateTime.TryParseExact(
                input,
                "yyyy.MM.dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
            {
                return date;
            }

            Console.WriteLine("Érvénytelen dátum. Próbáld újra!");
        }
    }



    static (DateTime Start, DateTime End)? ReadValidDatePeriod()
    {
        while (true)
        {
            DateTime? startDate =
                ReadValidDate("Kezdő dátum");

            if (startDate == null)
                return null;

            DateTime? endDate =
                ReadValidDate("Záró dátum");

            if (endDate == null)
                return null;

            if (startDate.Value > endDate.Value)
            {
                Console.WriteLine(
                    "A kezdő dátum nem lehet későbbi a záró dátumnál.");

                Console.WriteLine("Add meg újra az időszakot!");
                continue;
            }

            return (startDate.Value, endDate.Value);
        }
    }

    static Customer? ReadExistingCustomer(
    CustomerService customerService,
    string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} (0 = vissza): ");
            string input = (Console.ReadLine() ?? "").Trim();

            if (input == "0")
                return null;

            if (!int.TryParse(input, out int customerId)
                || customerId <= 0)
            {
                Console.WriteLine(
                    "Érvénytelen ügyfélazonosító. Próbáld újra!");
                continue;
            }

            Customer? customer =
                customerService.GetCustomerById(customerId);

            if (customer == null)
            {
                Console.WriteLine(
                    "Nem található ilyen ügyfél. Próbáld újra!");
                continue;
            }

            return customer;
        }
    }


}
