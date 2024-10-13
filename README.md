Youtube video with Logitech G920 working with Force Feedback (FFB): https://www.youtube.com/watch?v=pIR5bgpmE7E

Logitech G920 FreePIE script: https://gist.github.com/cyberluke/5ad828d52a3726dc9bf8fcc36ca17c17

- Requires latest VJoy driver from this fork: https://github.com/njz3/vJoy/
- Fixed SlimDX Ramp Effect
- All effects working & tested on Novalogic Flight simulators and Logitech G940
- Added Logitech G940 LED interface
- Tested on Windows 10

**Important info if GUI won't appear**:
  When downloading ZIP in Windows from website, it will lock the dlls and you need to right click them -> Properties -> and there is "Unblock this dll" checkbox. I don't know why Windows keeps doing it.

FreePIE with extended API & FFB fixed 
=====================================

Programmable Input Emulator
 
Latest downloadable installer can be found [here](https://github.com/cyberluke/FreePIE/releases/tag/2.0-FFB)

[Please visit wiki or scripting and plugin reference manual](https://github.com/AndersMalmgren/FreePIE/wiki)

**FreePIE** (Programmable Input Emulator) is a application for bridging and emulating input devices. It has applications primarily in video gaming but can also be used for VR interaction, remote control, and other applications. A typical application might be controlling the mouse in a PC game using a Wiimote. Device control schemes are customized for specific applications by executing scripts from the FreePIE GUI. The script language is based on the **Python** syntax and offers non-programmers an easy way to interface devices.

FreePIE is very similar to the popular utility GlovePIE, but encourages open development and integration with any device. The software is designed to allow third party developers to add their own I/O plugins either through direct integration into the core library or through a separately compiled plugin mechanism.

FreePIE is licensed under GPLv2 
