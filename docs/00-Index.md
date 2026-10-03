# CateringFlow ERP+CRM - Project Documentation

> **System**: CateringFlow - Catering Business ERP + CRM  
> **Framework**: ASP.NET Core 8 MVC  
> **Database**: SQL Server (Entity Framework Core)  
> **Auth**: Firebase Authentication + Cookie RBAC  

---

## Table of Contents

### 1. [[01-Frontend-Prototype]]
All transaction screens with screenshots and descriptions.

### 2. [[02-Backend-Prototype]]
Source code screenshots with API/Algo usage per screen.

### 3. [[03-API-Functions-Features]]
API endpoints, functions, and how they work.

### 4. [[04-Security-Features]]
Security middleware, authentication, and RBAC implementation.

---

## Project Structure

```
cateringflow/
├── Controllers/          # 15 MVC Controllers
├── Models/               # 18 Entity Models
├── Views/                # Razor Views (Client + SuperAdmin)
├── Services/             # RBAC Service + Firebase Settings
├── Middleware/            # 4 Security Middlewares
├── Data/                 # DbContext + Seed Data
├── Migrations/           # EF Core Migrations
└── wwwroot/              # Static assets (JS, CSS, images)
```

## Key Screenshots Checklist

> [!tip] How to Use
> 1. Open the project in Visual Studio and run it
> 2. Take screenshots of each screen listed below
> 3. Place screenshots in `docs/screenshots/` folder
> 4. Update the `![[filename]]` references in each doc
