using System;
using InventorySystem.Features.AccessControl;
using InventorySystem.Features.ProductCatalog;
using InventorySystem.Features.Stakeholders;
using InventorySystem.Features.InventoryControl;

namespace InventorySystem
{
    class Program
    {
        // Instancias estáticas de nuestros servicios (Simulando Inyección de Dependencias)
        static UserService _userService = new UserService();
        static ProductRepository _productRepo = new ProductRepository();
        static StakeholderService _stakeholderService = new StakeholderService();
        static StockService _stockService = new StockService();
        
        // Guardamos el usuario que inició sesión para la Auditoría
        static User? _currentUser;

        static void Main(string[] args)
        {
            // 1. INICIALIZACIÓN DEL SISTEMA (BOOTSTRAP)
            Console.WriteLine("Iniciando sistema... Configurando base de datos...");
            InitializeDatabase();
            
            // 2. CREACIÓN DE USUARIO POR DEFECTO (Si es la primera vez)
            // Esto permite entrar al sistema sin tocar la BD manualmente.
            _userService.RegisterUser("admin", "admin123", "Admin", "SYSTEM_BOOTSTRAP");

            // 3. PANTALLA DE LOGIN
            while (_currentUser == null)
            {
                ShowLoginScreen();
            }

            // 4. BUCLE PRINCIPAL DEL MENÚ
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"=== SISTEMA DE INVENTARIO (Usuario: {_currentUser.Username}) ===");
                Console.WriteLine("1. Gestión de Productos (Catálogo)");
                Console.WriteLine("2. Gestión de Actores (Clientes/Proveedores)");
                Console.WriteLine("3. CONTROL DE INVENTARIO (Entradas/Salidas/Stock)");
                Console.WriteLine("4. Salir");
                Console.Write("Seleccione una opción: ");

