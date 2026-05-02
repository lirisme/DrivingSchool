import pyodbc
import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

# Подключение к БД
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

# Загрузка данных
df = pd.read_sql("""
    SELECT 
        s.CompletedLessons,
        s.MissedLessons,
        ISNULL(vc.LessonsCount / 2, 28) AS TotalLessons,
        CASE WHEN er.Result = 1 THEN 1 ELSE 0 END AS Passed
    FROM Students s
    LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
    LEFT JOIN ExamRecords er ON s.Id = er.StudentId
    WHERE er.Id IS NOT NULL
""", conn)

# Настройка русских шрифтов
plt.rcParams['font.family'] = ['DejaVu Sans']

# Создаём 4 графика
fig, axes = plt.subplots(2, 2, figsize=(14, 10))

# График 1: Результаты экзаменов
results = df['Passed'].value_counts()
axes[0,0].bar(['Не сдал', 'Сдал'], results.values, color=['red', 'green'])
axes[0,0].set_title('Рисунок 1. Результаты экзаменов')
axes[0,0].set_ylabel('Количество студентов')
for i, v in enumerate(results.values):
    axes[0,0].text(i, v + 1, str(v), ha='center')

# График 2: Уроки
passed_lessons = df[df['Passed'] == 1]['CompletedLessons']
failed_lessons = df[df['Passed'] == 0]['CompletedLessons']
axes[0,1].hist(passed_lessons, alpha=0.5, label='Сдал', color='green', bins=15)
axes[0,1].hist(failed_lessons, alpha=0.5, label='Не сдал', color='red', bins=15)
axes[0,1].set_title('Рисунок 2. Распределение уроков: сдавшие vs несдавшие')
axes[0,1].set_xlabel('Количество уроков')
axes[0,1].set_ylabel('Количество студентов')
axes[0,1].legend()

# График 3: Прогулы
passed_missed = df[df['Passed'] == 1]['MissedLessons']
failed_missed = df[df['Passed'] == 0]['MissedLessons']
axes[1,0].hist(passed_missed, alpha=0.5, label='Сдал', color='green', bins=10)
axes[1,0].hist(failed_missed, alpha=0.5, label='Не сдал', color='red', bins=10)
axes[1,0].set_title('Рисунок 3. Распределение прогулов: сдавшие vs несдавшие')
axes[1,0].set_xlabel('Количество прогулов')
axes[1,0].set_ylabel('Количество студентов')
axes[1,0].legend()

# График 4: Корреляционная матрица
corr = df[['CompletedLessons', 'MissedLessons', 'TotalLessons', 'Passed']].corr()
sns.heatmap(corr, annot=True, cmap='coolwarm', fmt='.2f', ax=axes[1,1])
axes[1,1].set_title('Рисунок 4. Корреляционная матрица')

plt.tight_layout()
plt.savefig('eda_4graphs.png', dpi=150)
print("✅ 4 графика сохранены в eda_4graphs.png")
plt.show()
