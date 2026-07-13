+sq_object delete all
do_wait time 3
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_End 	[Getobjpos Info_Pos_ZwergTmp]
#sq_pen move P_Start {-2 0 0}
#sq_pen move P_End {-2 0 0}
sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Zwerg P_End
call_method [Object 1] Editor_Set_Info {{name Fredine} {gender female}}
call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a
sq_object summon Dummy_Muetze_b
do_wait
link_obj [Object 2] [Object 0] 4
link_obj [Object 3] [Object 1] 4
sq_wait none
do_action run P_End 0
do_action run P_Start 1
do_wait time 5.1
do_action wait 0 0
do_action wait 0 1
set_anim [Actor 0] rebound 11 1
set_anim [Actor 1] rebound 11 1
gametime factor 0.3
do_wait time 3
gametime factor 1
sq_wait 1
do_action run P_End 0
do_action run P_Start 1

+sq_object delete all
+gametime factor 1
do_wait time 2
