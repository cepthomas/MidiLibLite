# MidiLibLite
Lighter version of MidiLib. Mainly the low level stuff.



No logging, up to host. except...
Errors throw MidiLibException,...

define "sub" => sub-beat + others

other devices:
 - OSCin uses oscin:port for DeviceName
 - OSCout uses oscout:host:port for DeviceName
 - NULL uses nullout:name for DeviceName


