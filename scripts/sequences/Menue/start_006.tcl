+sq_object delete all
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_Beam 	P_Start
sq_pen move P_Beam	{-5 0 0}
sq_pen set P_Part P_Beam
sq_pen move P_Part {0 -1.5 0}
sq_pen set P_Mitte [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Mitte {-11.2 0 -5.0}
sq_pen set P_Schlaf P_Mitte
sq_pen move P_Schlaf {0.9 0 -1.8}

sq_object summon Zwerg P_Beam
call_method [Object 0] Editor_Set_Info {{name Tarzan} {gender male}}
call_method [Object 0] init
do_wait time 1
#do_particle create 1 P_Part {-0.2 0 0.1} 8 1
do_wait time 0.5
#do_particle create 1 P_Part {-0.2 0 0.1} 8 1
sq_actor find Zwerg
sq_object summon Spitzhacke P_Start
sq_object summon Dummy_Muetze_stein_a P_Start
link_obj [Object 1] [Object 0] 0
link_obj [Object 2] [Object 0] 4
do_wait time 0.1
sq_actor express 0 normal_dizzy
do_action walktired P_Mitte 0
do_wait time 3.0
sq_wait 0
do_wait
#do_particle create 1 P_Part {-0.2 0 0.1} 4 1
do_action rotate 0.9 0
do_action anim scout 0
do_action rotate 5.0 0
do_action anim scout 0
do_action anim toolputaway_a 0
link_obj [Object 1]
sq_object beam 1 P_Start
do_action anim toolputaway_b 0
do_action anim hatofhead 0
link_obj [Object 2] [Object 0] 0
do_action anim hatofhand 0
sq_object summon Zwerg P_Start
call_method [Object 3] Editor_Set_Info {{name Jane} {gender female}}
call_method [Object 3] init
link_obj [Object 1] [Object 3] 0
link_obj [Object 2]
sq_object beam 2 P_Start
do_action anim hatofgone 0
do_action anim tired 0
do_action anim hatongone 0
sq_object summon Dummy_Muetze_a P_Start
link_obj [Object 4] [Object 0] 0
do_action anim hatonhand 0
link_obj [Object 4] [Object 0] 4
+sq_object delete 2
do_action anim hatonhead 0
do_action walktired P_Schlaf 0
do_action rotate 2.4 0
sq_actor find Zwerg
# Abzweigung des Scripts möglich
do_script change st006_end_2 0.4 10
sq_actor express 1 bad_normal
sq_object summon Dummy_Muetze_stein_b P_Start
link_obj [Object 4] [Actor 1] 4
sq_actor actionlist 0 {{anim sleepside} loop}
sq_wait 1
do_action anim laydown 0
change_particlesource [Actor 0] 1 4 {0 0 0} {0 0 0} 2 1 0 10
set_particlesource [Actor 0] 1 1
sq_actor express 0 good_sleep
do_action run P_Mitte 1
do_action rotate 3.9 1
do_action anim jumpb 1
sq_actor actionlist 0 {}
do_action anim kickmachine 1
sq_actor express 0 bad_dizzy
#sq_actor focus 0 1
set_particlesource [Actor 0] 1 0
do_action anim sleeptosit 0
sq_actor setrot 0 0.8
do_action anim impatient 1
do_action anim sitfloorstill 0
do_action anim talkacngc 1
do_action anim standup 0
do_action anim showright 1
sq_wait 0
sq_actor actionlist 1 {{anim talkacnga} {anim talkacpoc} {anim showright} {anim teeter_w} {anim jumpa} {anim talkacpoc} {walk P_Beam}}
do_action rotate 0 1
do_action rotate 1 0
do_action anim teeter_t 0
do_action anim talkrepoa 0
do_action anim talkrentc 0
do_action rotate 5.2 0
do_action anim hatofhead 0
link_obj [Object 3] [Object 0] 0
do_action anim hatofhand 0
link_obj [Object 3]
sq_object beam 3 P_Start
do_action anim hatofgone 0
do_action anim hatongone 0
sq_object summon Dummy_Muetze_stein_a P_Start
link_obj [Object 5] [Object 0] 0
do_action anim hatonhand 0
link_obj [Object 5] [Object 0] 4
sq_object delete 3
do_action anim hatonhead 0
sq_wait all
do_action walktired P_Beam 0
+sq_object delete all
do_wait time 2
