@echo off
REM Builds dist\QuickPick.exe (silent background app, no window)
python -m pip install -r requirements.txt
python -m PyInstaller --onefile --noconsole --name QuickPick quickpick.py
echo.
echo Done. Double-click dist\QuickPick.exe. It asks for your API key once, then runs in the tray.
pause
