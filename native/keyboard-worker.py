"""Local Ctrl-only worker: no hooks, no text/other keys, no files, no network."""
import ctypes
import os
import sys
import threading
import time

user32 = ctypes.WinDLL('user32', use_last_error=True)
kernel32 = ctypes.WinDLL('kernel32', use_last_error=True)
user32.GetAsyncKeyState.argtypes = [ctypes.c_int]
user32.GetAsyncKeyState.restype = ctypes.c_short
user32.keybd_event.argtypes = [ctypes.c_ubyte, ctypes.c_ubyte, ctypes.c_uint, ctypes.c_size_t]
kernel32.OpenProcess.argtypes = [ctypes.c_uint, ctypes.c_bool, ctypes.c_uint]
kernel32.OpenProcess.restype = ctypes.c_void_p
kernel32.WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_uint]
kernel32.WaitForSingleObject.restype = ctypes.c_uint
kernel32.CloseHandle.argtypes = [ctypes.c_void_p]

parent = kernel32.OpenProcess(0x00100000, False, int(sys.argv[1]))
if not parent:
    raise OSError(ctypes.get_last_error(), 'Cannot retain screenshot parent')
probe = '--probe' in sys.argv[2:]
stop = threading.Event()
threading.Thread(target=lambda: (sys.stdin.buffer.read(), stop.set()), daemon=True).start()
print('READY', flush=True)
start = time.monotonic()
latched = False
pressed = released = False
samples = paired = 0
reported = False
try:
    while not stop.wait(0.015):
        if kernel32.WaitForSingleObject(parent, 0) != 258:
            break
        elapsed = time.monotonic() - start
        if probe and not pressed and elapsed > 0.25:
            user32.keybd_event(0xA2, 0x1D, 0, 0)
            user32.keybd_event(0xA3, 0x1D, 1, 0)
            pressed = True
        if probe and pressed and not released and elapsed > 0.55:
            user32.keybd_event(0xA2, 0x1D, 2, 0)
            user32.keybd_event(0xA3, 0x1D, 3, 0)
            released = True
        samples += 1
        if probe and elapsed > 1 and not reported:
            print('PROBE samples=' + str(samples) + ' paired=' + str(paired), flush=True)
            reported = True
        left = bool(user32.GetAsyncKeyState(0xA2) & 0x8000)
        right = bool(user32.GetAsyncKeyState(0xA3) & 0x8000)
        if left and right:
            paired += 1
        if not left and not right:
            latched = False
        if left and right and not latched:
            latched = True
            print('TRIGGER', flush=True)
finally:
    if probe:
        user32.keybd_event(0xA2, 0x1D, 2, 0)
        user32.keybd_event(0xA3, 0x1D, 3, 0)
    kernel32.CloseHandle(parent)
