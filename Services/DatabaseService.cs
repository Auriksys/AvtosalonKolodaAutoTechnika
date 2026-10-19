using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using FDNJ_CSharp.Models;

namespace FDNJ_CSharp.Services;

public class DatabaseService
{
    private static DatabaseService? _instance;
    public static DatabaseService Instance => _instance ??= new DatabaseService();

    private string _dbPath = string.Empty;

    public string DbPath => _dbPath;

    public DatabaseService()
    {
        InitializeDatabasePath();
        EnsureDatabaseInitialized();
    }

    private void InitializeDatabasePath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string directPath = Path.Combine(baseDir, "avtosalon.db");

        if (File.Exists(directPath))
        {
            _dbPath = directPath;
            return;
        }

        // Try checking project directory if running in IDE/Debug
        DirectoryInfo? parent = Directory.GetParent(baseDir);
        for (int i = 0; i < 4 && parent != null; i++)
        {
            string candidate = Path.Combine(parent.FullName, "avtosalon.db");
            if (File.Exists(candidate))
            {
                // Copy to baseDir for local execution if not present
                try
                {
                    File.Copy(candidate, directPath, false);
                    _dbPath = directPath;
                    return;
                }
                catch
                {
                    _dbPath = candidate;
                    return;
                }
            }
            parent = parent.Parent;
        }

