// Este archivo lo trabajo leandro

using System; // Importa el espacio de nombres System, que contiene clases fundamentales y tipos de datos.
using System.Collections.Generic; // Permite el uso de colecciones genéricas, como listas y diccionarios.
using Microsoft.Data.Sqlite; // Importa la biblioteca para trabajar con bases de datos SQLite.
using InventorySystem.Domain; // Importa el espacio de nombres que contiene la lógica de dominio del sistema de inventario.
using InventorySystem.Infraestructure; // Importa el espacio de nombres que contiene la infraestructura necesaria para el sistema de inventario.


namespace InventorySystem.Application
{
    /// <summary>
    /// Servicio responsable de manejar la lógica del stock.
    /// Aquí vive toda la lógica de negocio avanzada sobre entradas y salidas.
    /// Implementa FIFO para descontar inventario correctamente.
    /// </summary>
    public class StockService
    {
        /// <summary>
        /// Crea la tabla Batches en SQLite si no existe.
        /// Esta tabla almacena cada lote de productos (no se mezcla inventario).
        /// </summary>
        public void EnsureTableExists()
        {
            // Usamos "using" para garantizar que la conexión se cierre automáticamente.
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open(); // Abre la conexión a SQLite
                var command = connection.CreateCommand(); // Crea un comando SQL

                // SQL de creación de tabla.
                // Cada lote representa una entrada individual de stock.
                command.CommandText = 
                @"
                    CREATE TABLE IF NOT EXISTS Batches (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,   -- Identificador único del lote
                        ProductId INTEGER NOT NULL,             -- A qué producto pertenece
                        SupplierId INTEGER NOT NULL,            -- De qué proveedor vino
                        Quantity INTEGER NOT NULL,              -- Cantidad actual del lote
                        CostPrice REAL NOT NULL,                -- Precio de compra unitario
                        EntryDate TEXT NOT NULL,                -- Fecha en que ingresó al almacén
                        ExpirationDate TEXT,                    -- Fecha de expiración (si aplica)
                        
                        -- Auditoría para rastrear cambios
                        CreatedAt TEXT NOT NULL, CreatedBy TEXT NOT NULL,
                        LastModifiedAt TEXT NOT NULL, LastModifiedBy TEXT NOT NULL,
                        IsDeleted INTEGER DEFAULT 0,            -- Soft delete
                        DeletedAt TEXT, DeletedBy TEXT
                    );
                ";

                command.ExecuteNonQuery(); // Ejecuta el SQL (creación de tabla)
            }
        }

