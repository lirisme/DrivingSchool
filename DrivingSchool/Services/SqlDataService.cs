using DrivingSchool.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace DrivingSchool.Services
{
    public class SqlDataService
    {
        private readonly string _connectionString;

        public SqlDataService()
        {
            // Получаем строку подключения из конфигурации
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DrivingSchoolDB"].ConnectionString;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Проверка подключения к базе данных
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Загрузка всех студентов с информацией о стоимости обучения
        /// </summary>
        public StudentCollection LoadStudents()
        {
            var collection = new StudentCollection { Students = new List<Student>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                SELECT s.*, 
                       vc.Code as CategoryCode,
                       vc.FullName as CategoryName,
                       sg.Name as GroupName,
                       e.LastName + ' ' + e.FirstName as InstructorName,
                       c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo,
                       t.Name as TariffName,
                       ISNULL(s.CompletedLessons, 0) as CompletedLessons,  -- ДОБАВЬТЕ ЭТУ СТРОКУ
                       ISNULL((
                           SELECT SUM(Amount) 
                           FROM Payments p 
                           WHERE p.StudentId = s.Id
                       ), 0) as PaidAmount
                FROM Students s
                LEFT JOIN VehicleCategories vc ON s.VehicleCategoryId = vc.Id
                LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                LEFT JOIN Employees e ON s.InstructorId = e.Id
                LEFT JOIN Cars c ON s.CarId = c.Id
                LEFT JOIN Tariffs t ON s.TariffId = t.Id
                ORDER BY s.LastName, s.FirstName", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Students.Add(MapStudent(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки студентов: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Загрузка студента по ID
        /// </summary>
        public Student LoadStudent(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                SELECT s.*, 
                       vc.Code as CategoryCode,
                       vc.FullName as CategoryName,
                       sg.Name as GroupName,
                       e.LastName + ' ' + e.FirstName as InstructorName,
                       c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo,
                       t.Name as TariffName,
                       ISNULL(s.CompletedLessons, 0) as CompletedLessons,  -- ДОБАВЬТЕ ЭТУ СТРОКУ
                       ISNULL((
                           SELECT SUM(Amount) 
                           FROM Payments p 
                           WHERE p.StudentId = s.Id
                       ), 0) as PaidAmount
                FROM Students s
                LEFT JOIN VehicleCategories vc ON s.VehicleCategoryId = vc.Id
                LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                LEFT JOIN Employees e ON s.InstructorId = e.Id
                LEFT JOIN Cars c ON s.CarId = c.Id
                LEFT JOIN Tariffs t ON s.TariffId = t.Id
                WHERE s.Id = @Id", conn);

                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapStudent(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки студента: {ex.Message}");
                throw new Exception($"Ошибка загрузки студента: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Сохранение студента (добавление или обновление)
        /// </summary>
        public int SaveStudent(Student student)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    int studentId;

                    if (student.Id > 0)
                    {
                        // Обновление
                        var cmd = new SqlCommand(@"
                    UPDATE Students 
                    SET LastName = @LastName, 
                        FirstName = @FirstName, 
                        MiddleName = @MiddleName,
                        BirthDate = @BirthDate, 
                        BirthPlace = @BirthPlace, 
                        Phone = @Phone,
                        Email = @Email, 
                        Citizenship = @Citizenship, 
                        Gender = @Gender,
                        GroupId = @GroupId, 
                        VehicleCategoryId = @VehicleCategoryId,
                        InstructorId = @InstructorId, 
                        CarId = @CarId,
                        TuitionAmount = @TuitionAmount,
                        DiscountAmount = @DiscountAmount,
                        TariffId = @TariffId,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                        AddStudentParameters(cmd, student);
                        cmd.Parameters.AddWithValue("@Id", student.Id);
                        cmd.ExecuteNonQuery();
                        studentId = student.Id;
                    }
                    else
                    {
                        // Вставка нового студента
                        var cmd = new SqlCommand(@"
                    INSERT INTO Students 
                    (LastName, FirstName, MiddleName, BirthDate, BirthPlace, Phone, Email, 
                     Citizenship, Gender, GroupId, VehicleCategoryId, InstructorId, CarId,
                     TuitionAmount, DiscountAmount, TariffId, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES 
                    (@LastName, @FirstName, @MiddleName, @BirthDate, @BirthPlace, @Phone, @Email,
                     @Citizenship, @Gender, @GroupId, @VehicleCategoryId, @InstructorId, @CarId,
                     @TuitionAmount, @DiscountAmount, @TariffId, GETDATE())", conn);

                        AddStudentParameters(cmd, student);
                        studentId = (int)cmd.ExecuteScalar();
                    }

                    // Синхронизация с StudentTuitions
                    SyncStudentTuition(conn, studentId, student.TuitionAmount, student.DiscountAmount, student.TariffId);

                    return studentId;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения студента: {ex.Message}");
                throw new Exception($"Ошибка сохранения студента: {ex.Message}");
            }
        }

        private void SyncStudentTuition(SqlConnection conn, int studentId, decimal tuitionAmount, decimal discountAmount, int? tariffId)
        {
            // Проверяем, есть ли запись в StudentTuitions
            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM StudentTuitions WHERE StudentId = @StudentId", conn);
            checkCmd.Parameters.AddWithValue("@StudentId", studentId);

            int exists = (int)checkCmd.ExecuteScalar();

            if (exists > 0)
            {
                // Обновляем существующую запись
                var cmd = new SqlCommand(@"
            UPDATE StudentTuitions 
            SET FullAmount = @FullAmount, 
                Discount = @Discount,
                TariffId = @TariffId,
                ModifiedDate = GETDATE()
            WHERE StudentId = @StudentId", conn);

                cmd.Parameters.AddWithValue("@StudentId", studentId);
                cmd.Parameters.AddWithValue("@FullAmount", tuitionAmount);
                cmd.Parameters.AddWithValue("@Discount", discountAmount);
                cmd.Parameters.AddWithValue("@TariffId", tariffId.HasValue ? (object)tariffId.Value : DBNull.Value);

                cmd.ExecuteNonQuery();
            }
            else if (tuitionAmount > 0 || discountAmount > 0 || tariffId.HasValue)
            {
                // Создаем новую запись только если есть данные
                var cmd = new SqlCommand(@"
            INSERT INTO StudentTuitions (StudentId, FullAmount, Discount, TariffId, CreatedDate)
            VALUES (@StudentId, @FullAmount, @Discount, @TariffId, GETDATE())", conn);

                cmd.Parameters.AddWithValue("@StudentId", studentId);
                cmd.Parameters.AddWithValue("@FullAmount", tuitionAmount);
                cmd.Parameters.AddWithValue("@Discount", discountAmount);
                cmd.Parameters.AddWithValue("@TariffId", tariffId.HasValue ? (object)tariffId.Value : DBNull.Value);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Удаление студента
        /// </summary>
        public void DeleteStudent(int studentId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM Students WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", studentId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления студента: {ex.Message}");
                throw new Exception($"Ошибка удаления студента: {ex.Message}");
            }
        }

        /// <summary>
        /// Поиск студентов
        /// </summary>
        public StudentCollection SearchStudents(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return LoadStudents();

            var collection = new StudentCollection { Students = new List<Student>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT s.*, 
                               vc.Code as CategoryCode,
                               vc.FullName as CategoryName,
                               sg.Name as GroupName,
                               e.LastName + ' ' + e.FirstName as InstructorName,
                               c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
                        FROM Students s
                        LEFT JOIN VehicleCategories vc ON s.VehicleCategoryId = vc.Id
                        LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                        LEFT JOIN Employees e ON s.InstructorId = e.Id
                        LEFT JOIN Cars c ON s.CarId = c.Id
                        WHERE s.LastName LIKE @Search 
                           OR s.FirstName LIKE @Search
                           OR s.MiddleName LIKE @Search
                           OR s.Phone LIKE @Search
                           OR s.Email LIKE @Search
                        ORDER BY s.LastName, s.FirstName", conn);

                    cmd.Parameters.AddWithValue("@Search", $"%{searchText}%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Students.Add(MapStudent(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска студентов: {ex.Message}");
                throw new Exception($"Ошибка поиска студентов: {ex.Message}");
            }

            return collection;
        }

        private Student MapStudent(SqlDataReader reader)
        {
            try
            {
                return new Student
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    LastName = reader["LastName"]?.ToString() ?? "",
                    FirstName = reader["FirstName"]?.ToString() ?? "",
                    MiddleName = reader["MiddleName"]?.ToString() ?? "",
                    BirthDate = reader["BirthDate"] as DateTime? ?? DateTime.Now.AddYears(-18),
                    BirthPlace = reader["BirthPlace"]?.ToString() ?? "",
                    Phone = reader["Phone"]?.ToString() ?? "",
                    Email = reader["Email"]?.ToString() ?? "",
                    Citizenship = reader["Citizenship"]?.ToString() ?? "Российская Федерация",
                    Gender = reader["Gender"]?.ToString() ?? "",
                    GroupId = reader["GroupId"] as int? ?? 0,
                    VehicleCategoryId = reader["VehicleCategoryId"] as int? ?? 0,
                    InstructorId = reader["InstructorId"] as int? ?? 0,
                    CarId = reader["CarId"] as int? ?? 0,
                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                    CategoryCode = reader["CategoryCode"]?.ToString() ?? "",
                    CategoryName = reader["CategoryName"]?.ToString() ?? "",
                    GroupName = reader["GroupName"]?.ToString() ?? "",
                    InstructorName = reader["InstructorName"]?.ToString() ?? "",
                    CarInfo = reader["CarInfo"]?.ToString() ?? "",
                    TuitionAmount = reader["TuitionAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TuitionAmount"]) : 0,
                    DiscountAmount = reader["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(reader["DiscountAmount"]) : 0,
                    TariffId = reader["TariffId"] as int?,
                    TariffName = reader["TariffName"]?.ToString() ?? "",
                    PaidAmount = reader["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PaidAmount"]) : 0,

                    // ДОБАВЬТЕ ЭТУ СТРОКУ:
                    CompletedLessons = reader["CompletedLessons"] != DBNull.Value ? Convert.ToInt32(reader["CompletedLessons"]) : 0
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка маппинга студента: {ex.Message}");
                return new Student();
            }
        }

        private void AddStudentParameters(SqlCommand cmd, Student student)
        {
            cmd.Parameters.AddWithValue("@LastName", student.LastName ?? "");
            cmd.Parameters.AddWithValue("@FirstName", student.FirstName ?? "");
            cmd.Parameters.AddWithValue("@MiddleName", (object)student.MiddleName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BirthDate", student.BirthDate);
            cmd.Parameters.AddWithValue("@BirthPlace", (object)student.BirthPlace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", student.Phone ?? "");
            cmd.Parameters.AddWithValue("@Email", (object)student.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Citizenship", (object)student.Citizenship ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Gender", (object)student.Gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@GroupId", student.GroupId > 0 ? (object)student.GroupId : DBNull.Value);
            cmd.Parameters.AddWithValue("@VehicleCategoryId", student.VehicleCategoryId > 0 ? (object)student.VehicleCategoryId : DBNull.Value);
            cmd.Parameters.AddWithValue("@InstructorId", student.InstructorId > 0 ? (object)student.InstructorId : DBNull.Value);
            cmd.Parameters.AddWithValue("@CarId", student.CarId > 0 ? (object)student.CarId : DBNull.Value);
            // Добавьте в конец метода AddStudentParameters:
            cmd.Parameters.AddWithValue("@TuitionAmount", student.TuitionAmount);
            cmd.Parameters.AddWithValue("@DiscountAmount", student.DiscountAmount);
            cmd.Parameters.AddWithValue("@TariffId", student.TariffId.HasValue && student.TariffId.Value > 0 ? (object)student.TariffId.Value : DBNull.Value);
        }


        /// <summary>
        /// Загрузка всех категорий ТС
        /// </summary>
        public VehicleCategoryCollection LoadVehicleCategories()
        {
            var collection = new VehicleCategoryCollection { Categories = new List<VehicleCategory>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT * FROM VehicleCategories ORDER BY Code", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Categories.Add(new VehicleCategory
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Code = reader["Code"]?.ToString() ?? "",
                                FullName = reader["FullName"]?.ToString() ?? "",
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
                throw new Exception($"Ошибка загрузки категорий: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Сохранение категории
        /// </summary>
        public int SaveVehicleCategory(VehicleCategory category)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (category.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                            UPDATE VehicleCategories 
                            SET Code = @Code, FullName = @FullName, ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", category.Id);
                        cmd.Parameters.AddWithValue("@Code", category.Code);
                        cmd.Parameters.AddWithValue("@FullName", category.FullName);

                        cmd.ExecuteNonQuery();
                        return category.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                            INSERT INTO VehicleCategories (Code, FullName, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES (@Code, @FullName, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@Code", category.Code);
                        cmd.Parameters.AddWithValue("@FullName", category.FullName);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения категории: {ex.Message}");
                throw new Exception($"Ошибка сохранения категории: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление категории (с проверкой использования)
        /// </summary>
        public bool DeleteVehicleCategory(int categoryId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, используется ли категория
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE VehicleCategoryId = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", categoryId);

                    int usedCount = (int)checkCmd.ExecuteScalar();

                    if (usedCount > 0)
                        return false;

                    var cmd = new SqlCommand("DELETE FROM VehicleCategories WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", categoryId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления категории: {ex.Message}");
                throw new Exception($"Ошибка удаления категории: {ex.Message}");
            }
        }


        /// <summary>
        /// Загрузка всех сотрудников
        /// </summary>
        public EmployeeCollection LoadEmployees()
        {
            var collection = new EmployeeCollection { Employees = new List<Employee>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT * FROM Employees ORDER BY LastName, FirstName", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Employees.Add(new Employee
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                LastName = reader["LastName"]?.ToString() ?? "",
                                FirstName = reader["FirstName"]?.ToString() ?? "",
                                MiddleName = reader["MiddleName"]?.ToString() ?? "",
                                Position = reader["Position"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "Активен",
                                Phone = reader["Phone"]?.ToString() ?? "",
                                Email = reader["Email"]?.ToString() ?? "",
                                HireDate = reader["HireDate"] as DateTime? ?? DateTime.Now,
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки сотрудников: {ex.Message}");
                throw new Exception($"Ошибка загрузки сотрудников: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Сохранение сотрудника
        /// </summary>
        public int SaveEmployee(Employee employee)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (employee.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                            UPDATE Employees 
                            SET LastName = @LastName, FirstName = @FirstName, MiddleName = @MiddleName,
                                Position = @Position, Status = @Status, Phone = @Phone, Email = @Email,
                                HireDate = @HireDate, ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", employee.Id);
                        cmd.Parameters.AddWithValue("@LastName", employee.LastName);
                        cmd.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        cmd.Parameters.AddWithValue("@MiddleName", (object)employee.MiddleName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Position", (object)employee.Position ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)employee.Status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)employee.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)employee.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HireDate", employee.HireDate);

                        cmd.ExecuteNonQuery();
                        return employee.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                            INSERT INTO Employees (LastName, FirstName, MiddleName, Position, Status, Phone, Email, HireDate, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES (@LastName, @FirstName, @MiddleName, @Position, @Status, @Phone, @Email, @HireDate, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@LastName", employee.LastName);
                        cmd.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        cmd.Parameters.AddWithValue("@MiddleName", (object)employee.MiddleName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Position", (object)employee.Position ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)employee.Status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)employee.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)employee.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@HireDate", employee.HireDate);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения сотрудника: {ex.Message}");
                throw new Exception($"Ошибка сохранения сотрудника: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление сотрудника
        /// </summary>
        public bool DeleteEmployee(int employeeId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, является ли сотрудник инструктором
                    var checkCarCmd = new SqlCommand("SELECT COUNT(*) FROM Cars WHERE InstructorId = @Id", conn);
                    checkCarCmd.Parameters.AddWithValue("@Id", employeeId);

                    if ((int)checkCarCmd.ExecuteScalar() > 0)
                        return false;

                    var checkStudentCmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE InstructorId = @Id", conn);
                    checkStudentCmd.Parameters.AddWithValue("@Id", employeeId);

                    if ((int)checkStudentCmd.ExecuteScalar() > 0)
                        return false;

                    var cmd = new SqlCommand("DELETE FROM Employees WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", employeeId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления сотрудника: {ex.Message}");
                throw new Exception($"Ошибка удаления сотрудника: {ex.Message}");
            }
        }


        /// <summary>
        /// Загрузка всех автомобилей
        /// </summary>
        public CarCollection LoadCars()
        {
            var collection = new CarCollection { Cars = new List<Car>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT c.*, e.LastName + ' ' + e.FirstName as InstructorName
                        FROM Cars c
                        LEFT JOIN Employees e ON c.InstructorId = e.Id
                        ORDER BY c.Brand, c.Model", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Cars.Add(new Car
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Brand = reader["Brand"]?.ToString() ?? "",
                                Model = reader["Model"]?.ToString() ?? "",
                                LicensePlate = reader["LicensePlate"]?.ToString() ?? "",
                                Year = reader["Year"] as int? ?? DateTime.Now.Year,
                                Color = reader["Color"]?.ToString() ?? "",
                                Category = reader["Category"]?.ToString() ?? "",
                                VIN = reader["VIN"]?.ToString() ?? "",
                                InstructorId = reader["InstructorId"] as int? ?? 0,
                                IsActive = reader["IsActive"] as bool? ?? true,
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                InstructorName = reader["InstructorName"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки автомобилей: {ex.Message}");
                throw new Exception($"Ошибка загрузки автомобилей: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Сохранение автомобиля
        /// </summary>
        public int SaveCar(Car car)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (car.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                            UPDATE Cars 
                            SET Brand = @Brand, Model = @Model, LicensePlate = @LicensePlate,
                                Year = @Year, Color = @Color, Category = @Category, VIN = @VIN,
                                InstructorId = @InstructorId, IsActive = @IsActive, ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", car.Id);
                        cmd.Parameters.AddWithValue("@Brand", car.Brand);
                        cmd.Parameters.AddWithValue("@Model", car.Model);
                        cmd.Parameters.AddWithValue("@LicensePlate", car.LicensePlate);
                        cmd.Parameters.AddWithValue("@Year", car.Year > 0 ? (object)car.Year : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Color", (object)car.Color ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Category", (object)car.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VIN", (object)car.VIN ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@InstructorId", car.InstructorId > 0 ? (object)car.InstructorId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", car.IsActive);

                        cmd.ExecuteNonQuery();
                        return car.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                            INSERT INTO Cars (Brand, Model, LicensePlate, Year, Color, Category, VIN, InstructorId, IsActive, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES (@Brand, @Model, @LicensePlate, @Year, @Color, @Category, @VIN, @InstructorId, @IsActive, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@Brand", car.Brand);
                        cmd.Parameters.AddWithValue("@Model", car.Model);
                        cmd.Parameters.AddWithValue("@LicensePlate", car.LicensePlate);
                        cmd.Parameters.AddWithValue("@Year", car.Year > 0 ? (object)car.Year : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Color", (object)car.Color ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Category", (object)car.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@VIN", (object)car.VIN ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@InstructorId", car.InstructorId > 0 ? (object)car.InstructorId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", car.IsActive);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                throw new Exception("Автомобиль с таким госномером уже существует");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения автомобиля: {ex.Message}");
                throw new Exception($"Ошибка сохранения автомобиля: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление автомобиля
        /// </summary>
        public bool DeleteCar(int carId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, используется ли автомобиль
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE CarId = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", carId);

                    if ((int)checkCmd.ExecuteScalar() > 0)
                        return false;

                    var cmd = new SqlCommand("DELETE FROM Cars WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", carId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления автомобиля: {ex.Message}");
                throw new Exception($"Ошибка удаления автомобиля: {ex.Message}");
            }
        }


        /// <summary>
        /// Загрузка всех учебных групп
        /// </summary>
        public StudyGroupCollection LoadStudyGroups()
        {
            var collection = new StudyGroupCollection { Groups = new List<StudyGroup>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT g.*, COUNT(s.Id) as StudentCount
                        FROM StudyGroups g
                        LEFT JOIN Students s ON g.Id = s.GroupId
                        GROUP BY g.Id, g.Name, g.Category, g.Status, g.StartDate, g.EndDate, 
                                 g.Duration, g.CreatedDate, g.ModifiedDate
                        ORDER BY g.StartDate DESC", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Groups.Add(new StudyGroup
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader["Name"]?.ToString() ?? "",
                                Category = reader["Category"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "Активна",
                                StartDate = reader["StartDate"] as DateTime? ?? DateTime.Now,
                                EndDate = reader["EndDate"] as DateTime? ?? DateTime.Now.AddMonths(3),
                                Duration = reader["Duration"]?.ToString() ?? "",
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                StudentCount = reader["StudentCount"] as int? ?? 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки групп: {ex.Message}");
                throw new Exception($"Ошибка загрузки групп: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Сохранение учебной группы
        /// </summary>
        public int SaveStudyGroup(StudyGroup group)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (group.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                            UPDATE StudyGroups 
                            SET Name = @Name, Category = @Category, Status = @Status,
                                StartDate = @StartDate, EndDate = @EndDate, Duration = @Duration,
                                ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", group.Id);
                        cmd.Parameters.AddWithValue("@Name", group.Name);
                        cmd.Parameters.AddWithValue("@Category", (object)group.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)group.Status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StartDate", group.StartDate);
                        cmd.Parameters.AddWithValue("@EndDate", group.EndDate);
                        cmd.Parameters.AddWithValue("@Duration", (object)group.Duration ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                        return group.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                            INSERT INTO StudyGroups (Name, Category, Status, StartDate, EndDate, Duration, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES (@Name, @Category, @Status, @StartDate, @EndDate, @Duration, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@Name", group.Name);
                        cmd.Parameters.AddWithValue("@Category", (object)group.Category ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)group.Status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@StartDate", group.StartDate);
                        cmd.Parameters.AddWithValue("@EndDate", group.EndDate);
                        cmd.Parameters.AddWithValue("@Duration", (object)group.Duration ?? DBNull.Value);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения группы: {ex.Message}");
                throw new Exception($"Ошибка сохранения группы: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление учебной группы
        /// </summary>
        public bool DeleteStudyGroup(int groupId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, есть ли студенты в группе
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE GroupId = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", groupId);

                    if ((int)checkCmd.ExecuteScalar() > 0)
                        return false;

                    var cmd = new SqlCommand("DELETE FROM StudyGroups WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", groupId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления группы: {ex.Message}");
                throw new Exception($"Ошибка удаления группы: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка всех платежей
        /// </summary>
        public PaymentCollection LoadPayments()
        {
            var collection = new PaymentCollection { Payments = new List<Payment>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT p.*, s.LastName + ' ' + s.FirstName as StudentName
                        FROM Payments p
                        JOIN Students s ON p.StudentId = s.Id
                        ORDER BY p.PaymentDate DESC", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Payments.Add(new Payment
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                PaymentType = reader["PaymentType"]?.ToString() ?? "",
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                StudentName = reader["StudentName"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки платежей: {ex.Message}");
                throw new Exception($"Ошибка загрузки платежей: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Загрузка платежей студента
        /// </summary>
        public List<Payment> LoadStudentPayments(int studentId)
        {
            var payments = new List<Payment>();

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT * FROM Payments 
                        WHERE StudentId = @StudentId 
                        ORDER BY PaymentDate DESC", conn);

                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new Payment
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                PaymentDate = reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                                PaymentType = reader["PaymentType"]?.ToString() ?? "",
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки платежей студента: {ex.Message}");
                throw new Exception($"Ошибка загрузки платежей студента: {ex.Message}");
            }

            return payments;
        }

        /// <summary>
        /// Добавление платежа
        /// </summary>
        public int AddPayment(Payment payment)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var cmd = new SqlCommand(@"
                        INSERT INTO Payments (StudentId, PaymentDate, Amount, PaymentType, CreatedDate)
                        OUTPUT INSERTED.Id
                        VALUES (@StudentId, @PaymentDate, @Amount, @PaymentType, GETDATE())", conn);

                    cmd.Parameters.AddWithValue("@StudentId", payment.StudentId);
                    cmd.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                    cmd.Parameters.AddWithValue("@Amount", payment.Amount);
                    cmd.Parameters.AddWithValue("@PaymentType", (object)payment.PaymentType ?? DBNull.Value);

                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления платежа: {ex.Message}");
                throw new Exception($"Ошибка добавления платежа: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление платежа
        /// </summary>
        public void UpdatePayment(Payment payment)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var cmd = new SqlCommand(@"
                        UPDATE Payments 
                        SET PaymentDate = @PaymentDate, 
                            Amount = @Amount, 
                            PaymentType = @PaymentType
                        WHERE Id = @Id", conn);

                    cmd.Parameters.AddWithValue("@Id", payment.Id);
                    cmd.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                    cmd.Parameters.AddWithValue("@Amount", payment.Amount);
                    cmd.Parameters.AddWithValue("@PaymentType", (object)payment.PaymentType ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления платежа: {ex.Message}");
                throw new Exception($"Ошибка обновления платежа: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление платежа
        /// </summary>
        public void DeletePayment(int paymentId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand("DELETE FROM Payments WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", paymentId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления платежа: {ex.Message}");
                throw new Exception($"Ошибка удаления платежа: {ex.Message}");
            }
        }


        /// <summary>
        /// Загрузка стоимости обучения для студента
        /// </summary>
        public StudentTuition LoadStudentTuition(int studentId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT t.*, s.LastName + ' ' + s.FirstName as StudentName
                        FROM StudentTuitions t
                        JOIN Students s ON t.StudentId = s.Id
                        WHERE t.StudentId = @StudentId", conn);

                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new StudentTuition
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                TariffId = reader["TariffId"] as int?,
                                FullAmount = reader.GetDecimal(reader.GetOrdinal("FullAmount")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                StudentName = reader["StudentName"]?.ToString() ?? ""
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки стоимости обучения: {ex.Message}");
                throw new Exception($"Ошибка загрузки стоимости обучения: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Сохранение стоимости обучения
        /// </summary>
        public int SaveStudentTuition(StudentTuition tuition)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (tuition.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                            UPDATE StudentTuitions 
                            SET TariffId = @TariffId, FullAmount = @FullAmount, 
                                Discount = @Discount, ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", tuition.Id);
                        cmd.Parameters.AddWithValue("@TariffId", (object)tuition.TariffId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FullAmount", tuition.FullAmount);
                        cmd.Parameters.AddWithValue("@Discount", tuition.Discount);

                        cmd.ExecuteNonQuery();
                        return tuition.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                            INSERT INTO StudentTuitions (StudentId, TariffId, FullAmount, Discount, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES (@StudentId, @TariffId, @FullAmount, @Discount, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@StudentId", tuition.StudentId);
                        cmd.Parameters.AddWithValue("@TariffId", (object)tuition.TariffId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FullAmount", tuition.FullAmount);
                        cmd.Parameters.AddWithValue("@Discount", tuition.Discount);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения стоимости обучения: {ex.Message}");
                throw new Exception($"Ошибка сохранения стоимости обучения: {ex.Message}");
            }
        }



        /// <summary>
        /// Получение статистики по задолженностям
        /// </summary>
        public List<DebtInfo> GetDebts(decimal minDebt = 0)
        {
            var debts = new List<DebtInfo>();

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // ИСПРАВЛЕНО: Используем FullAmount и Discount вместо FinalAmount
                    var cmd = new SqlCommand(@"
                        SELECT 
                            s.Id,
                            s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') AS StudentName,
                            s.Phone,
                            sg.Name AS GroupName,
                            ISNULL(st.FullAmount, 0) AS FullAmount,
                            ISNULL(st.Discount, 0) AS Discount,
                            ISNULL(st.FullAmount, 0) - ISNULL(st.Discount, 0) AS FinalAmount,
                            ISNULL((
                                SELECT SUM(p.Amount) 
                                FROM Payments p 
                                WHERE p.StudentId = s.Id
                            ), 0) AS Paid,
                            (ISNULL(st.FullAmount, 0) - ISNULL(st.Discount, 0)) - 
                            ISNULL((
                                SELECT SUM(p.Amount) 
                                FROM Payments p 
                                WHERE p.StudentId = s.Id
                            ), 0) AS Debt
                        FROM Students s
                        LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                        LEFT JOIN StudentTuitions st ON s.Id = st.StudentId
                        WHERE st.FullAmount IS NOT NULL
                        HAVING (ISNULL(st.FullAmount, 0) - ISNULL(st.Discount, 0)) - 
                               ISNULL((
                                   SELECT SUM(p.Amount) 
                                   FROM Payments p 
                                   WHERE p.StudentId = s.Id
                               ), 0) > @MinDebt
                        ORDER BY Debt DESC", conn);

                    cmd.Parameters.Add("@MinDebt", SqlDbType.Decimal).Value = minDebt;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            debts.Add(new DebtInfo
                            {
                                StudentId = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentName = reader["StudentName"]?.ToString() ?? "",
                                Phone = reader["Phone"]?.ToString() ?? "",
                                GroupName = reader["GroupName"]?.ToString() ?? "",
                                FullAmount = reader.GetDecimal(reader.GetOrdinal("FullAmount")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                                FinalAmount = reader.GetDecimal(reader.GetOrdinal("FinalAmount")),
                                Paid = reader.GetDecimal(reader.GetOrdinal("Paid")),
                                Debt = reader.GetDecimal(reader.GetOrdinal("Debt"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения задолженностей: {ex.Message}");
                throw new Exception($"Ошибка получения задолженностей: {ex.Message}");
            }

            return debts;
        }

        /// <summary>
        /// Получение общей статистики
        /// </summary>
        public DatabaseStatistics GetDatabaseStatistics()
        {
            var stats = new DatabaseStatistics();

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // ИСПРАВЛЕНО: Более эффективный и правильный запрос
                    var cmd = new SqlCommand(@"
                        SELECT 
                            (SELECT COUNT(*) FROM Students) as StudentsCount,
                            (SELECT COUNT(*) FROM Employees) as EmployeesCount,
                            (SELECT COUNT(*) FROM Cars) as CarsCount,
                            (SELECT COUNT(*) FROM StudyGroups) as GroupsCount,
                            (SELECT COUNT(*) FROM Payments) as PaymentsCount,
                            (SELECT ISNULL(SUM(Amount), 0) FROM Payments) as TotalPayments,
                            (SELECT COUNT(*) FROM Students WHERE GroupId IS NOT NULL) as StudentsInGroups,
                            (SELECT ISNULL(SUM(
                                (ISNULL(st.FullAmount, 0) - ISNULL(st.Discount, 0)) - 
                                ISNULL((
                                    SELECT SUM(p.Amount) 
                                    FROM Payments p 
                                    WHERE p.StudentId = st.StudentId
                                ), 0)
                            ), 0) 
                             FROM StudentTuitions st
                             WHERE (ISNULL(st.FullAmount, 0) - ISNULL(st.Discount, 0)) > 
                                   ISNULL((
                                       SELECT SUM(p.Amount) 
                                       FROM Payments p 
                                       WHERE p.StudentId = st.StudentId
                                   ), 0)) as TotalDebt", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats.StudentsCount = reader.GetInt32(0);
                            stats.EmployeesCount = reader.GetInt32(1);
                            stats.CarsCount = reader.GetInt32(2);
                            stats.GroupsCount = reader.GetInt32(3);
                            stats.PaymentsCount = reader.GetInt32(4);
                            stats.TotalPayments = reader.GetDecimal(5);
                            stats.StudentsInGroups = reader.GetInt32(6);
                            stats.TotalDebt = reader.GetDecimal(7);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения статистики: {ex.Message}");
            }

            return stats;
        }

        /// <summary>
        /// Загрузка всех паспортных данных
        /// </summary>
        public StudentPassportDataCollection LoadPassportData()
        {
            var collection = new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        SELECT p.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                        FROM StudentPassportData p
                        JOIN Students s ON p.StudentId = s.Id
                        ORDER BY s.LastName, s.FirstName", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            collection.Passports.Add(new StudentPassportData
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                DocumentType = reader["DocumentType"]?.ToString() ?? "",
                                Series = reader["Series"]?.ToString() ?? "",
                                Number = reader["Number"]?.ToString() ?? "",
                                IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                DivisionCode = reader["DivisionCode"]?.ToString() ?? "",
                                IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                StudentName = reader["StudentName"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки паспортных данных: {ex.Message}");
                throw new Exception($"Ошибка загрузки паспортных данных: {ex.Message}");
            }

            return collection;
        }

        /// <summary>
        /// Загрузка паспортных данных студента
        /// </summary>
        public StudentPassportData LoadStudentPassport(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT p.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentPassportData p
                JOIN Students s ON p.StudentId = s.Id
                WHERE p.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentPassportData
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    DocumentType = reader["DocumentType"]?.ToString() ?? "",
                                    Series = reader["Series"]?.ToString() ?? "",
                                    Number = reader["Number"]?.ToString() ?? "",
                                    IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                    DivisionCode = reader["DivisionCode"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки паспортных данных студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки паспортных данных студента: {ex.Message}");
                }

                return null;
            }

        /// <summary>
        /// Сохранение паспортных данных
        /// </summary>
        public int SavePassportData(StudentPassportData passport)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (passport.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                    UPDATE StudentPassportData 
                    SET DocumentType = @DocumentType, Series = @Series, Number = @Number,
                        IssuedBy = @IssuedBy, DivisionCode = @DivisionCode, IssueDate = @IssueDate,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", passport.Id);
                        cmd.Parameters.AddWithValue("@DocumentType", passport.DocumentType ?? "");
                        cmd.Parameters.AddWithValue("@Series", passport.Series ?? "");
                        cmd.Parameters.AddWithValue("@Number", passport.Number ?? "");
                        cmd.Parameters.AddWithValue("@IssuedBy", passport.IssuedBy ?? "");
                        cmd.Parameters.AddWithValue("@DivisionCode", (object)passport.DivisionCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IssueDate", passport.IssueDate);

                        cmd.ExecuteNonQuery();
                        return passport.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                    INSERT INTO StudentPassportData (StudentId, DocumentType, Series, Number, IssuedBy, DivisionCode, IssueDate, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @DocumentType, @Series, @Number, @IssuedBy, @DivisionCode, @IssueDate, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@StudentId", passport.StudentId);
                        cmd.Parameters.AddWithValue("@DocumentType", passport.DocumentType ?? "");
                        cmd.Parameters.AddWithValue("@Series", passport.Series ?? "");
                        cmd.Parameters.AddWithValue("@Number", passport.Number ?? "");
                        cmd.Parameters.AddWithValue("@IssuedBy", passport.IssuedBy ?? "");
                        cmd.Parameters.AddWithValue("@DivisionCode", (object)passport.DivisionCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IssueDate", passport.IssueDate);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения паспортных данных: {ex.Message}");
                throw new Exception($"Ошибка сохранения паспортных данных: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление паспортных данных
        /// </summary>
        public void DeletePassportData(int passportId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentPassportData WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", passportId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления паспортных данных: {ex.Message}");
                    throw new Exception($"Ошибка удаления паспортных данных: {ex.Message}");
                }
            }


            /// <summary>
            /// Загрузка всех данных СНИЛС
            /// </summary>
            public StudentSNILSCollection LoadSNILSData()
            {
                var collection = new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT s.*, st.LastName + ' ' + st.FirstName + ISNULL(' ' + st.MiddleName, '') as StudentName
                FROM StudentSNILS s
                JOIN Students st ON s.StudentId = st.Id
                ORDER BY st.LastName, st.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.SNILSList.Add(new StudentSNILS
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Number = reader["Number"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime?,
                                    IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных СНИЛС: {ex.Message}");
                    throw new Exception($"Ошибка загрузки данных СНИЛС: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Загрузка данных СНИЛС студента
            /// </summary>
            public StudentSNILS LoadStudentSNILS(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT s.*, st.LastName + ' ' + st.FirstName + ISNULL(' ' + st.MiddleName, '') as StudentName
                FROM StudentSNILS s
                JOIN Students st ON s.StudentId = st.Id
                WHERE s.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentSNILS
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Number = reader["Number"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime?,
                                    IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных СНИЛС студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки данных СНИЛС студента: {ex.Message}");
                }

                return null;
            }

            /// <summary>
            /// Сохранение данных СНИЛС
            /// </summary>
            public int SaveSNILSData(StudentSNILS snils)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        if (snils.Id > 0)
                        {
                            var cmd = new SqlCommand(@"
                    UPDATE StudentSNILS 
                    SET Number = @Number, IssueDate = @IssueDate, IssuedBy = @IssuedBy,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", snils.Id);
                            cmd.Parameters.AddWithValue("@Number", snils.Number ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", (object)snils.IssueDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssuedBy", (object)snils.IssuedBy ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                            return snils.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO StudentSNILS (StudentId, Number, IssueDate, IssuedBy, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @Number, @IssueDate, @IssuedBy, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@StudentId", snils.StudentId);
                            cmd.Parameters.AddWithValue("@Number", snils.Number ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", (object)snils.IssueDate ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssuedBy", (object)snils.IssuedBy ?? DBNull.Value);

                            return (int)cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения данных СНИЛС: {ex.Message}");
                    throw new Exception($"Ошибка сохранения данных СНИЛС: {ex.Message}");
                }
            }

            /// <summary>
            /// Удаление данных СНИЛС
            /// </summary>
            public void DeleteSNILSData(int snilsId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentSNILS WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", snilsId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления данных СНИЛС: {ex.Message}");
                    throw new Exception($"Ошибка удаления данных СНИЛС: {ex.Message}");
                }
            }


            /// <summary>
            /// Загрузка всех медицинских справок
            /// </summary>
            public StudentMedicalCertificateCollection LoadMedicalData()
            {
                var collection = new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT m.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentMedicalCertificates m
                JOIN Students s ON m.StudentId = s.Id
                ORDER BY s.LastName, s.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Certificates.Add(new StudentMedicalCertificate
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Series = reader["Series"]?.ToString() ?? "",
                                    Number = reader["Number"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    ValidUntil = reader["ValidUntil"] as DateTime? ?? DateTime.Now.AddYears(1),
                                    MedicalInstitution = reader["MedicalInstitution"]?.ToString() ?? "",
                                    Categories = reader["Categories"]?.ToString() ?? "",
                                    Region = reader["Region"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки медицинских справок: {ex.Message}");
                    throw new Exception($"Ошибка загрузки медицинских справок: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Загрузка медицинской справки студента
            /// </summary>
            public StudentMedicalCertificate LoadStudentMedical(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT m.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentMedicalCertificates m
                JOIN Students s ON m.StudentId = s.Id
                WHERE m.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentMedicalCertificate
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Series = reader["Series"]?.ToString() ?? "",
                                    Number = reader["Number"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    ValidUntil = reader["ValidUntil"] as DateTime? ?? DateTime.Now.AddYears(1),
                                    MedicalInstitution = reader["MedicalInstitution"]?.ToString() ?? "",
                                    Categories = reader["Categories"]?.ToString() ?? "",
                                    Region = reader["Region"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки медицинской справки студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки медицинской справки студента: {ex.Message}");
                }

                return null;
            }

            /// <summary>
            /// Сохранение медицинской справки
            /// </summary>
            public int SaveMedicalData(StudentMedicalCertificate medical)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        if (medical.Id > 0)
                        {
                            var cmd = new SqlCommand(@"
                    UPDATE StudentMedicalCertificates 
                    SET Series = @Series, Number = @Number, IssueDate = @IssueDate,
                        ValidUntil = @ValidUntil, MedicalInstitution = @MedicalInstitution,
                        Categories = @Categories, Region = @Region, ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", medical.Id);
                            cmd.Parameters.AddWithValue("@Series", medical.Series ?? "");
                            cmd.Parameters.AddWithValue("@Number", medical.Number ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", medical.IssueDate);
                            cmd.Parameters.AddWithValue("@ValidUntil", medical.ValidUntil);
                            cmd.Parameters.AddWithValue("@MedicalInstitution", medical.MedicalInstitution ?? "");
                            cmd.Parameters.AddWithValue("@Categories", (object)medical.Categories ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Region", (object)medical.Region ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                            return medical.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO StudentMedicalCertificates 
                    (StudentId, Series, Number, IssueDate, ValidUntil, MedicalInstitution, Categories, Region, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @Series, @Number, @IssueDate, @ValidUntil, @MedicalInstitution, @Categories, @Region, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@StudentId", medical.StudentId);
                            cmd.Parameters.AddWithValue("@Series", medical.Series ?? "");
                            cmd.Parameters.AddWithValue("@Number", medical.Number ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", medical.IssueDate);
                            cmd.Parameters.AddWithValue("@ValidUntil", medical.ValidUntil);
                            cmd.Parameters.AddWithValue("@MedicalInstitution", medical.MedicalInstitution ?? "");
                            cmd.Parameters.AddWithValue("@Categories", (object)medical.Categories ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Region", (object)medical.Region ?? DBNull.Value);

                            return (int)cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения медицинской справки: {ex.Message}");
                    throw new Exception($"Ошибка сохранения медицинской справки: {ex.Message}");
                }
            }

            /// <summary>
            /// Удаление медицинской справки
            /// </summary>
            public void DeleteMedicalData(int medicalId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentMedicalCertificates WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", medicalId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления медицинской справки: {ex.Message}");
                    throw new Exception($"Ошибка удаления медицинской справки: {ex.Message}");
                }
            }


            /// <summary>
            /// Загрузка всех адресов регистрации
            /// </summary>
            public StudentRegistrationAddressCollection LoadAddresses()
            {
                var collection = new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT a.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentRegistrationAddresses a
                JOIN Students s ON a.StudentId = s.Id
                ORDER BY s.LastName, s.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Addresses.Add(new StudentRegistrationAddress
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Region = reader["Region"]?.ToString() ?? "",
                                    City = reader["City"]?.ToString() ?? "",
                                    Street = reader["Street"]?.ToString() ?? "",
                                    House = reader["House"]?.ToString() ?? "",
                                    Building = reader["Building"]?.ToString() ?? "",
                                    Apartment = reader["Apartment"]?.ToString() ?? "",
                                    PostalCode = reader["PostalCode"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки адресов регистрации: {ex.Message}");
                    throw new Exception($"Ошибка загрузки адресов регистрации: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Загрузка адреса регистрации студента
            /// </summary>
            public StudentRegistrationAddress LoadStudentAddress(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT a.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentRegistrationAddresses a
                JOIN Students s ON a.StudentId = s.Id
                WHERE a.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentRegistrationAddress
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Region = reader["Region"]?.ToString() ?? "",
                                    City = reader["City"]?.ToString() ?? "",
                                    Street = reader["Street"]?.ToString() ?? "",
                                    House = reader["House"]?.ToString() ?? "",
                                    Building = reader["Building"]?.ToString() ?? "",
                                    Apartment = reader["Apartment"]?.ToString() ?? "",
                                    PostalCode = reader["PostalCode"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки адреса регистрации студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки адреса регистрации студента: {ex.Message}");
                }

                return null;
            }

            /// <summary>
            /// Сохранение адреса регистрации
            /// </summary>
            public int SaveAddressData(StudentRegistrationAddress address)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        if (address.Id > 0)
                        {
                            var cmd = new SqlCommand(@"
                    UPDATE StudentRegistrationAddresses 
                    SET Region = @Region, City = @City, Street = @Street, House = @House,
                        Building = @Building, Apartment = @Apartment, PostalCode = @PostalCode,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", address.Id);
                            cmd.Parameters.AddWithValue("@Region", address.Region ?? "");
                            cmd.Parameters.AddWithValue("@City", address.City ?? "");
                            cmd.Parameters.AddWithValue("@Street", address.Street ?? "");
                            cmd.Parameters.AddWithValue("@House", address.House ?? "");
                            cmd.Parameters.AddWithValue("@Building", (object)address.Building ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Apartment", (object)address.Apartment ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PostalCode", (object)address.PostalCode ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                            return address.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO StudentRegistrationAddresses 
                    (StudentId, Region, City, Street, House, Building, Apartment, PostalCode, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @Region, @City, @Street, @House, @Building, @Apartment, @PostalCode, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@StudentId", address.StudentId);
                            cmd.Parameters.AddWithValue("@Region", address.Region ?? "");
                            cmd.Parameters.AddWithValue("@City", address.City ?? "");
                            cmd.Parameters.AddWithValue("@Street", address.Street ?? "");
                            cmd.Parameters.AddWithValue("@House", address.House ?? "");
                            cmd.Parameters.AddWithValue("@Building", (object)address.Building ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Apartment", (object)address.Apartment ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PostalCode", (object)address.PostalCode ?? DBNull.Value);

                            return (int)cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения адреса регистрации: {ex.Message}");
                    throw new Exception($"Ошибка сохранения адреса регистрации: {ex.Message}");
                }
            }

            /// <summary>
            /// Удаление адреса регистрации
            /// </summary>
            public void DeleteAddressData(int addressId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentRegistrationAddresses WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", addressId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления адреса регистрации: {ex.Message}");
                    throw new Exception($"Ошибка удаления адреса регистрации: {ex.Message}");
                }
            }


            /// <summary>
            /// Загрузка всех свидетельств об окончании
            /// </summary>
            public StudentCertificateCollection LoadCertificates()
            {
                var collection = new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT c.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName,
                       vc.Code as CategoryCode, vc.FullName as CategoryName
                FROM StudentCertificates c
                JOIN Students s ON c.StudentId = s.Id
                JOIN VehicleCategories vc ON c.VehicleCategoryId = vc.Id
                ORDER BY s.LastName, s.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Certificates.Add(new StudentCertificate
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    CertificateSeries = reader["CertificateSeries"]?.ToString() ?? "",
                                    CertificateNumber = reader["CertificateNumber"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    VehicleCategoryId = reader.GetInt32(reader.GetOrdinal("VehicleCategoryId")),
                                    CategoryCode = reader["CategoryCode"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? "",
                                    CategoryName = reader["CategoryName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки свидетельств: {ex.Message}");
                    throw new Exception($"Ошибка загрузки свидетельств: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Загрузка свидетельства студента
            /// </summary>
            public StudentCertificate LoadStudentCertificate(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT c.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName,
                       vc.Code as CategoryCode, vc.FullName as CategoryName
                FROM StudentCertificates c
                JOIN Students s ON c.StudentId = s.Id
                JOIN VehicleCategories vc ON c.VehicleCategoryId = vc.Id
                WHERE c.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentCertificate
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    CertificateSeries = reader["CertificateSeries"]?.ToString() ?? "",
                                    CertificateNumber = reader["CertificateNumber"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    VehicleCategoryId = reader.GetInt32(reader.GetOrdinal("VehicleCategoryId")),
                                    CategoryCode = reader["CategoryCode"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? "",
                                    CategoryName = reader["CategoryName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки свидетельства студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки свидетельства студента: {ex.Message}");
                }

                return null;
            }

        /// <summary>
        /// Сохранение свидетельства - ИСПРАВЛЕНО
        /// </summary>
        public int SaveCertificateData(StudentCertificate certificate)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    if (certificate.Id > 0)
                    {
                        var cmd = new SqlCommand(@"
                    UPDATE StudentCertificates 
                    SET CertificateSeries = @CertificateSeries, 
                        CertificateNumber = @CertificateNumber,
                        IssueDate = @IssueDate, 
                        VehicleCategoryId = @VehicleCategoryId,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);  // Убрали CategoryCode

                        cmd.Parameters.AddWithValue("@Id", certificate.Id);
                        cmd.Parameters.AddWithValue("@CertificateSeries", certificate.CertificateSeries ?? "");
                        cmd.Parameters.AddWithValue("@CertificateNumber", certificate.CertificateNumber ?? "");
                        cmd.Parameters.AddWithValue("@IssueDate", certificate.IssueDate);
                        cmd.Parameters.AddWithValue("@VehicleCategoryId", certificate.VehicleCategoryId);

                        cmd.ExecuteNonQuery();
                        return certificate.Id;
                    }
                    else
                    {
                        var cmd = new SqlCommand(@"
                    INSERT INTO StudentCertificates 
                    (StudentId, CertificateSeries, CertificateNumber, IssueDate, VehicleCategoryId, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @CertificateSeries, @CertificateNumber, @IssueDate, @VehicleCategoryId, GETDATE())", conn);  // Убрали CategoryCode

                        cmd.Parameters.AddWithValue("@StudentId", certificate.StudentId);
                        cmd.Parameters.AddWithValue("@CertificateSeries", certificate.CertificateSeries ?? "");
                        cmd.Parameters.AddWithValue("@CertificateNumber", certificate.CertificateNumber ?? "");
                        cmd.Parameters.AddWithValue("@IssueDate", certificate.IssueDate);
                        cmd.Parameters.AddWithValue("@VehicleCategoryId", certificate.VehicleCategoryId);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения свидетельства: {ex.Message}");
                throw new Exception($"Ошибка сохранения свидетельства: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление свидетельства
        /// </summary>
        public void DeleteCertificateData(int certificateId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentCertificates WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", certificateId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления свидетельства: {ex.Message}");
                    throw new Exception($"Ошибка удаления свидетельства: {ex.Message}");
                }
            }


            /// <summary>
            /// Загрузка всех водительских удостоверений
            /// </summary>
            public StudentDrivingLicenseCollection LoadDrivingLicenses()
            {
                var collection = new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT l.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentDrivingLicenses l
                JOIN Students s ON l.StudentId = s.Id
                ORDER BY s.LastName, s.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Licenses.Add(new StudentDrivingLicense
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Series = reader["Series"]?.ToString() ?? "",
                                    Number = reader["Number"]?.ToString() ?? "",
                                    LicenseCateg = reader["LicenseCateg"]?.ToString() ?? "",
                                    IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                    DivisionCode = reader["DivisionCode"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    ExpiryDate = reader["ExpiryDate"] as DateTime? ?? DateTime.Now.AddYears(10),
                                    ExperienceYears = reader["ExperienceYears"] as int? ?? 0,
                                    Status = reader["Status"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки водительских удостоверений: {ex.Message}");
                    throw new Exception($"Ошибка загрузки водительских удостоверений: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Загрузка водительского удостоверения студента
            /// </summary>
            public StudentDrivingLicense LoadStudentDrivingLicense(int studentId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand(@"
                SELECT l.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName
                FROM StudentDrivingLicenses l
                JOIN Students s ON l.StudentId = s.Id
                WHERE l.StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new StudentDrivingLicense
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Series = reader["Series"]?.ToString() ?? "",
                                    Number = reader["Number"]?.ToString() ?? "",
                                    LicenseCateg = reader["LicenseCateg"]?.ToString() ?? "",
                                    IssuedBy = reader["IssuedBy"]?.ToString() ?? "",
                                    DivisionCode = reader["DivisionCode"]?.ToString() ?? "",
                                    IssueDate = reader["IssueDate"] as DateTime? ?? DateTime.Now,
                                    ExpiryDate = reader["ExpiryDate"] as DateTime? ?? DateTime.Now.AddYears(10),
                                    ExperienceYears = reader["ExperienceYears"] as int? ?? 0,
                                    Status = reader["Status"]?.ToString() ?? "",
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки водительского удостоверения студента: {ex.Message}");
                    throw new Exception($"Ошибка загрузки водительского удостоверения студента: {ex.Message}");
                }

                return null;
            }

            /// <summary>
            /// Сохранение водительского удостоверения
            /// </summary>
            public int SaveDrivingLicense(StudentDrivingLicense license)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        if (license.Id > 0)
                        {
                            var cmd = new SqlCommand(@"
                    UPDATE StudentDrivingLicenses 
                    SET Series = @Series, Number = @Number, LicenseCateg = @LicenseCateg,
                        IssuedBy = @IssuedBy, DivisionCode = @DivisionCode, IssueDate = @IssueDate,
                        ExpiryDate = @ExpiryDate, ExperienceYears = @ExperienceYears, Status = @Status,
                        ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", license.Id);
                            cmd.Parameters.AddWithValue("@Series", license.Series ?? "");
                            cmd.Parameters.AddWithValue("@Number", license.Number ?? "");
                            cmd.Parameters.AddWithValue("@LicenseCateg", (object)license.LicenseCateg ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssuedBy", license.IssuedBy ?? "");
                            cmd.Parameters.AddWithValue("@DivisionCode", (object)license.DivisionCode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssueDate", license.IssueDate);
                            cmd.Parameters.AddWithValue("@ExpiryDate", license.ExpiryDate);
                            cmd.Parameters.AddWithValue("@ExperienceYears", license.ExperienceYears);
                            cmd.Parameters.AddWithValue("@Status", (object)license.Status ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                            return license.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO StudentDrivingLicenses 
                    (StudentId, Series, Number, LicenseCateg, IssuedBy, DivisionCode, IssueDate, ExpiryDate, ExperienceYears, Status, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @Series, @Number, @LicenseCateg, @IssuedBy, @DivisionCode, @IssueDate, @ExpiryDate, @ExperienceYears, @Status, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@StudentId", license.StudentId);
                            cmd.Parameters.AddWithValue("@Series", license.Series ?? "");
                            cmd.Parameters.AddWithValue("@Number", license.Number ?? "");
                            cmd.Parameters.AddWithValue("@LicenseCateg", (object)license.LicenseCateg ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssuedBy", license.IssuedBy ?? "");
                            cmd.Parameters.AddWithValue("@DivisionCode", (object)license.DivisionCode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IssueDate", license.IssueDate);
                            cmd.Parameters.AddWithValue("@ExpiryDate", license.ExpiryDate);
                            cmd.Parameters.AddWithValue("@ExperienceYears", license.ExperienceYears);
                            cmd.Parameters.AddWithValue("@Status", (object)license.Status ?? DBNull.Value);

                            return (int)cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения водительского удостоверения: {ex.Message}");
                    throw new Exception($"Ошибка сохранения водительского удостоверения: {ex.Message}");
                }
            }

            /// <summary>
            /// Удаление водительского удостоверения
            /// </summary>
            public void DeleteDrivingLicense(int licenseId)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("DELETE FROM StudentDrivingLicenses WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", licenseId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления водительского удостоверения: {ex.Message}");
                    throw new Exception($"Ошибка удаления водительского удостоверения: {ex.Message}");
                }
            }


        /// <summary>
        /// Загрузка всех стоимостей обучения
        /// </summary>
        public StudentTuitionCollection LoadStudentTuitions()
        {
            var collection = new StudentTuitionCollection { Tuitions = new List<StudentTuition>() };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    // ИСПРАВЛЕНО: Убрали FinalAmount из запроса
                    var cmd = new SqlCommand(@"
                        SELECT t.*, 
                               s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName,
                               tar.Name as TariffName,
                               ISNULL((
                                   SELECT SUM(p.Amount) 
                                   FROM Payments p 
                                   WHERE p.StudentId = t.StudentId
                               ), 0) as PaidAmount
                        FROM StudentTuitions t
                        JOIN Students s ON t.StudentId = s.Id
                        LEFT JOIN Tariffs tar ON t.TariffId = tar.Id
                        ORDER BY s.LastName, s.FirstName", conn);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tuition = new StudentTuition
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                TariffId = reader["TariffId"] as int?,
                                FullAmount = reader.GetDecimal(reader.GetOrdinal("FullAmount")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                                CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                StudentName = reader["StudentName"]?.ToString() ?? "",
                                TariffName = reader["TariffName"]?.ToString() ?? "",
                                PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount"))
                            };

                            collection.Tuitions.Add(tuition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки стоимостей обучения: {ex.Message}");
                throw new Exception($"Ошибка загрузки стоимостей обучения: {ex.Message}");
            }

            return collection;
        }
    


            /// <summary>
            /// Загрузка всех тарифов
            /// </summary>
            public TariffCollection LoadTariffs()
            {
                var collection = new TariffCollection { Tariffs = new List<Tariff>() };

                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        var cmd = new SqlCommand("SELECT * FROM Tariffs ORDER BY Name", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Tariffs.Add(new Tariff
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader["Name"]?.ToString() ?? "",
                                    Description = reader["Description"]?.ToString() ?? "",
                                    Category = reader["Category"]?.ToString() ?? "",
                                    DurationMonths = reader.GetInt32(reader.GetOrdinal("DurationMonths")),
                                    PracticeHours = reader["PracticeHours"] as int? ?? 0,
                                    BaseCost = reader.GetDecimal(reader.GetOrdinal("BaseCost")),
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки тарифов: {ex.Message}");
                    throw new Exception($"Ошибка загрузки тарифов: {ex.Message}");
                }

                return collection;
            }

            /// <summary>
            /// Сохранение тарифа
            /// </summary>
            public int SaveTariff(Tariff tariff)
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();

                        if (tariff.Id > 0)
                        {
                            var cmd = new SqlCommand(@"
                    UPDATE Tariffs 
                    SET Name = @Name, Description = @Description, Category = @Category,
                        DurationMonths = @DurationMonths, PracticeHours = @PracticeHours,
                        BaseCost = @BaseCost, ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", tariff.Id);
                            cmd.Parameters.AddWithValue("@Name", tariff.Name ?? "");
                            cmd.Parameters.AddWithValue("@Description", (object)tariff.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Category", (object)tariff.Category ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DurationMonths", tariff.DurationMonths);
                            cmd.Parameters.AddWithValue("@PracticeHours", tariff.PracticeHours);
                            cmd.Parameters.AddWithValue("@BaseCost", tariff.BaseCost);

                            cmd.ExecuteNonQuery();
                            return tariff.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO Tariffs (Name, Description, Category, DurationMonths, PracticeHours, BaseCost, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@Name, @Description, @Category, @DurationMonths, @PracticeHours, @BaseCost, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@Name", tariff.Name ?? "");
                            cmd.Parameters.AddWithValue("@Description", (object)tariff.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Category", (object)tariff.Category ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DurationMonths", tariff.DurationMonths);
                            cmd.Parameters.AddWithValue("@PracticeHours", tariff.PracticeHours);
                            cmd.Parameters.AddWithValue("@BaseCost", tariff.BaseCost);

                            return (int)cmd.ExecuteScalar();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения тарифа: {ex.Message}");
                    throw new Exception($"Ошибка сохранения тарифа: {ex.Message}");
                }
            }

        /// <summary>
        /// Удаление тарифа
        /// </summary>
        public bool DeleteTariff(int tariffId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, используется ли тариф
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM StudentTuitions WHERE TariffId = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", tariffId);

                    if ((int)checkCmd.ExecuteScalar() > 0)
                        return false;

                    var cmd = new SqlCommand("DELETE FROM Tariffs WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", tariffId);

                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления тарифа: {ex.Message}");
                throw new Exception($"Ошибка удаления тарифа: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление стоимости обучения студента
        /// </summary>
        public void UpdateStudentTuition(int studentId, decimal tuitionAmount, decimal discountAmount, int? tariffId = null)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                UPDATE Students 
                SET TuitionAmount = @TuitionAmount,
                    DiscountAmount = @DiscountAmount,
                    TariffId = @TariffId,
                    ModifiedDate = GETDATE()
                WHERE Id = @StudentId", conn);

                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    cmd.Parameters.AddWithValue("@TuitionAmount", tuitionAmount);
                    cmd.Parameters.AddWithValue("@DiscountAmount", discountAmount);
                    cmd.Parameters.AddWithValue("@TariffId", tariffId.HasValue && tariffId.Value > 0 ? (object)tariffId.Value : DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления стоимости обучения: {ex.Message}");
                throw new Exception($"Ошибка обновления стоимости обучения: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение информации о платежах и остатке для студента
        /// </summary>
        public (decimal tuition, decimal discount, decimal paid, decimal final, decimal remaining) GetStudentPaymentInfo(int studentId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
        SELECT 
            ISNULL(st.FullAmount, s.TuitionAmount) as TuitionAmount,
            ISNULL(st.Discount, s.DiscountAmount) as DiscountAmount,
            ISNULL((
                SELECT SUM(Amount) 
                FROM Payments p 
                WHERE p.StudentId = s.Id
            ), 0) as PaidAmount,
            t.Name as TariffName
        FROM Students s
        LEFT JOIN StudentTuitions st ON s.Id = st.StudentId
        LEFT JOIN Tariffs t ON ISNULL(st.TariffId, s.TariffId) = t.Id
        WHERE s.Id = @StudentId", conn);

                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var tuition = reader["TuitionAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TuitionAmount"]) : 0;
                            var discount = reader["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(reader["DiscountAmount"]) : 0;
                            var paid = reader["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(reader["PaidAmount"]) : 0;
                            var final = tuition - discount;
                            var remaining = final - paid;

                            Debug.WriteLine($"GetStudentPaymentInfo: tuition={tuition}, discount={discount}, paid={paid}, final={final}, remaining={remaining}");

                            return (tuition, discount, paid, final, remaining > 0 ? remaining : 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка получения информации о платежах: {ex.Message}");
            }

            return (0, 0, 0, 0, 0);
        }

        /// <summary>
        /// Обновление стоимости обучения студента
        /// </summary>
        public void UpdateStudentTuition(int studentId, decimal tuitionAmount, decimal discountAmount)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Проверяем, есть ли запись в StudentTuitions
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM StudentTuitions WHERE StudentId = @StudentId", conn);
                    checkCmd.Parameters.AddWithValue("@StudentId", studentId);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        // Обновляем существующую запись
                        var cmd = new SqlCommand(@"
                    UPDATE StudentTuitions 
                    SET FullAmount = @FullAmount, 
                        Discount = @Discount,
                        ModifiedDate = GETDATE()
                    WHERE StudentId = @StudentId", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);
                        cmd.Parameters.AddWithValue("@FullAmount", tuitionAmount);
                        cmd.Parameters.AddWithValue("@Discount", discountAmount);

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Создаем новую запись
                        var cmd = new SqlCommand(@"
                    INSERT INTO StudentTuitions (StudentId, FullAmount, Discount, CreatedDate)
                    VALUES (@StudentId, @FullAmount, @Discount, GETDATE())", conn);

                        cmd.Parameters.AddWithValue("@StudentId", studentId);
                        cmd.Parameters.AddWithValue("@FullAmount", tuitionAmount);
                        cmd.Parameters.AddWithValue("@Discount", discountAmount);

                        cmd.ExecuteNonQuery();
                    }

                    // Также обновляем поля в Students для обратной совместимости
                    var updateStudentCmd = new SqlCommand(@"
                UPDATE Students 
                SET TuitionAmount = @TuitionAmount,
                    DiscountAmount = @DiscountAmount,
                    ModifiedDate = GETDATE()
                WHERE Id = @StudentId", conn);

                    updateStudentCmd.Parameters.AddWithValue("@StudentId", studentId);
                    updateStudentCmd.Parameters.AddWithValue("@TuitionAmount", tuitionAmount);
                    updateStudentCmd.Parameters.AddWithValue("@DiscountAmount", discountAmount);

                    updateStudentCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления стоимости обучения: {ex.Message}");
                throw new Exception($"Ошибка обновления стоимости обучения: {ex.Message}");
            }
        }

        public bool MergeGroups(int newGroupId, List<int> sourceGroupIds)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. ПРОВЕРЯЕМ, ЧТО НОВАЯ ГРУППА СУЩЕСТВУЕТ
                        string checkGroupSql = "SELECT COUNT(*) FROM StudyGroups WHERE Id = @NewGroupId";
                        using (var checkCmd = new SqlCommand(checkGroupSql, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@NewGroupId", newGroupId);
                            int groupExists = (int)checkCmd.ExecuteScalar();

                            if (groupExists == 0)
                            {
                                throw new Exception($"Группа с ID {newGroupId} не найдена в базе данных");
                            }
                        }

                        // 2. Переносим студентов в новую группу
                        string updateStudentsSql = @"
                    UPDATE Students 
                    SET GroupId = @NewGroupId, ModifiedDate = GETDATE()
                    WHERE GroupId = @OldGroupId";

                        foreach (var groupId in sourceGroupIds)
                        {
                            using (var cmd = new SqlCommand(updateStudentsSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NewGroupId", newGroupId);
                                cmd.Parameters.AddWithValue("@OldGroupId", groupId);

                                int updated = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Перенесено студентов из группы {groupId}: {updated}");
                            }
                        }

                        // 3. Проверяем, что студентов больше нет в исходных группах
                        foreach (var groupId in sourceGroupIds)
                        {
                            string checkStudentsSql = "SELECT COUNT(*) FROM Students WHERE GroupId = @GroupId";
                            using (var checkCmd = new SqlCommand(checkStudentsSql, connection, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@GroupId", groupId);
                                int remainingStudents = (int)checkCmd.ExecuteScalar();

                                if (remainingStudents > 0)
                                {
                                    throw new Exception($"В группе {groupId} осталось {remainingStudents} студентов после переноса!");
                                }
                            }
                        }

                        // 4. Удаляем исходные группы
                        string deleteGroupsSql = "DELETE FROM StudyGroups WHERE Id = @GroupId";

                        foreach (var groupId in sourceGroupIds)
                        {
                            using (var cmd = new SqlCommand(deleteGroupsSql, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@GroupId", groupId);
                                int deleted = cmd.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"Удалена группа {groupId}");
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Ошибка в транзакции: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        // =====================================================
        // Методы для бронирования уроков вождения
        // =====================================================

        /// <summary>
        /// Загрузка доступных слотов для инструктора и автомобиля
        /// </summary>
        public LessonSlotCollection LoadAvailableSlots(int instructorId, int carId, DateTime startDate, DateTime endDate)
        {
            var collection = new LessonSlotCollection { Slots = new List<LessonSlot>() };

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT ls.*, 
                   e.LastName + ' ' + e.FirstName as InstructorName,
                   c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
            FROM LessonSlots ls
            LEFT JOIN Employees e ON ls.InstructorId = e.Id
            LEFT JOIN Cars c ON ls.CarId = c.Id
            WHERE ls.InstructorId = @InstructorId 
              AND ls.CarId = @CarId
              AND ls.LessonDate BETWEEN @StartDate AND @EndDate
              AND ls.IsAvailable = 1
            ORDER BY ls.LessonDate, ls.StartTime", conn);

                cmd.Parameters.AddWithValue("@InstructorId", instructorId);
                cmd.Parameters.AddWithValue("@CarId", carId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        collection.Slots.Add(new LessonSlot
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            InstructorId = reader.GetInt32(reader.GetOrdinal("InstructorId")),
                            CarId = reader.GetInt32(reader.GetOrdinal("CarId")),
                            LessonDate = reader.GetDateTime(reader.GetOrdinal("LessonDate")),
                            StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                            EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                            IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            InstructorName = reader["InstructorName"]?.ToString() ?? "",
                            CarInfo = reader["CarInfo"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return collection;
        }

        /// <summary>
        /// Бронирование урока
        /// </summary>
        public int BookLesson(int studentId, int slotId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем информацию о слоте
                        var slotCmd = new SqlCommand(@"
                    SELECT InstructorId, CarId, LessonDate, StartTime, EndTime 
                    FROM LessonSlots 
                    WHERE Id = @SlotId AND IsAvailable = 1", conn, transaction);
                        slotCmd.Parameters.AddWithValue("@SlotId", slotId);

                        int instructorId = 0, carId = 0;
                        DateTime lessonDate = DateTime.Today;
                        TimeSpan startTime = TimeSpan.Zero, endTime = TimeSpan.Zero;

                        using (var reader = slotCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                instructorId = reader.GetInt32(0);
                                carId = reader.GetInt32(1);
                                lessonDate = reader.GetDateTime(2);
                                startTime = TimeSpan.Parse(reader[3].ToString());
                                endTime = TimeSpan.Parse(reader[4].ToString());
                            }
                            else
                            {
                                throw new Exception("Слот не найден или уже занят");
                            }
                        }

                        // Создаем бронирование
                        var insertCmd = new SqlCommand(@"
                    INSERT INTO DrivingLessons (StudentId, InstructorId, CarId, SlotId, LessonDate, StartTime, EndTime, Status, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @InstructorId, @CarId, @SlotId, @LessonDate, @StartTime, @EndTime, 'Booked', GETDATE())", conn, transaction);

                        insertCmd.Parameters.AddWithValue("@StudentId", studentId);
                        insertCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        insertCmd.Parameters.AddWithValue("@CarId", carId);
                        insertCmd.Parameters.AddWithValue("@SlotId", slotId);
                        insertCmd.Parameters.AddWithValue("@LessonDate", lessonDate);
                        insertCmd.Parameters.AddWithValue("@StartTime", startTime);
                        insertCmd.Parameters.AddWithValue("@EndTime", endTime);

                        int lessonId = (int)insertCmd.ExecuteScalar();

                        // Закрываем слот
                        var updateSlotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 0 WHERE Id = @SlotId", conn, transaction);
                        updateSlotCmd.Parameters.AddWithValue("@SlotId", slotId);
                        updateSlotCmd.ExecuteNonQuery();

                        transaction.Commit();
                        return lessonId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка уроков студента
        /// </summary>
        public List<DrivingLesson> LoadStudentLessons(int studentId)
        {
            var lessons = new List<DrivingLesson>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
    SELECT dl.*, 
           s.LastName + ' ' + s.FirstName as StudentName,
           e.LastName + ' ' + e.FirstName as InstructorName,
           c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
    FROM DrivingLessons dl
    JOIN Students s ON dl.StudentId = s.Id
    JOIN Employees e ON dl.InstructorId = e.Id
    JOIN Cars c ON dl.CarId = c.Id
    WHERE dl.StudentId = @StudentId
    ORDER BY dl.LessonDate DESC, dl.StartTime DESC", conn);

                cmd.Parameters.AddWithValue("@StudentId", studentId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lessons.Add(new DrivingLesson
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                            InstructorId = reader.GetInt32(reader.GetOrdinal("InstructorId")),
                            CarId = reader.GetInt32(reader.GetOrdinal("CarId")),
                            SlotId = reader["SlotId"] as int?,
                            LessonDate = reader.GetDateTime(reader.GetOrdinal("LessonDate")),
                            StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                            EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                            Status = reader["Status"]?.ToString() ?? "Booked",
                            CanceledAt = reader["CanceledAt"] as DateTime?,
                            IsCancelledByStudent = reader["IsCancelledByStudent"] as bool? ?? false,
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsExtra = reader["IsExtra"] != DBNull.Value && Convert.ToBoolean(reader["IsExtra"]), // ДОБАВЬТЕ ЭТУ СТРОКУ
                            StudentName = reader["StudentName"]?.ToString() ?? "",
                            InstructorName = reader["InstructorName"]?.ToString() ?? "",
                            CarInfo = reader["CarInfo"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return lessons;
        }

        /// <summary>
        /// Отмена урока
        /// </summary>
        public string CancelLesson(int lessonId, DateTime cancelTime)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем информацию об уроке
                var cmd = new SqlCommand(@"
            SELECT LessonDate, StartTime, StudentId, InstructorId, CarId, SlotId
            FROM DrivingLessons WHERE Id = @LessonId", conn);
                cmd.Parameters.AddWithValue("@LessonId", lessonId);

                DateTime lessonDate;
                TimeSpan startTime;
                int instructorId, carId;
                int? slotId;  // ✅ ИСПРАВЛЕНО: nullable int объявлен отдельно

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return "Урок не найден";
                    lessonDate = reader.GetDateTime(0);
                    startTime = TimeSpan.Parse(reader[1].ToString());
                    instructorId = reader.GetInt32(2);
                    carId = reader.GetInt32(3);
                    slotId = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4);
                }

                // Проверяем, можно ли отменить без штрафа (за 24 часа)
                var lessonDateTime = lessonDate + startTime;
                var hoursBefore = (lessonDateTime - cancelTime).TotalHours;

                bool isWithin24Hours = hoursBefore <= 24;

                if (isWithin24Hours)
                {
                    // Отмена за 24 часа - без штрафа
                    var updateCmd = new SqlCommand(@"
                UPDATE DrivingLessons 
                SET Status = 'Cancelled', CanceledAt = @CancelTime, IsCancelledByStudent = 1
                WHERE Id = @LessonId", conn);
                    updateCmd.Parameters.AddWithValue("@LessonId", lessonId);
                    updateCmd.Parameters.AddWithValue("@CancelTime", cancelTime);
                    updateCmd.ExecuteNonQuery();

                    // Возвращаем слот в доступные
                    if (slotId.HasValue)
                    {
                        var slotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 1 WHERE Id = @SlotId", conn);
                        slotCmd.Parameters.AddWithValue("@SlotId", slotId.Value);
                        slotCmd.ExecuteNonQuery();
                    }

                    return "Урок отменен без штрафа";
                }
                else
                {
                    // Отмена менее чем за 24 часа - урок считается пропущенным
                    var updateCmd = new SqlCommand(@"
                UPDATE DrivingLessons 
                SET Status = 'NoShow', CanceledAt = @CancelTime, IsCancelledByStudent = 1
                WHERE Id = @LessonId", conn);
                    updateCmd.Parameters.AddWithValue("@LessonId", lessonId);
                    updateCmd.Parameters.AddWithValue("@CancelTime", cancelTime);
                    updateCmd.ExecuteNonQuery();

                    // Обновляем счетчик пропусков у студента
                    var studentCmd = new SqlCommand(@"
                UPDATE Students 
                SET MissedLessons = ISNULL(MissedLessons, 0) + 1
                WHERE Id = @StudentId", conn);
                    studentCmd.Parameters.AddWithValue("@StudentId", instructorId);  // ✅ ИСПРАВЛЕНО: используем instructorId из reader
                    studentCmd.ExecuteNonQuery();

                    return "Урок засчитан как пропущенный (отмена менее чем за 24 часа)";
                }
            }
        }

        private DrivingLesson MapDrivingLesson(SqlDataReader reader)
        {
            return new DrivingLesson
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                InstructorId = reader.GetInt32(reader.GetOrdinal("InstructorId")),
                CarId = reader.GetInt32(reader.GetOrdinal("CarId")),
                SlotId = reader["SlotId"] as int?,
                LessonDate = reader.GetDateTime(reader.GetOrdinal("LessonDate")),
                StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                Status = reader["Status"]?.ToString() ?? "Booked",
                CanceledAt = reader["CanceledAt"] as DateTime?,
                IsCancelledByStudent = reader["IsCancelledByStudent"] as bool? ?? false,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StudentName = reader["StudentName"]?.ToString() ?? "",
                InstructorName = reader["InstructorName"]?.ToString() ?? "",
                CarInfo = reader["CarInfo"]?.ToString() ?? ""
            };
        }

        /// <summary>
        /// Создание слотов на месяц для всех инструкторов
        /// </summary>
        public void GenerateSlotsForNextMonth()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем всех инструкторов и их машины
                var instructors = new List<(int Id, int CarId)>();
                var cmd = new SqlCommand(@"
            SELECT e.Id, c.Id AS CarId 
            FROM Employees e
            INNER JOIN Cars c ON e.Id = c.InstructorId
            WHERE e.Position LIKE '%инструктор%' AND c.IsActive = 1", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        instructors.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }

                if (instructors.Count == 0) return;

                // Определяем диапазон: начало следующего месяца
                var today = DateTime.Today;
                var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Временные слоты
                var timeSlots = new[]
                {
                new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
                new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0),
                new TimeSpan(13, 0, 0), new TimeSpan(15, 0, 0),
                new TimeSpan(15, 0, 0), new TimeSpan(17, 0, 0),
                new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0)
        };

                foreach (var (instructorId, carId) in instructors)
                {
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        // Выходные пропускаем (воскресенье)
                        if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                        for (int i = 0; i < timeSlots.Length; i += 2)
                        {
                            // Проверяем, нет ли уже такого слота
                            var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM LessonSlots 
                        WHERE InstructorId = @InstructorId 
                          AND CarId = @CarId 
                          AND LessonDate = @LessonDate 
                          AND StartTime = @StartTime", conn);
                            checkCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                            checkCmd.Parameters.AddWithValue("@CarId", carId);
                            checkCmd.Parameters.AddWithValue("@LessonDate", date);
                            checkCmd.Parameters.AddWithValue("@StartTime", timeSlots[i]);

                            if ((int)checkCmd.ExecuteScalar() == 0)
                            {
                                var insertCmd = new SqlCommand(@"
                            INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable)
                            VALUES (@InstructorId, @CarId, @LessonDate, @StartTime, @EndTime, 1)", conn);
                                insertCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                                insertCmd.Parameters.AddWithValue("@CarId", carId);
                                insertCmd.Parameters.AddWithValue("@LessonDate", date);
                                insertCmd.Parameters.AddWithValue("@StartTime", timeSlots[i]);
                                insertCmd.Parameters.AddWithValue("@EndTime", timeSlots[i + 1]);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Создает слоты на следующие 2 месяца для всех инструкторов
        /// </summary>
        public void CreateSlotsForAllInstructors()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем всех инструкторов и их машины
                var cmd = new SqlCommand(@"
            SELECT e.Id, c.Id 
            FROM Employees e
            INNER JOIN Cars c ON e.Id = c.InstructorId
            WHERE c.IsActive = 1", conn);  // Убрал проверку на позицию

                var instructors = new List<(int InstId, int CarId)>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        instructors.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }

                if (instructors.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Нет инструкторов с привязанными машинами");
                    return;
                }

                // НАЧИНАЕМ С ПЕРВОГО ДНЯ ТЕКУЩЕГО МЕСЯЦА
                var today = DateTime.Today;
                var startDate = new DateTime(today.Year, today.Month, 1);
                var endDate = startDate.AddMonths(2).AddDays(-1);

                var timeSlots = new[]
                {
            new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0),
            new TimeSpan(13, 0, 0), new TimeSpan(15, 0, 0),
            new TimeSpan(15, 0, 0), new TimeSpan(17, 0, 0),
            new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0)
        };

                int totalInserted = 0;

                foreach (var (instId, carId) in instructors)
                {
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                        for (int i = 0; i < timeSlots.Length; i += 2)
                        {
                            // Проверяем, нет ли уже такого слота
                            var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM LessonSlots 
                        WHERE InstructorId = @InstId 
                          AND CarId = @CarId 
                          AND LessonDate = @Date 
                          AND StartTime = @Start", conn);
                            checkCmd.Parameters.AddWithValue("@InstId", instId);
                            checkCmd.Parameters.AddWithValue("@CarId", carId);
                            checkCmd.Parameters.AddWithValue("@Date", date);
                            checkCmd.Parameters.AddWithValue("@Start", timeSlots[i]);

                            if ((int)checkCmd.ExecuteScalar() == 0)
                            {
                                var insertCmd = new SqlCommand(@"
                            INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable, CreatedDate)
                            VALUES (@InstId, @CarId, @Date, @Start, @End, 1, GETDATE())", conn);
                                insertCmd.Parameters.AddWithValue("@InstId", instId);
                                insertCmd.Parameters.AddWithValue("@CarId", carId);
                                insertCmd.Parameters.AddWithValue("@Date", date);
                                insertCmd.Parameters.AddWithValue("@Start", timeSlots[i]);
                                insertCmd.Parameters.AddWithValue("@End", timeSlots[i + 1]);
                                insertCmd.ExecuteNonQuery();
                                totalInserted++;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Создано слотов: {totalInserted}");
            }
        }

        /// <summary>
        /// Загрузка ВСЕХ слотов для инструктора и автомобиля (включая занятые)
        /// </summary>
        public LessonSlotCollection LoadAllSlots(int instructorId, int carId, DateTime startDate, DateTime endDate)
        {
            var collection = new LessonSlotCollection { Slots = new List<LessonSlot>() };

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
    SELECT ls.*, 
           e.LastName + ' ' + e.FirstName as InstructorName,
           c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
    FROM LessonSlots ls
    LEFT JOIN Employees e ON ls.InstructorId = e.Id
    LEFT JOIN Cars c ON ls.CarId = c.Id
    WHERE ls.InstructorId = @InstructorId 
      AND ls.CarId = @CarId
      AND ls.LessonDate BETWEEN @StartDate AND @EndDate
    ORDER BY ls.LessonDate, ls.StartTime", conn);

                cmd.Parameters.AddWithValue("@InstructorId", instructorId);
                cmd.Parameters.AddWithValue("@CarId", carId);
                cmd.Parameters.AddWithValue("@StartDate", startDate);
                cmd.Parameters.AddWithValue("@EndDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        collection.Slots.Add(new LessonSlot
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            InstructorId = reader.GetInt32(reader.GetOrdinal("InstructorId")),
                            CarId = reader.GetInt32(reader.GetOrdinal("CarId")),
                            LessonDate = reader.GetDateTime(reader.GetOrdinal("LessonDate")),
                            StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                            EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                            IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                            CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                            InstructorName = reader["InstructorName"]?.ToString() ?? "",
                            CarInfo = reader["CarInfo"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return collection;
        }

        public void EnsureSlotsExist(DateTime date, int instructorId, int carId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем первый и последний день ВЫБРАННОГО месяца
                var firstDay = new DateTime(date.Year, date.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                // Добавляем также следующий месяц для непрерывности
                var nextMonthFirstDay = firstDay.AddMonths(1);
                var nextMonthLastDay = nextMonthFirstDay.AddMonths(1).AddDays(-1);

                // Проверяем наличие слотов для КОНКРЕТНОГО инструктора и машины на текущий месяц
                var checkCmd = new SqlCommand(@"
SELECT COUNT(*) FROM LessonSlots 
WHERE InstructorId = @InstructorId 
  AND CarId = @CarId
  AND LessonDate BETWEEN @StartDate AND @EndDate", conn);
                checkCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                checkCmd.Parameters.AddWithValue("@CarId", carId);
                checkCmd.Parameters.AddWithValue("@StartDate", firstDay);
                checkCmd.Parameters.AddWithValue("@EndDate", lastDay);

                int count = (int)checkCmd.ExecuteScalar();

                // Если слотов нет для этого инструктора/машины - создаем
                if (count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Нет слотов для инструктора {instructorId}, машины {carId} на {firstDay:MMMM yyyy}, создаю...");
                    CreateSlotsForInstructorAndCar(instructorId, carId, firstDay, lastDay);
                }

                // Также проверяем и создаем слоты на следующий месяц для плавного перехода
                var checkNextCmd = new SqlCommand(@"
SELECT COUNT(*) FROM LessonSlots 
WHERE InstructorId = @InstructorId 
  AND CarId = @CarId
  AND LessonDate BETWEEN @StartDate AND @EndDate", conn);
                checkNextCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                checkNextCmd.Parameters.AddWithValue("@CarId", carId);
                checkNextCmd.Parameters.AddWithValue("@StartDate", nextMonthFirstDay);
                checkNextCmd.Parameters.AddWithValue("@EndDate", nextMonthLastDay);

                int nextCount = (int)checkNextCmd.ExecuteScalar();

                if (nextCount == 0)
                {
                    CreateSlotsForInstructorAndCar(instructorId, carId, nextMonthFirstDay, nextMonthLastDay);
                }
            }
        }

        /// <summary>
        /// Создает слоты для конкретного инструктора и машины на указанный период
        /// </summary>
        public void CreateSlotsForInstructorAndCar(int instructorId, int carId, DateTime startDate, DateTime endDate)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var timeSlots = new[]
                {
            new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0),
            new TimeSpan(13, 0, 0), new TimeSpan(15, 0, 0),
            new TimeSpan(15, 0, 0), new TimeSpan(17, 0, 0),
            new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0)
        };

                int totalInserted = 0;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    for (int i = 0; i < timeSlots.Length; i += 2)
                    {
                        // Проверяем существование слота
                        var checkCmd = new SqlCommand(@"
            SELECT COUNT(*) FROM LessonSlots 
            WHERE InstructorId = @InstructorId 
              AND CarId = @CarId 
              AND LessonDate = @Date 
              AND StartTime = @Start", conn);
                        checkCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        checkCmd.Parameters.AddWithValue("@CarId", carId);
                        checkCmd.Parameters.AddWithValue("@Date", date);
                        checkCmd.Parameters.AddWithValue("@Start", timeSlots[i]);

                        if ((int)checkCmd.ExecuteScalar() == 0)
                        {
                            var insertCmd = new SqlCommand(@"
                INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable, CreatedDate)
                VALUES (@InstructorId, @CarId, @Date, @Start, @End, 1, GETDATE())", conn);
                            insertCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                            insertCmd.Parameters.AddWithValue("@CarId", carId);
                            insertCmd.Parameters.AddWithValue("@Date", date);
                            insertCmd.Parameters.AddWithValue("@Start", timeSlots[i]);
                            insertCmd.Parameters.AddWithValue("@End", timeSlots[i + 1]);
                            insertCmd.ExecuteNonQuery();
                            totalInserted++;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Создано слотов для инструктора {instructorId}: {totalInserted} за период {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
            }
        }

        /// <summary>
        /// Создает слоты на указанный период для всех инструкторов
        /// </summary>
        public void CreateSlotsForPeriod(DateTime startDate, DateTime endDate)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем всех инструкторов и их машины
                var cmd = new SqlCommand(@"
            SELECT e.Id, c.Id 
            FROM Employees e
            INNER JOIN Cars c ON e.Id = c.InstructorId
            WHERE c.IsActive = 1", conn);

                var instructors = new List<(int InstId, int CarId)>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        instructors.Add((reader.GetInt32(0), reader.GetInt32(1)));
                    }
                }

                if (instructors.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Нет инструкторов с привязанными машинами");
                    return;
                }

                var timeSlots = new[]
                {
            new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0),
            new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0),
            new TimeSpan(13, 0, 0), new TimeSpan(15, 0, 0),
            new TimeSpan(15, 0, 0), new TimeSpan(17, 0, 0),
            new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0)
        };

                int totalInserted = 0;

                foreach (var (instId, carId) in instructors)
                {
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        // Воскресенье - выходной
                        if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                        for (int i = 0; i < timeSlots.Length; i += 2)
                        {
                            // Проверяем существование слота
                            var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM LessonSlots 
                        WHERE InstructorId = @InstId 
                          AND CarId = @CarId 
                          AND LessonDate = @Date 
                          AND StartTime = @Start", conn);
                            checkCmd.Parameters.AddWithValue("@InstId", instId);
                            checkCmd.Parameters.AddWithValue("@CarId", carId);
                            checkCmd.Parameters.AddWithValue("@Date", date);
                            checkCmd.Parameters.AddWithValue("@Start", timeSlots[i]);

                            if ((int)checkCmd.ExecuteScalar() == 0)
                            {
                                var insertCmd = new SqlCommand(@"
                            INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable, CreatedDate)
                            VALUES (@InstId, @CarId, @Date, @Start, @End, 1, GETDATE())", conn);
                                insertCmd.Parameters.AddWithValue("@InstId", instId);
                                insertCmd.Parameters.AddWithValue("@CarId", carId);
                                insertCmd.Parameters.AddWithValue("@Date", date);
                                insertCmd.Parameters.AddWithValue("@Start", timeSlots[i]);
                                insertCmd.Parameters.AddWithValue("@End", timeSlots[i + 1]);
                                insertCmd.ExecuteNonQuery();
                                totalInserted++;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Создано слотов: {totalInserted} за период {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
            }
        }

        /// <summary>
        /// Отмена брони без штрафа (слот освобождается)
        /// </summary>
        public void CancelLessonOnly(int lessonId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем SlotId
                        var getSlotCmd = new SqlCommand("SELECT SlotId FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        getSlotCmd.Parameters.AddWithValue("@LessonId", lessonId);
                        var slotId = getSlotCmd.ExecuteScalar() as int?;

                        // Удаляем урок
                        var deleteCmd = new SqlCommand("DELETE FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        deleteCmd.Parameters.AddWithValue("@LessonId", lessonId);
                        deleteCmd.ExecuteNonQuery();

                        // Освобождаем слот
                        if (slotId.HasValue)
                        {
                            var slotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 1 WHERE Id = @SlotId", conn, transaction);
                            slotCmd.Parameters.AddWithValue("@SlotId", slotId.Value);
                            slotCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Отметить урок как проведенный
        /// </summary>
        public void MarkLessonAsCompleted(int lessonId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем информацию об уроке
                        var getInfoCmd = new SqlCommand(@"
                    SELECT StudentId, SlotId, InstructorId, CarId, LessonDate, StartTime 
                    FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        getInfoCmd.Parameters.AddWithValue("@LessonId", lessonId);

                        int studentId = 0;
                        int? slotId = null;
                        int instructorId = 0;
                        int carId = 0;
                        DateTime lessonDate = DateTime.Today;
                        TimeSpan startTime = TimeSpan.Zero;

                        using (var reader = getInfoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentId = reader.GetInt32(0);
                                slotId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
                                instructorId = reader.GetInt32(2);
                                carId = reader.GetInt32(3);
                                lessonDate = reader.GetDateTime(4);
                                startTime = TimeSpan.Parse(reader[5].ToString());
                            }
                            else
                            {
                                throw new Exception("Урок не найден");
                            }
                        }

                        // Обновляем статус урока
                        var updateCmd = new SqlCommand(@"
                    UPDATE DrivingLessons 
                    SET Status = 'Completed' 
                    WHERE Id = @LessonId", conn, transaction);
                        updateCmd.Parameters.AddWithValue("@LessonId", lessonId);
                        updateCmd.ExecuteNonQuery();

                        // Обновляем счетчик проведенных уроков у студента
                        var studentCmd = new SqlCommand(@"
                    UPDATE Students 
                    SET CompletedLessons = ISNULL(CompletedLessons, 0) + 1
                    WHERE Id = @StudentId", conn, transaction);
                        studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                        studentCmd.ExecuteNonQuery();

                        // Если слот был занят, освобождаем его
                        if (slotId.HasValue)
                        {
                            var slotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 1 WHERE Id = @SlotId", conn, transaction);
                            slotCmd.Parameters.AddWithValue("@SlotId", slotId.Value);
                            slotCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Отметить урок как прогул
        /// </summary>
        public void MarkLessonAsNoShow(int lessonId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем информацию об уроке
                        var getInfoCmd = new SqlCommand(@"
                    SELECT StudentId, SlotId, InstructorId, CarId, LessonDate, StartTime 
                    FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        getInfoCmd.Parameters.AddWithValue("@LessonId", lessonId);

                        int studentId = 0;
                        int? slotId = null;

                        using (var reader = getInfoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentId = reader.GetInt32(0);
                                slotId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
                            }
                            else
                            {
                                throw new Exception("Урок не найден");
                            }
                        }

                        // Обновляем статус урока
                        var updateCmd = new SqlCommand(@"
                    UPDATE DrivingLessons 
                    SET Status = 'NoShow' 
                    WHERE Id = @LessonId", conn, transaction);
                        updateCmd.Parameters.AddWithValue("@LessonId", lessonId);
                        updateCmd.ExecuteNonQuery();

                        // Обновляем счетчик пропусков у студента
                        var studentCmd = new SqlCommand(@"
                    UPDATE Students 
                    SET MissedLessons = ISNULL(MissedLessons, 0) + 1
                    WHERE Id = @StudentId", conn, transaction);
                        studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                        studentCmd.ExecuteNonQuery();

                        // Если слот был занят, освобождаем его
                        if (slotId.HasValue)
                        {
                            var slotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 1 WHERE Id = @SlotId", conn, transaction);
                            slotCmd.Parameters.AddWithValue("@SlotId", slotId.Value);
                            slotCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Ручное бронирование на любую дату и время
        /// </summary>
        public void ManualBookLesson(int studentId, int instructorId, int carId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Проверяем или создаем слот
                        var checkSlotCmd = new SqlCommand(@"
                    SELECT Id, IsAvailable FROM LessonSlots 
                    WHERE InstructorId = @InstructorId AND CarId = @CarId 
                    AND LessonDate = @Date AND StartTime = @StartTime", conn, transaction);
                        checkSlotCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        checkSlotCmd.Parameters.AddWithValue("@CarId", carId);
                        checkSlotCmd.Parameters.AddWithValue("@Date", date);
                        checkSlotCmd.Parameters.AddWithValue("@StartTime", startTime);

                        int slotId = 0;
                        bool isAvailable = false;

                        using (var reader = checkSlotCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                slotId = reader.GetInt32(0);
                                isAvailable = reader.GetBoolean(1);
                            }
                        }

                        // Если слота нет - создаем
                        if (slotId == 0)
                        {
                            var insertSlotCmd = new SqlCommand(@"
                        INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable, CreatedDate)
                        VALUES (@InstructorId, @CarId, @Date, @StartTime, @EndTime, 1, GETDATE());
                        SELECT SCOPE_IDENTITY();", conn, transaction);
                            insertSlotCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                            insertSlotCmd.Parameters.AddWithValue("@CarId", carId);
                            insertSlotCmd.Parameters.AddWithValue("@Date", date);
                            insertSlotCmd.Parameters.AddWithValue("@StartTime", startTime);
                            insertSlotCmd.Parameters.AddWithValue("@EndTime", endTime);

                            slotId = Convert.ToInt32(insertSlotCmd.ExecuteScalar());
                            isAvailable = true;
                        }

                        // Проверяем, свободен ли слот
                        if (!isAvailable)
                        {
                            throw new Exception("Этот слот уже занят другим студентом!");
                        }

                        // Проверяем, нет ли уже урока у этого студента на это время
                        var checkLessonCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM DrivingLessons 
                    WHERE StudentId = @StudentId 
                    AND LessonDate = @Date 
                    AND StartTime = @StartTime", conn, transaction);
                        checkLessonCmd.Parameters.AddWithValue("@StudentId", studentId);
                        checkLessonCmd.Parameters.AddWithValue("@Date", date);
                        checkLessonCmd.Parameters.AddWithValue("@StartTime", startTime);

                        int existingCount = (int)checkLessonCmd.ExecuteScalar();
                        if (existingCount > 0)
                        {
                            throw new Exception("У студента уже есть урок на это время!");
                        }

                        // Бронируем урок
                        var insertLessonCmd = new SqlCommand(@"
                    INSERT INTO DrivingLessons (StudentId, InstructorId, CarId, SlotId, LessonDate, StartTime, EndTime, Status, CreatedAt)
                    VALUES (@StudentId, @InstructorId, @CarId, @SlotId, @Date, @StartTime, @EndTime, 'Booked', GETDATE())", conn, transaction);
                        insertLessonCmd.Parameters.AddWithValue("@StudentId", studentId);
                        insertLessonCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        insertLessonCmd.Parameters.AddWithValue("@CarId", carId);
                        insertLessonCmd.Parameters.AddWithValue("@SlotId", slotId);
                        insertLessonCmd.Parameters.AddWithValue("@Date", date);
                        insertLessonCmd.Parameters.AddWithValue("@StartTime", startTime);
                        insertLessonCmd.Parameters.AddWithValue("@EndTime", endTime);
                        insertLessonCmd.ExecuteNonQuery();

                        // Закрываем слот
                        var updateSlotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 0 WHERE Id = @SlotId", conn, transaction);
                        updateSlotCmd.Parameters.AddWithValue("@SlotId", slotId);
                        updateSlotCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Автоматическая отметка прошедших уроков как проведенных
        /// </summary>
        public void AutoCompletePastLessons()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Находим все уроки со статусом Booked, которые уже прошли
                var cmd = new SqlCommand(@"
            UPDATE DrivingLessons 
            SET Status = 'Completed' 
            WHERE Status = 'Booked' 
            AND LessonDate < CAST(GETDATE() AS DATE)", conn);

                int updated = cmd.ExecuteNonQuery();
                if (updated > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Автоматически отмечено {updated} прошедших уроков");
                }
            }
        }

        /// <summary>
        /// Получение количества уроков для категории студента (часы / 2)
        /// </summary>
        public int GetLessonsCountByCategory(int studentId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT ISNULL(vc.LessonsCount, 28) 
            FROM Students s
            LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
            WHERE s.Id = @StudentId", conn);
                cmd.Parameters.AddWithValue("@StudentId", studentId);

                var result = cmd.ExecuteScalar();
                int hours = result != null ? Convert.ToInt32(result) : 28;

                // Делим часы на 2, так как один урок = 2 часа
                return hours / 2;
            }
        }

        /// <summary>
        /// Проверка и бронирование урока (с учетом допов)
        /// </summary>
        public bool TryBookLesson(int studentId, int slotId, out string message)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                // Получаем категорию студента и количество уроков
                var getCategoryCmd = new SqlCommand(@"
            SELECT c.LessonsCount, c.Name 
            FROM Students s
            JOIN Categories c ON s.CategoryId = c.Id
            WHERE s.Id = @StudentId", conn);
                getCategoryCmd.Parameters.AddWithValue("@StudentId", studentId);

                int totalLessons = 28;
                string categoryName = "";

                using (var reader = getCategoryCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        totalLessons = reader.GetInt32(0);
                        categoryName = reader.GetString(1);
                    }
                }

                // Считаем проведенные и пропущенные уроки
                var lessons = LoadStudentLessons(studentId);
                var completed = lessons.Count(l => l.Status == "Completed");
                var noShow = lessons.Count(l => l.Status == "NoShow");
                var booked = lessons.Count(l => l.Status == "Booked");

                int used = completed + noShow;
                int remaining = totalLessons - used;

                // Если есть свободные уроки
                if (remaining > 0)
                {
                    message = "";
                    return true;
                }

                // Если уроки закончились - спрашиваем про доп
                message = $"У студента закончились уроки по категории '{categoryName}' (всего {totalLessons}).\n\nОтметить как дополнительный урок?";
                return false;
            }
        }

        /// <summary>
        /// Бронирование дополнительного урока
        /// </summary>
        public void BookExtraLesson(int studentId, int slotId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем информацию о слоте
                        var slotCmd = new SqlCommand(@"
                    SELECT InstructorId, CarId, LessonDate, StartTime, EndTime 
                    FROM LessonSlots 
                    WHERE Id = @SlotId AND IsAvailable = 1", conn, transaction);
                        slotCmd.Parameters.AddWithValue("@SlotId", slotId);

                        int instructorId = 0, carId = 0;
                        DateTime lessonDate = DateTime.Today;
                        TimeSpan startTime = TimeSpan.Zero, endTime = TimeSpan.Zero;

                        using (var reader = slotCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                instructorId = reader.GetInt32(0);
                                carId = reader.GetInt32(1);
                                lessonDate = reader.GetDateTime(2);
                                startTime = TimeSpan.Parse(reader[3].ToString());
                                endTime = TimeSpan.Parse(reader[4].ToString());
                            }
                            else
                            {
                                throw new Exception("Слот не найден или уже занят");
                            }
                        }

                        // Создаем бронирование как дополнительный урок
                        var insertCmd = new SqlCommand(@"
                    INSERT INTO DrivingLessons (StudentId, InstructorId, CarId, SlotId, LessonDate, StartTime, EndTime, Status, CreatedAt, IsExtra)
                    VALUES (@StudentId, @InstructorId, @CarId, @SlotId, @LessonDate, @StartTime, @EndTime, 'Booked', GETDATE(), 1)", conn, transaction);

                        insertCmd.Parameters.AddWithValue("@StudentId", studentId);
                        insertCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        insertCmd.Parameters.AddWithValue("@CarId", carId);
                        insertCmd.Parameters.AddWithValue("@SlotId", slotId);
                        insertCmd.Parameters.AddWithValue("@LessonDate", lessonDate);
                        insertCmd.Parameters.AddWithValue("@StartTime", startTime);
                        insertCmd.Parameters.AddWithValue("@EndTime", endTime);
                        insertCmd.ExecuteNonQuery();

                        // Закрываем слот
                        var updateSlotCmd = new SqlCommand("UPDATE LessonSlots SET IsAvailable = 0 WHERE Id = @SlotId", conn, transaction);
                        updateSlotCmd.Parameters.AddWithValue("@SlotId", slotId);
                        updateSlotCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Сброс статуса урока (исправление ошибки)
        /// </summary>
        /// <summary>
        /// Сброс статуса урока (удаление урока, слот становится свободным)
        /// </summary>
        public void ResetLessonStatus(int lessonId)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Получаем информацию об уроке
                        var getInfoCmd = new SqlCommand(@"
                    SELECT StudentId, SlotId, Status
                    FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        getInfoCmd.Parameters.AddWithValue("@LessonId", lessonId);

                        int studentId = 0;
                        int? slotId = null;
                        string oldStatus = "";

                        using (var reader = getInfoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                studentId = reader.GetInt32(0);
                                slotId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1);
                                oldStatus = reader[2]?.ToString() ?? "";
                            }
                            else
                            {
                                throw new Exception("Урок не найден");
                            }
                        }

                        // Если урок был проведен или пропущен - откатываем счетчики
                        if (oldStatus == "Completed")
                        {
                            var studentCmd = new SqlCommand(@"
                        UPDATE Students 
                        SET CompletedLessons = ISNULL(CompletedLessons, 0) - 1
                        WHERE Id = @StudentId", conn, transaction);
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                            studentCmd.ExecuteNonQuery();
                        }
                        else if (oldStatus == "NoShow")
                        {
                            var studentCmd = new SqlCommand(@"
                        UPDATE Students 
                        SET MissedLessons = ISNULL(MissedLessons, 0) - 1
                        WHERE Id = @StudentId", conn, transaction);
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                            studentCmd.ExecuteNonQuery();
                        }

                        // Удаляем урок
                        var deleteCmd = new SqlCommand(@"
                    DELETE FROM DrivingLessons WHERE Id = @LessonId", conn, transaction);
                        deleteCmd.Parameters.AddWithValue("@LessonId", lessonId);
                        deleteCmd.ExecuteNonQuery();

                        // Освобождаем слот (делаем доступным)
                        if (slotId.HasValue)
                        {
                            var slotCmd = new SqlCommand(@"
                        UPDATE LessonSlots SET IsAvailable = 1 WHERE Id = @SlotId", conn, transaction);
                            slotCmd.Parameters.AddWithValue("@SlotId", slotId.Value);
                            slotCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Получение списка инструкторов (полный для поиска)
        /// </summary>
        public List<Employee> GetInstructorsList()
        {
            var instructors = new List<Employee>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT e.Id, e.LastName, e.FirstName, e.MiddleName, e.Position, 
                   ISNULL(c.Id, 0) as CarId
            FROM Employees e
            LEFT JOIN Cars c ON e.Id = c.InstructorId
            WHERE e.Position LIKE '%инструктор%' OR e.Position LIKE '%вожден%'
            GROUP BY e.Id, e.LastName, e.FirstName, e.MiddleName, e.Position, c.Id
            ORDER BY e.LastName, e.FirstName", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var employee = new Employee
                        {
                            Id = reader.GetInt32(0),
                            LastName = reader[1]?.ToString() ?? "",
                            FirstName = reader[2]?.ToString() ?? "",
                            MiddleName = reader[3]?.ToString(),
                            Position = reader[4]?.ToString() ?? "",
                            CarId = reader.GetInt32(5)
                        };
                        instructors.Add(employee);
                    }
                }
            }

            return instructors;
        }

        /// <summary>
        /// Получение списка инструкторов (упрощенный для выбора)
        /// </summary>
        public List<(int Id, string Name, int CarId)> GetInstructors()
        {
            var instructors = new List<(int Id, string Name, int CarId)>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT e.Id, 
                   e.LastName + ' ' + e.FirstName + ' - ' + ISNULL(e.Position, 'Инструктор') as Name,
                   ISNULL(c.Id, 0) as CarId
            FROM Employees e
            LEFT JOIN Cars c ON e.Id = c.InstructorId
            WHERE e.Position LIKE '%инструктор%' OR e.Position LIKE '%вожден%'
            GROUP BY e.Id, e.LastName, e.FirstName, e.Position, c.Id
            ORDER BY e.LastName, e.FirstName", conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        instructors.Add((
                            Id: reader.GetInt32(0),
                            Name: reader[1]?.ToString() ?? "Инструктор",
                            CarId: reader.GetInt32(2)
                        ));
                    }
                }
            }

            return instructors;
        }

        /// <summary>
        /// Получение временных слотов инструктора
        /// </summary>
        public List<string> GetInstructorTimeSlots(int instructorId)
        {
            var timeSlots = new List<string>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT CONVERT(varchar(5), StartTime, 108) + ' - ' + CONVERT(varchar(5), EndTime, 108) as TimeSlot
            FROM LessonSlots 
            WHERE InstructorId = @InstructorId
            GROUP BY StartTime, EndTime
            ORDER BY StartTime", conn);
                cmd.Parameters.AddWithValue("@InstructorId", instructorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        timeSlots.Add(reader["TimeSlot"].ToString());
                    }
                }
            }

            return timeSlots;
        }

        /// <summary>
        /// Получение уроков инструктора
        /// </summary>
        public List<DrivingLesson> LoadInstructorLessons(int instructorId)
        {
            var lessons = new List<DrivingLesson>();

            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            SELECT dl.*, 
                   s.LastName + ' ' + s.FirstName as StudentName,
                   e.LastName + ' ' + e.FirstName as InstructorName,
                   c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
            FROM DrivingLessons dl
            JOIN Students s ON dl.StudentId = s.Id
            JOIN Employees e ON dl.InstructorId = e.Id
            JOIN Cars c ON dl.CarId = c.Id
            WHERE dl.InstructorId = @InstructorId
            ORDER BY dl.LessonDate DESC, dl.StartTime DESC", conn);
                cmd.Parameters.AddWithValue("@InstructorId", instructorId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lessons.Add(new DrivingLesson
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                            InstructorId = reader.GetInt32(reader.GetOrdinal("InstructorId")),
                            CarId = reader.GetInt32(reader.GetOrdinal("CarId")),
                            SlotId = reader["SlotId"] as int?,
                            LessonDate = reader.GetDateTime(reader.GetOrdinal("LessonDate")),
                            StartTime = TimeSpan.Parse(reader["StartTime"].ToString()),
                            EndTime = TimeSpan.Parse(reader["EndTime"].ToString()),
                            Status = reader["Status"]?.ToString() ?? "Booked",
                            CanceledAt = reader["CanceledAt"] as DateTime?,
                            IsCancelledByStudent = reader["IsCancelledByStudent"] as bool? ?? false,
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsExtra = reader["IsExtra"] != DBNull.Value && Convert.ToBoolean(reader["IsExtra"]),
                            StudentName = reader["StudentName"]?.ToString() ?? "",
                            InstructorName = reader["InstructorName"]?.ToString() ?? "",
                            CarInfo = reader["CarInfo"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return lessons;
        }

        /// <summary>
        /// Добавление временного слота
        /// </summary>
        public void AddTimeSlot(int instructorId, int carId, string startTime, string endTime)
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                var startDate = DateTime.Today.AddMonths(-6);
                var endDate = DateTime.Today.AddMonths(6);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM LessonSlots 
                WHERE InstructorId = @InstructorId AND CarId = @CarId 
                AND LessonDate = @Date AND StartTime = @StartTime", conn);
                    checkCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                    checkCmd.Parameters.AddWithValue("@CarId", carId);
                    checkCmd.Parameters.AddWithValue("@Date", date);
                    checkCmd.Parameters.AddWithValue("@StartTime", startTime);

                    if ((int)checkCmd.ExecuteScalar() == 0)
                    {
                        var insertCmd = new SqlCommand(@"
                    INSERT INTO LessonSlots (InstructorId, CarId, LessonDate, StartTime, EndTime, IsAvailable, CreatedDate)
                    VALUES (@InstructorId, @CarId, @Date, @StartTime, @EndTime, 1, GETDATE())", conn);
                        insertCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        insertCmd.Parameters.AddWithValue("@CarId", carId);
                        insertCmd.Parameters.AddWithValue("@Date", date);
                        insertCmd.Parameters.AddWithValue("@StartTime", startTime);
                        insertCmd.Parameters.AddWithValue("@EndTime", endTime);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Удаление временного слота
        /// </summary>
        public void DeleteTimeSlot(int instructorId, int carId, string startTime, string endTime)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var deleteLessonsCmd = new SqlCommand(@"
                    DELETE FROM DrivingLessons 
                    WHERE SlotId IN (SELECT Id FROM LessonSlots 
                                     WHERE InstructorId = @InstructorId AND CarId = @CarId 
                                     AND StartTime = @StartTime AND EndTime = @EndTime)", conn, transaction);
                        deleteLessonsCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        deleteLessonsCmd.Parameters.AddWithValue("@CarId", carId);
                        deleteLessonsCmd.Parameters.AddWithValue("@StartTime", startTime);
                        deleteLessonsCmd.Parameters.AddWithValue("@EndTime", endTime);
                        deleteLessonsCmd.ExecuteNonQuery();

                        var deleteSlotsCmd = new SqlCommand(@"
                    DELETE FROM LessonSlots 
                    WHERE InstructorId = @InstructorId AND CarId = @CarId 
                    AND StartTime = @StartTime AND EndTime = @EndTime", conn, transaction);
                        deleteSlotsCmd.Parameters.AddWithValue("@InstructorId", instructorId);
                        deleteSlotsCmd.Parameters.AddWithValue("@CarId", carId);
                        deleteSlotsCmd.Parameters.AddWithValue("@StartTime", startTime);
                        deleteSlotsCmd.Parameters.AddWithValue("@EndTime", endTime);
                        deleteSlotsCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Проверка, были ли внесены платежи у студента
        /// </summary>
        public bool HasPayments(int studentId)
        {
            var payments = LoadStudentPayments(studentId);
            return payments.Any();
        }

        /// <summary>
        /// Проверка, был ли внесен первый платеж
        /// </summary>
        public bool HasFirstPayment(int studentId)
        {
            var payments = LoadStudentPayments(studentId);
            return payments.Any();
        }

        /// <summary>
        /// Проверка необходимости второго платежа после 10 уроков (включая прогулы)
        /// </summary>
        public bool CheckSecondPaymentRequired(int studentId, out decimal remainingAmount)
        {
            remainingAmount = 0;

            var student = LoadStudent(studentId);
            if (student == null) return false;

            var payments = LoadStudentPayments(studentId);
            var totalPaid = payments.Sum(p => p.Amount);

            var lessons = LoadStudentLessons(studentId);
            // Считаем проведенные + прогулы (списанные уроки)
            var usedLessons = lessons.Count(l => l.Status == "Completed" || l.Status == "NoShow");

            // Проверяем, прошло ли 10 уроков (включая прогулы)
            if (payments.Any() && usedLessons >= 10)
            {
                // Проверяем, не был ли уже внесен второй платеж
                var hasSecondPayment = payments.Any(p => p.PaymentType == "SecondPayment" ||
                                                        p.PaymentType == "Основной платеж" ||
                                                        p.PaymentType == "Остаток" ||
                                                        (p.PaymentType == "Платеж" && payments.Count > 1));

                if (!hasSecondPayment)
                {
                    var finalAmount = student.FinalAmount;
                    remainingAmount = finalAmount - totalPaid;
                    return remainingAmount > 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверка, был ли внесен второй платеж
        /// </summary>
        public bool HasSecondPayment(int studentId)
        {
            var payments = LoadStudentPayments(studentId);
            // Если есть больше одного платежа или есть платеж с типом "Основной платеж"
            return payments.Count > 1 ||
                   payments.Any(p => p.PaymentType == "SecondPayment" ||
                                    p.PaymentType == "Основной платеж" ||
                                    p.PaymentType == "Остаток");
        }

        /// <summary>
        /// Получение количества проведенных уроков
        /// </summary>
        public int GetCompletedLessonsCount(int studentId)
        {
            var lessons = LoadStudentLessons(studentId);
            return lessons.Count(l => l.Status == "Completed");
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }
    } // <-- Закрывающая скобка класса SqlDataService (СТРОКА 2559)

    public class DebtInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Phone { get; set; }
        public string GroupName { get; set; }
        public decimal FullAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Debt { get; set; }

        // ИСПРАВЛЕНО: Правильные статусы
        public string DebtStatus
        {
            get
            {
                if (Debt > 0) return "Долг";
                if (Debt < 0) return "Переплата";
                return "Оплачено";
            }
        }

        public string DebtColor
        {
            get
            {
                if (Debt > 0) return "Red";
                if (Debt < 0) return "Orange";
                return "Green";
            }
        }

        public string DebtFormatted => $"{(Debt > 0 ? Debt : 0):N2} руб.";
        public string OverpaymentFormatted => $"{(Debt < 0 ? -Debt : 0):N2} руб.";
        public string PaidFormatted => $"{Paid:N2} руб.";
        public string FinalAmountFormatted => $"{FinalAmount:N2} руб.";
    }

    public class DatabaseStatistics
    {
        public int StudentsCount { get; set; }
        public int EmployeesCount { get; set; }
        public int CarsCount { get; set; }
        public int GroupsCount { get; set; }
        public int PaymentsCount { get; set; }
        public decimal TotalPayments { get; set; }
        public int StudentsInGroups { get; set; }
        public decimal TotalDebt { get; set; }

        public string TotalPaymentsFormatted => $"{TotalPayments:N2} руб.";
        public string TotalDebtFormatted => $"{TotalDebt:N2} руб.";
    }

    /// <summary>
    /// Класс для информации о долгах студентов
    /// </summary>
    public class StudentDebtInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Phone { get; set; }
        public string GroupName { get; set; }
        public string CategoryCode { get; set; }
        public decimal TuitionAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }

        public int PaymentProgress
        {
            get
            {
                if (FinalAmount == 0) return 0;
                var progress = (int)((PaidAmount / FinalAmount) * 100);
                return Math.Min(100, Math.Max(0, progress));
            }
        }

        public string DebtStatus => DebtAmount > 0 ? $"Долг: {DebtAmount:N2} руб." : "Оплачено";
        public string FinalAmountFormatted => $"{FinalAmount:N2} руб.";
        public string PaidAmountFormatted => $"{PaidAmount:N2} руб.";
        public string DebtAmountFormatted => $"{DebtAmount:N2} руб.";
    }
} // <-- Закрывающая скобка namespace