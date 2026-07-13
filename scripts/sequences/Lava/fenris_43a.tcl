sound play equake7 1
sq_screenvibe equake7
adaptive_sound changetheme atmolava
do_wait time 5
start_fade 2 0
do_wait time 2
sq_color 0 {255 118 57}
sq_audio open Fenris_36
sq_text file Urwald
set_anim [Actor 0] sitstandloop 0 2
+sq_pen set zurueck [Getobjpos Fenris_001]
sq_pen set wech [Getobjpos Fenris_Stuhl]
sq_pen move wech { 30 0 0 }
sq_pen set Stuhl [Getobjpos Fenris_Stuhl]
do_action beam Stuhl 0
sq_pen set TrollSource [Getobjpos Info_Pos_Zwerg]
sq_pen set Troll1 TrollSource
sq_pen move Troll1 { 0.7 -0.8 -8.5 }
sq_pen set Troll2 TrollSource
sq_pen move Troll2 { -1.65 -0.8 -11.5 }
sq_pen set Troll3 TrollSource
sq_pen move Troll3 { 0.7 -0.8 -15.5 }
sq_pen set Troll4 TrollSource
sq_pen move Troll4 { 2.6 -0.8 -15.5 }
sq_pen set Troll5 TrollSource
sq_pen move Troll5 { 4.5 -0.8 -15.5 }
sq_pen set Text01 TrollSource
sq_pen move Text01 { 4 -3.5 -11.5 }
sq_pen set Text02 TrollSource
sq_pen move Text02 { 10 -3.5 -11.5 }
sq_pen set Text03 TrollSource
sq_pen move Text03 { 8 -2.5 -8.5 }
sq_pen set Text04 TrollSource
sq_pen move Text04 { 2 -2.5 -8.5 }
sq_pen set Text05 TrollSource
sq_pen move Text05 { 11.5 -3.5 -8.5 }
sq_object summon Troll Stuhl
sq_object summon Troll Stuhl
sq_object summon Troll Stuhl
sq_object summon Troll Stuhl
sq_object summon Troll Stuhl
sq_actor find Fenris_Stuhl 20 1
do_action beam wech 1
sq_object summon Zwerg Text01
sq_actor find Troll 20 5
sq_actor find Zwerg
set_visibility [Actor 7] 0
sq_actor actionlist { 2 6 4 5 } { { anim sit_sleep_doze } loop }
sq_actor actionlist 3 { { { anim sit_eatndrink_wipe } { anim sit_misc_gape } { anim sit_misc_headshake } { anim sit_idle_a } } loop }
do_action beam Troll1 2
do_action rotate back 2
do_action beam Troll2 3
do_action rotate right 3
do_action beam Troll3 4
do_action rotate front 4
do_action beam Troll4 5
do_action rotate front 5
do_action beam Troll4 5
do_action rotate front 5
do_action beam Troll5 6
do_action rotate front 6
sq_pen set Fenris Stuhl
sq_pen set Cam01 Fenris
sq_pen move Cam01 { -17 -1.5 0 }
sq_pen set Cam02 Fenris
sq_pen move Cam02 { 0 -2 0 }
sq_pen set Cam03 Fenris
sq_pen move Cam03 { -4 -2 0 }
sq_pen set Cam04 Fenris
sq_pen move Cam04 { -10 -1.8 0 }
sq_pen set Cam05 Fenris
sq_pen move Cam05 { -4 -3.5 0 }
sq_pen set Cam06 Fenris
sq_pen move Cam06 { 0 -6 0 }
sq_pen set Cam07 Fenris
sq_pen move Cam07 { -3 -3.5 0 }
sq_camera fix Cam01 0.8 0.05 0.2
do_wait time 3
set_roty [Actor 0] 1.97
start_fade 3 1
sq_camera addset inspeed 0.0 {{ s 0.3 1.0} { s 0.6 0.4} { s 1.0 0.4}}
sq_camera addset outspeed 0.4 {{ s 0.5 1.0} { s 1.0 0.0}}

sq_camera selset inspeed
sq_camera move Cam02 0.8 0.05 0.2 0.1
sq_wait 0
do_wait time 4
set_anim [Actor 0] sitfist 0 0
do_text 043aa 0 NoAnim 43aa Auto Off
sq_wait none
do_action anim sitfist 0
do_wait time 0.8
-sq_screenvibe kawumm
sq_camera selset outspeed
sq_camera move Cam03 0.8 -0.3 -0.7 0.4
sq_actor actionlist { 4 5 6 } { { { anim sit_idle_a } } loop }
sq_actor actionlist 2 { { anim sit_idle_a } { anim sit_sleep_fallasleep } loopstart { anim sit_sleep_doze } loop }
do_action anim sit_sleep_getup { 2 6 5 4 }
do_action beam Text03 7
sq_wait 0
do_wait
sq_wait none
do_text 043ab 0 { sittalkd sittalkd } 43ab Auto Off
do_wait time 2
sq_camera fix Cam04 0.71 -0.3 -0.4
sq_wait 0
do_wait
sq_camera selset inout
sq_camera fix Cam06 1.5 -0.2 -0.9
sq_wait none
gametime factor 0.8
do_action anim sitplantstart 0
do_wait time 1
do_text 043ac 0 NoAnim 43ac Auto Off
sq_camera move Cam06 1.5 -0.2 0.2 0.2
sq_pen move Cam04 { 1 0 0 }
do_wait time 4
sq_sound 43ae 0
do_wait time 0
-sound play fe_schritt2 1
-sq_screenvibe steps
sq_actor actionlist 0 { { anim sitplantloop } loop }
do_action anim sitplantloop 0
do_wait time 2
gametime factor 1

sq_wait none
sq_camera fix Cam03 0.7 -0.1 -0.2
do_wait time 1.7
do_text 043af 0 NoAnim 43ag Auto Off
do_action anim 36chef 6
do_action beam Text03 7
do_wait time 2.5
sq_camera fix Cam04 0.71 -0.3 -0.6
do_action anim sit_misc_headshake 3
do_wait time 1.5
sq_camera move Cam07 1.2 0.17 0.4 0.2
do_wait time 3
do_text 043ag 0 NoAnim 43af Auto Off
do_wait time 2
start_fade 4 0
do_wait time 4
+do_action beam Stuhl 1
+sq_object delete all
+do_action beam zurueck 0
adaptive_sound changetheme cave
start_fade 2 1
do_wait time 2
+cancel_fade

