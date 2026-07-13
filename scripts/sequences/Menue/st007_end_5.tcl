do_wait time 2
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen set P_Mitte1	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte1 { 5 0 -1.5 }
sq_pen set P_Mitte2	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte2 { 4.8 0 -3.5 }

sq_pen set P_Mitte3	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte3 { 17.028 0 0 }
sq_pen set P_Mitte4	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte4 { 12.028 0 -1.5 }
sq_pen set P_Mitte5	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte5 { 12.228 0 -3.5 }

sq_pen set P_Mitte6	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte6 { 8 0 -3.5 }
sq_pen set P_Mitte7	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Mitte7 { 9 0 -3.5 }

sq_object summon Zwerg P_Start
call_method [Object 0] Editor_Set_Info {{name Fred} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a P_Start
do_wait
link_obj [Object 1] [Actor 0] 4
sq_object summon Zwerg P_Mitte3
call_method [Object 0] Editor_Set_Info {{name Fred_der_Klon} {gender male}}
call_method [Object 0] init
do_wait time 0.2
sq_actor find Zwerg
do_wait time 0.1
sq_object summon Dummy_Muetze_a P_Start
do_wait
link_obj [Object 3] [Actor 1] 4
sq_wait 1
sq_actor express { 0 1 } normal_tired
do_action walktired P_Mitte1 0
do_action walktired P_Mitte4 1
do_action rotate front { 0 1 }
do_action anim scout { 0 1 }
do_action walktired P_Mitte2 0
do_action walktired P_Mitte5 1
do_action rotate 0.7 0
do_action rotate 5.4 1
sq_wait none
sq_actor actionlist { 0 1 } { { anim sleepside } loop }
sq_actor express { 0 1 } good_sleep
do_action anim laydown { 0 1 }
do_wait time 2
change_particlesource [Actor 0] 1 4 {0 0 0} {0 0 0} 2 1 0 10
set_particlesource [Actor 0] 1 1
change_particlesource [Actor 1] 2 4 {0 0 0} {0 0 0} 2 1 0 10
set_particlesource [Actor 1] 2 1
do_wait time 1
do_wait time 10
set_particlesource [Actor 0] 1 0
set_particlesource [Actor 1] 2 0
sq_actor express { 0 1 } good_awake
do_wait time 1
sq_actor actionlist { 0 1 } {}
sq_wait 1
do_action anim sleeptostand { 0 1 }
do_action rotate front { 0 1 }
do_action anim stretch { 0 1 }
do_action wait 0.5 { 0 1 }
do_action rotate 1 0
do_action rotate 0 1
do_action wait 1 { 0 1 }
do_action anim scratchhead { 0 1 }
do_action walk P_Mitte6 0
do_action walk P_Mitte7 1
do_action wait 1 { 0 1 }
do_action anim jumpa { 0 1 }
do_action wait 1 { 0 1 }
do_action anim scratchhead { 0 1 }
do_action anim jumpb { 0 1 }
do_action rotate front { 0 1 }
do_action rotate left 0
do_action rotate right 1
do_action anim dontknow { 0 1 }
do_action walk P_Start 0
do_action walk P_Mitte3 1
+sq_object delete all
+do_wait