                switch (Console.ReadLine())
                {
                    case "1": ManageCatalog(); break;
                    case "2": ManageStakeholders(); break;
                    case "3": ManageInventory(); break;
                    case "4": exit = true; break;
                }
            }
        }

        // --- MÉTODOS DE CONFIGURACIÓN ---

        static void InitializeDatabase()
        {
            // Llamamos a cada servicio para que cree sus tablas si no existen
            _userService.EnsureTableExists();
            _productRepo.EnsureTableExists();
            _stakeholderService.EnsureTablesExist();
            _stockService.EnsureTableExists();
        }

        static void ShowLoginScreen()
        {
            Console.Clear();
            Console.WriteLine("=== LOGIN ===");
            Console.Write("Usuario: ");
            string user = Console.ReadLine() ?? "";
            Console.Write("Contraseña: ");
            string pass = Console.ReadLine() ?? "";

            var loggedUser = _userService.Login(user, pass);
            if (loggedUser != null)
            {
                _currentUser = loggedUser;
                Console.WriteLine("¡Bienvenido!");
            }
            else
            {
                Console.WriteLine("Credenciales incorrectas. Presione una tecla para reintentar.");
                Console.ReadKey();
            }
        }

        // --- MÓDULO 1: CATÁLOGO ---

        static void ManageCatalog()
        {
            Console.Clear();
            Console.WriteLine("--- CATÁLOGO DE PRODUCTOS ---");
            Console.WriteLine("1. Ver lista de productos");
            Console.WriteLine("2. Crear nuevo producto");
            Console.Write("Opción: ");
            
            if (Console.ReadLine() == "2")
            {
                Console.Write("Nombre del Producto: ");
                string name = Console.ReadLine() ?? "Sin Nombre";
                Console.Write("SKU (Código único): ");
                string sku = Console.ReadLine() ?? "000";
                
                Console.WriteLine("Categoría (0 = Groceries, 1 = Electronics): ");
                string catInput = Console.ReadLine();
                ProductCategory category = (catInput == "1") ? ProductCategory.Electronics : ProductCategory.Groceries;

                Console.WriteLine("¿Es perecedero? (s/n): ");
                bool isPerishable = (Console.ReadLine()?.ToLower() == "s");

                try
                {
                    var newProd = new Product 
                    { 
                        Name = name, 
                        Sku = sku, 
                        Category = category, 
                        IsPerishable = isPerishable 
                    };
                    
                    // Pasamos el usuario actual para que quede registrado QUIÉN creó el producto
                    _productRepo.CreateProduct(newProd, _currentUser.Username);
                    Console.WriteLine("Producto creado exitosamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            else
            {
                var list = _productRepo.GetAllProducts();
                foreach (var p in list)
                {
                    Console.WriteLine($"ID: {p.Id} | SKU: {p.Sku} | {p.Name} | Cat: {p.Category} | Perecedero: {p.IsPerishable}");
                }
            }
            Console.WriteLine("\nPresione una tecla para volver...");
            Console.ReadKey();
        }

        // --- MÓDULO 2: ACTORES ---

        static void ManageStakeholders()
        {
            Console.Clear();
            Console.WriteLine("--- GESTIÓN DE ACTORES ---");
            Console.WriteLine("1. Registrar Proveedor");
            Console.WriteLine("2. Registrar Cliente");
            Console.WriteLine("3. Ver Proveedores");
            Console.Write("Opción: ");

            var op = Console.ReadLine();
            if (op == "1")
            {
                Console.Write("Nombre Proveedor: ");
                string name = Console.ReadLine() ?? "";
                Console.Write("Email Contacto: ");
                string email = Console.ReadLine() ?? "";
                
                _stakeholderService.CreateSupplier(new Supplier { Name = name, ContactEmail = email }, _currentUser.Username);
                Console.WriteLine("Proveedor registrado.");
            }
            else if (op == "3")
            {
                var list = _stakeholderService.GetAllSuppliers();
                foreach (var s in list) Console.WriteLine($"ID: {s.Id} | {s.Name} ({s.ContactEmail})");
            }
            // (Omitimos lógica de cliente para brevedad, es similar a proveedor)
            
            Console.WriteLine("\nPresione una tecla para volver...");
            Console.ReadKey();
        }

// --- MÓDULO 3: INVENTARIO (CORE) ---
        // ACTUALIZADO: Con tabla visual de stock
        static void ManageInventory()
        {
            Console.Clear();
            Console.WriteLine("--- CONTROL DE INVENTARIO (FIFO) ---");
            
            // PASO 1: MOSTRAR TABLA DE RESUMEN AUTOMÁTICAMENTE
            Console.WriteLine("\n--- ESTADO ACTUAL DEL ALMACÉN ---");
            Console.WriteLine("{0,-5} | {1,-20} | {2,-10} | {3,-15}", "ID", "PRODUCTO", "STOCK", "CATEGORÍA");
            Console.WriteLine(new string('-', 60));

            var allProducts = _productRepo.GetAllProducts();
            
            foreach (var p in allProducts)
            {
                // Calculamos el stock en tiempo real sumando los lotes
                int totalStock = _stockService.GetTotalStock(p.Id);
                
                // Formato de tabla alineada
                Console.WriteLine("{0,-5} | {1,-20} | {2,-10} | {3,-15}", 
                    p.Id, 
                    Truncate(p.Name, 20), 
                    totalStock, 
                    p.Category);
            }
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();

            // PASO 2: MENÚ DE ACCIONES
            Console.WriteLine("1. Registrar ENTRADA (Compra - Nuevo Lote)");
            Console.WriteLine("2. Registrar SALIDA (Venta - FIFO)");
            Console.WriteLine("3. Volver al menú principal");
            Console.Write("Opción: ");

            var op = Console.ReadLine();

            if (op == "1") // COMPRA
            {
                Console.WriteLine("\n--- NUEVA ENTRADA (LOTE) ---");
                // Ya tiene la tabla arriba para ver el ID
                Console.Write("ID Producto: "); 
                if (!int.TryParse(Console.ReadLine(), out int pid)) return;

                Console.Write("ID Proveedor: "); int sid = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("Cantidad: "); int qty = int.Parse(Console.ReadLine() ?? "0");
                Console.Write("Costo Unitario: "); decimal cost = decimal.Parse(Console.ReadLine() ?? "0");
                
                Console.Write("¿Tiene fecha expiración? (s/n): ");
                DateTime? expDate = null;
                if (Console.ReadLine()?.ToLower() == "s")
                {
                    Console.Write("Fecha (yyyy-mm-dd): ");
                    if(DateTime.TryParse(Console.ReadLine(), out DateTime parsedDate))
                        expDate = parsedDate;
                }

                var batch = new Batch
                {
                    ProductId = pid,
                    SupplierId = sid,
                    Quantity = qty,
                    CostPrice = cost,
                    EntryDate = DateTime.Now,
                    ExpirationDate = expDate
                };

                _stockService.RegisterEntry(batch, _currentUser.Username);
                Console.WriteLine("✅ Lote registrado correctamente.");
            }
            else if (op == "2") // VENTA (FIFO)
            {
                Console.WriteLine("\n--- NUEVA SALIDA (VENTA) ---");
                // Ya tiene la tabla arriba para ver el ID
                Console.Write("ID Producto a vender: "); 
                if (!int.TryParse(Console.ReadLine(), out int pid)) return;
                
                int stock = _stockService.GetTotalStock(pid);
                if (stock == 0)
                {
                    Console.WriteLine("⚠️ Error: Este producto no tiene stock disponible.");
                }
                else
                {
                    Console.Write($"Cantidad a vender (Máx {stock}): "); 
                    int qty = int.Parse(Console.ReadLine() ?? "0");

                    try
                    {
                        _stockService.RegisterExit(pid, qty, _currentUser.Username);
                        Console.WriteLine("✅ Venta registrada. El stock se descontó de los lotes más antiguos.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: {ex.Message}");
                    }
                }
            }
            
            if (op != "3")
            {
                Console.WriteLine("\nPresione una tecla para continuar...");
                Console.ReadKey();
            }
        }

        // Función auxiliar para cortar textos largos en la tabla
        static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars - 3) + "...";
        }
    }
}