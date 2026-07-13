foreach item [obj_query 0 "-class Troll"] {set_sequenceactive $item 1}
sq_wait all
sq_pen set Z [Getobjpos Feuerstelle 0]
sq_pen setz Z 14.0
sq_pen set C Z
do_action beam Z 0
sq_pen move Z {8 0 0}
sq_pen move C {4 0 0}
sq_camera fix C 1.2 -0.2 -0.4
sq_wait none
do_action walk Z 0
do_wait time 2
sq_camera follow 0
sq_wait all
do_wait
sq_camera stop
sq_pen move C {5.5 0 0}
sq_camera move C 1.0 -0.2 0.4 0.4
sq_pen move Z {8 0 0}
do_action anim tired 0
do_action anim scratchhead 0
do_action anim invent_done 0
sq_wait none
do_action sneak Z 0
do_wait time 2
sq_pen move C {5 0 0}
sq_camera fix C 0.9 -0.2 -0.8
sq_wait all
do_wait


+foreach item [obj_query 0 "-class Troll"] {set_sequenceactive $item 0}