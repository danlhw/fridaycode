using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchoolApp;

internal static class Program
{
    private const string ApiKey = "__ANTHROPIC_API_KEY__";
    private const string Model = "claude-sonnet-4-6";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";
    private const int MaxTokens = 2500;

    // Hotkeys: Ctrl+Shift+Space (answer) / Ctrl+Shift+Q (pin context).
    // F-keys were unreliable: some school PCs have them grabbed by IT/remote-management agents.
    private const int VK_SPACE = 0x20;
    private const int VK_Q = 0x51;
    private const int VK_F7 = 0x76;
    private const int VK_F8 = 0x77;
    private const int HOTKEY_PIN_ID = 0x6A66;
    private const int HOTKEY_ANSWER_ID = 0x6A65;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint MOD_ANSWER = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT; // Ctrl+Shift+Space
    private const uint MOD_PIN = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT;    // Ctrl+Shift+Q

    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const string SystemPrompt =
        "You are a silent academic answering assistant. The user sends one or more screenshots of their computer. Output ONLY the answer text that should be pasted directly into an answer field. No preamble. No 'The answer is'. No labels like 'Answer:'. No quotation marks around the answer. No commentary on what you see.\n\n" +
        "MULTI-SCREEN INPUT (when present):\n" +
        "If a 'PINNED CONTEXT' image is provided BEFORE the 'CURRENT SCREEN' image, treat the pinned image as supporting reference material the user captured from an earlier page (e.g., a case scenario, patient vignette, source passage, formula sheet, or shared diagram). The QUESTION you must answer is ALWAYS on the CURRENT SCREEN image. Do NOT answer questions visible on the pinned context image. Use pinned context only to inform your answer to the current question.\n\n" +
        "PROCEDURE:\n" +
        "1. Identify the PRIMARY question on the CURRENT SCREEN. It is almost always the largest, most prominent block of text, or the text immediately above an empty answer/text input field. Ignore: browser chrome, navigation menus, sidebars, ads, timers, progress bars, names of other students, chat panels, taskbars.\n" +
        "2. Read all supporting context that the question depends on. Sources of context, in order of priority: (a) the pinned context image if provided, (b) any passage / data / diagram on the current screen, (c) general subject knowledge.\n" +
        "3. Detect any point/mark value indicator near or attached to the question. Common formats: '[3 marks]', '(5 points)', '/10', '[2 pts]', 'Worth 4 marks', '(4)'. Use it to scale answer LENGTH per the rules below.\n\n" +
        "LENGTH SCALING BY MARKS (applies to free-text and to MCQ reasoning):\n" +
        "  1-2 marks  -> exactly 2 short sentences answering the question\n" +
        "  3-5 marks  -> AT LEAST 5 sentences covering every key reasoning point and every concept the question asks about\n" +
        "  6-10 marks -> 8-12 sentences in structured paragraph(s) with definitions, mechanisms, and at least one example or piece of evidence per point\n" +
        "  11+ marks  -> EXACTLY 12-15 sentences (HARD CAP: NEVER MORE THAN 15) in a clearly structured response (introduction sentence, body covering each point in turn, brief conclusion). Cover all aspects in depth, but stay within the 15-sentence ceiling. If you have more to say, condense each sentence rather than adding more sentences.\n" +
        "  If no mark value is visible, default to a complete-but-concise answer scaled to the question's apparent depth.\n\n" +
        "OUTPUT FORMAT BY QUESTION TYPE:\n" +
        "- Multiple choice: Output the letter, a period, a space, the option text, a period, a space, then BRIEF REASONING that follows the LENGTH SCALING above (treat the marks as scaling the reasoning portion). Example for [3 marks]: 'C. Mitochondrion. Mitochondria carry out aerobic cellular respiration through the electron transport chain on the inner membrane, producing the bulk of cellular ATP. Glycolysis begins in the cytosol but the high-yield steps (Krebs cycle and oxidative phosphorylation) happen inside the mitochondrial matrix and inner membrane. Other listed organelles serve different roles: ribosomes synthesise proteins, chloroplasts perform photosynthesis only in plant cells, and the endoplasmic reticulum handles lipid synthesis and protein folding. Therefore mitochondrion is the only correct answer for cellular respiration. This explains why cells with high energy demand (e.g. muscle, neurons) contain large numbers of mitochondria.'\n" +
        "- Fill-in-the-blank / very short answer: Output ONLY the missing word(s) or phrase. No sentence framing. If the answer is numeric, ALWAYS include the unit (e.g. '0.25 g', '37.5 mL', '60 bpm') even if the question doesn't repeat the unit.\n" +
        "- Numeric / math: ALWAYS output the final value WITH UNITS, even when the answer is a single number. Examples of correct numeric output: '0.25 g', '50 mg', '7.5 cm', '120 mmHg', '3.2 mol/L'. NEVER output a bare number with no unit when the question implies a unit. Show working ONLY if mark value is >= 4 marks; otherwise just the answer-with-unit.\n" +
        "- Long-form / essay / extended response: Output the answer text directly, scaled to the marks per LENGTH SCALING. Use paragraph breaks where useful. Match the formality and depth implied by the marks. For 11+ marks, NEVER exceed 15 sentences total.\n" +
        "- Code: Output ONLY the code (no markdown fences, no commentary) unless the question explicitly asks for explanation.\n\n" +
        "HARD RULES:\n" +
        "- Match the language of the question (English, French, etc.).\n" +
        "- Be confident, direct, exam-ready.\n" +
        "- Never apologise. Never say 'I cannot determine' or 'I cannot see clearly'. If the question is partly unreadable, give the most likely correct answer based on what is visible.\n" +
        "- Never include meta commentary about being an AI, about the screenshot, or about your reasoning process.\n" +
        "- Never wrap the answer in quotes unless the answer is literally a quotation.";

