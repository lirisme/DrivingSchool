import pyodbc
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score
import joblib

# Подключение к БД
conn = pyodbc.connect('DRIVER={SQL Server};SERVER=localhost;DATABASE=DrivingSchoolDB;Trusted_Connection=yes')

# Очень простой запрос без сложных подзапросов
sql = """
SELECT 
    s.CompletedLessons,
    s.MissedLessons,
    ISNULL(vc.LessonsCount / 2, 28) AS TotalLessons,
    CASE 
        WHEN er.Result = 1 THEN 1 
        ELSE 0 
    END AS Passed
FROM Students s
LEFT JOIN VehicleCategories vc ON s.CategoryId = vc.Id
LEFT JOIN ExamRecords er ON s.Id = er.StudentId
WHERE er.Id IS NOT NULL
"""

df = pd.read_sql(sql, conn)

print(f"Всего записей: {len(df)}")
print(f"Сдали: {df['Passed'].sum()}, Не сдали: {len(df) - df['Passed'].sum()}")

# Признаки
features = ['CompletedLessons', 'MissedLessons', 'TotalLessons']
X = df[features].fillna(0)
y = df['Passed']

# Разделение
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

print(f"\nОбучающая: {len(X_train)}, Тестовая: {len(X_test)}")

# 1. Логистическая регрессия
print("\n=== Логистическая регрессия ===")
lr = LogisticRegression(max_iter=1000)
lr.fit(X_train, y_train)
y_pred_lr = lr.predict(X_test)

print(f"Accuracy: {accuracy_score(y_test, y_pred_lr):.3f}")
print(f"Precision: {precision_score(y_test, y_pred_lr):.3f}")
print(f"Recall: {recall_score(y_test, y_pred_lr):.3f}")
print(f"F1: {f1_score(y_test, y_pred_lr):.3f}")

# 2. Random Forest
print("\n=== Random Forest ===")
rf = RandomForestClassifier(n_estimators=100, max_depth=5, random_state=42)
rf.fit(X_train, y_train)
y_pred_rf = rf.predict(X_test)

print(f"Accuracy: {accuracy_score(y_test, y_pred_rf):.3f}")
print(f"Precision: {precision_score(y_test, y_pred_rf):.3f}")
print(f"Recall: {recall_score(y_test, y_pred_rf):.3f}")
print(f"F1: {f1_score(y_test, y_pred_rf):.3f}")

# Сохраняем модель
joblib.dump(rf, 'model.pkl')
print("\n✅ Модель сохранена: model.pkl")

# Важность признаков
importances = pd.DataFrame({'feature': features, 'importance': rf.feature_importances_})
print("\n📊 Важность признаков:")
print(importances.sort_values('importance', ascending=False))