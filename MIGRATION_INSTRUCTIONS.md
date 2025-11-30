# Database Migration Instructions

This document explains how to create and apply the database migration for the new Notes and MonthlySummary features.

## Prerequisites

Make sure you have the .NET EF Core tools installed:
```powershell
dotnet tool install --global dotnet-ef
```

## Steps to Create and Apply Migration

1. **Navigate to the API project directory** (where Program.cs is located):
```powershell
cd GdeWebAPI
```

2. **Create the migration**:
```powershell
dotnet ef migrations add AddNotesAndMonthlySummaries -p ..\GdeWebDB -s .
```

This command:
- Creates a new migration named `AddNotesAndMonthlySummaries`
- Uses `GdeWebDB` as the project containing the DbContext
- Uses `GdeWebAPI` as the startup project (where Program.cs is)

3. **Apply the migration to the database**:
```powershell
dotnet ef database update -p ..\GdeWebDB -s .
```

This will:
- Apply the migration to your SQLite database (gde.db)
- Create the new tables: `A_NOTE` and `A_MONTHLY_SUMMARY`

## Verify the Migration

After applying the migration, you can verify that the tables were created by checking the database or by running:

```powershell
dotnet ef migrations list -p ..\GdeWebDB -s .
```

## Rollback (if needed)

If you need to rollback the migration:

```powershell
dotnet ef database update <PreviousMigrationName> -p ..\GdeWebDB -s .
```

Or to remove the last migration:

```powershell
dotnet ef migrations remove -p ..\GdeWebDB -s .
```

## Notes

- The migration will create two new tables:
  - `A_NOTE`: Stores user notes for courses
  - `A_MONTHLY_SUMMARY`: Stores AI-generated monthly summaries
  
- Foreign key relationships are set up with cascade delete for data integrity
- Indexes are created for performance on common queries (user + course, user + year + month)

