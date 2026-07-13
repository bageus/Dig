show_loading yes
obj_clear

call data/GameScripts/oberwelt.tcl

show_loading no

sel /obj
set fr [new FogRemover "" {280 20 10} {0 0 0}]
call_method $fr fog_remove 0 -212 -15
set fr [new FogRemover "" {380 50 10} {0 0 0}]
call_method $fr fog_remove 0 -80 -25

timer_event [obj_query 0 "-class Trigger_Tutorial"] evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+13]
timer_event [obj_query 0 "-class Trigger_Tutorial"] evt_timer1 -repeat 0 -userid 0 -attime [expr [gettime]+18]
call_method [obj_query 0 "-class Trigger_Tutorial"] set_tournament
//timer_event [obj_query 0 "-class Trigger_Tutorial"] selfdestroy -repeat 0 -userid 0 -attime [expr [gettime]+18.5]

//STOPIFNOT TUT
keybind set F6 "s2125"
keybind set F7 "s2130"
keybind set F8 "s2140"
keybind set F9 "s2160"
