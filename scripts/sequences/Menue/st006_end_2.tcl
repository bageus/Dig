# End-Variante 2 von start_006
sq_actor express 1 good_awake
sq_object summon Dummy_Muetze_b P_Start
link_obj [Object 4] [Actor 1] 4
+sq_object delete 1
sq_actor actionlist 0 {{anim sleepside} loop}
sq_wait 1
do_action anim laydown 0
change_particlesource [Actor 0] 1 4 {0 0 0} {0 0 0} 2 1 0 10
set_particlesource [Actor 0] 1 1
sq_actor express 0 good_sleep
do_action run P_Mitte 1
do_action rotate 4.2 1
do_action anim jumpb 1
sq_actor actionlist 0 {}
do_action anim kickmachine 1
do_action anim standloopa 1
do_action anim scratch 1
do_action anim cheer 1
do_action anim talkacngc 1
do_action anim teeter_w 1
sq_pen set P_Wecker P_Schlaf
sq_pen move P_Wecker {-0.7 0 0.1}
do_action walk P_Wecker 1
do_action rotate 4.3 1
do_action anim put 1
do_action rotate 5.1 1
do_action anim talkrengc 1
do_action run P_Beam 1
sq_wait none
do_wait time 3
sq_pen move P_Wecker {0.77 -0.43 -1.64}
sq_object summon Wecker P_Wecker
set_roty [Object 4] 0.08
sq_pen set P_WR P_Wecker
sq_pen set P_WL P_Wecker
sq_pen move P_WR {0.1 0 0}
sq_pen move P_WL {-0.1 0 0}
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
set_particlesource [Actor 0] 1 0
sq_actor eyes 0 {2}
sq_actor mouth 0 {2}
do_action anim sleeptostand 0
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
do_action anim shock 0
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_WL
do_wait
sq_object beam 4 P_WR
do_wait
sq_object beam 4 P_Wecker
sq_wait 0
do_wait
sq_object beam 4 P_Start
sq_pen move P_Mitte {0.2 0 0.2}
sq_pen set P_M2 P_Mitte
sq_pen set P_M3 P_Mitte
sq_pen set P_M4 P_Mitte
sq_pen set P_M5 P_Mitte
sq_pen set P_M6 P_Mitte
sq_pen set P_M7 P_Mitte
sq_pen move P_M2 {-0.9 0 0.8}
sq_pen move P_M3 {0.2 0 2.0}
sq_pen move P_M4 {0.9 0 1.2}
sq_pen move P_M5 {1.2 0 2.8}
sq_pen move P_M6 {2.0 0 2.2}
sq_pen move P_M7 {2.4 0 3.5}
sq_actor setrot 0 P_M2
do_action panicflee P_M2 0
sq_actor setrot 0 P_M3
do_action panicflee P_M3 0
sq_actor setrot 0 P_M4
do_action panicflee P_M4 0
sq_actor setrot 0 P_M2
do_action panicflee P_M2 0
sq_actor setrot 0 P_Mitte
do_action panicflee P_Mitte 0
sq_actor setrot 0 P_M3
do_action panicflee P_M3 0
sq_actor setrot 0 P_M2
do_action panicflee P_M2 0
sq_actor setrot 0 P_M4
do_action panicflee P_M4 0
sq_actor setrot 0 P_M5
do_action panicflee P_M5 0
sq_actor setrot 0 P_M6
do_action panicflee P_M6 0
sq_actor setrot 0 P_M7
do_action panicflee P_M7 0
sq_actor setrot 0 P_Beam
do_action flee P_Beam 0
+sq_object delete all
do_wait time 2