        /// <summary>
        /// Registra una entrada de inventario generando un nuevo LOTE.
        /// NO se modifica stock existente; se crea un historial limpio.
        /// </summary>
        public void RegisterEntry(Batch newBatch, string creatorName)
        {
            // Registro de lote (INSERT)
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Insert SQL: agrega un lote nuevo a la tabla.
                command.CommandText = 
                @"
                    INSERT INTO Batches 
                    (ProductId, SupplierId, Quantity, CostPrice, EntryDate, ExpirationDate, 
                     CreatedAt, CreatedBy, LastModifiedAt, LastModifiedBy, IsDeleted)
                    VALUES 
                    ($prodId, $supId, $qty, $cost, $entry, $exp, 
                     $date, $creator, $date, $creator, 0)
                ";

                // Asignación de valores a parámetros para evitar SQL Injection.
                command.Parameters.AddWithValue("$prodId", newBatch.ProductId);
                command.Parameters.AddWithValue("$supId", newBatch.SupplierId);
                command.Parameters.AddWithValue("$qty", newBatch.Quantity);
                command.Parameters.AddWithValue("$cost", newBatch.CostPrice);

                // Guardamos la fecha ISO 8601 (formato "o") para compatibilidad total
                command.Parameters.AddWithValue("$entry", newBatch.EntryDate.ToString("o"));

                // Manejo de fecha de expiración opcional
                if (newBatch.ExpirationDate.HasValue)
                    command.Parameters.AddWithValue("$exp", newBatch.ExpirationDate.Value.ToString("o"));
                else
                    command.Parameters.AddWithValue("$exp", DBNull.Value); // Se inserta NULL en SQLite

                // Auditoría
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                command.Parameters.AddWithValue("$creator", creatorName);

                // Ejecuta el INSERT
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Registra la salida (venta) de stock aplicando FIFO.
        /// Toma siempre primero los lotes más antiguos.
        /// </summary>
        public void RegisterExit(int productId, int quantityRequired, string userAuditor)
        {
            // Trae todos los lotes activos del producto ordenados desde el más antiguo.
            var batches = GetBatchesForProduct(productId);

            // Variable que se irá descontando conforme se vaya consumiendo stock.
            int quantityToDeduct = quantityRequired;

            // Calculamos stock total disponible
            int totalStock = 0;
            foreach(var b in batches) totalStock += b.Quantity;

            // Validación: si el stock total NO alcanza → error
            if (totalStock < quantityRequired)
            {
                throw new Exception($"Stock insuficiente. Disponible: {totalStock}, Solicitado: {quantityRequired}");
            }

            // Apertura de conexión para actualizar varios lotes dentro del mismo ciclo
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                // Recorre lotes en orden FIFO
                foreach (var batch in batches)
                {
                    if (quantityToDeduct <= 0) break; // Si ya cubrimos la demanda → terminamos

                    int deduction = 0; // Cantidad que se resta a este lote

                    // Si este lote puede cubrir lo que falta
                    if (batch.Quantity >= quantityToDeduct)
                    {
                        deduction = quantityToDeduct;  // Tomamos solo lo necesario
                        batch.Quantity -= deduction;   // Reducimos el lote
                        quantityToDeduct = 0;          // Ya no falta nada
                    }
                    else
                    {
                        // Lote insuficiente: consumirlo completo
                        deduction = batch.Quantity;
                        quantityToDeduct -= deduction; // Todavía falta cubrir
                        batch.Quantity = 0;            // Lote agotado
                    }

                    // Actualizamos este lote en la base de datos
                    var command = connection.CreateCommand();
                    command.CommandText = 
                    @"
                        UPDATE Batches 
                        SET Quantity = $qty,     -- Nueva cantidad del lote
                            LastModifiedAt = $date,
                            LastModifiedBy = $user 
                        WHERE Id = $id
                    ";
                    
                    command.Parameters.AddWithValue("$qty", batch.Quantity);
                    command.Parameters.AddWithValue("$date", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("$user", userAuditor);
                    command.Parameters.AddWithValue("$id", batch.Id);
                    
                    command.ExecuteNonQuery(); // Ejecuta el UPDATE
                }
            }
        }

        /// <summary>
        /// Obtiene los lotes de un producto en orden FIFO.
        /// Solo devuelve lotes activos con cantidad > 0.
        /// </summary>
        private List<Batch> GetBatchesForProduct(int productId)
        {
            var list = new List<Batch>();

            // Conexión a base de datos
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Query FIFO: los lotes más antiguos primero
                command.CommandText = 
                @"
                    SELECT Id, Quantity, EntryDate 
                    FROM Batches 
                    WHERE ProductId = $pid AND Quantity > 0 AND IsDeleted = 0
                    ORDER BY EntryDate ASC    -- FIFO garantizado
                ";

                command.Parameters.AddWithValue("$pid", productId);

                // Ejecutamos el SELECT
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Construcción del objeto Batch parcial (solo lo necesario)
                        list.Add(new Batch
                        {
                            Id = reader.GetInt32(0),
                            Quantity = reader.GetInt32(1),
                            EntryDate = DateTime.Parse(reader.GetString(2)) // Se convierte STRING → DateTime
                        });
                    }
                }
            }

            return list; // Devuelve lotes ordenados correctamente
        }

        /// <summary>
        /// Devuelve el stock total de un producto sumando todos los lotes activos.
        /// </summary>
        public int GetTotalStock(int productId)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // SUMA todas las cantidades de los lotes que no están eliminados
                command.CommandText = 
                    "SELECT SUM(Quantity) FROM Batches WHERE ProductId = $pid AND IsDeleted = 0";

                command.Parameters.AddWithValue("$pid", productId);

                var result = command.ExecuteScalar(); // Ejecuta y devuelve un valor único

                // Si la suma existe → se convierte a entero
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }

                // Si no hay stock → retorna 0
                return 0;
            }
        }

        /// <summary>
        /// Devuelve todos los lotes activos con stock positivo.
        /// Ideal para reportes de inventario detallados.
        /// </summary>
        public List<Batch> GetAllActiveBatches()
        {
            var list = new List<Batch>();

            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                
                // Selecciona todos los lotes que aún tienen stock
                command.CommandText = 
                @"
                    SELECT Id, ProductId, SupplierId, Quantity, CostPrice, EntryDate, ExpirationDate 
                    FROM Batches 
                    WHERE Quantity > 0 AND IsDeleted = 0
                    ORDER BY ProductId ASC, EntryDate ASC
                ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Lectura de fecha de expiración opcional
                        DateTime? expDate = null;
                        if (!reader.IsDBNull(6))
                            expDate = DateTime.Parse(reader.GetString(6));

                        // Construye objeto Batch
                        list.Add(new Batch
                        {
                            Id = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            SupplierId = reader.GetInt32(2),
                            Quantity = reader.GetInt32(3),
                            CostPrice = reader.GetDecimal(4),
                            EntryDate = DateTime.Parse(reader.GetString(5)),
                            ExpirationDate = expDate
                        });
                    }
                }
            }

            return list; // Devuelve lista ordenada por producto y antigüedad
        }
    }
}
// --------------------------------------------------------------------------------------
//  StockService.cs  |  Servicio de Lógica de Inventario
//
//  Este servicio administra toda la lógica del stock del sistema. Se encarga de crear la 
//  tabla de lotes en SQLite, registrar entradas de inventario, descontar salidas aplicando 
//  el método FIFO, obtener el stock total disponible y listar los lotes activos. Aquí vive 
//  la lógica central que controla cómo se mueve el inventario dentro y fuera del almacén.
// --------------------------------------------------------------------------------------
