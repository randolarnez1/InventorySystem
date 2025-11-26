using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using InventorySystem.Shared;

namespace InventorySystem.Features.Stakeholders
{
    /// <summary>
    /// [LOGICA DE NEGOCIO Y DATOS]
    /// Gestiona tanto a Proveedores como a Clientes.
    /// Centralizamos aquí para no crear múltiples repositorios pequeños.
    /// </summary>
    public class StakeholderService
    {
        /// <summary>
        /// [INFRAESTRUCTURA]
        /// Crea las tablas Suppliers y Customers si no existen.
        /// </summary>
        public void EnsureTablesExist()
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // 1. Tabla Proveedores
                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS Suppliers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ContactEmail TEXT,
                        -- Auditoría
                        CreatedAt TEXT NOT NULL, CreatedBy TEXT NOT NULL,
                        LastModifiedAt TEXT NOT NULL, LastModifiedBy TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0, DeletedAt TEXT, DeletedBy TEXT
                    );

                    -- 2. Tabla Clientes
                    CREATE TABLE IF NOT EXISTS Customers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        TaxId TEXT NOT NULL,
                        -- Auditoría
                        CreatedAt TEXT NOT NULL, CreatedBy TEXT NOT NULL,
                        LastModifiedAt TEXT NOT NULL, LastModifiedBy TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0, DeletedAt TEXT, DeletedBy TEXT
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        // --- SECCIÓN PROVEEDORES ---

        public void CreateSupplier(Supplier supplier, string creatorName)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Suppliers (Name, ContactEmail, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                    VALUES ($name, $email, $date, $creator, $date, $creator, 0)";

                command.Parameters.AddWithValue("$name", supplier.Name);
                command.Parameters.AddWithValue("$email", supplier.ContactEmail);
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$creator", creatorName);

                command.ExecuteNonQuery();
            }
        }

        public List<Supplier> GetAllSuppliers()
        {
            var list = new List<Supplier>();
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, ContactEmail FROM Suppliers WHERE IsDeleted = 0";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Supplier 
                        { 
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ContactEmail = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }
            return list;
        }

        // --- SECCIÓN CLIENTES ---

        public void CreateCustomer(Customer customer, string creatorName)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Customers (Name, TaxId, CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                    VALUES ($name, $tax, $date, $creator, $date, $creator, 0)";

                command.Parameters.AddWithValue("$name", customer.Name);
                command.Parameters.AddWithValue("$tax", customer.TaxId);
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$creator", creatorName);

                command.ExecuteNonQuery();
            }
        }
    }
}