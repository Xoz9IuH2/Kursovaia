# Запуск VOENCOM туннеля
# Сначала запусти веб-API: cd App\web-api && dotnet run
# Потом запусти этот скрипт в другом терминале

Write-Host "Запуск serveo.net туннеля..." -ForegroundColor Cyan

$sshProcess = Start-Process -FilePath "ssh" -ArgumentList "-R", "80:localhost:5000", "serveo.net" -NoNewWindow -PassThru -RedirectStandardOutput "C:\serveo_output.txt"

Start-Sleep -Seconds 3

if ($sshProcess.HasExited) {
    Write-Host "Ошибка запуска!" -ForegroundColor Red
    exit 1
}

Write-Host "Туннель запущен!" -ForegroundColor Green
Write-Host ""

# Читаем URL из вывода
$output = Get-Content "C:\serveo_output.txt" -Raw

if ($output -match "https://[\w\-]+\.serveousercontent\.com") {
    $url = $matches[0]
    Write-Host "URL для мобильного приложения:" -ForegroundColor Yellow
    Write-Host $url -ForegroundColor Cyan
    
    # Сохраняем в файл
    $url | Out-File "C:\voencom_url.txt"
    Write-Host ""
    Write-Host "URL сохранён в C:\voencom_url.txt" -ForegroundColor Gray
} else {
    Write-Host "Жду URL..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Нажми Ctrl+C для остановки" -ForegroundColor Gray

# Ждём завершения
while (-not $sshProcess.HasExited) {
    Start-Sleep -Seconds 1
}