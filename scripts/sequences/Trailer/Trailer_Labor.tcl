sq_actor find Zwerg 300 2 3
sq_pen set forschen0 [Getobjpos Info_Pos_Troll 0]
sq_pen set forschen1 forschen0
sq_pen set kamera forschen0
sq_pen move kamera {0 -6 0}
do_action beam forschen0 0
sq_pen move forschen1 {-6 0 4}
do_action beam forschen1 1
sq_camera fix kamera 1.1 0 0
do_wait time 3
sq_camera move 0 1 0 0 0.1
sq_wait all
do_action rotate back 0
do_action rotate right 1
set_anim [obj_query 0 "-class Labor -limit 1"] labor.anim 0 1
sq_wait none
sq_pen move forschen1 {5 0 0}
do_action walk forschen1 1
sq_wait 0
do_action anim work 0
do_action anim walkatfloor 0
do_action anim kontrol 0
do_action anim invent_b 0
do_tooltakeout Reagenzglas 0
sq_wait none
do_action rotate back 1
sq_wait 0
sq_wait 1
set_anim [obj_query 0 "-class Labor -limit 1"] labor.anim 0 1
do_wait time 1
do_action anim lookup 1
sq_pen move kamera {-1 6 0}
sq_camera fix kamera 0.9 -0.3 -0.4
sq_wait all
do_action rotate forschen1 0
do_action rotate forschen0 1
do_action anim talkd 0
sq_pen move forschen0 {-0.3 0 2}
sq_wait 0
do_action walk forschen0 0
sq_wait all
do_action anim talkb 1
sq_camera fix kamera 0.9 0 0
do_action anim takedrugs 0
sq_pen move forschen0 {0.3 0 1}
do_action rotate forschen0 0
do_action anim die 0
sq_wait 1
do_action rotate right 1
do_action anim dontknow 1

do_wait