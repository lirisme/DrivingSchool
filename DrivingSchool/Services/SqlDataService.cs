using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DrivingSchool.Models;

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
        /// Загрузка всех студентов
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
                       c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
                FROM Students s
                LEFT JOIN VehicleCategories vc ON s.VehicleCategoryId = vc.Id
                LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                LEFT JOIN Employees e ON s.InstructorId = e.Id
                LEFT JOIN Cars c ON s.CarId = c.Id
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
                // НЕ выкидываем исключение, а возвращаем пустую коллекцию
                // throw new Exception($"Ошибка загрузки студентов: {ex.Message}");
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
                               c.Brand + ' ' + c.Model + ' (' + c.LicensePlate + ')' as CarInfo
                        FROM Students s
                        LEFT JOIN VehicleCategories vc ON s.VehicleCategoryId = vc.Id
                        LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                        LEFT JOIN Employees e ON s.InstructorId = e.Id
                        LEFT JOIN Cars c ON s.CarId = c.Id
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
                                ModifiedDate = GETDATE()
                            WHERE Id = @Id", conn);

                        AddStudentParameters(cmd, student);
                        cmd.Parameters.AddWithValue("@Id", student.Id);

                        cmd.ExecuteNonQuery();
                        return student.Id;
                    }
                    else
                    {
                        // Вставка нового студента
                        var cmd = new SqlCommand(@"
                            INSERT INTO Students 
                            (LastName, FirstName, MiddleName, BirthDate, BirthPlace, Phone, Email, 
                             Citizenship, Gender, GroupId, VehicleCategoryId, InstructorId, CarId, CreatedDate)
                            OUTPUT INSERTED.Id
                            VALUES 
                            (@LastName, @FirstName, @MiddleName, @BirthDate, @BirthPlace, @Phone, @Email,
                             @Citizenship, @Gender, @GroupId, @VehicleCategoryId, @InstructorId, @CarId, GETDATE())", conn);

                        AddStudentParameters(cmd, student);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения студента: {ex.Message}");
                throw new Exception($"Ошибка сохранения студента: {ex.Message}");
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
                    CarInfo = reader["CarInfo"]?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка маппинга студента: {ex.Message}");
                return new Student(); // Возвращаем пустого студента в случае ошибки
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

                    var cmd = new SqlCommand(@"
                        SELECT 
                            s.Id,
                            s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') AS StudentName,
                            s.Phone,
                            sg.Name AS GroupName,
                            st.FullAmount,
                            st.Discount,
                            st.FinalAmount,
                            ISNULL(SUM(p.Amount), 0) AS Paid,
                            st.FinalAmount - ISNULL(SUM(p.Amount), 0) AS Debt
                        FROM Students s
                        LEFT JOIN StudyGroups sg ON s.GroupId = sg.Id
                        LEFT JOIN StudentTuitions st ON s.Id = st.StudentId
                        LEFT JOIN Payments p ON s.Id = p.StudentId
                        WHERE st.FinalAmount IS NOT NULL
                        GROUP BY s.Id, s.LastName, s.FirstName, s.MiddleName, s.Phone, 
                                 sg.Name, st.FullAmount, st.Discount, st.FinalAmount
                        HAVING st.FinalAmount - ISNULL(SUM(p.Amount), 0) > @MinDebt
                        ORDER BY Debt DESC", conn);

                    cmd.Parameters.AddWithValue("@MinDebt", minDebt);

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

                    var cmd = new SqlCommand(@"
                        SELECT 
                            (SELECT COUNT(*) FROM Students) as StudentsCount,
                            (SELECT COUNT(*) FROM Employees) as EmployeesCount,
                            (SELECT COUNT(*) FROM Cars) as CarsCount,
                            (SELECT COUNT(*) FROM StudyGroups) as GroupsCount,
                            (SELECT COUNT(*) FROM Payments) as PaymentsCount,
                            (SELECT ISNULL(SUM(Amount), 0) FROM Payments) as TotalPayments,
                            (SELECT COUNT(*) FROM Students WHERE GroupId IS NOT NULL) as StudentsInGroups,
                            (SELECT ISNULL(SUM(FinalAmount - ISNULL((SELECT SUM(Amount) FROM Payments p WHERE p.StudentId = st.StudentId), 0)), 0) 
                             FROM StudentTuitions st) as TotalDebt", conn);

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
            /// Сохранение свидетельства
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
                    SET CertificateSeries = @CertificateSeries, CertificateNumber = @CertificateNumber,
                        IssueDate = @IssueDate, VehicleCategoryId = @VehicleCategoryId,
                        CategoryCode = @CategoryCode, ModifiedDate = GETDATE()
                    WHERE Id = @Id", conn);

                            cmd.Parameters.AddWithValue("@Id", certificate.Id);
                            cmd.Parameters.AddWithValue("@CertificateSeries", certificate.CertificateSeries ?? "");
                            cmd.Parameters.AddWithValue("@CertificateNumber", certificate.CertificateNumber ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", certificate.IssueDate);
                            cmd.Parameters.AddWithValue("@VehicleCategoryId", certificate.VehicleCategoryId);
                            cmd.Parameters.AddWithValue("@CategoryCode", (object)certificate.CategoryCode ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                            return certificate.Id;
                        }
                        else
                        {
                            var cmd = new SqlCommand(@"
                    INSERT INTO StudentCertificates 
                    (StudentId, CertificateSeries, CertificateNumber, IssueDate, VehicleCategoryId, CategoryCode, CreatedDate)
                    OUTPUT INSERTED.Id
                    VALUES (@StudentId, @CertificateSeries, @CertificateNumber, @IssueDate, @VehicleCategoryId, @CategoryCode, GETDATE())", conn);

                            cmd.Parameters.AddWithValue("@StudentId", certificate.StudentId);
                            cmd.Parameters.AddWithValue("@CertificateSeries", certificate.CertificateSeries ?? "");
                            cmd.Parameters.AddWithValue("@CertificateNumber", certificate.CertificateNumber ?? "");
                            cmd.Parameters.AddWithValue("@IssueDate", certificate.IssueDate);
                            cmd.Parameters.AddWithValue("@VehicleCategoryId", certificate.VehicleCategoryId);
                            cmd.Parameters.AddWithValue("@CategoryCode", (object)certificate.CategoryCode ?? DBNull.Value);

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
                        var cmd = new SqlCommand(@"
                SELECT t.*, s.LastName + ' ' + s.FirstName + ISNULL(' ' + s.MiddleName, '') as StudentName,
                       tar.Name as TariffName
                FROM StudentTuitions t
                JOIN Students s ON t.StudentId = s.Id
                LEFT JOIN Tariffs tar ON t.TariffId = tar.Id
                ORDER BY s.LastName, s.FirstName", conn);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                collection.Tuitions.Add(new StudentTuition
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    TariffId = reader["TariffId"] as int?,
                                    FullAmount = reader.GetDecimal(reader.GetOrdinal("FullAmount")),
                                    Discount = reader.GetDecimal(reader.GetOrdinal("Discount")),
                                    CreatedDate = reader["CreatedDate"] as DateTime? ?? DateTime.Now,
                                    ModifiedDate = reader["ModifiedDate"] as DateTime?,
                                    StudentName = reader["StudentName"]?.ToString() ?? "",
                                    TariffName = reader["TariffName"]?.ToString() ?? ""
                                });
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

        public string DebtStatus => Debt <= 0 ? "Оплачено" : "Долг";
        public string DebtColor => Debt <= 0 ? "Green" : "Red";
        public string DebtFormatted => $"{Debt:N2} руб.";
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
} // <-- Закрывающая скобка namespace