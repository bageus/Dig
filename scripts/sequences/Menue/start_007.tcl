+sq_object delete all
do_script change st007_end_5 0.15
do_wait time 2
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set P_End 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_Mitte1	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte1 { 5 0 -1.5 }
sq_pen set P_Mitte2	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte2 { 4.8 0 -3.5 }
sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a P_Start
do_wait
link_obj [Object 1] [Actor 0] 4
sq_wait 0
sq_actor express 0 normal_tired
do_action walktired P_Mitte1 0
do_action rotate front 0
do_action anim scout 0
do_action walktired P_Mitte2 0
do_action rotate 0.7 0
sq_wait none
sq_actor actionlist 0 { { anim sleepside } loop }
sq_actor express 0 good_sleep
do_action anim laydown 0
do_wait time 2
change_particlesource [Actor 0] 1 4 {0 0 0} {0 0 0} 2 1 0 10
set_particlesource [Actor 0] 1 1
do_wait time 1

do_script change st007_end_2 0.25
do_script change st007_end_3 0.33
do_script change st007_end_4 0.50

do_wait time 20
set_particlesource [Actor 0] 1 0
sq_actor express 0 good_awake
do_wait time 1
sq_actor actionlist 0 {}
sq_wait 0
do_action anim sleeptostand 0
do_action rotate front 0
do_action anim stretch 0
do_action walkfit P_Start 0
+sq_object delete all
+do_wait
