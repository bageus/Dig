sq_text file Urwald
sq_pen set C1 	[Getobjpos Info_Pos_Zwerg]
sq_pen move C1 {-13 0 -4}
sq_camera fix C1 1.38 -0.355 0.165
sq_pen set P_Start 	[Getobjpos Info_Pos_Zwerg]
sq_pen set P_End 	[Getobjpos Info_Pos_ZwergTmp]
sq_pen move P_Start { -5 0 0 }

sq_object summon Zwerg P_Start
call_method [Object 0] init
sq_actor find Zwerg

sq_object summon Zwerg P_End 4
call_method [Object 1] init
sq_actor find Zwerg

sq_object summon Taucherglocke
sq_object summon Dummy_Muetze_a
sq_object summon Dummy_Muetze_arbeitslos_a
do_wait
link_obj [Object 3] [Object 0] 4
link_obj [Object 4] [Object 1] 4
sq_pen set P_Mitte_r [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Mitte_r {-16 0 -2}

sq_pen set P_Kampf_r [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Kampf_r {-14.5 0 -2.5}

sq_pen set P_Mitte_l [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Mitte_l {-12 0 -3}

sq_pen set P_Kampf_l [Getobjpos Info_Pos_Zwerg]
sq_pen move P_Kampf_l {-12.5 0 -2.5}

sq_wait none
do_action walk P_Mitte_l 1
do_action walk P_Mitte_r 0
do_wait time 0.5
do_action walk P_Start 0
do_action walk P_End 1

do_action walk P_Mitte_l 1
do_wait time 1.5
sq_wait 1
do_action walk P_Mitte_r 0
do_action anim hatongone 1
call_method [Object 2] let_it_look_like_darth_vaders
link_obj [Object 2] [Actor 1] 0
do_action anim hatonhand 1
link_obj [Object 2] [Actor 1] 4
do_action anim hatonhead 1
do_action rotate 0 1
sq_wait none
do_text 005ma 1 Auto Auto {70 30}
sq_wait 0
do_action anim shock 0
do_action rotate 1 0
sq_wait 1
do_text 005mb 1 Auto Auto {70 30}
do_action anim tooltakeout_a 1
sq_object summon Lichtschwert
set_anim [Object 3] lichtschwert.drinne 0 0
link_obj [Object 3] [Actor 1] 0
do_action anim tooltakeout_b 1
do_action anim standtotwohand 1

sq_wait 0
set_anim [Object 3] lichtschwert.raus 0 1
sq_actor actionlist 1 { { anim twohandstillani } loop }
do_action anim twohandstillani 1
do_wait time 0.6
set_anim [Object 3] lichtschwert.standard 0 0


do_action anim tooltakeout_a 0
sq_object summon Lichtschwert
set_anim [Object 4] lichtschwert.drinne 0 0
link_obj [Object 4] [Actor 0] 0
do_action anim tooltakeout_b 0
do_action anim standtotwohand 0
sq_wait none
do_action anim twohandstillani 0
set_anim [Object 4] lichtschwert.raus 0 1
do_wait time 0.6
do_action anim twohandstillani 0
set_anim [Object 4] lichtschwert.standard 0 0
sq_actor actionlist 1 { {rotate P_Kampf_r} }
sq_actor actionlist 0 { {rotate P_Kampf_l} }
sq_wait none
do_action run P_Kampf_l 1
sq_wait 0
do_action run P_Kampf_r 0
sq_actor actionlist 1 {{anim swordmidstroke} {anim swordjump} {anim standtosword} {anim swordheadstroke} {anim swordmiddlehitm} {anim swordheadstab} {anim swordback} }
sq_actor actionlist 0 {{anim swordmiddleblo} {anim swordbotstab} {anim standtosword} {anim swordduck} {anim swordmidstab} {anim swordside} {anim swordmasterstroke} }
sq_wait 1
do_action anim standtosword {0 1}
sq_wait 0
do_action flee P_Start 1
do_wait time 2.5

sq_actor actionlist 0 {}
sq_wait 0
do_action rotate P_Start 0
do_action anim swordtostand 0
do_action anim dontknow 0
do_action anim standtotwohand 0
sq_wait none
do_action anim twohandstillani 0
set_anim [Object 4] lichtschwert.rein 0 1
do_wait time 0.8
set_anim [Object 4] lichtschwert.drinne 0 0
sq_wait 0
do_action anim twohandtostand 0
do_action anim toolputaway_a 0
link_obj [Object 4]
global P_Start;set_pos [Object 4] $P_Start
sq_object delete 4
do_action anim toolputaway_b 0


do_action walk P_End 0
+sq_object delete all
