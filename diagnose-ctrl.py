import ctypes
import json
import time

# Diagnostic reads ONLY the two requested Ctrl keys, for a fixed 45-second window.
user32 = ctypes.WinDLL('user32', use_last_error=True)
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetAsyncKeyState.restype = ctypes.c_short
previous = None
both = 0
print(json.dumps({'phase': 'started', 'durationSeconds': 45, 'keys': ['LeftCtrl', 'RightCtrl']}), flush=True)
end = time.monotonic() + 45
while time.monotonic() < end:
    state = (bool(user32.GetAsyncKeyState(0xA2) & 0x8000), bool(user32.GetAsyncKeyState(0xA3) & 0x8000))
    if state != previous:
        print(json.dumps({'leftCtrl': state[0], 'rightCtrl': state[1]}), flush=True)
        previous = state
    if all(state):
        both += 1
    time.sleep(0.015)
print(json.dumps({'phase': 'finished', 'bothDetected': both > 0, 'bothSamples': both}), flush=True)
