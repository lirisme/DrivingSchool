import json
import os

def handler(event, context):
    try:
        if isinstance(event, dict) and 'body' in event:
            body = json.loads(event['body'])
        else:
            body = event
        
        data = body.get('data', {})
        
        completed = data.get('CompletedLessons', 0)
        missed = data.get('MissedLessons', 0)
        total = data.get('TotalRequiredLessons', 28)
        
        progress = completed / total if total > 0 else 0
        progress_percent = int(progress * 100)
        
        suggestions = []
        
        if completed >= total:
            suggestions.append(f"✅ Все {total} уроков пройдены")
        else:
            suggestions.append(f"📚 Пройдено {completed} из {total} уроков ({progress_percent}%)")
        
        if missed > 0:
            missed_percent = int(missed / total * 100)
            suggestions.append(f"⚠️ {missed} прогулов ({missed_percent}% от всех уроков)")
        
        # Простая логика
        if progress_percent >= 80 and missed <= 2:
            score = 85
            recommendation = "✅ Студент ГОТОВ к экзамену"
            can_exam = True
            level = "Высокая"
        elif missed > 5:
            score = max(0, progress_percent - missed * 5)
            recommendation = "❌ Студент НЕ ГОТОВ. Слишком много прогулов"
            can_exam = False
            level = "Низкая"
        elif progress_percent >= 60:
            score = progress_percent - missed * 3
            recommendation = "📊 Студент может сдавать экзамен"
            can_exam = True
            level = "Средняя"
        else:
            score = progress_percent
            recommendation = "⚠️ Студент НЕ ГОТОВ. Требуется больше занятий"
            can_exam = False
            level = "Низкая"
        
        if missed > 2:
            suggestions.append(f"💡 Рекомендуется взять {missed} дополнительных урока")
        
        return {
            'statusCode': 200,
            'body': json.dumps({
                'score': score,
                'level': level,
                'recommendation': recommendation,
                'can_exam': can_exam,
                'suggestions': suggestions
            }, ensure_ascii=False),
            'headers': {'Content-Type': 'application/json; charset=utf-8'}
        }
        
    except Exception as e:
        return {
            'statusCode': 500,
            'body': json.dumps({'error': str(e)}),
            'headers': {'Content-Type': 'application/json'}
        }
