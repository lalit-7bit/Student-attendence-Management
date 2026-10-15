#!/usr/bin/env pwsh

# Stop on error
$ErrorActionPreference = "Stop"

$dbName = "SmartAttend"
$schemaFile = "Database\Schema.sql"
$seedFile = "Database\DemoSeed.sql"

Write-Host "🔄 Setting up SQL Server LocalDB database..."

# Ensure LocalDB is running
Write-Host "Starting LocalDB instance (MSSQLLocalDB)..."
sqllocaldb start MSSQLLocalDB 2>$null | Out-Null

# Run schema script
Write-Host "Creating database schema..."
try {
	& sqlcmd -S "(localdb)\MSSQLLocalDB" -i $schemaFile -e
	Write-Host "✅ Schema created successfully"
}
catch {
	Write-Host "❌ Failed to create schema: $_"
	exit 1
}

# Run seed script if it exists
if (Test-Path $seedFile) {
	Write-Host "Seeding database with demo data..."
	try {
		& sqlcmd -S "(localdb)\MSSQLLocalDB" -i $seedFile -e
		Write-Host "✅ Database seeded successfully"
	}
	catch {
		Write-Host "⚠️  Seeding failed (non-critical): $_"
	}
}

Write-Host ""
Write-Host "✅ Database setup complete!"
Write-Host "Connection string: Server=(localdb)\MSSQLLocalDB;Database=$dbName;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
