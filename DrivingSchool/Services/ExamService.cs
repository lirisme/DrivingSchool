using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DrivingSchool.Models;

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
                           MaxStudents, CurrentStudents, ExaminerName, Location, IsAvailable
                    FROM ExamSchedules
                    WHERE [Type] = @Type 
                        AND [Stage] = @Stage 
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

        /// <summary>
        /// Записать ученика на экзамен
        /// </summary>
        public async Task<bool> RegisterForExamAsync(int studentId, int scheduleId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var checkSql = "SELECT COUNT(*) FROM ExamRegistrations WHERE StudentId = @StudentId AND ScheduleId = @ScheduleId";
                        using (var checkCmd = new SqlCommand(checkSql, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@StudentId", studentId);
                            checkCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                            var exists = (int)await checkCmd.ExecuteScalarAsync();
                            if (exists > 0)
                                return false;
                        }

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
                                IsAvailable = CASE WHEN CurrentStudents + 1 >= MaxStudents THEN 0 ELSE 1 END
                            WHERE Id = @ScheduleId";

                        using (var updateCmd = new SqlCommand(updateSql, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@ScheduleId", scheduleId);
                            await updateCmd.ExecuteNonQueryAsync();
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
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "UPDATE ExamSchedules SET IsAvailable = 0 WHERE Id = @Id";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", scheduleId);
                    await cmd.ExecuteNonQueryAsync();
                    return true;
                }
            }
        }
    }
}