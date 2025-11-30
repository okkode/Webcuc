# Activate User: zsuzs@gmail.com

The user profile for "zsuzs@gmail.com" is already configured to be **ACTIVE = true** in the seed data (`GdeWebDB/GdeDbContext.cs`).

## For New Databases

If you create a new database, the user will automatically be activated because the seed data sets `ACTIVE = true`.

## For Existing Databases

If your database already exists and the user is not activated, you have two options:

### Option 1: Update via SQL (Recommended)

If you have SQLite command-line tools or a SQLite browser:

```sql
UPDATE T_USER 
SET ACTIVE = 1,
    MODIFICATIONDATE = datetime('now')
WHERE EMAIL = 'zsuzs@gmail.com';
```

### Option 2: Use Entity Framework Migration

1. Create a new migration:
   ```powershell
   cd GdeWebAPI
   dotnet ef migrations add ActivateUserZsuzs -p ..\GdeWebDB -s .
   ```

2. Manually edit the migration file to add the SQL:
   ```csharp
   migrationBuilder.Sql("UPDATE T_USER SET ACTIVE = 1 WHERE EMAIL = 'zsuzs@gmail.com';");
   ```

3. Apply the migration:
   ```powershell
   dotnet ef database update -p ..\GdeWebDB -s .
   ```

### Option 3: Use the Admin Interface

If you have access to the admin interface:
1. Log in as an admin user
2. Go to Settings > Users
3. Find the user with email "zsuzs@gmail.com"
4. Click the activation toggle to activate the user

## Verification

To verify the user is activated, you can check:

```sql
SELECT USERID, EMAIL, FIRSTNAME, LASTNAME, ACTIVE 
FROM T_USER 
WHERE EMAIL = 'zsuzs@gmail.com';
```

The `ACTIVE` column should show `1` (true).

