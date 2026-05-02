import pyodbc
import pandas as pd
import matplotlib.pyplot as plt

# Подключение к БД
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

# Загрузка данных
sql = """
SELECT 
    s.CompletedLessons,
    s.MissedLessons,
    ISNULL(vc.LessonsCount / 2, 28) AS TotalLessons,
    CASE WHEN er.Result = 1 THEN 1 ELSE 0 END AS Passed
FROM Students s
LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
LEFT JOIN ExamRecords er ON s.Id = er.StudentId
WHERE er.Id IS NOT NULL
"""

df = pd.read_sql(sql, conn)

# Настройка русских шрифтов
plt.rcParams['font.family'] = ['DejaVu Sans']

# Создаём 2 графика
fig, axes = plt.subplots(1, 2, figsize=(14, 5))

# График 1: Уроки
passed = df[df['Passed'] == 1]['CompletedLessons']
failed = df[df['Passed'] == 0]['CompletedLessons']

axes[0].hist(passed, alpha=0.5, label='Сдавшие', color='green', bins=15)
axes[0].hist(failed, alpha=0.5, label='Не сдавшие', color='red', bins=15)
axes[0].set_xlabel('Количество уроков')
axes[0].set_ylabel('Количество студентов')
axes[0].set_title('Распределение уроков: сдавшие vs несдавшие')
axes[0].legend()

# График 2: Прогулы
passed_missed = df[df['Passed'] == 1]['MissedLessons']
failed_missed = df[df['Passed'] == 0]['MissedLessons']

axes[1].hist(passed_missed, alpha=0.5, label='Сдавшие', color='green', bins=10)
axes[1].hist(failed_missed, alpha=0.5, label='Не сдавшие', color='red', bins=10)
axes[1].set_xlabel('Количество прогулов')
axes[1].set_ylabel('Количество студентов')
axes[1].set_title('Распределение прогулов: сдавшие vs несдавшие')
axes[1].legend()

plt.tight_layout()
plt.savefig('eda_analysis.png', dpi=150)
print("✅ График сохранён: eda_analysis.png")
plt.show()
