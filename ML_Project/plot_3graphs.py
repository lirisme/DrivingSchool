import pyodbc
import pandas as pd
import matplotlib.pyplot as plt

conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

sql = """
SELECT 
    s.CompletedLessons,
    s.MissedLessons,
    CASE WHEN er.Result = 1 THEN 'Сдал' ELSE 'Не сдал' END AS Result
FROM Students s
LEFT JOIN ExamRecords er ON s.Id = er.StudentId
WHERE er.Id IS NOT NULL
"""

df = pd.read_sql(sql, conn)

# Настройка русских шрифтов
plt.rcParams['font.family'] = ['DejaVu Sans']

fig, axes = plt.subplots(1, 3, figsize=(15, 5))

# График 1: Результаты
results = df['Result'].value_counts()
axes[0].bar(results.index, results.values, color=['red', 'green'])
axes[0].set_title('1. Результаты экзаменов')
axes[0].set_ylabel('Количество студентов')

# График 2: Уроки
passed = df[df['Result'] == 'Сдал']['CompletedLessons']
failed = df[df['Result'] == 'Не сдал']['CompletedLessons']
axes[1].hist(passed, alpha=0.5, label='Сдал', color='green', bins=15)
axes[1].hist(failed, alpha=0.5, label='Не сдал', color='red', bins=15)
axes[1].set_title('2. Уроки: сдавшие vs несдавшие')
axes[1].set_xlabel('Количество уроков')
axes[1].legend()

# График 3: Прогулы
passed_missed = df[df['Result'] == 'Сдал']['MissedLessons']
failed_missed = df[df['Result'] == 'Не сдал']['MissedLessons']
axes[2].hist(passed_missed, alpha=0.5, label='Сдал', color='green', bins=10)
axes[2].hist(failed_missed, alpha=0.5, label='Не сдал', color='red', bins=10)
axes[2].set_title('3. Прогулы: сдавшие vs несдавшие')
axes[2].set_xlabel('Количество прогулов')
axes[2].legend()

plt.tight_layout()
plt.savefig('eda_analysis.png', dpi=150)
print("✅ 3 графика сохранены: eda_analysis.png")
plt.show()
