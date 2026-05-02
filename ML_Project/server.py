import json
from http.server import HTTPServer, BaseHTTPRequestHandler

def predict(completed, missed, total):
    progress = completed / total if total > 0 else 0
    progress_percent = int(progress * 100)
    penalty = min(30, missed * 5)
    score = max(0, progress_percent - penalty)
    
    if score >= 80:
        recommendation = "✅ Студент ГОТОВ к экзамену"
        can_exam = True
        level = "Высокая"
    elif score >= 60:
        recommendation = "📊 Студент может сдавать экзамен"
        can_exam = True
        level = "Средняя"
    else:
        recommendation = "⚠️ Студент НЕ ГОТОВ к экзамену"
        can_exam = False
        level = "Низкая"
    
    suggestions = []
    if completed >= total:
        suggestions.append(f"✅ Все {total} уроков пройдены")
    else:
        suggestions.append(f"📚 Пройдено {completed} из {total} уроков ({progress_percent}%)")
    if missed > 0:
        suggestions.append(f"⚠️ {missed} прогулов")
    
    return score, level, recommendation, can_exam, suggestions

class Handler(BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path == '/predict':
            length = int(self.headers['Content-Length'])
            data = json.loads(self.rfile.read(length))
            student = data.get('data', {})
            
            completed = student.get('CompletedLessons', 0)
            missed = student.get('MissedLessons', 0)
            total = student.get('TotalRequiredLessons', 28)
            
            score, level, rec, can, sugg = predict(completed, missed, total)
            
            response = json.dumps({
                'score': score,
                'level': level,
                'recommendation': rec,
                'can_exam': can,
                'suggestions': sugg
            }, ensure_ascii=False)
            
            self.send_response(200)
            self.send_header('Content-Type', 'application/json; charset=utf-8')
            self.end_headers()
            self.wfile.write(response.encode('utf-8'))
    
    def do_GET(self):
        if self.path == '/health':
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(b'{"status": "ok"}')
        else:
            self.send_response(404)

if __name__ == '__main__':
    server = HTTPServer(('0.0.0.0', 8080), Handler)
    print('Server running on port 8080')
    server.serve_forever()
