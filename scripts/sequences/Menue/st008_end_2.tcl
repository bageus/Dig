# Alternatives Ende Start_008
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_Zwerg]
sq_pen move P_Start { -5 0 0 }
sq_pen set P_Start_Form 	[Getobjpos Info_Pos_Zwerg]
sq_pen move P_Start_Form 	{ -5 0 0 }
sq_pen set P_End 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set P_Death 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_End_Form P_End
sq_pen set P_Mitte [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Mitte {-13 0 -2}
sq_pen move P_Death {-7 0 -1}
sq_pen form P_Start_Form RowHorMi 4
sq_pen form P_End_Form RowHorMi 3

sq_wait none
do_action run P_End_Form 3
do_wait time 0.2
do_action run P_End_Form 2
sq_wait 1
sq_object summon Axt_3 P_Start
link_obj [Object 5] [Object 0] 0
sq_object summon Schild_3 P_Start
link_obj [Object 6] [Object 0] 1
link_obj [Object 1]
set_pos [Object 1] [Getobjpos Info_Pos_Zwerg]
sq_object summon Dummy_Muetze_kampf_03_a P_Start
link_obj [Object 7] [Object 0] 4
do_action run P_Death 1
do_action rotate left 1
do_action anim troll.stehen_hinten_get_tot 1
do_wait time 1
sq_wait 0
sq_pen set P_Hackschnitzel P_Death
sq_pen move P_Hackschnitzel { -2.5 0 -2 }
sq_pen move P_Death { -2 0 0 }
do_action walk P_Hackschnitzel 0
do_action rotate front 0
sq_actor express good_awake
do_action anim swordupstart 0
do_action anim sworduploop 0
do_action anim sworduploop 0
do_action anim sworduploop 0
do_action anim swordupend 0
do_action rotate right 0
sq_wait none
do_action anim swordtwist 0
do_particle create 8 P_Death {0.1 -0.20 -0.5} 100 1
do_wait time 0.5
do_particle create 8 P_Death {-0.1 -0.25 0 } 100 1
do_wait 0
do_action anim swordtwist 0
do_particle create 8 P_Death {-0.2 -0.30 0} 100 1
do_wait time 0.5
do_particle create 8 P_Death {0.05 -0.20 0 } 100 1
do_wait 0
do_action anim swordtwist 0
do_particle create 8 P_Death {-0.1 -0.40 0} 100 3
do_wait time 0.5
do_particle create 8 P_Death {0.1 -0.35 0 } 100 2
destruct [Actor 1]
sq_object delete 1
do_wait 0
sq_wait 0
do_action run P_End_Form 0
+sq_object delete all
do_wait time 2
