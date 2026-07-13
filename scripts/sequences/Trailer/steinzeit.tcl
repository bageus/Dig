sq_wait none
sq_camera fix {99 37 14} 1.3 -0.4 0.5
sq_pen set z0 TriggerPos
sq_pen set z1 z0
sq_pen set z2 z0
sq_pen set z3 z0
sq_pen move z0 {-1.2 0 0.2}
sq_pen move z1 {0 0 1.5}
sq_pen move z2 {0.1 0 -2.3}
sq_pen move z3 {-5 0 -1}
do_action beam {99.4 38.5 5} 3
do_action beam z0 0
do_action beam z1 1
do_action beam z2 2
do_action rotate left 3
do_action rotate right 0
do_action rotate back 1
sq_wait all
do_action rotate TriggerPos 2
sq_pen move z1 {0.5 0 -0.5}
global actors; set_anim [lindex $actors 3] mann.schlafen_boden_loop 0 2
global actors; set_anim [lindex $actors 0] mann.pusten_feuer 0 2
global weiter; set weiter [obj_query this "-class Feuerstelle"];timer_event this feuer -repeat 0 -userid 0 -attime [expr [gettime]+0.7]
global actors; action [lindex $actors 2] anim warmhands {action this anim bend {action this anim hungry {action this anim sitdown {action this anim sitflooreat {action this anim sitflooreat {set_anim this mann.sitzen_boden_stand 0 2}}}}}}
sq_wait 1
set_anim [obj_query this "-class Feuerstelle -limit 1"] feuerstelle.pilz 0 2
sq_camera move {99 37 14} 0.8 -0.4 0.5 0.15
do_action anim workatfire 1
do_action anim workatfire 1
do_action walk z1 1
do_action rotate back 1
do_action anim windstart 1
set_anim [obj_query this "-class Feuerstelle -limit 1"] feuerstelle.pilzdreh 0 2
do_action anim windloop 1
do_action anim windloop 1
do_action anim windloop 1
do_action anim windloop 1
do_action anim windloop 1
set_anim [obj_query this "-class Feuerstelle -limit 1"] feuerstelle.standard 0 2
do_action anim windend 1
global weiter; set weiter 0
sq_pen move z2 {1 0 0}
sq_wait none
do_action walk z2 1
do_action walk z3 2
global actors; set_anim [lindex $actors 0] mann.haende_waermen 0 2
sq_camera fix {102.5 37.5 14} 0.8 -0.2 -0.8
sq_wait 1
do_wait
do_action rotate 0 1
do_action anim sitdown 1
do_action anim sitfloorstill 1
do_wait time 3
do_wait