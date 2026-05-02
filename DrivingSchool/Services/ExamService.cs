using DrivingSchool.Models;
using DrivingSchool.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DrivingSchool.Services
{
    public class ExamService
    {
        private readonly string _connectionString;

        public ExamService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Получить статус экзаменов ученика
        /// </summary>
        public async Task<StudentExamStatus> GetStudentExamStatusAsync(int studentId)
        {
            var status = new StudentExamStatus();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
            SELECT [Type], [Stage], [Result]
            FROM ExamRecords
            WHERE StudentId = @StudentId
            AND [Result] = 1";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var examType = (ExamType)reader.GetInt32(reader.GetOrdinal("Type"));
                            var examStage = (ExamStage)reader.GetInt32(reader.GetOrdinal("Stage"));

                            if (examType == ExamType.Internal && examStage == ExamStage.Theory)
                                status.InternalTheoryPassed = true;
                            else if (examType == ExamType.Internal && examStage == ExamStage.Practice)
                                status.InternalPracticePassed = true;
                            else if (examType == ExamType.GIBDD && examStage == ExamStage.Theory)
                                status.GIBDDTheoryPassed = true;
                            else if (examType == ExamType.GIBDD && examStage == ExamStage.Practice)
                                status.GIBDDPracticePassed = true;
                        }
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Получить все экзамены (без фильтрации по дате)
        /// </summary>
        public async Task<List<ExamSchedule>> GetAllSchedulesAsync(ExamType type, ExamStage stage)
        {
            var schedules = new List<ExamSchedule>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
            SELECT Id, [Type], [Stage], ExamDate, StartTime, EndTime, 
                   MaxStudents, CurrentStudents, ExaminerName, Location, 
                   IsAvailable, IsConducted
            FROM ExamSchedules
            WHERE [Type] = @Type AND [Stage] = @Stage
            ORDER BY ExamDate, StartTime";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Type", (int)type);
                    command.Parameters.AddWithValue("@Stage", (int)stage);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            schedules.Add(new ExamSchedule
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Type = (ExamType)reader.GetInt32(reader.GetOrdinal("Type")),
                                Stage = (ExamStage)reader.GetInt32(reader.GetOrdinal("Stage")),
                                ExamDate = reader.GetDateTime(reader.GetOrdinal("ExamDate")),
                                StartTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                                EndTime = reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                                MaxStudents = reader.GetInt32(reader.GetOrdinal("MaxStudents")),
                                CurrentStudents = reader.GetInt32(reader.GetOrdinal("CurrentStudents")),
                                IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                                IsConducted = reader.GetBoolean(reader.GetOrdinal("IsConducted")),
                                ExaminerName = reader.IsDBNull(reader.GetOrdinal("ExaminerName")) ? "Не назначен" : reader.GetString(reader.GetOrdinal("ExaminerName")),
                                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? "Автошкола" : reader.GetString(reader.GetOrdinal("Location"))
                            });
                        }
                    }
                }
            }

            return schedules;
        }

        /// <summary>
        /// Получить доступные слоты для экзаменов
        /// </summary>
        public async Task<List<ExamSchedule>> GetAvailableSchedulesAsync(ExamType type, ExamStage stage)
        {
            var schedules = new List<ExamSchedule>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                    SELECT Id, [Type], [Stage], ExamDate, StartTime, EndTime, 
                           MaxStudents, CurrentStudents, ExaminerName, Location, IsAvailable
                    FROM ExamSchedules
                    WHERE [Type] = @Type 
                        AND [Stage] = @Stage 
                        AND IsAvailable = 1 
                        AND ExamDate >= CAST(GETDATE() AS DATE)
                        AND CurrentStudents < MaxStudents
                    ORDER BY ExamDate, StartTime";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Type", (int)type);
                    command.Parameters.AddWithValue("@Stage", (int)stage);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var schedule = new ExamSchedule
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Type = (ExamType)reader.GetInt32(reader.GetOrdinal("Type")),
                                Stage = (ExamStage)reader.GetInt32(reader.GetOrdinal("Stage")),
                                ExamDate = reader.GetDateTime(reader.GetOrdinal("ExamDate")),
                                StartTime = reader.GetTimeSpan(reader.GetOrdinal("StartTime")),
                                EndTime = reader.GetTimeSpan(reader.GetOrdinal("EndTime")),
                                MaxStudents = reader.GetInt32(reader.GetOrdinal("MaxStudents")),
                                CurrentStudents = reader.GetInt32(reader.GetOrdinal("CurrentStudents")),
                                IsAvailable = reader.GetBoolean(reader.GetOrdinal("IsAvailable")),
                                ExaminerName = reader.IsDBNull(reader.GetOrdinal("ExaminerName"))
                                    ? "Не назначен"
                                    : reader.GetString(reader.GetOrdinal("ExaminerName")),
                                Location = reader.IsDBNull(reader.GetOrdinal("Location"))
                                    ? "Не указано"
                                    : reader.GetString(reader.GetOrdinal("Location"))
                            };

                            schedules.Add(schedule);
                        }
                    }
                }
            }

            return schedules;
        }

        /// <summary>
        /// Создать новый слот в расписании экзаменов
        /// </summary>
        public async Task<int> CreateExamScheduleAsync(ExamSchedule schedule)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ExamSchedules (
                        [Type], [Stage], ExamDate, StartTime, EndTime, 
                        MaxStudents, CurrentStudents, ExaminerName, Location, IsAvailable
                    ) VALUES (
                        @Type, @Stage, @ExamDate, @StartTime, @EndTime,
                        @MaxStudents, @CurrentStudents, @ExaminerName, @Location, @IsAvailable
                    );
                    SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Type", (int)schedule.Type);
                    command.Parameters.AddWithValue("@Stage", (int)schedule.Stage);
                    command.Parameters.AddWithValue("@ExamDate", schedule.ExamDate);
                    command.Parameters.AddWithValue("@StartTime", schedule.StartTime);
                    command.Parameters.AddWithValue("@EndTime", schedule.EndTime);
                    command.Parameters.AddWithValue("@MaxStudents", schedule.MaxStudents);
                    command.Parameters.AddWithValue("@CurrentStudents", schedule.CurrentStudents);
                    command.Parameters.AddWithValue("@ExaminerName", schedule.ExaminerName ?? "Не назначен");
                    command.Parameters.AddWithValue("@Location", schedule.Location ?? "");
                    command.Parameters.AddWithValue("@IsAvailable", schedule.IsAvailable);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<List<StudentExamSummary>> GetRegisteredStudentsForExamAsync(int scheduleId)
        {
            var students = new List<StudentExamSummary>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
            SELECT s.Id, s.LastName + ' ' + s.FirstName AS StudentName,
                   ISNULL(vc.Code, 'B') AS CategoryCode
            FROM ExamRegistrations er
            INNER JOIN Students s ON er.StudentId = s.Id
            LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
            WHERE er.ScheduleId = @ScheduleId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            students.Add(new StudentExamSummary
                            {
                                StudentId = reader.GetInt32(0),
                                StudentName = reader.GetString(1),
                                CategoryCode = reader.GetString(2)
                            });
                        }
                    }
                }
            }

            return students;
        }

        public async Task<int> SaveExamResultAsync(ExamRecord exam)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                INSERT INTO ExamRecords (
                    StudentId, ScheduleId, [Type], [Stage], ExamDate, [Result], 
                    Score, AttemptNumber, ExaminerName, Notes, CreatedDate
                ) VALUES (
                    @StudentId, @ScheduleId, @Type, @Stage, @ExamDate, @Result,
                    @Score, @AttemptNumber, @ExaminerName, @Notes, @CreatedDate
                );
                SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@StudentId", exam.StudentId);
                        command.Parameters.AddWithValue("@ScheduleId", exam.ScheduleId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Type", (int)exam.Type);
                        command.Parameters.AddWithValue("@Stage", (int)exam.Stage);
                        command.Parameters.AddWithValue("@ExamDate", exam.ExamDate);
                        command.Parameters.AddWithValue("@Result", (int)exam.Result);
                        command.Parameters.AddWithValue("@Score", exam.Score > 0 ? (object)exam.Score : DBNull.Value);
                        command.Parameters.AddWithValue("@AttemptNumber", exam.AttemptNumber);
                        command.Parameters.AddWithValue("@ExaminerName", exam.ExaminerName ?? "Экзаменатор");
                        command.Parameters.AddWithValue("@Notes", exam.Notes ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        var result = await command.ExecuteScalarAsync();
                        int id = Convert.ToInt32(result);
                        System.Diagnostics.Debug.WriteLine($"Сохранен результат: StudentId={exam.StudentId}, Stage={exam.Stage}, Result={exam.Result}, Id={id}");
                        return id;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка SaveExamResultAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Стек: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Записать ученика на экзамен
        /// </summary>
        public async Task<(bool Success, string Message)> RegisterForExamAsync(int studentId, int scheduleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var checkSql = "SELECT COUNT(*) FROM ExamRegistrations WHERE StudentId = @StudentId AND ScheduleId = @ScheduleId";
                using (var checkCmd = new SqlCommand(checkSql, connection))
                {
                    checkCmd.Parameters.AddWithValue("@StudentId", studentId);
                    checkCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    var exists = (int)await checkCmd.ExecuteScalarAsync();
                    if (exists > 0)
                        return (false, "Уже записан на этот экзамен");
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var insertSql = @"
                    INSERT INTO ExamRegistrations (StudentId, ScheduleId, RegistrationDate)
                    VALUES (@StudentId, @ScheduleId, GETDATE())";

                        using (var insertCmd = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCmd.Parameters.AddWithValue("@StudentId", studentId);
                            insertCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                            await insertCmd.ExecuteNonQueryAsync();
                        }

                        var updateSql = @"
                    UPDATE ExamSchedules 
                    SET CurrentStudents = CurrentStudents + 1,
                        IsAvailable = CASE WHEN CurrentStudents + 1 >= MaxStudents THEN 0 ELSE 1 END,
                        ModifiedDate = GETDATE()
                    WHERE Id = @ScheduleId";

                        using (var updateCmd = new SqlCommand(updateSql, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return (true, "Записан успешно");
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
        /// Получить количество записанных учеников
        /// </summary>
        public async Task<int> GetRegisteredStudentsCountAsync(int scheduleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT COUNT(*) FROM ExamRegistrations WHERE ScheduleId = @ScheduleId";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }

        /// <summary>
        /// Удалить экзамен
        /// </summary>
        public async Task<bool> DeleteExamScheduleAsync(int scheduleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var deleteRegSql = "DELETE FROM ExamRegistrations WHERE ScheduleId = @ScheduleId";
                        using (var cmd = new SqlCommand(deleteRegSql, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        var deleteExamSql = "DELETE FROM ExamSchedules WHERE Id = @Id";
                        using (var cmd = new SqlCommand(deleteExamSql, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", scheduleId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                        return true;
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
        /// Отметить экзамен как проведенный
        /// </summary>
        public async Task<bool> MarkExamAsConductedAsync(int scheduleId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "UPDATE ExamSchedules SET IsAvailable = 0, IsConducted = 1 WHERE Id = @Id";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", scheduleId);
                        int rows = await cmd.ExecuteNonQueryAsync();
                        System.Diagnostics.Debug.WriteLine($"Отметка экзамена {scheduleId} как проведенного. Затронуто строк: {rows}");
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка MarkExamAsConductedAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<StudentExamMark>> GetRegisteredStudentsWithStatusAsync(int scheduleId, ExamType examType, ExamStage examStage)
        {
            var students = new List<StudentExamMark>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
            SELECT 
                s.Id, 
                s.LastName + ' ' + s.FirstName AS StudentName,
                ISNULL(vc.Code, 'B') AS CategoryCode,
                s.Phone,
                -- Уже сдан этот экзамен ранее
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = @ExamStage AND Result = 1), 0) AS AlreadyPassed,
                -- Внутренние экзамены
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = 0 AND Stage = 0 AND Result = 1), 0) AS InternalTheoryPassed,
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = 0 AND Stage = 1 AND Result = 1), 0) AS InternalPracticePassed,
                -- Попытки
                ISNULL((SELECT COUNT(*) FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 0), 0) AS TheoryAttempts,
                ISNULL((SELECT COUNT(*) FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 1), 0) AS PracticeAttempts,
                -- Уже сдано в этом типе экзамена
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 0 AND Result = 1), 0) AS TheoryAlreadyPassed,
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 1 AND Result = 1), 0) AS PracticeAlreadyPassed
            FROM ExamRegistrations er
            INNER JOIN Students s ON er.StudentId = s.Id
            LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
            WHERE er.ScheduleId = @ScheduleId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    cmd.Parameters.AddWithValue("@ExamType", (int)examType);
                    cmd.Parameters.AddWithValue("@ExamStage", (int)examStage);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var student = new StudentExamMark
                            {
                                StudentId = reader.GetInt32(0),
                                StudentName = reader.GetString(1),
                                CategoryCode = reader.GetString(2),
                                Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                AlreadyPassed = reader.GetInt32(4) == 1,
                                InternalTheoryPassed = reader.GetInt32(5) == 1,
                                InternalPracticePassed = reader.GetInt32(6) == 1,
                                TheoryAttempts = reader.GetInt32(7),
                                PracticeAttempts = reader.GetInt32(8),
                                TheoryAlreadyPassed = reader.GetInt32(9) == 1,
                                PracticeAlreadyPassed = reader.GetInt32(10) == 1,
                                TheoryPassed = reader.GetInt32(9) == 1,
                                PracticePassed = reader.GetInt32(10) == 1
                            };
                            students.Add(student);
                        }
                    }
                }
            }

            return students;
        }

        /// <summary>
        /// Получить студентов, записанных на экзамен (для общего экзамена без привязки к этапу)
        /// </summary>
        public async Task<List<StudentExamMark>> GetRegisteredStudentsWithStatusForGeneralExamAsync(int scheduleId, ExamType examType)
        {
            var students = new List<StudentExamMark>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
            SELECT 
                s.Id, 
                s.LastName + ' ' + s.FirstName AS StudentName,
                ISNULL(vc.Code, 'B') AS CategoryCode,
                s.Phone,
                -- Внутренние экзамены
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = 0 AND Stage = 0 AND Result = 1), 0) AS InternalTheoryPassed,
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = 0 AND Stage = 1 AND Result = 1), 0) AS InternalPracticePassed,
                -- Попытки по теории для этого типа экзамена
                ISNULL((SELECT COUNT(*) FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 0), 0) AS TheoryAttempts,
                -- Попытки по практике для этого типа экзамена
                ISNULL((SELECT COUNT(*) FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 1), 0) AS PracticeAttempts,
                -- Уже сдано теории в этом типе экзамена
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 0 AND Result = 1), 0) AS TheoryAlreadyPassed,
                -- Уже сдано практики в этом типе экзамена
                ISNULL((SELECT TOP 1 1 FROM ExamRecords WHERE StudentId = s.Id AND Type = @ExamType AND Stage = 1 AND Result = 1), 0) AS PracticeAlreadyPassed
            FROM ExamRegistrations er
            INNER JOIN Students s ON er.StudentId = s.Id
            LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
            WHERE er.ScheduleId = @ScheduleId";

                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                    cmd.Parameters.AddWithValue("@ExamType", (int)examType);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var student = new StudentExamMark
                            {
                                StudentId = reader.GetInt32(0),
                                StudentName = reader.GetString(1),
                                CategoryCode = reader.GetString(2),
                                Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                InternalTheoryPassed = reader.GetInt32(4) == 1,
                                InternalPracticePassed = reader.GetInt32(5) == 1,
                                TheoryAttempts = reader.GetInt32(6),
                                PracticeAttempts = reader.GetInt32(7),
                                TheoryAlreadyPassed = reader.GetInt32(8) == 1,
                                PracticeAlreadyPassed = reader.GetInt32(9) == 1,
                                TheoryPassed = reader.GetInt32(8) == 1,
                                PracticePassed = reader.GetInt32(9) == 1,
                                TheoryEditable = reader.GetInt32(8) != 1,
                                PracticeEditable = reader.GetInt32(9) != 1
                            };
                            students.Add(student);
                        }
                    }
                }
            }

            return students;
        }
    }
}