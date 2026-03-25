from flask import Flask, request, jsonify
from flask_cors import CORS

app = Flask(__name__)
CORS(app)

@app.route('/recommend', methods=['POST'])
def recommend():
    data = request.json
    student = data.get('data', {})

    completed = student.get('completed_lessons', 0)
    total = student.get('total_required_lessons', 28)
    missed = student.get('missed_lessons', 0)
    max_gap = student.get('max_gap_days', 0)
    theory_attempts = student.get('theory_attempts', 0)
    practice_attempts = student.get('practice_attempts', 0)

    progress = completed / total if total > 0 else 0
    score = progress * 35
    score -= min(25, missed * 2)

    if max_gap > 14:
        score -= 15
    elif max_gap > 7:
        score -= 8

    score -= (theory_attempts + practice_attempts) * 8
    score = max(0, min(100, score))

    if score >= 80:
        recommendation = "✅ Студент ГОТОВ к экзамену"
        can_exam = True
        level = "Высокая"
    elif score >= 60:
        recommendation = "📊 Студент может сдавать, но требуется подготовка"
        can_exam = True
        level = "Средняя"
    else:
        recommendation = "⚠️ Студент НЕ ГОТОВ к экзамену"
        can_exam = False
        level = "Низкая"

    suggestions = []
    if progress < 0.7:
        suggestions.append(f"📚 Пройдено {completed} из {total} уроков")
    if missed > 0:
        suggestions.append(f"⚠️ {missed} прогулов. Рекомендуется {missed + 2} доп. урока")
    if max_gap > 14:
        suggestions.append(f"📅 Перерыв {max_gap} дней. Нужны дополнительные занятия")

    return jsonify({
        'score': round(score, 1),
        'level': level,
        'recommendation': recommendation,
        'can_exam': can_exam,
        'suggestions': suggestions
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=False)
