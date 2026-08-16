using Infrastructure;
using Models;

namespace Seed;

public sealed record SeedResult(int Brands, int Models, int Categories, int Cars, int Images, int Parts);

public static class SeedData
{
    public static SeedResult Run(SorchaDbContext db)
    {
        var brandCount = 0;
        var modelCount = 0;
        var categoryCount = 0;
        var carCount = 0;
        var imageCount = 0;
        var partCategoryCount = 0;
        var partCount = 0;

        var brandRows = new (string Name, string Country)[]
        {
            ("Toyota", "Japan"), ("Honda", "Japan"), ("Ford", "USA"),
            ("Chevrolet", "USA"), ("BMW", "Germany"), ("Mercedes-Benz", "Germany"),
            ("Audi", "Germany"), ("Hyundai", "South Korea"), ("Kia", "South Korea"),
            ("Tesla", "USA")
        };

        var brandIds = new Dictionary<string, Guid>();
        foreach (var (name, country) in brandRows)
        {
            var existing = db.Brands.FirstOrDefault(b => b.Name == name);
            if (existing is null)
            {
                existing = new Brand { Name = name, Country = country };
                db.Brands.Add(existing);
                db.SaveChanges();
                brandCount++;
            }
            brandIds[name] = existing.Id;
        }

        var modelRows = new (string Brand, string Name, int? YearStart)[]
        {
            ("Toyota", "Camry", 1982), ("Toyota", "Corolla", 1966), ("Toyota", "RAV4", 1994), ("Toyota", "Highlander", 2000),
            ("Honda", "Civic", 1972), ("Honda", "Accord", 1976), ("Honda", "CR-V", 1995),
            ("Ford", "F-150", 1975), ("Ford", "Mustang", 1964), ("Ford", "Escape", 2000),
            ("Chevrolet", "Silverado", 1999), ("Chevrolet", "Tahoe", 1995), ("Chevrolet", "Camaro", 1966),
            ("BMW", "3 Series", 1975), ("BMW", "5 Series", 1972), ("BMW", "X5", 1999),
            ("Mercedes-Benz", "C-Class", 1993), ("Mercedes-Benz", "E-Class", 1993), ("Mercedes-Benz", "GLE", 2015),
            ("Audi", "A4", 1994), ("Audi", "A6", 1994), ("Audi", "Q5", 2008),
            ("Hyundai", "Elantra", 1990), ("Hyundai", "Sonata", 1985), ("Hyundai", "Tucson", 2004),
            ("Kia", "Sportage", 1993), ("Kia", "Telluride", 2019), ("Kia", "Optima", 2000),
            ("Tesla", "Model 3", 2017), ("Tesla", "Model Y", 2020), ("Tesla", "Model S", 2012)
        };

        var modelIds = new Dictionary<string, Guid>();
        foreach (var (brand, name, yearStart) in modelRows)
        {
            var key = $"{brand}|{name}";
            var existing = db.CarModels.FirstOrDefault(m => m.Name == name && m.BrandId == brandIds[brand]);
            if (existing is null)
            {
                existing = new CarModel { BrandId = brandIds[brand], Name = name, YearStart = yearStart };
                db.CarModels.Add(existing);
                db.SaveChanges();
                modelCount++;
            }
            modelIds[key] = existing.Id;
        }

        var categoryRows = new (string Name, string Slug)[]
        {
            ("Sedan", "sedan"), ("SUV", "suv"), ("Truck", "truck"),
            ("Coupe", "coupe"), ("Hatchback", "hatchback"), ("Electric", "electric")
        };

        var categoryIds = new Dictionary<string, Guid>();
        foreach (var (name, slug) in categoryRows)
        {
            var existing = db.Categories.FirstOrDefault(c => c.Name == name);
            if (existing is null)
            {
                existing = new Category { Name = name, Slug = slug };
                db.Categories.Add(existing);
                db.SaveChanges();
                categoryCount++;
            }
            categoryIds[name] = existing.Id;
        }

        var seller = db.Users.OrderBy(u => u.CreatedAt).FirstOrDefault();

        var images = new[]
        {
            "/DesktopBackground/1_classicsportscars_porsche911.jpg",
            "/DesktopBackground/2_classicsportscars_ferrarienzo.jpg",
            "/DesktopBackground/3_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/4_classicsportscars_jaguare-type.jpg",
            "/DesktopBackground/5_classicsportscars_mercedes-benz300sl.jpg",
            "/DesktopBackground/6_classicsportscars_lamborghinimiura.jpg",
            "/DesktopBackground/7_classicsportscars_astonmartinvolante.jpg",
            "/DesktopBackground/8_classicsportscars_mazdamiata.jpg",
            "/DesktopBackground/9_classicsportscars_mclarenf1.jpg",
            "/DesktopBackground/10_classicsportscars_ferrari330.jpg",
            "/DesktopBackground/11_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/12_classicsportscars_porsche911.jpg",
            "/DesktopBackground/13_classicsportscars_mercedes-benz300sl.jpg",
            "/DesktopBackground/14_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/15_classicsportscars_astonmartindb2-4.jpg",
            "/DesktopBackground/16_classicsportscars_chevroletcorvette.jpg",
            "/DesktopBackground/17_classicsportscars_lamborghinis.jpg",
            "/DesktopBackground/18_classicsportscars_ferraritestarossa.jpg",
            "/DesktopBackground/19_classicsportscars_astonmartindb4.jpg"
        };

        var carRows = new (string Brand, string Model, string Category, int Year, decimal Price, int Mileage, string Color, int ImageIndex)[]
        {
            ("Toyota", "Camry", "Sedan", 2019, 22500, 45000, "Silver", 0),
            ("Toyota", "Camry", "Sedan", 2021, 26900, 22000, "White", 1),
            ("Toyota", "Camry", "Sedan", 2016, 16800, 78000, "Black", 2),
            ("Toyota", "Corolla", "Sedan", 2020, 18900, 38000, "Blue", 3),
            ("Toyota", "Corolla", "Sedan", 2018, 14500, 60000, "Red", 4),
            ("Toyota", "Corolla", "Sedan", 2022, 21400, 15000, "Gray", 5),
            ("Toyota", "RAV4", "SUV", 2021, 31200, 28000, "Green", 6),
            ("Toyota", "RAV4", "SUV", 2019, 27500, 40000, "Silver", 7),
            ("Toyota", "RAV4", "SUV", 2023, 35800, 9000, "White", 8),
            ("Toyota", "Highlander", "SUV", 2020, 38900, 33000, "Black", 9),
            ("Honda", "Civic", "Hatchback", 2020, 19800, 35000, "White", 10),
            ("Honda", "Civic", "Hatchback", 2022, 24900, 12000, "Red", 11),
            ("Honda", "Civic", "Hatchback", 2018, 15200, 70000, "Black", 12),
            ("Honda", "Accord", "Sedan", 2021, 27400, 20000, "Blue", 13),
            ("Honda", "Accord", "Sedan", 2019, 23100, 45000, "Silver", 14),
            ("Honda", "CR-V", "SUV", 2020, 29500, 32000, "Gray", 15),
            ("Honda", "CR-V", "SUV", 2022, 33200, 18000, "White", 16),
            ("Ford", "F-150", "Truck", 2021, 41500, 25000, "Red", 17),
            ("Ford", "F-150", "Truck", 2019, 33800, 52000, "Black", 18),
            ("Ford", "Mustang", "Coupe", 2020, 36900, 18000, "Yellow", 0),
            ("Ford", "Mustang", "Coupe", 2022, 44500, 6000, "Blue", 1),
            ("Ford", "Escape", "SUV", 2021, 26700, 21000, "White", 2),
            ("Chevrolet", "Silverado", "Truck", 2022, 48200, 14000, "Black", 3),
            ("Chevrolet", "Silverado", "Truck", 2020, 39900, 38000, "Silver", 4),
            ("Chevrolet", "Tahoe", "SUV", 2021, 58300, 16000, "White", 5),
            ("Chevrolet", "Camaro", "Coupe", 2020, 34200, 22000, "Red", 6),
            ("BMW", "3 Series", "Sedan", 2021, 41800, 19000, "Blue", 7),
            ("BMW", "3 Series", "Sedan", 2019, 32500, 44000, "White", 8),
            ("BMW", "5 Series", "Sedan", 2020, 48900, 30000, "Black", 9),
            ("BMW", "X5", "SUV", 2021, 62400, 17000, "Gray", 10),
            ("BMW", "X5", "SUV", 2019, 48700, 41000, "White", 11),
            ("Mercedes-Benz", "C-Class", "Sedan", 2020, 38400, 26000, "Silver", 12),
            ("Mercedes-Benz", "C-Class", "Sedan", 2022, 46900, 8000, "Black", 13),
            ("Mercedes-Benz", "E-Class", "Sedan", 2021, 58200, 15000, "White", 14),
            ("Mercedes-Benz", "GLE", "SUV", 2020, 63800, 23000, "Blue", 15),
            ("Audi", "A4", "Sedan", 2021, 39700, 18000, "Gray", 16),
            ("Audi", "A4", "Sedan", 2019, 29800, 46000, "White", 17),
            ("Audi", "A6", "Sedan", 2020, 47500, 27000, "Black", 18),
            ("Audi", "Q5", "SUV", 2021, 43900, 20000, "Blue", 0),
            ("Hyundai", "Elantra", "Sedan", 2021, 19600, 24000, "White", 1),
            ("Hyundai", "Elantra", "Sedan", 2019, 14900, 58000, "Gray", 2),
            ("Hyundai", "Sonata", "Sedan", 2020, 22800, 31000, "Silver", 3),
            ("Hyundai", "Tucson", "SUV", 2022, 29400, 12000, "Red", 4),
            ("Kia", "Sportage", "SUV", 2021, 24900, 26000, "White", 5),
            ("Kia", "Telluride", "SUV", 2022, 44600, 15000, "Black", 6),
            ("Kia", "Optima", "Sedan", 2020, 20300, 33000, "Blue", 7),
            ("Tesla", "Model 3", "Electric", 2021, 37900, 29000, "White", 8),
            ("Tesla", "Model 3", "Electric", 2023, 44700, 5000, "Black", 9),
            ("Tesla", "Model Y", "Electric", 2022, 52900, 11000, "Blue", 10),
            ("Tesla", "Model S", "Electric", 2021, 72400, 19000, "Red", 11)
        };

        if (seller is not null)
        {
            foreach (var (brand, model, category, year, price, mileage, color, imageIndex) in carRows)
            {
                var brandId = brandIds[brand];
                var modelId = modelIds[$"{brand}|{model}"];
                var categoryId = categoryIds[category];
                var exists = db.Cars.Any(c =>
                    c.BrandId == brandId && c.ModelId == modelId &&
                    c.Year == year && c.Price == price && c.Color == color);
                if (exists) continue;

                var car = new Car
                {
                    BrandId = brandId, ModelId = modelId, CategoryId = categoryId,
                    SellerId = seller.Id, Year = year, Price = price, Mileage = mileage,
                    Color = color, Status = "Active"
                };
                db.Cars.Add(car);
                db.SaveChanges();
                carCount++;

                db.ProductImages.Add(new ProductImage { ProductId = car.Id, Url = images[imageIndex], IsPrimary = true });
                db.ProductImages.Add(new ProductImage { ProductId = car.Id, Url = images[(imageIndex + 7) % images.Length], IsPrimary = false });
                db.SaveChanges();
                imageCount += 2;
            }
        }

        var partCategoryRows = new (string Name, string Slug)[]
        {
            ("Engine", "engine"), ("Battery", "battery"), ("Brakes", "brakes"),
            ("Filters", "filters"), ("Electrical", "electrical"), ("Fluids", "fluids")
        };

        var partCategoryIds = new Dictionary<string, Guid>();
        foreach (var (name, slug) in partCategoryRows)
        {
            var existing = db.Categories.FirstOrDefault(c => c.Name == name && c.Type == "Part");
            if (existing is null)
            {
                existing = new Category { Name = name, Slug = slug, Type = "Part" };
                db.Categories.Add(existing);
                db.SaveChanges();
                partCategoryCount++;
            }
            partCategoryIds[name] = existing.Id;
        }

        if (seller is not null)
        {
            var partRows = new (string Category, string Name, string Brand, string Sku, decimal Price, int Qty, string Specs)[]
            {
                ("Battery", "12V 75Ah Battery", "Bosch", "BOS-12V75", 129.99m, 25, """{"voltage":12,"capacityAh":75,"cca":680}"""),
                ("Battery", "12V 60Ah Battery", "Varta", "VAR-12V60", 99.99m, 30, """{"voltage":12,"capacityAh":60,"cca":540}"""),
                ("Engine", "2.0L Turbo Engine", "Ford", "FOR-2.0T", 4200.00m, 3, """{"displacementL":2.0,"powerHp":250,"fuelType":"Petrol"}"""),
                ("Engine", "1.6L Petrol Engine", "Hyundai", "HYU-1.6", 3400.00m, 4, """{"displacementL":1.6,"powerHp":132,"fuelType":"Petrol"}"""),
                ("Brakes", "Front Brake Pads", "Brembo", "BRE-FP01", 79.99m, 40, """{"position":"Front","material":"Ceramic"}"""),
                ("Brakes", "Rear Brake Rotors", "Bosch", "BOS-RR02", 149.99m, 18, """{"position":"Rear","diameterMm":280}"""),
                ("Filters", "Oil Filter", "Mann", "MAN-OF01", 9.99m, 100, """{"type":"Oil","threadMm":3.5}"""),
                ("Filters", "Air Filter", "K&N", "KN-AF02", 24.99m, 60, """{"type":"Air","performance":true}"""),
                ("Electrical", "Alternator 150A", "Denso", "DEN-ALT150", 210.00m, 8, """{"amperage":150,"voltage":12}"""),
                ("Electrical", "Starter Motor", "Bosch", "BOS-SM01", 180.00m, 10, """{"type":"Starter","voltage":12}"""),
                ("Fluids", "Engine Oil 5W-30 (5L)", "Castrol", "CAS-OIL5W30", 45.99m, 50, """{"viscosity":"5W-30","volumeL":5}"""),
                ("Fluids", "Coolant Concentrate", "Prestone", "PRE-COOL", 18.99m, 35, """{"type":"Coolant","volumeL":4}""")
            };

            foreach (var (category, name, brand, sku, price, qty, specs) in partRows)
            {
                if (db.Parts.Any(p => p.Sku == sku)) continue;

                var part = new Part
                {
                    Name = name, Brand = brand, Sku = sku, CategoryId = partCategoryIds[category],
                    SellerId = seller.Id, Price = price, Quantity = qty, Status = "Active",
                    Specs = specs, ProductType = "Part"
                };
                db.Parts.Add(part);
                db.SaveChanges();
                partCount++;
            }
        }

        return new SeedResult(brandCount, modelCount, categoryCount, carCount, imageCount, partCount);
    }
}