        _dbPath = directPath;
    }

    private SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    private void EnsureDatabaseInitialized()
    {
        using var conn = GetConnection();

        // Create base tables if they do not exist
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS users(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            login TEXT UNIQUE NOT NULL,
            password TEXT NOT NULL,
            role TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS suppliers(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            contact_person TEXT,
            phone TEXT,
            email TEXT,
            address TEXT
        );
        CREATE TABLE IF NOT EXISTS clients(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            full_name TEXT NOT NULL,
            phone TEXT,
            email TEXT,
            comment TEXT
        );
        CREATE TABLE IF NOT EXISTS cars(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            vin TEXT UNIQUE NOT NULL,
            brand TEXT NOT NULL,
            model TEXT NOT NULL,
            year INTEGER NOT NULL,
            color TEXT,
            body_type TEXT,
            engine TEXT,
            power INTEGER DEFAULT 0,
            transmission TEXT,
            drive TEXT,
            mileage INTEGER DEFAULT 0,
            price REAL NOT NULL,
            status TEXT NOT NULL DEFAULT 'В наличии',
            arrival_date TEXT NOT NULL,
            supplier_id INTEGER,
            image_path TEXT,
            FOREIGN KEY(supplier_id) REFERENCES suppliers(id)
        );
        CREATE TABLE IF NOT EXISTS sales(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            car_id INTEGER NOT NULL,
            client_id INTEGER NOT NULL,
            manager_id INTEGER,
            sale_date TEXT NOT NULL,
            price REAL NOT NULL,
            payment_method TEXT,
            FOREIGN KEY(car_id) REFERENCES cars(id),
            FOREIGN KEY(client_id) REFERENCES clients(id),
            FOREIGN KEY(manager_id) REFERENCES users(id)
        );
        CREATE TABLE IF NOT EXISTS bookings(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            car_id INTEGER NOT NULL,
            client_id INTEGER NOT NULL,
            manager_id INTEGER,
            booking_date TEXT NOT NULL,
            expiration_date TEXT,
            FOREIGN KEY(car_id) REFERENCES cars(id),
            FOREIGN KEY(client_id) REFERENCES clients(id),
            FOREIGN KEY(manager_id) REFERENCES users(id)
        );
        CREATE TABLE IF NOT EXISTS history(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            car_id INTEGER,
            action TEXT NOT NULL,
            created_at TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();

        // Migration: check if image_path column exists in cars
        bool hasImagePath = false;
        using (var infoCmd = conn.CreateCommand())
        {
            infoCmd.CommandText = "PRAGMA table_info(cars);";
            using var reader = infoCmd.ExecuteReader();
            while (reader.Read())
            {
                string col = reader.GetString(1);
                if (string.Equals(col, "image_path", StringComparison.OrdinalIgnoreCase))
                {
                    hasImagePath = true;
                    break;
                }
            }
        }

        if (!hasImagePath)
        {
            using var alterCmd = conn.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE cars ADD COLUMN image_path TEXT;";
            alterCmd.ExecuteNonQuery();
        }

        // Auto-assign sample preview images & clean up joke data / seed rich inventory
        using (var seedDataCmd = conn.CreateCommand())
        {
            seedDataCmd.CommandText = @"
            -- Clean up joke entries if present
            UPDATE cars SET brand = 'Hyundai', model = 'Tucson N-Line', year = 2023, color = 'Серый металлик',
                            body_type = 'Кроссовер', engine = '2.5 л', power = 190, transmission = 'АКПП',
                            drive = 'Полный (4WD)', mileage = 18500, price = 3450000, image_path = 'suv_preview.png'
            WHERE id = 6 AND brand LIKE 'Huy%';

            UPDATE suppliers SET name = 'ООО «АвтоИмпорт Восток»', contact_person = 'Волков Сергей Андреевич',
                                 phone = '+7 (924) 400-12-88', email = 'info@autoimport-dv.ru',
                                 address = 'г. Владивосток, ул. Снеговая, 42'
            WHERE id = 2 AND name LIKE '%Говно%';

            UPDATE clients SET full_name = 'Белов Артём Викторович', comment = 'Интересуется премиум седанами BMW / Mercedes'
            WHERE id = 7 AND full_name LIKE '%Какаш%';

            UPDATE clients SET full_name = 'Громов Денис Олегович', comment = 'Рассматривает покупку в автокредит'
            WHERE id = 8 AND full_name LIKE '%Какаш%';

            -- Assign car preview images
            UPDATE cars SET image_path = 'skyline_bnr32.png' WHERE id = 1 AND (image_path IS NULL OR image_path = '');
            UPDATE cars SET image_path = 'toyota_chaser.png' WHERE id = 2 AND (image_path IS NULL OR image_path = '');
            UPDATE cars SET image_path = 'honda_nbox.png' WHERE id = 3 AND (image_path IS NULL OR image_path = '');
            UPDATE cars SET image_path = 'sedan_preview.png' WHERE id = 4 AND (image_path IS NULL OR image_path = '');
            UPDATE cars SET image_path = 'skyline_bnr32.png' WHERE id = 5 AND (image_path IS NULL OR image_path = '');
            UPDATE cars SET image_path = 'suv_preview.png' WHERE id = 6 AND (image_path IS NULL OR image_path = '');";
            seedDataCmd.ExecuteNonQuery();
        }

        // Seed additional realistic cars if fewer than 10 cars exist
        using (var carCountCmd = conn.CreateCommand())
        {
            carCountCmd.CommandText = "SELECT COUNT(*) FROM cars;";
            long carCount = Convert.ToInt64(carCountCmd.ExecuteScalar() ?? 0L);
            if (carCount < 10)
            {
                using var seedMoreCmd = conn.CreateCommand();
                seedMoreCmd.CommandText = @"
                INSERT OR IGNORE INTO suppliers(id, name, contact_person, phone, email, address) VALUES
                (3, 'ООО «Евро Авто Премиум»', 'Краснов Илья Борисович', '+7 (495) 780-55-10', 'supply@euroauto-vip.ru', 'г. Москва, Ленинградское ш., 68'),
                (4, 'JDM Trade Co. Japan', 'Танака Кенджи', '+81 3-5555-0192', 'export@jdmtrade.jp', 'Токио, Минато-ку, Сиодомэ 2-1');

                INSERT OR IGNORE INTO cars(vin, brand, model, year, color, body_type, engine, power, transmission, drive, mileage, price, status, arrival_date, supplier_id, image_path) VALUES
                ('WBSJF01090G900101', 'BMW', 'M5 Competition', 2022, 'Синий металлик', 'Седан', '4.4 л V8 TwinTurbo', 625, 'АКПП', 'Полный (4WD)', 14200, 11800000, 'В наличии', '2026-09-15', 3, 'bmw_m5.png'),
                ('WP0ZZZ99ZNS200202', 'Porsche', '911 Carrera 4S', 2023, 'Красный', 'Купе', '3.0 л Boxer Turbo', 450, 'Робот', 'Полный (4WD)', 6800, 15900000, 'В наличии', '2026-09-20', 3, 'porsche_911.png'),
                ('JTNB11HK803300303', 'Toyota', 'Camry XV70 3.5', 2022, 'Белый жемчуг', 'Седан', '3.5 л V6', 249, 'АКПП', 'Передний (FWD)', 29400, 3650000, 'В наличии', '2026-09-25', 2, 'camry_xv70.png'),
                ('W1K2130801A400404', 'Mercedes-Benz', 'E 200 AMG Line', 2023, 'Черный обсидиан', 'Седан', '2.0 л Turbo', 197, 'АКПП', 'Полный (4WD)', 12100, 6450000, 'Забронирован', '2026-10-01', 3, 'mercedes_e200.png'),
                ('JTJBAMCA402500505', 'Lexus', 'RX 350 F-Sport', 2023, 'Серебристый', 'Внедорожник', '2.4 л Turbo', 279, 'АКПП', 'Полный (4WD)', 9500, 8200000, 'В наличии', '2026-10-03', 4, 'lexus_rx350.png'),
                ('WAUZZZF28NN600606', 'Audi', 'RS6 Avant Performance', 2023, 'Изумрудный', 'Универсал', '4.0 л V8 TFSI', 630, 'АКПП', 'Полный (4WD)', 7900, 14500000, 'В наличии', '2026-10-05', 3, 'audi_rs6.png'),
                ('KMHL341CBNU700707', 'Hyundai', 'Sonata DN8', 2022, 'Графитовый', 'Седан', '2.5 л MPI', 180, 'АКПП', 'Передний (FWD)', 34000, 2790000, 'Продан', '2026-09-10', 2, 'hyundai_sonata.png');";
                seedMoreCmd.ExecuteNonQuery();
            }
        }

        // Ensure diverse sales and bookings exist so analytics & tables are rich
        using (var salesCountCmd = conn.CreateCommand())
        {
            salesCountCmd.CommandText = "SELECT COUNT(*) FROM sales;";
            long salesCount = Convert.ToInt64(salesCountCmd.ExecuteScalar() ?? 0L);
            if (salesCount < 5)
            {
                using var seedSalesCmd = conn.CreateCommand();
                seedSalesCmd.CommandText = @"
                INSERT INTO sales(car_id, client_id, manager_id, sale_date, price, payment_method)
                SELECT c.id, 1, 2, '2026-09-28', c.price, 'Автокредит'
                FROM cars c WHERE c.vin = 'KMHL341CBNU700707' AND NOT EXISTS (SELECT 1 FROM sales WHERE car_id = c.id);

                INSERT INTO sales(car_id, client_id, manager_id, sale_date, price, payment_method)
                SELECT 2, 2, 2, '2026-09-18', 2500000, 'Банковская карта'
                WHERE NOT EXISTS (SELECT 1 FROM sales WHERE payment_method = 'Банковская карта');

                INSERT INTO sales(car_id, client_id, manager_id, sale_date, price, payment_method)
                SELECT 1, 3, 2, '2026-10-02', 4500000, 'Банковский перевод'
                WHERE NOT EXISTS (SELECT 1 FROM sales WHERE payment_method = 'Банковский перевод');

                INSERT INTO bookings(car_id, client_id, manager_id, booking_date, expiration_date)
                SELECT c.id, 4, 2, '2026-10-10', '2026-10-25'
                FROM cars c WHERE c.vin = 'W1K2130801A400404' AND NOT EXISTS (SELECT 1 FROM bookings WHERE car_id = c.id);

                INSERT INTO history(car_id, action, created_at) VALUES
                (7, 'Поступление на склад: BMW M5 Competition (625 л.с.)', '2026-10-08 11:20:00'),
                (8, 'Поступление на склад: Porsche 911 Carrera 4S (450 л.с.)', '2026-10-09 14:15:00'),
                (10, 'Оформлена бронь для клиента до 2026-10-25 (Mercedes-Benz E 200)', '2026-10-10 16:40:00'),
                (11, 'Пройдена предпродажная диагностика Lexus RX 350 F-Sport', '2026-10-11 10:05:00'),
                (12, 'Обновлена галерея фотографий Audi RS6 Avant Performance', '2026-10-12 18:30:00');";
                try { seedSalesCmd.ExecuteNonQuery(); } catch { }
            }
        }

        // Default users seed
        using var userCountCmd = conn.CreateCommand();
        userCountCmd.CommandText = "SELECT COUNT(*) FROM users;";
        long userCount = (long)(userCountCmd.ExecuteScalar() ?? 0L);
        if (userCount == 0)
        {
            using var seedCmd = conn.CreateCommand();
            seedCmd.CommandText = @"
            INSERT INTO users(login, password, role) VALUES
            ('admin', 'admin', 'admin'),
            ('manager', 'manager', 'manager'),
            ('director', 'director', 'director');";
            seedCmd.ExecuteNonQuery();
        }
    }

    #region Authentication & Users
    public User? Authenticate(string login, string password)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, login, password, role FROM users WHERE login = @login AND password = @password LIMIT 1;";
        cmd.Parameters.AddWithValue("@login", login.Trim());
        cmd.Parameters.AddWithValue("@password", password);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                Password = reader.GetString(2),
                Role = reader.GetString(3)
            };
        }
        return null;
    }

    public List<User> GetUsers()
    {
        var list = new List<User>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, login, password, role FROM users ORDER BY id ASC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new User
            {
                Id = reader.GetInt32(0),
                Login = reader.GetString(1),
                Password = reader.GetString(2),
                Role = reader.GetString(3)
            });
        }
        return list;
    }

    public void AddUser(User user)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO users(login, password, role) VALUES(@l, @p, @r);";
        cmd.Parameters.AddWithValue("@l", user.Login.Trim());
        cmd.Parameters.AddWithValue("@p", user.Password);
        cmd.Parameters.AddWithValue("@r", user.Role);
        cmd.ExecuteNonQuery();
    }

    public void UpdateUserPassword(int userId, string newPassword)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE users SET password = @p WHERE id = @id;";
        cmd.Parameters.AddWithValue("@p", newPassword);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteUser(int userId)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
    }
    #endregion

    #region Cars
    public List<Car> GetCars()
    {
        var list = new List<Car>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT c.id, c.vin, c.brand, c.model, c.year, c.color, c.body_type, c.engine,
               c.power, c.transmission, c.drive, c.mileage, c.price, c.status,
               c.arrival_date, c.supplier_id, c.image_path, s.name as supplier_name
        FROM cars c
        LEFT JOIN suppliers s ON s.id = c.supplier_id
        ORDER BY c.id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Car
            {
                Id = reader.GetInt32(0),
                Vin = reader.GetString(1),
                Brand = reader.GetString(2),
                Model = reader.GetString(3),
                Year = reader.GetInt32(4),
                Color = reader.IsDBNull(5) ? "" : reader.GetString(5),
                BodyType = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Engine = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Power = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                Transmission = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Drive = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Mileage = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                Price = Convert.ToDecimal(reader.GetValue(12)),
                Status = reader.GetString(13),
                ArrivalDate = reader.IsDBNull(14) ? "" : reader.GetString(14),
                SupplierId = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                ImagePath = reader.IsDBNull(16) ? null : reader.GetString(16),
                SupplierName = reader.IsDBNull(17) ? null : reader.GetString(17)
            });
        }
        return list;
    }

    public int AddCar(Car car)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
            INSERT INTO cars(vin, brand, model, year, color, body_type, engine, power,
                             transmission, drive, mileage, price, status, arrival_date,
                             supplier_id, image_path)
            VALUES(@vin, @brand, @model, @year, @color, @body_type, @engine, @power,
                   @transmission, @drive, @mileage, @price, @status, @arrival_date,
                   @supplier_id, @image_path);
            SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@vin", car.Vin.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@brand", car.Brand.Trim());
            cmd.Parameters.AddWithValue("@model", car.Model.Trim());
            cmd.Parameters.AddWithValue("@year", car.Year);
            cmd.Parameters.AddWithValue("@color", car.Color.Trim());
            cmd.Parameters.AddWithValue("@body_type", car.BodyType.Trim());
            cmd.Parameters.AddWithValue("@engine", car.Engine.Trim());
            cmd.Parameters.AddWithValue("@power", car.Power);
            cmd.Parameters.AddWithValue("@transmission", car.Transmission.Trim());
            cmd.Parameters.AddWithValue("@drive", car.Drive.Trim());
            cmd.Parameters.AddWithValue("@mileage", car.Mileage);
            cmd.Parameters.AddWithValue("@price", (double)car.Price);
            cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(car.Status) ? "В наличии" : car.Status);
            cmd.Parameters.AddWithValue("@arrival_date", string.IsNullOrWhiteSpace(car.ArrivalDate) ? DateTime.Today.ToString("yyyy-MM-dd") : car.ArrivalDate);
            cmd.Parameters.AddWithValue("@supplier_id", (object?)car.SupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@image_path", (object?)car.ImagePath ?? DBNull.Value);

            int carId = Convert.ToInt32(cmd.ExecuteScalar());

            // Log to history
            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", $"Автомобиль {car.Brand} {car.Model} добавлен в каталог");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
            return carId;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void UpdateCar(Car car)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
            UPDATE cars SET
                vin = @vin, brand = @brand, model = @model, year = @year,
                color = @color, body_type = @body_type, engine = @engine,
                power = @power, transmission = @transmission, drive = @drive,
                mileage = @mileage, price = @price, status = @status,
                supplier_id = @supplier_id, image_path = @image_path
            WHERE id = @id;";

            cmd.Parameters.AddWithValue("@id", car.Id);
            cmd.Parameters.AddWithValue("@vin", car.Vin.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@brand", car.Brand.Trim());
            cmd.Parameters.AddWithValue("@model", car.Model.Trim());
            cmd.Parameters.AddWithValue("@year", car.Year);
            cmd.Parameters.AddWithValue("@color", car.Color.Trim());
            cmd.Parameters.AddWithValue("@body_type", car.BodyType.Trim());
            cmd.Parameters.AddWithValue("@engine", car.Engine.Trim());
            cmd.Parameters.AddWithValue("@power", car.Power);
            cmd.Parameters.AddWithValue("@transmission", car.Transmission.Trim());
            cmd.Parameters.AddWithValue("@drive", car.Drive.Trim());
            cmd.Parameters.AddWithValue("@mileage", car.Mileage);
            cmd.Parameters.AddWithValue("@price", (double)car.Price);
            cmd.Parameters.AddWithValue("@status", car.Status);
            cmd.Parameters.AddWithValue("@supplier_id", (object?)car.SupplierId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@image_path", (object?)car.ImagePath ?? DBNull.Value);
            cmd.ExecuteNonQuery();

            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", car.Id);
            histCmd.Parameters.AddWithValue("@action", $"Параметры автомобиля {car.Brand} {car.Model} обновлены");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void ChangeCarStatus(int carId, string newStatus, string? note = null)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE cars SET status = @st WHERE id = @id;";
            cmd.Parameters.AddWithValue("@st", newStatus);
            cmd.Parameters.AddWithValue("@id", carId);
            cmd.ExecuteNonQuery();

            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            string actionText = string.IsNullOrWhiteSpace(note) ? $"Статус изменен на: {newStatus}" : $"Статус: {newStatus} ({note})";
            histCmd.Parameters.AddWithValue("@action", actionText);
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void UpdateCarImage(int carId, string? imagePath)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE cars SET image_path = @img WHERE id = @id;";
            cmd.Parameters.AddWithValue("@img", (object?)imagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", carId);
            cmd.ExecuteNonQuery();

            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", "Обновлена фотография автомобиля");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void DeleteCar(int carId)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "DELETE FROM cars WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", carId);
            cmd.ExecuteNonQuery();

            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", $"Автомобиль #{carId} удален из базы");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }
    #endregion

    #region Clients
    public List<Client> GetClients()
    {
        var list = new List<Client>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, full_name, phone, email, comment FROM clients ORDER BY id DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Client
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Comment = reader.IsDBNull(4) ? "" : reader.GetString(4)
            });
        }
        return list;
    }

    public void AddClient(Client client)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO clients(full_name, phone, email, comment) VALUES(@fn, @ph, @em, @com);";
        cmd.Parameters.AddWithValue("@fn", client.FullName.Trim());
        cmd.Parameters.AddWithValue("@ph", client.Phone.Trim());
        cmd.Parameters.AddWithValue("@em", client.Email.Trim());
        cmd.Parameters.AddWithValue("@com", client.Comment.Trim());
        cmd.ExecuteNonQuery();
    }

    public void UpdateClient(Client client)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE clients SET full_name = @fn, phone = @ph, email = @em, comment = @com WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", client.Id);
        cmd.Parameters.AddWithValue("@fn", client.FullName.Trim());
        cmd.Parameters.AddWithValue("@ph", client.Phone.Trim());
        cmd.Parameters.AddWithValue("@em", client.Email.Trim());
        cmd.Parameters.AddWithValue("@com", client.Comment.Trim());
        cmd.ExecuteNonQuery();
    }

    public void DeleteClient(int id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM clients WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
    #endregion

    #region Suppliers
    public List<Supplier> GetSuppliers()
    {
        var list = new List<Supplier>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, contact_person, phone, email, address FROM suppliers ORDER BY id DESC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Supplier
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ContactPerson = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Email = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Address = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }
        return list;
    }

    public void AddSupplier(Supplier supplier)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO suppliers(name, contact_person, phone, email, address) VALUES(@n, @cp, @ph, @em, @addr);";
        cmd.Parameters.AddWithValue("@n", supplier.Name.Trim());
        cmd.Parameters.AddWithValue("@cp", supplier.ContactPerson.Trim());
        cmd.Parameters.AddWithValue("@ph", supplier.Phone.Trim());
        cmd.Parameters.AddWithValue("@em", supplier.Email.Trim());
        cmd.Parameters.AddWithValue("@addr", supplier.Address.Trim());
        cmd.ExecuteNonQuery();
    }

    public void UpdateSupplier(Supplier supplier)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE suppliers SET name = @n, contact_person = @cp, phone = @ph, email = @em, address = @addr WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", supplier.Id);
        cmd.Parameters.AddWithValue("@n", supplier.Name.Trim());
        cmd.Parameters.AddWithValue("@cp", supplier.ContactPerson.Trim());
        cmd.Parameters.AddWithValue("@ph", supplier.Phone.Trim());
        cmd.Parameters.AddWithValue("@em", supplier.Email.Trim());
        cmd.Parameters.AddWithValue("@addr", supplier.Address.Trim());
        cmd.ExecuteNonQuery();
    }

    public void DeleteSupplier(int id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM suppliers WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
    #endregion

    #region Sales
    public List<Sale> GetSales()
    {
        var list = new List<Sale>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT s.id, s.car_id, s.client_id, s.manager_id, s.sale_date, s.price, s.payment_method,
               c.brand || ' ' || c.model AS car_title, c.vin AS car_vin,
               cl.full_name AS client_name, cl.phone AS client_phone,
               COALESCE(u.login, '—') AS manager_login
        FROM sales s
        JOIN cars c ON c.id = s.car_id
        JOIN clients cl ON cl.id = s.client_id
        LEFT JOIN users u ON u.id = s.manager_id
        ORDER BY s.id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Sale
            {
                Id = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                ClientId = reader.GetInt32(2),
                ManagerId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                SaleDate = reader.GetString(4),
                Price = Convert.ToDecimal(reader.GetValue(5)),
                PaymentMethod = reader.IsDBNull(6) ? "Наличные" : reader.GetString(6),
                CarTitle = reader.IsDBNull(7) ? "Автомобиль" : reader.GetString(7),
                CarVin = reader.IsDBNull(8) ? "" : reader.GetString(8),
                ClientName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                ClientPhone = reader.IsDBNull(10) ? "" : reader.GetString(10),
                ManagerLogin = reader.IsDBNull(11) ? "—" : reader.GetString(11)
            });
        }
        return list;
    }

    public void AddSale(int carId, int clientId, int? managerId, decimal price, string paymentMethod)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
            INSERT INTO sales(car_id, client_id, manager_id, sale_date, price, payment_method)
            VALUES(@cid, @clid, @mid, @dt, @pr, @pm);";
            cmd.Parameters.AddWithValue("@cid", carId);
            cmd.Parameters.AddWithValue("@clid", clientId);
            cmd.Parameters.AddWithValue("@mid", (object?)managerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dt", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@pr", (double)price);
            cmd.Parameters.AddWithValue("@pm", paymentMethod);
            cmd.ExecuteNonQuery();

            // Mark car as sold
            using var carCmd = conn.CreateCommand();
            carCmd.Transaction = trans;
            carCmd.CommandText = "UPDATE cars SET status = 'Продан' WHERE id = @cid;";
            carCmd.Parameters.AddWithValue("@cid", carId);
            carCmd.ExecuteNonQuery();

            // If there was an active booking on this car, remove it
            using var bookCmd = conn.CreateCommand();
            bookCmd.Transaction = trans;
            bookCmd.CommandText = "DELETE FROM bookings WHERE car_id = @cid;";
            bookCmd.Parameters.AddWithValue("@cid", carId);
            bookCmd.ExecuteNonQuery();

            // History
            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", $"Оформлена продажа. Сумма: {price:N0} ₽, Оплата: {paymentMethod}");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }
    #endregion

    #region Bookings
    public List<Booking> GetBookings()
    {
        var list = new List<Booking>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        SELECT b.id, b.car_id, b.client_id, b.manager_id, b.booking_date, b.expiration_date,
               c.brand || ' ' || c.model AS car_title, c.vin AS car_vin, c.price AS car_price,
               cl.full_name AS client_name, cl.phone AS client_phone,
               COALESCE(u.login, '—') AS manager_login
        FROM bookings b
        JOIN cars c ON c.id = b.car_id
        JOIN clients cl ON cl.id = b.client_id
        LEFT JOIN users u ON u.id = b.manager_id
        ORDER BY b.id DESC;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Booking
            {
                Id = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                ClientId = reader.GetInt32(2),
                ManagerId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                BookingDate = reader.GetString(4),
                ExpirationDate = reader.IsDBNull(5) ? "" : reader.GetString(5),
                CarTitle = reader.IsDBNull(6) ? "Автомобиль" : reader.GetString(6),
                CarVin = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CarPrice = Convert.ToDecimal(reader.GetValue(8)),
                ClientName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                ClientPhone = reader.IsDBNull(10) ? "" : reader.GetString(10),
                ManagerLogin = reader.IsDBNull(11) ? "—" : reader.GetString(11)
            });
        }
        return list;
    }

    public void AddBooking(int carId, int clientId, int? managerId, string expirationDate)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
            INSERT INTO bookings(car_id, client_id, manager_id, booking_date, expiration_date)
            VALUES(@cid, @clid, @mid, @bdate, @edate);";
            cmd.Parameters.AddWithValue("@cid", carId);
            cmd.Parameters.AddWithValue("@clid", clientId);
            cmd.Parameters.AddWithValue("@mid", (object?)managerId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bdate", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@edate", expirationDate);
            cmd.ExecuteNonQuery();

            // Mark car as booked
            using var carCmd = conn.CreateCommand();
            carCmd.Transaction = trans;
            carCmd.CommandText = "UPDATE cars SET status = 'Забронирован' WHERE id = @cid;";
            carCmd.Parameters.AddWithValue("@cid", carId);
            carCmd.ExecuteNonQuery();

            // History
            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", $"Автомобиль забронирован до {expirationDate}");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void CancelBooking(int bookingId, int carId)
    {
        using var conn = GetConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "DELETE FROM bookings WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", bookingId);
            cmd.ExecuteNonQuery();

            using var carCmd = conn.CreateCommand();
            carCmd.Transaction = trans;
            carCmd.CommandText = "UPDATE cars SET status = 'В наличии' WHERE id = @cid;";
            carCmd.Parameters.AddWithValue("@cid", carId);
            carCmd.ExecuteNonQuery();

            using var histCmd = conn.CreateCommand();
            histCmd.Transaction = trans;
            histCmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @action, @dt);";
            histCmd.Parameters.AddWithValue("@cid", carId);
            histCmd.Parameters.AddWithValue("@action", "Бронирование отменено, автомобиль возвращен в наличие");
            histCmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            histCmd.ExecuteNonQuery();

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void ConvertBookingToSale(Booking booking, string paymentMethod)
    {
        AddSale(booking.CarId, booking.ClientId, booking.ManagerId, booking.CarPrice, paymentMethod);
    }
    #endregion

    #region History & Audit
    public List<HistoryItem> GetHistory(int limit = 200)
    {
        var list = new List<HistoryItem>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
        SELECT h.id, h.car_id, h.action, h.created_at,
               COALESCE(c.brand || ' ' || c.model, '—') AS car_title
        FROM history h
        LEFT JOIN cars c ON c.id = h.car_id
        ORDER BY h.id DESC
        LIMIT {limit};";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new HistoryItem
            {
                Id = reader.GetInt32(0),
                CarId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Action = reader.GetString(2),
                CreatedAt = reader.GetString(3),
                CarTitle = reader.IsDBNull(4) ? "—" : reader.GetString(4)
            });
        }
        return list;
    }

    public void AddHistory(int? carId, string action)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO history(car_id, action, created_at) VALUES(@cid, @act, @dt);";
        cmd.Parameters.AddWithValue("@cid", (object?)carId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@act", action);
        cmd.Parameters.AddWithValue("@dt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }
    #endregion

    #region Analytics & Dashboard
    public DashboardStats GetDashboardStats()
    {
        var stats = new DashboardStats();
        using var conn = GetConnection();

        // Car counts
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM cars;";
            stats.TotalCars = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM cars WHERE status = 'В наличии';";
            stats.InStockCars = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM cars WHERE status = 'Забронирован';";
            stats.BookedCars = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM cars WHERE status = 'Продан';";
            stats.SoldCars = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM cars WHERE status IN ('На диагностике', 'На ремонте');";
            stats.InDiagnosticsOrRepairCars = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // Total stock value (cars in stock or booked)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(price), 0) FROM cars WHERE status != 'Продан';";
            stats.TotalStockValue = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
        }

        // Sales totals
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*), COALESCE(SUM(price), 0), COALESCE(AVG(price), 0) FROM sales;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                stats.TotalSalesCount = Convert.ToInt32(r.GetValue(0));
                stats.TotalRevenue = Convert.ToDecimal(r.GetValue(1));
                stats.AverageSalePrice = Convert.ToDecimal(r.GetValue(2));
            }
        }

        // Clients and suppliers
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM clients;";
            stats.TotalClients = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM suppliers;";
            stats.TotalSuppliers = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // Top brands
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT brand, COUNT(*) as c, COALESCE(SUM(price), 0) FROM cars GROUP BY brand ORDER BY c DESC, SUM(price) DESC LIMIT 7;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int count = Convert.ToInt32(r.GetValue(1));
                decimal sum = Convert.ToDecimal(r.GetValue(2));
                double pct = stats.TotalCars > 0 ? (double)count / stats.TotalCars * 100 : 0;
                stats.TopBrands.Add(new KeyValueStat
                {
                    Key = r.GetString(0),
                    Count = count,
                    Value = sum,
                    Percentage = pct,
                    SubText = $"{count} авто ({pct:F0}%) • {sum:N0} ₽"
                });
            }
        }

        // Payment methods (always include all 4 standard payment methods)
        var paymentDict = new Dictionary<string, (int Count, decimal Sum)>
        {
            ["Наличные"] = (0, 0m),
            ["Автокредит"] = (0, 0m),
            ["Банковская карта"] = (0, 0m),
            ["Банковский перевод"] = (0, 0m)
        };
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT payment_method, COUNT(*), COALESCE(SUM(price), 0) FROM sales GROUP BY payment_method;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string pm = r.IsDBNull(0) ? "Наличные" : r.GetString(0);
                int count = Convert.ToInt32(r.GetValue(1));
                decimal sum = Convert.ToDecimal(r.GetValue(2));
                paymentDict[pm] = (count, sum);
            }
        }
        foreach (var kv in paymentDict)
        {
            double pct = stats.TotalRevenue > 0 ? (double)kv.Value.Sum / (double)stats.TotalRevenue * 100 : 0;
            stats.PaymentMethods.Add(new KeyValueStat
            {
                Key = kv.Key,
                Count = kv.Value.Count,
                Value = kv.Value.Sum,
                Percentage = pct,
                SubText = $"{kv.Value.Count} сдел. • {kv.Value.Sum:N0} ₽"
            });
        }

        // Car statuses (always include all 5 statuses)
        var statusDict = new Dictionary<string, int>
        {
            ["В наличии"] = 0,
            ["Забронирован"] = 0,
            ["Продан"] = 0,
            ["На диагностике"] = 0,
            ["На ремонте"] = 0
        };
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT status, COUNT(*) FROM cars GROUP BY status;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string st = r.GetString(0);
                int count = Convert.ToInt32(r.GetValue(1));
                statusDict[st] = count;
            }
        }
        foreach (var kv in statusDict)
        {
            double pct = stats.TotalCars > 0 ? (double)kv.Value / stats.TotalCars * 100 : 0;
            stats.CarStatuses.Add(new KeyValueStat
            {
                Key = kv.Key,
                Count = kv.Value,
                Percentage = pct,
                SubText = $"{kv.Value} ед. ({pct:F0}%)"
            });
        }

        stats.RecentHistory = GetHistory(14);
        return stats;
    }
    #endregion

    #region Backup & Restore
    public void BackupDatabase(string targetFilePath)
    {
        // Flush and copy
        using (var conn = GetConnection())
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
            try { cmd.ExecuteNonQuery(); } catch { }
        }

        File.Copy(_dbPath, targetFilePath, true);
    }

    public void RestoreDatabase(string sourceFilePath)
    {
        // Close connections by recreating service instance if needed
        SqliteConnection.ClearAllPools();
        File.Copy(sourceFilePath, _dbPath, true);
    }
    #endregion
}
