// PrePoMax for macOS - the Windows API functions the shared PrePoMax code imports from kernel32.dll and user32.dll. The
// library is installed under the names .NET looks for (libkernel32.dll.dylib, libuser32.dll.dylib, libuser32.dylib). The
// window functions call back into the WinForms compatibility layer, which registers its callbacks at startup; the console,
// job object and hook functions report that nothing was done, which the callers treat as running without them.

#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <pthread.h>
#include <signal.h>
#include <unistd.h>
#include <dlfcn.h>
#include <stdlib.h>

typedef intptr_t (*SendMessageCallback)(intptr_t hwnd, int msg, intptr_t wParam, intptr_t lParam);
typedef int (*IntCallback3)(intptr_t a, int b, int c);
typedef intptr_t (*WindowFromPointCallback)(int x, int y);
typedef int (*WindowTextCallback)(intptr_t hwnd, uint16_t *buffer, int max);
typedef int (*BoolCallback1)(intptr_t a);
typedef int (*EnumWindowsCallback)(intptr_t enumProc, intptr_t lParam);
typedef int (*KeyboardStateCallback)(uint8_t *state);

static SendMessageCallback _sendMessage;
static SendMessageCallback _postMessage;
static IntCallback3 _getScrollPos;          // hwnd, bar, unused
static IntCallback3 _setScrollPos;          // hwnd, bar, pos
static WindowFromPointCallback _windowFromPoint;
static WindowTextCallback _getWindowText;
static BoolCallback1 _isWindowVisible;
static EnumWindowsCallback _enumThreadWindows;
static KeyboardStateCallback _getKeyboardState;

// Registration by the WinForms compatibility layer
void Win32Compat_SetCallbacks(SendMessageCallback sendMessage, SendMessageCallback postMessage,
                              IntCallback3 getScrollPos, IntCallback3 setScrollPos,
                              WindowFromPointCallback windowFromPoint, WindowTextCallback getWindowText,
                              BoolCallback1 isWindowVisible, EnumWindowsCallback enumThreadWindows,
                              KeyboardStateCallback getKeyboardState)
{
    _sendMessage = sendMessage;
    _postMessage = postMessage;
    _getScrollPos = getScrollPos;
    _setScrollPos = setScrollPos;
    _windowFromPoint = windowFromPoint;
    _getWindowText = getWindowText;
    _isWindowVisible = isWindowVisible;
    _enumThreadWindows = enumThreadWindows;
    _getKeyboardState = getKeyboardState;
}

// kernel32 ------------------------------------------------------------------------------------------------------------
// The standard handles exist: the console of the terminal or the redirection of the launcher
intptr_t GetStdHandle(int nStdHandle) { return nStdHandle == -10 ? 1 : nStdHandle == -11 ? 2 : nStdHandle == -12 ? 3 : 0; }
int SetStdHandle(int nStdHandle, intptr_t handle) { return 1; }
int AttachConsole(int processId) { return 0; }
int FreeConsole(void) { return 1; }
intptr_t GetConsoleWindow(void) { return 0; }
int SetConsoleCtrlHandler(intptr_t handler, int add) { return 1; }
intptr_t GetCurrentProcess(void) { return (intptr_t)-1; }
int TerminateProcess(intptr_t process, uint32_t exitCode)
{
    if (process == (intptr_t)-1) _exit((int)exitCode);
    return 0;
}
int GetCurrentThreadId(void)
{
    uint64_t tid = 0;
    pthread_threadid_np(NULL, &tid);
    return (int)tid;
}
intptr_t GetModuleHandleW(const uint16_t *name) { return 1; }
intptr_t GetModuleHandle(const uint16_t *name) { return 1; }
intptr_t GetModuleHandleA(const char *name) { return 1; }
intptr_t LoadLibrary(const char *fileName) { return (intptr_t)dlopen(fileName, RTLD_NOW); }
intptr_t LoadLibraryA(const char *fileName) { return (intptr_t)dlopen(fileName, RTLD_NOW); }
// The long path is the short path
uint32_t GetShortPathName(const uint16_t *longPath, uint16_t *shortPath, uint32_t length)
{
    uint32_t n = 0;
    while (longPath[n]) n++;
    if (shortPath != NULL && length > n)
    {
        memcpy(shortPath, longPath, (n + 1) * sizeof(uint16_t));
        return n;
    }
    return n + 1;
}
uint32_t GetShortPathNameW(const uint16_t *longPath, uint16_t *shortPath, uint32_t length)
{
    return GetShortPathName(longPath, shortPath, length);
}
// Job objects: the child processes are ended by the job code of PrePoMax on macOS
intptr_t CreateJobObject(intptr_t attributes, const uint16_t *name) { return 0; }
intptr_t CreateJobObjectW(intptr_t attributes, const uint16_t *name) { return 0; }
int SetInformationJobObject(intptr_t job, int infoClass, intptr_t info, uint32_t length) { return 0; }
int AssignProcessToJobObject(intptr_t job, intptr_t process) { return 0; }
int CloseHandle(intptr_t handle) { return 1; }