    private const string UserTextSingle = "Answer the question on this screen. Output only the answer text.";

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_CONTROL_KEY = 0x11;
    private const int VK_SHIFT_KEY = 0x10;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static int _busy;
    private static byte[]? _pinnedContext;
    private static HiddenForm? _form;

    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        _form = new HiddenForm();
        _form.AnswerHotkeyPressed += OnAnswerHotkey;
        _form.PinHotkeyPressed += OnPinHotkey;

        // Defense in depth: RegisterHotKey AND a polling thread.
        // RegisterHotKey is faster and intercepts the keys, but can fail silently
        // in restricted environments. Polling via GetAsyncKeyState always works
        // because it's a passive read of system key state.
        var pollThread = new Thread(PollHotkeysLoop) { IsBackground = true };
        pollThread.Start();

        Application.Run(_form);
    }

    private static void PollHotkeysLoop()
    {
        bool answerWasDown = false;
        bool pinWasDown = false;
        const int pressed = unchecked((short)0x8000);

        while (true)
        {
            try
            {
                bool ctrl = (GetAsyncKeyState(VK_CONTROL_KEY) & pressed) != 0;
                bool shift = (GetAsyncKeyState(VK_SHIFT_KEY) & pressed) != 0;
                bool space = (GetAsyncKeyState(VK_SPACE) & pressed) != 0;
                bool q = (GetAsyncKeyState(VK_Q) & pressed) != 0;
                bool f7 = (GetAsyncKeyState(VK_F7) & pressed) != 0;
                bool f8 = (GetAsyncKeyState(VK_F8) & pressed) != 0;

                // Either (Ctrl+Shift+Space) OR (F8 alone) triggers an answer.
                // Either (Ctrl+Shift+Q) OR (F7 alone) pins the current screen.
                // Redundant input paths so we work on more keyboards.
                bool answerDown = (ctrl && shift && space) || f8;
                bool pinDown = (ctrl && shift && q) || f7;

                if (answerDown && !answerWasDown) OnAnswerHotkey();
                if (pinDown && !pinWasDown) OnPinHotkey();

                answerWasDown = answerDown;
                pinWasDown = pinDown;
            }
            catch
            {
                // never let polling crash the app
            }
            Thread.Sleep(40);
        }
    }

    private static void OnPinHotkey()
    {
        // F7: capture current screen and stash as pinned context. Silent, no API call.
        _ = Task.Run(() =>
        {
            try
            {
                byte[] png = CapturePrimaryScreenPng();
                Interlocked.Exchange(ref _pinnedContext, png);
            }
            catch
            {
                // silent
            }
        });
    }

    private static void OnAnswerHotkey()
    {
        // Ctrl+Shift+Space: capture current screen, send pinned (if any) + current to Claude.
        // For MCQ answers (start with "A." / "B." / etc): put ONLY the letter in clipboard
        // and do NOT auto-paste — user pastes into URL bar manually to read it.
        // For non-MCQ answers: paste the full answer into the focused text field.
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(50).ConfigureAwait(false);
                byte[] current = CapturePrimaryScreenPng();
                byte[]? pinned = Interlocked.CompareExchange(ref _pinnedContext, null, null);
                string answer = await CallClaudeAsync(pinned, current).ConfigureAwait(false);
                if (string.IsNullOrEmpty(answer) || _form is null || _form.IsDisposed) return;

                if (IsMcqAnswer(answer))
                {
                    string letter = answer.Substring(0, 1);
                    _form.Invoke(new Action(() => CopyLetterOnly(letter)));
                }
                else
                {
                    _form.Invoke(new Action(() => PasteOnUiThread(answer)));
                }
            }
            catch
            {
                // silent
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        });
    }

    private static bool IsMcqAnswer(string answer)
    {
        // Detect "A." / "B." / ... / "H." prefix optionally followed by space or newline.
        if (string.IsNullOrEmpty(answer) || answer.Length < 2) return false;
        char first = answer[0];
        if (first < 'A' || first > 'H') return false;
        if (answer[1] != '.') return false;
        if (answer.Length == 2) return true;
        char third = answer[2];
        return third == ' ' || third == '\n' || third == '\r' || third == '\t';
    }

    private static void CopyLetterOnly(string letter)
    {
        // MCQ mode: clipboard holds ONLY the letter (e.g. "B"). User pastes manually
        // into the URL bar to read it, then clicks the matching radio button.
        // We deliberately DO NOT restore the previous clipboard — the user needs the
        // letter to remain available for their manual Ctrl+V.
        try
        {
            Clipboard.SetText(letter);
        }
        catch
        {
            // silent
        }
    }

    private sealed class HiddenForm : Form
    {
        public event Action? AnswerHotkeyPressed;
        public event Action? PinHotkeyPressed;
        private bool _hotkeysRegistered;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;        // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000;  // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public HiddenForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Opacity = 0;
            Size = new Size(1, 1);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-2000, -2000);
            Load += OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            if (!_hotkeysRegistered)
            {
                // Try to register all four (RegisterHotKey is faster + intercepts
                // keys when it works). If any fail silently, the GetAsyncKeyState
                // polling loop catches them anyway.
                RegisterHotKey(Handle, HOTKEY_ANSWER_ID, MOD_ANSWER, (uint)VK_SPACE);
                RegisterHotKey(Handle, HOTKEY_PIN_ID, MOD_PIN, (uint)VK_Q);
                RegisterHotKey(Handle, HOTKEY_ANSWER_ID + 0x100, MOD_NOREPEAT, (uint)VK_F8);
                RegisterHotKey(Handle, HOTKEY_PIN_ID + 0x100, MOD_NOREPEAT, (uint)VK_F7);
                _hotkeysRegistered = true;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }
            base.SetVisibleCore(false);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = (int)m.WParam;
                if (id == HOTKEY_ANSWER_ID || id == HOTKEY_ANSWER_ID + 0x100)
                {
                    AnswerHotkeyPressed?.Invoke();
                    return;
                }
                if (id == HOTKEY_PIN_ID || id == HOTKEY_PIN_ID + 0x100)
                {
                    PinHotkeyPressed?.Invoke();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (_hotkeysRegistered && IsHandleCreated)
            {
                UnregisterHotKey(Handle, HOTKEY_ANSWER_ID);
                UnregisterHotKey(Handle, HOTKEY_PIN_ID);
                UnregisterHotKey(Handle, HOTKEY_ANSWER_ID + 0x100);
                UnregisterHotKey(Handle, HOTKEY_PIN_ID + 0x100);
                _hotkeysRegistered = false;
            }
            base.Dispose(disposing);
        }
    }

    private static byte[] CapturePrimaryScreenPng()
    {
        var bounds = Screen.PrimaryScreen!.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static async Task<string> CallClaudeAsync(byte[]? pinnedPng, byte[] currentPng)
    {
        string currentB64 = Convert.ToBase64String(currentPng);
        var content = new List<object>();

        if (pinnedPng is not null)
        {
            string pinnedB64 = Convert.ToBase64String(pinnedPng);
            content.Add(new { type = "text", text = "PINNED CONTEXT (reference material from an earlier page; do NOT answer any question shown here):" });
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = pinnedB64 }
            });
            content.Add(new { type = "text", text = "CURRENT SCREEN (the question to answer is on this screen; use the pinned context above only as supporting reference):" });
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = currentB64 }
            });
            content.Add(new { type = "text", text = "Answer the question on the CURRENT SCREEN. Output only the answer text." });
        }
        else
        {
            content.Add(new
            {
                type = "image",
                source = new { type = "base64", media_type = "image/png", data = currentB64 }
            });
            content.Add(new { type = "text", text = UserTextSingle });
        }

        var payload = new
        {
            model = Model,
            max_tokens = MaxTokens,
            system = SystemPrompt,
            messages = new[]
            {
                new { role = "user", content = content.ToArray() }
            }
        };

        string json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", ApiKey);
        req.Headers.Add("anthropic-version", ApiVersion);

        try
        {
            using var resp = await Http.SendAsync(req).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return string.Empty;
            string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("content", out var contentEl)) return string.Empty;

            var sb = new StringBuilder();
            foreach (var item in contentEl.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var t) &&
                    t.GetString() == "text" &&
                    item.TryGetProperty("text", out var txt))
                {
                    sb.Append(txt.GetString());
                }
            }
            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void PasteOnUiThread(string text)
    {
        string original = string.Empty;
        try
        {
            if (Clipboard.ContainsText()) original = Clipboard.GetText();
        }
        catch { }

        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            return;
        }

        Thread.Sleep(50);

        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        Thread.Sleep(300);

        try
        {
            if (!string.IsNullOrEmpty(original)) Clipboard.SetText(original);
            else Clipboard.Clear();
        }
        catch { }
    }
}
