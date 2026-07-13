+sq_object delete all
do_wait time 2
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set P_End 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_Mitte1	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte1 { 5 0 -2.5 }
sq_pen set P_Mitte2	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte2 { 6.5 0 -3 }
sq_pen set P_Mitte3	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte3 { 10 0 -2.5 }
sq_pen set P_Mitte4	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte4 { 8.2 0 0 }
sq_pen setz P_Mitte4 0.5
sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Zwerg P_Start
call_method [Object 1] Editor_Set_Info {{name Fredine} {gender female}}
call_method [Object 1] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a P_Start
sq_object summon Dummy_Muetze_b P_Start
do_wait
link_obj [Object 2] [Actor 0] 4
link_obj [Object 3] [Actor 1] 4
do_wait time 1
sq_actor express 0 bad_normal

sq_wait none
sq_actor actionlist 1 { { anim standloopa } { anim standloopb } { anim standloopc } { rotate 0 } { anim talkrentb } { anim talkrengb } { rotate P_End } { anim talkacngb } { walkfit P_End } }
do_action walkfit P_Mitte2 1
do_wait time 5
do_action flee P_Mitte1 0
sq_wait 0
do_wait
do_action rotate 1 0
do_action anim talkacnta 0
do_action anim talkacpoa 0
do_action anim talkacpob 0
do_action anim talkacntc 0
do_action anim talkrepob 0
do_action anim talkacpob 0
sq_actor express 0 bad_dizzy
do_wait time 3
do_action flee P_Mitte3 0
do_action rotate 1 0
sq_actor actionlist 1 { { anim talkacnga } { walkfit P_End } }
do_action rotate 0 1
do_action anim standloopa 0
do_action anim standloopb 0
do_action anim scratchhead 0
do_action anim standloopa 0
do_action anim teeter_t 0
do_action walktired P_Mitte4 0
do_action wait 0.5 0
do_action rotate front 0
do_action wait 1 0
do_action anim wipenose 0
do_action rotate back 0
do_script change st009_end_2 0.3
do_action wait 2 0
do_action rotate front 0
do_action walkfit P_Mitte3 0
do_action rotate P_End 0
do_action anim talkacnta 0
do_action anim talkacngb 0
do_action anim warmbutt 0
do_action walkfit P_Start 0
+sq_object delete all
+do_wait

