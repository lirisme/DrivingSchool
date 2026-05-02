import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

# Подключение к БД
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

# Загружаем реальные данные
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

print(f"Всего студентов: {len(df)}")
print(f"Сдали: {df['Passed'].sum()}, Не сдали: {len(df) - df['Passed'].sum()}")

# Создаём графики
fig, axes = plt.subplots(2, 2, figsize=(12, 10))

# 1. Распределение результатов
df['Passed'].value_counts().plot(kind='bar', ax=axes[0,0], color=['red', 'green'])
axes[0,0].set_title('Результаты экзаменов')
axes[0,0].set_xticklabels(['Не сдал', 'Сдал'])

# 2. Уроки vs результат
axes[0,1].scatter(df['CompletedLessons'], df['Passed'], alpha=0.5)
axes[0,1].set_title('Зависимость: Уроки → Результат')
axes[0,1].set_xlabel('Проведённые уроки')
axes[0,1].set_ylabel('Сдал (1) / Не сдал (0)')

# 3. Прогулы vs результат
axes[1,0].scatter(df['MissedLessons'], df['Passed'], alpha=0.5, color='orange')
axes[1,0].set_title('Зависимость: Прогулы → Результат')
axes[1,0].set_xlabel('Прогулы')
axes[1,0].set_ylabel('Сдал (1) / Не сдал (0)')

# 4. Корреляция
corr = df[['CompletedLessons', 'MissedLessons', 'TotalLessons', 'Passed']].corr()
sns.heatmap(corr, annot=True, cmap='coolwarm', ax=axes[1,1])
axes[1,1].set_title('Корреляция признаков')

plt.tight_layout()
plt.savefig('eda_analysis_real.png', dpi=150)
print("✅ График сохранён: eda_analysis_real.png")
plt.show()
