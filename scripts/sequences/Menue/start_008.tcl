+sq_object delete all
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_Zwerg]
sq_pen move P_Start { -5 0 0 }
sq_pen set P_Start_Form 	[Getobjpos Info_Pos_Zwerg]
sq_pen move P_Start_Form 	{ -5 0 0 }
sq_pen set P_End 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set P_Mitte [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Mitte {-13 0 -2}
sq_pen form P_Start_Form RowHorMi 4

sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
sq_object summon Dummy_Muetze_a
do_wait
link_obj [Object 1] [Object 0] 4
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_wait 0
do_action walk P_Mitte 0
do_action rotate P_End 0
sq_wait none
sq_object summon Troll P_End
do_wait time 0.2
sq_actor find Troll
do_wait time 0.2
do_action anim shock 0
do_wait time 1
sq_wait 1
do_action run P_Start_Form 0
do_action run P_Mitte 1
sq_wait none
do_action anim troll.stehen_drohen 1
sq_object summon Troll P_End
sq_actor find Troll
sq_object summon Troll P_End
sq_actor find Troll
do_wait time 0.1
do_action run P_Start_Form 0
do_action run P_Start_Form 2
do_wait time 0.1
sq_wait 3
do_action run P_Start_Form 1
do_action run P_Mitte 3
do_action anim troll.laufen_zu_stolpern 3
do_action anim troll.stolpern_zu_stehen 3
sq_wait all
do_action run P_Start_Form 3
#Abzweigung des Scripts möglich
do_script change st008_end_2 1
+sq_object delete all
do_wait time 2
