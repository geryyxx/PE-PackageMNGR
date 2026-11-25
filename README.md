# Package Delivery Management System

A console-based package delivery management application developed in C# as part of a Computer Science BSc university course at the University of Pannonia.

## Overview

The application provides a system for managing customers, shipments, package tracking and delivery operations using a relational database.

## Features

- Customer registration and management
- Package creation and management
- Multiple package types
- Shipment processing and delivery status management
- Package tracking and tracking history
- Customer-based package search
- Statistical reports
- Input validation and error handling
- Persistent data storage

## Technologies

- C#
- .NET 10
- Entity Framework Core
- SQLite
- LINQ
- Visual Studio
- Git

## Project Structure

| Directory | Purpose |
|---|---|
| Data | Database context and configuration |
| Models | Domain entities and package types |
| Enums | Package statuses and predefined values |
| Services | Business logic and application operations |
| Migrations | Database schema migrations |
| Program.cs | Application entry point and console interface |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio or another compatible C# development environment

### Installation

```bash
git clone https://github.com/geryyxx/PE-PackageMNGR.git
cd PE-PackageMNGR/PackageDelivery
dotnet restore
```

### Database Setup

Apply the Entity Framework Core migrations using:

```bash
dotnet ef database update
```

The Entity Framework Core CLI tools are required for this command.

### Run the Application

```bash
dotnet run
```

## Learning Objectives

This project demonstrates practical experience with:

- Object-oriented programming
- Inheritance and polymorphism
- Relational database management
- Object-relational mapping
- Separation of business logic and data access
- LINQ queries
- Exception handling and input validation

## Project Context

Developed as a university coursework project using a provided starter project, extended with application functionality and database persistence.
