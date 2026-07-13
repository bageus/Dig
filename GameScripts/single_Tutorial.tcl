show_loading yes
obj_clear

call data/GameScripts/oberwelt.tcl

show_loading no

sel /obj
set fr [new FogRemover "" {172 20 10} {0 0 0}]
call_method $fr fog_remove 0 -120 -15
set fr [new FogRemover "" {42 12 10} {0 0 0}]
call_method $fr fog_remove 0 -10 -12
set fr [new FogRemover "" {26 20 10} {0 0 0}]
call_method $fr fog_remove 0 -10 -16
set fr [new FogRemover "" {39 25 10} {0 0 0}]
call_method $fr fog_remove 0 -4 -4
set fr [new FogRemover "" {365 45 10} {0 0 0}]
call_method $fr fog_remove 0 -40 -20
set fr [new FogRemover "" {47 28 15} {0 0 0}]
call_method $fr fog_remove 0 10 10
call_method $fr timer_delete 20
set fr [new FogRemover "" {38 30 15} {0 0 0}]
call_method $fr fog_remove 0 4 4
call_method $fr timer_delete 20

timer_event [obj_query this "-class Trigger_Tutorial"] evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+13]
timer_event [obj_query this "-class Trigger_Tutorial"] evt_timer1 -repeat 0 -userid 0 -attime [expr [gettime]+18]

//# STOPIFNOT TUT
keybind set F6 "s2010"
keybind set F7 "s2020"
keybind set F8 "sdig"
keybind set F9 "s2500"
keybind set F10 "s2015"
keybind set F11 "s2022"
keybind set F12 "sdoor"