// user32 --------------------------------------------------------------------------------------------------------------
intptr_t SendMessage(intptr_t hwnd, int msg, intptr_t wParam, intptr_t lParam)
{
    return _sendMessage != NULL ? _sendMessage(hwnd, msg, wParam, lParam) : 0;
}
intptr_t SendMessageW(intptr_t hwnd, int msg, intptr_t wParam, intptr_t lParam) { return SendMessage(hwnd, msg, wParam, lParam); }
int PostMessage(intptr_t hwnd, uint32_t msg, intptr_t wParam, intptr_t lParam)
{
    if (_postMessage == NULL) return 0;
    _postMessage(hwnd, (int)msg, wParam, lParam);
    return 1;
}
int PostMessageW(intptr_t hwnd, uint32_t msg, intptr_t wParam, intptr_t lParam) { return PostMessage(hwnd, msg, wParam, lParam); }
int GetScrollPos(intptr_t hwnd, int bar) { return _getScrollPos != NULL ? _getScrollPos(hwnd, bar, 0) : 0; }
int SetScrollPos(intptr_t hwnd, int bar, int pos, int redraw) { return _setScrollPos != NULL ? _setScrollPos(hwnd, bar, pos) : 0; }
// SCROLLINFO: cbSize, fMask, nMin, nMax, nPage, nPos, nTrackPos
int GetScrollInfo(intptr_t hwnd, int bar, int32_t *info)
{
    if (info == NULL) return 0;
    int pos = GetScrollPos(hwnd, bar);
    info[2] = 0;
    info[5] = pos;
    info[6] = pos;
    return 1;
}
intptr_t WindowFromPoint(int64_t point)
{
    int x = (int)(point & 0xFFFFFFFF), y = (int)(point >> 32);
    return _windowFromPoint != NULL ? _windowFromPoint(x, y) : 0;
}
int GetWindowText(intptr_t hwnd, uint16_t *text, int max) { return _getWindowText != NULL ? _getWindowText(hwnd, text, max) : 0; }
int GetWindowTextW(intptr_t hwnd, uint16_t *text, int max) { return GetWindowText(hwnd, text, max); }
int GetWindowTextLength(intptr_t hwnd) { return _getWindowText != NULL ? _getWindowText(hwnd, NULL, 0) : 0; }
int SetWindowText(intptr_t hwnd, const uint16_t *text) { return 0; }
int IsWindowVisible(intptr_t hwnd) { return _isWindowVisible != NULL ? _isWindowVisible(hwnd) : 0; }
int EnumThreadWindows(int threadId, intptr_t callback, intptr_t lParam)
{
    return _enumThreadWindows != NULL ? _enumThreadWindows(callback, lParam) : 1;
}
int EnumChildWindows(intptr_t parent, intptr_t callback, intptr_t lParam) { return 1; }
int GetClassName(intptr_t hwnd, uint16_t *buffer, int max)
{
    // Every window of the application is a WinForms window
    const char *name = "WindowsForms10.Window";
    int n = (int)strlen(name);
    if (buffer == NULL || max <= 0) return 0;
    if (n > max - 1) n = max - 1;
    for (int i = 0; i < n; i++) buffer[i] = (uint16_t)name[i];
    buffer[n] = 0;
    return n;
}
int GetClassNameW(intptr_t hwnd, uint16_t *buffer, int max) { return GetClassName(hwnd, buffer, max); }
int GetWindowRect(intptr_t hwnd, int32_t *rect) { if (rect) memset(rect, 0, 16); return 0; }
int MoveWindow(intptr_t hwnd, int x, int y, int w, int h, int repaint) { return 0; }
int RedrawWindow(intptr_t hwnd, intptr_t rect, intptr_t region, uint32_t flags)
{
    // RDW_INVALIDATE repaints the control
    if (_sendMessage != NULL) _sendMessage(hwnd, 0x000F, 0, 0);
    return 1;
}
int GetDlgCtrlID(intptr_t hwnd) { return 0; }
intptr_t GetDlgItem(intptr_t dialog, int id) { return 0; }
int EndDialog(intptr_t dialog, intptr_t result) { return 0; }
// Hooks are not installed; the callers fall back to the events of the controls
intptr_t SetWindowsHookEx(int idHook, intptr_t proc, intptr_t module, int threadId) { return 0; }
intptr_t SetWindowsHookExW(int idHook, intptr_t proc, intptr_t module, int threadId) { return 0; }
int UnhookWindowsHookEx(intptr_t hook) { return 1; }
intptr_t CallNextHookEx(intptr_t hook, int code, intptr_t wParam, intptr_t lParam) { return 0; }
int GetKeyboardState(uint8_t *state)
{
    if (state == NULL) return 0;
    memset(state, 0, 256);
    if (_getKeyboardState != NULL) _getKeyboardState(state);
    return 1;
}
uint32_t MapVirtualKey(uint32_t code, uint32_t mapType) { return code; }
uint32_t MapVirtualKeyW(uint32_t code, uint32_t mapType) { return code; }
// The character of a virtual key with the shift state of the keyboard state, for the keys of a US layout
int ToUnicode(uint32_t vk, uint32_t scanCode, const uint8_t *state, uint16_t *buffer, int size, uint32_t flags)
{
    if (buffer == NULL || size < 1) return 0;
    int shift = state != NULL && (state[0x10] & 0x80);
    uint16_t c = 0;
    if (vk >= 'A' && vk <= 'Z') c = shift ? vk : vk + 32;
    else if (vk >= '0' && vk <= '9')
    {
        const char *shifted = ")!@#$%^&*(";
        c = shift ? (uint16_t)shifted[vk - '0'] : (uint16_t)vk;
    }
    else if (vk == 0x20) c = ' ';
    else if (vk >= 0x60 && vk <= 0x69) c = (uint16_t)('0' + vk - 0x60);
    else
    {
        switch (vk)
        {
            case 0xBA: c = shift ? ':' : ';'; break;
            case 0xBB: c = shift ? '+' : '='; break;
            case 0xBC: c = shift ? '<' : ','; break;
            case 0xBD: c = shift ? '_' : '-'; break;
            case 0xBE: c = shift ? '>' : '.'; break;
            case 0xBF: c = shift ? '?' : '/'; break;
            case 0xC0: c = shift ? '~' : '`'; break;
            case 0xDB: c = shift ? '{' : '['; break;
            case 0xDC: c = shift ? '|' : '\\'; break;
            case 0xDD: c = shift ? '}' : ']'; break;
            case 0xDE: c = shift ? '"' : '\''; break;
            case 0x6A: c = '*'; break;
            case 0x6B: c = '+'; break;
            case 0x6D: c = '-'; break;
            case 0x6E: c = '.'; break;
            case 0x6F: c = '/'; break;
        }
    }
    if (c == 0) return 0;
    buffer[0] = c;
    return 1;
}
int ToUnicodeEx(uint32_t vk, uint32_t scanCode, const uint8_t *state, uint16_t *buffer, int size, uint32_t flags, intptr_t layout)
{
    return ToUnicode(vk, scanCode, state, buffer, size, flags);
}
