using System;
using System.Runtime.InteropServices;
using System.Threading;
class KeyboardProbe {
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
 static void Main() {
  Console.WriteLine("start "+GetAsyncKeyState(0xA2)+" "+GetAsyncKeyState(0xA3));
  keybd_event(0xA2,0x1D,0,UIntPtr.Zero);keybd_event(0xA3,0x1D,1,UIntPtr.Zero);
  Thread.Sleep(100);Console.WriteLine("during "+GetAsyncKeyState(0xA2)+" "+GetAsyncKeyState(0xA3));
  keybd_event(0xA2,0x1D,2,UIntPtr.Zero);keybd_event(0xA3,0x1D,3,UIntPtr.Zero);
 }
}
