import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

# Подключение к БД
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

# Простой запрос
df = pd.read_sql("""
    SELECT 
        s.CompletedLessons,
        s.MissedLessons,
        ISNULL(vc.LessonsCount / 2, 28) AS TotalLessons,
        CASE WHEN EXISTS (SELECT 1 FROM ExamRecords WHERE StudentId = s.Id AND Result = 1) THEN 1 ELSE 0 END AS Passed
    FROM Students s
    LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
    WHERE s.Id IN (SELECT DISTINCT StudentId FROM DrivingLessons)
""", conn)

print(f"Всего студентов: {len(df)}")
print(f"Сдали: {df['Passed'].sum()}, Не сдали: {len(df) - df['Passed'].sum()}")

# Статистика
print("\n=== Статистика ===")
print(df.describe())

# Корреляция
correlations = df[['CompletedLessons', 'MissedLessons', 'TotalLessons', 'Passed']].corr()
print("\n=== Корреляция с результатом ===")
print(correlations['Passed'].sort_values(ascending=False))

# Визуализация
fig, axes = plt.subplots(1, 2, figsize=(12, 5))

# График 1: Уроки vs результат
axes[0].scatter(df['CompletedLessons'], df['Passed'], alpha=0.5)
axes[0].set_title('Зависимость: Уроки → Результат')
axes[0].set_xlabel('Проведённые уроки')
axes[0].set_ylabel('Сдал (1) / Не сдал (0)')

# График 2: Прогулы vs результат
axes[1].scatter(df['MissedLessons'], df['Passed'], alpha=0.5, color='orange')
axes[1].set_title('Зависимость: Прогулы → Результат')
axes[1].set_xlabel('Прогулы')
axes[1].set_ylabel('Сдал (1) / Не сдал (0)')

plt.tight_layout()
plt.savefig('eda_analysis.png', dpi=150)
print("\nГрафик сохранен: eda_analysis.png")
plt.show()