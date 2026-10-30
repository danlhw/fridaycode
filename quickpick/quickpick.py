"""
QuickPick - press F6 to auto-answer the quiz question under your cursor.

Runs silently in the background (no window, no taskbar entry).
Only sign it's running: a small tray icon by the clock (right-click > Quit).

First run asks for your Anthropic API key once and saves it to
%APPDATA%\\QuickPick\\config.json. Log file lives in the same folder.
"""

import base64
import ctypes
import io
import json
import logging
import os
import re
import sys
import threading
import time

import anthropic
import mss
import pyautogui
import pystray
from PIL import Image, ImageDraw
from pynput import keyboard

APP_NAME = "QuickPick"
DATA_DIR = os.path.join(os.environ.get("APPDATA", os.path.expanduser("~")), APP_NAME)
os.makedirs(DATA_DIR, exist_ok=True)
CONFIG_PATH = os.path.join(DATA_DIR, "config.json")
LOG_PATH = os.path.join(DATA_DIR, "quickpick.log")

logging.basicConfig(
    filename=LOG_PATH, level=logging.INFO,
    format="%(asctime)s %(message)s", datefmt="%H:%M:%S",
)
log = logging.info

# ---------- single instance ----------

def already_running():
    try:
        ctypes.windll.kernel32.CreateMutexW(None, False, "Global\\QuickPickSingleInstance")
        return ctypes.windll.kernel32.GetLastError() == 183  # ERROR_ALREADY_EXISTS
    except Exception:
        return False

# ---------- DPI + pyautogui ----------

try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    try:
        ctypes.windll.user32.SetProcessDPIAware()
    except Exception:
        pass

pyautogui.FAILSAFE = True  # flick mouse to a screen corner to abort mid-click
pyautogui.PAUSE = 0.05

# ---------- config ----------

DEFAULTS = {
    "api_key": "",
    "model": "claude-opus-5-5",
    "hotkey": "f6",
    "box_width": 1400,
    "box_height": 900,
    "max_image_side": 1568,
}


def load_config():
    cfg = dict(DEFAULTS)
    # Optional config.json next to the exe, then the saved one in AppData
    exe_dir = os.path.dirname(sys.executable if getattr(sys, "frozen", False) else os.path.abspath(__file__))
    for path in (os.path.join(exe_dir, "config.json"), CONFIG_PATH):
        if os.path.exists(path):
            try:
                with open(path, "r", encoding="utf-8") as f:
                    cfg.update({k: v for k, v in json.load(f).items() if v != ""})
            except Exception as e:
                log(f"Bad config {path}: {e}")
    cfg["api_key"] = cfg["api_key"] or os.environ.get("ANTHROPIC_API_KEY", "")
    return cfg


