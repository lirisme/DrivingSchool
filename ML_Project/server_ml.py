from flask import Flask, request, jsonify
import joblib
import os

app = Flask(__name__)

# Загружаем модель
model_path = '/app/model.pkl'
if os.path.exists(model_path):
    model = joblib.load(model_path)
else:
    model = None

@app.route('/predict', methods=['POST'])
def predict():
    data = request.json
    student = data.get('data', {})
    
    completed = student.get('CompletedLessons', 0)
    missed = student.get('MissedLessons', 0)
    total = student.get('TotalRequiredLessons', 28)
    
    if model is None:
        # fallback
        progress = completed / total if total > 0 else 0
        score = progress * 100
        recommendation = "✅ Студент ГОТОВ" if score >= 80 else "⚠️ НЕ ГОТОВ"
        can_exam = score >= 80
    else:
        features = [[completed, missed, total]]
        proba = model.predict_proba(features)[0][1]
        prediction = model.predict(features)[0]
        score = round(proba * 100, 1)
        if prediction == 1:
            recommendation = "✅ Студент ГОТОВ к экзамену"
            can_exam = True
        else:
            recommendation = "⚠️ Студент НЕ ГОТОВ к экзамену"
            can_exam = False
    
    suggestions = []
    if completed >= total:
        suggestions.append(f"✅ Все {total} уроков пройдены")
    else:
        suggestions.append(f"📚 Пройдено {completed} из {total} уроков")
    if missed > 0:
        suggestions.append(f"⚠️ {missed} прогулов")
    
    return jsonify({
        'score': score,
        'recommendation': recommendation,
        'can_exam': can_exam,
        'suggestions': suggestions
    })

@app.route('/health', methods=['GET'])
def health():
    return jsonify({'status': 'ok'})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
