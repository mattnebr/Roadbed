# Roadbed.IO

File I/O utilities with strongly-typed wrappers for CSV file operations using CsvHelper.

## Overview

This library provides type-safe file operations with a focus on CSV parsing and generation. It includes a simplified file info wrapper and a powerful CSV file handler that maps rows to DTOs using custom mappers.

## Installation
```bash
dotnet add package Roadbed.IO
```

## IoCsvFile\<T\>

Type-safe CSV file handler that maps CSV rows to your DTO objects.

#### Create Custom Entity Mapper
```csharp
using CsvHelper;
using Roadbed.IO;

public class FooDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
}

public class FooCsvMapper : ICsvEntityMapper<FooDto>
{
    public FooDto? MapEntity(CsvReader csvReader)
    {
        return new FooDto
        {
            Id = csvReader.GetField<int>("Id"),
            Name = csvReader.GetField<string>("Name"),
            Price = csvReader.GetField<decimal>("Price")
        };
    }
}
```

#### Read CSV from File
```csharp
// Synchronous
var mapper = new FooCsvMapper();
var csvFile = IoCsvFile<FooDto>.FromFile(@"C:\Data\export.csv", mapper);

foreach (var row in csvFile.DataRows)
{
    Console.WriteLine($"{row.Name}: ${row.Price}");
}

// Asynchronous
var csvFile = await IoCsvFile<FooDto>.FromFileAsync(@"C:\Data\export.csv", mapper);
```

#### Read CSV from String
```csharp
string csvContent = @"Id,Name,Price
1,Widget,9.99
2,Gadget,19.99";

var mapper = new FooCsvMapper();
var csvFile = IoCsvFile<FooDto>.FromString(csvContent, mapper);

// Async version
var csvFile = await IoCsvFile<FooDto>.FromStringAsync(csvContent, mapper);
```

#### Export to String
```csharp
var csvFile = IoCsvFile<FooDto>.FromFile(@"C:\Data\input.csv", mapper);

// Get CSV content as string
string csvContent = csvFile.ExportDataRowsAsContentString();

// With custom configuration
var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    Delimiter = ";",
    Encoding = Encoding.UTF8
};

string csvContent = csvFile.ExportDataRowsAsContentString(config);
```

## Requirements

- .NET 10.0+
- CsvHelper

## Related Packages

- **Roadbed.Crud** - CRUD patterns for entities and repositories