def save_config(cfg):
    with open(CONFIG_PATH, "w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2)


def ask_for_key():
    """Tiny one-time popup to paste the API key."""
    import tkinter as tk
    from tkinter import simpledialog
    root = tk.Tk()
    root.withdraw()
    root.attributes("-topmost", True)
    key = simpledialog.askstring(
        APP_NAME, "Paste your Anthropic API key (saved once, never asked again):",
        parent=root, show="*",
    )
    root.destroy()
    return (key or "").strip()


def message_box(text):
    try:
        ctypes.windll.user32.MessageBoxW(0, text, APP_NAME, 0x40)
    except Exception:
        pass


CFG = load_config()
client = None
busy = threading.Lock()

PROMPT = """This is a crop of a computer screen. The user's mouse cursor is at pixel ({cx}, {cy}) in this image, marked with a red ring.

Find the ONE quiz/form question closest to the cursor. Ignore every other question on screen.
Work out the correct answer. If it says "select all that apply" or uses checkboxes, pick every correct option.

Reply with ONLY this JSON, no other text:
{{
  "question": "the question text",
  "type": "single" | "multi" | "text",
  "answers": [{{"text": "option label", "x": 0, "y": 0}}],
  "text_answer": "only for type text, else empty"
}}

x,y = pixel coordinates in THIS image of the centre of the clickable option (radio/checkbox/button/label).
For type "text", give one answer whose x,y is the input box, and put what to type in text_answer.
If no question is visible near the cursor, return {{"question": "", "type": "none", "answers": [], "text_answer": ""}}."""

# ---------- core ----------

def grab_region():
    mx, my = pyautogui.position()
    with mss.mss() as sct:
        mon = next(
            (m for m in sct.monitors[1:]
             if m["left"] <= mx < m["left"] + m["width"] and m["top"] <= my < m["top"] + m["height"]),
            sct.monitors[1],
        )
        w = min(CFG["box_width"], mon["width"])
        h = min(CFG["box_height"], mon["height"])
        left = max(mon["left"], min(mx - w // 2, mon["left"] + mon["width"] - w))
        top = max(mon["top"], min(my - h // 2, mon["top"] + mon["height"] - h))
        shot = sct.grab({"left": left, "top": top, "width": w, "height": h})
        img = Image.frombytes("RGB", shot.size, shot.rgb)

    scale = 1.0
    longest = max(img.size)
    if longest > CFG["max_image_side"]:
        scale = CFG["max_image_side"] / longest
        img = img.resize((round(img.width * scale), round(img.height * scale)), Image.LANCZOS)

    cx, cy = round((mx - left) * scale), round((my - top) * scale)
    ImageDraw.Draw(img).ellipse([cx - 14, cy - 14, cx + 14, cy + 14], outline=(255, 0, 0), width=3)
    return img, (left, top, scale), (cx, cy), (mx, my)


def ask_claude(img, cx, cy):
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    b64 = base64.b64encode(buf.getvalue()).decode()
    resp = client.messages.create(
        model=CFG["model"],
        max_tokens=800,
        messages=[{
            "role": "user",
            "content": [
                {"type": "image", "source": {"type": "base64", "media_type": "image/png", "data": b64}},
                {"type": "text", "text": PROMPT.format(cx=cx, cy=cy)},
            ],
        }],
    )
    text = "".join(b.text for b in resp.content if b.type == "text")
    m = re.search(r"\{.*\}", text, re.S)
    if not m:
        raise ValueError(f"Couldn't parse reply: {text[:200]}")
    return json.loads(m.group(0))


def to_screen(x, y, origin):
    left, top, scale = origin
    return round(left + x / scale), round(top + y / scale)


def beep(ok):
    try:
        import winsound
        winsound.Beep(1200 if ok else 400, 120)
    except Exception:
        pass


def run_once():
    if not busy.acquire(blocking=False):
        return
    try:
        img, origin, (cx, cy), home = grab_region()
        t0 = time.time()
        result = ask_claude(img, cx, cy)
        log(f"Q: {result.get('question', '')[:120]}")

        kind = result.get("type")
        answers = result.get("answers", [])
        if kind == "none" or not answers:
            log("No question near cursor")
            beep(False)
            return

        if kind == "text":
            a = answers[0]
            pyautogui.click(*to_screen(a["x"], a["y"], origin))
            pyautogui.write(result.get("text_answer", ""), interval=0.01)
            log(f"Typed: {result.get('text_answer')}")
        else:
            for a in answers:
                pyautogui.click(*to_screen(a["x"], a["y"], origin))
                log(f"Clicked: {a.get('text', '')}")
                time.sleep(0.08)

        pyautogui.moveTo(*home)
        log(f"Done in {time.time() - t0:.1f}s")
        beep(True)
    except Exception as e:
        log(f"Error: {e}")
        beep(False)
    finally:
        busy.release()

# ---------- tray ----------

def make_icon():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([4, 4, 60, 60], fill=(20, 20, 20, 255))
    d.line([(20, 33), (29, 42), (45, 23)], fill=(255, 255, 255, 255), width=6)
    return img


def main():
    global client

    if already_running():
        return

    if not CFG["api_key"]:
        key = ask_for_key()
        if not key:
            message_box("No API key entered. QuickPick will close.")
            return
        CFG["api_key"] = key
        save_config({"api_key": key})

    client = anthropic.Anthropic(api_key=CFG["api_key"])
    target = getattr(keyboard.Key, CFG["hotkey"].lower(), keyboard.Key.f6)

    def on_press(key):
        if key == target:
            threading.Thread(target=run_once, daemon=True).start()

    listener = keyboard.Listener(on_press=on_press)
    listener.daemon = True
    listener.start()
    log("QuickPick started")

    def quit_app(icon, _item):
        log("QuickPick stopped")
        listener.stop()
        icon.stop()

    def open_log(_icon, _item):
        os.startfile(LOG_PATH)

    menu = pystray.Menu(
        pystray.MenuItem(f"QuickPick: press {CFG['hotkey'].upper()}", None, enabled=False),
        pystray.MenuItem("Open log", open_log),
        pystray.MenuItem("Quit", quit_app),
    )
    pystray.Icon(APP_NAME, make_icon(), f"QuickPick ({CFG['hotkey'].upper()})", menu).run()


if __name__ == "__main__":
    main()
