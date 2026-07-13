start_fade 2 0
do_wait time 2
sq_text file Lava
sq_audio open Fenris_300a
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s300a
#-----------------------------------------
sq_actor find Fenris_Stuhl
sq_pen set zurueck 0
sq_pen set wech 0
sq_pen move wech { 50 0 0 }
sq_pen set FenrisPos [Getobjpos Fenris_Stuhl]
sq_pen move FenrisPos { 0.3 0 -1 }
set_visibility [Actor 0] 0
call_method [obj_query 0 -class Fenris_Krug -owner -1] hide
#do_action beam FenrisPos 0
set_roty [Actor 0] 1.8
sq_object summon Zwerg TriggerPos
sq_pen set TrollSource [Getobjpos Info_Pos_Zwerg]
sq_pen set Troll1 TrollSource
sq_pen move Troll1 { 0.7 -0.8 -8.5 }
sq_pen set Troll2 TrollSource
sq_pen move Troll2 { -1.65 -0.8 -11.5 }
sq_pen set Troll3 TrollSource
sq_pen move Troll3 { 0.7 -0.8 -16 }
sq_pen set Troll4 TrollSource
sq_pen move Troll4 { 2.6 -0.8 -16 }
sq_pen set Troll5 TrollSource
sq_pen move Troll5 { 4.5 -0.8 -16 }
sq_pen set Troll6 TrollSource
sq_pen move Troll6 { 2.7 -1.5 -12 }
sq_actor find Zwerg
sq_pen set Text01 FenrisPos
sq_pen move Text01 { -2.7 -3.5 0 }
sq_pen set Text02 TrollSource
sq_pen move Text02 { 3.7 -1.5 -12 }
sq_pen set Text03 TrollSource
sq_pen move Text03 { 4.7 -2.5 -12 }
sq_pen set Cam01 FenrisPos
sq_pen move Cam01 { 0 -4 0 }
sq_pen set Cam02 FenrisPos
sq_pen move Cam02 { -5 -2.5 0 }
sq_pen set Cam04 FenrisPos
sq_pen move Cam04 { -6.0 -2.8 0 }
sq_pen set Cam03 FenrisPos
sq_pen move Cam03 { 0 -4.5 0 }
sq_pen set Cam05 FenrisPos
sq_pen move Cam05 { -3 -3 0 }
sq_camera selset inout
sq_camera fix Cam01 1.3 0 0.2
do_wait
sq_color 2 {255 118 57}
sq_object summon Troll Troll1 6
sq_actor find Troll 50 1 6
sq_actor actionlist 3 { { { anim sit_misc_gape } { anim sit_eatndrink_wipe } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action anim sit_eatndrink_drink 3
set_roty [Actor 3] 3.14
sq_object summon Troll Troll2 6
sq_actor find Troll 50 1 6
sq_actor actionlist 4 { { { anim sit_misc_gape } { anim sit_eatndrink_wipe } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action anim sit_eatndrink_drink 4
set_roty [Actor 4] 4.71
sq_object summon Troll Troll3 6
sq_actor find Troll 50 1 6
sq_actor actionlist 5 { { { anim sit_misc_gape } { anim sit_eatndrink_wipe } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action anim sit_eatndrink_drink 5
set_roty [Actor 5] 0
sq_object summon Troll Troll4 6
sq_actor find Troll 50 1 6
sq_actor actionlist 6 { { { anim sit_misc_gape } { anim sit_eatndrink_wipe } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action anim sit_eatndrink_drink 6
set_roty [Actor 6] 0
sq_object summon Troll Troll5 6
sq_actor find Troll 50 1 6
sq_actor actionlist 7 { { { anim sit_misc_gape } { anim sit_eatndrink_wipe } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action anim sit_eatndrink_drink 7
set_roty [Actor 7] 0
sq_object summon Fenris_001 FenrisPos
sq_actor find Fenris_001
do_wait
set_roty [Actor 8] 1.8
set_visibility [Actor 2] 0
sq_camera move Cam01 0.8 0 0.2 0.2
start_fade 2 1
sq_wait none
set_anim [Actor 8] sitdown 0 1
do_wait time 1.5
-sq_screenvibe steps
do_wait time 1.0
call_method [Actor 8] burnbaby
sq_wait none
do_text 300aa 2 NoAnim 300aa Auto Off
do_action anim sittalkb 8
do_wait time 2
sq_camera fix Cam02 0.8 -0.2 -0.71
do_action anim 36chef 6
do_wait time 0.1
do_action anim 36chef 7
do_wait time 0.3
do_action anim 36chef 5
do_wait time 1
sq_wait 2
sq_actor actionlist 8 { { anim sittalk b } loop }
do_action anim sittalkc 8
do_text 300ab 2 NoAnim 300ab Auto Off
sq_actor actionlist 6 {}
sq_wait 2
do_action anim 36chef 5
do_wait time 1
#do_action beam Text02 2
do_text 300ac 2 NoAnim 300ah Auto Off
change_particlesource [Actor 8] 20 34 {0 0 0} {0 0 0} 4096 16 0 2 0 1 0
do_action anim sitstandloop 8
do_wait time 0.6
sq_camera fix Cam01 0.8 0.1 0.4
sq_actor actionlist 8 {}
sq_wait 2
do_wait time 0.7
do_action anim sittalkc 8

do_text 300ad 2 NoAnim 300ac Auto Off
sq_camera move Cam01 1.2 0.1 0.4 0.4
sq_wait none
do_text 300ae 2 NoAnim 300ad Auto Off
sq_actor actionlist 8 { { anim sithairloop } { anim sithairloop } }
do_action anim sithairstart 8
do_wait time 0.8
do_wait time 3.8
set_anim [Actor 6] 300firestart 0 0
do_action beam Troll6 6
sq_camera fix Cam04 0.65 -0.2 -0.71
sq_wait none
do_action anim sithairloop 8
change_particlesource [Actor 6] 1 35 {0 0 0} {0.8 -0.2 -0.32} 2048 6 0 13
change_particlesource [Actor 8] 21 35 {0 0 0} {0 0 0} 2048 32 0 2 0 1 1
do_action anim 300firestart 6
do_wait time 2.4
set_particlesource [Actor 6] 1 1
sq_wait none
do_action anim 300fireloop 6
set_particlesource [Actor 8] 21 1
do_wait time 0.6
sq_camera fix Cam01 1.2 0 0.6 0.2
do_action anim sithairloop 8
gametime factor 0.5
do_wait time 1.5
sq_actor actionlist 8 {}
sq_wait none
+gametime factor 1
do_action anim 300giggle 5
sq_wait 8
set_particlesource [Actor 8] 20 0
do_action anim sithairstop 8
change_particlesource [Actor 8] 22 35 {0 0 0} {0 0 0} 2048 32 0 2 5 1 0
set_particlesource [Actor 8] 21 1
set_particlesource [Actor 8] 22 1
do_text 300ag 2 NoAnim 300ae Auto Off
do_action anim sithairstoploop 8
sq_camera move Cam01 0.8 0 0.6 0.2
do_action anim sithairstoploop 8
start_fade 3 0
do_action anim sithairstoploop 8
do_action anim sithairstoploop 8
do_action anim sithairstoploop 8
+sq_object delete all
+do_action beam zurueck 0
+set_visibility [Actor 0] 1
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow kathedrale
#-----------------------------------------
+call_method [obj_query 0 -class Fenris_Krug -owner -1] show
+do_wait
+cancel_fade



