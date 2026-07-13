#start_fade 0.5 0
do_wait time 0.5
sq_audio open Fenris_200a
sq_text file Lava
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s200a
#-----------------------------------------
sq_pen set Fenris 0
sq_pen set Fenriszurueck Fenris
sq_pen set wech Fenris
sq_pen move wech { -50 0 0 }
do_wait
sq_color 1 {255 118 57}
sq_pen set Fenris [Getobjpos Fenris_Stuhl]
sq_pen move Fenris { 30.5 0 -6 }
sq_pen set Trolle1 Fenris
sq_pen move Trolle1 { 2 0 0 }
sq_pen setz Trolle1 12
sq_pen form Trolle1 Circle 4 -30
sq_pen set Trolle2 Fenris
sq_pen move Trolle2 { -2 0 0 }
sq_pen setz Trolle2 12
sq_pen form Trolle2 Circle 5 37
sq_pen set Cam01 Fenris
sq_pen move Cam01 { 0 -4 0 }
sq_pen set Cam02 Fenris
sq_pen move Cam02 { -1 -3 0 }
sq_pen set Cam03 Fenris
sq_pen move Cam03 { 0 -3.7 0 }
sq_pen move Fenris { 0 -0.4 0 }
do_action beam wech 0
sq_object summon Fenris_002 Fenris
sq_actor find Fenris_002
sq_camera selset inout
do_wait
sq_object summon Troll Trolle1
sq_object summon Troll Trolle1
sq_object summon Troll Trolle1
sq_object summon Troll Trolle2
sq_object summon Troll Trolle2
sq_object summon Troll Trolle2
sq_object summon Troll Trolle2
sq_actor find Troll 50 7 0
do_wait
sq_actor actionlist { 2 3 4 5 6 7 8 } { { rotate 1 } }
do_action walk Trolle1 { 6 7 8 }
do_action walk Trolle2 { 2 3 4 5 }
set_anim [Actor 1] thronsit 0 2
sq_camera fix Cam01 0.65 -0.3 -0.8
start_fade 3 1
do_wait time 3
sq_wait 1
do_text 200aa 1 { throntalka } 200aa
do_action anim thronsit 1
do_action anim thronsit 1
sq_camera fix Cam02 1.2 0.24 0.2
do_action anim thronsit 1
do_action anim thronsit 1
do_text 200ab 1 { throntalka throntalkbstart throntalkbloop throntalkbstop} 200ab
sq_camera fix Cam03 1.1 0.35 -0.3
sq_actor actionlist { 4 6 7 } { { { rotate 2 } { rotate 3 } { rotate 5 } { rotate 8 } } { { anim scratch } { anim standanima } { anim lookaroundb } { anim lookaroundb } } { { anim scratch } { anim standanima } { anim lookaroundb } { anim lookaroundb } } { rotate 1 } }
sq_actor actionlist { 2 3 5 8 } { { { rotate 4 } { rotate 6 } { rotate 7 } } { { anim scratch } { anim standanima } { anim lookaroundb } { anim lookaroundb } } { { anim scratch } { anim standanima } { anim lookaroundb } { anim lookaroundb } } { rotate 1 } }
do_action wait 0.1 { 2 3 4 5 6 7 8 }
do_wait time 2
sq_wait none
-sq_screenvibe equake4
do_text 200ac 1 {throndespairstart throndespairloop throndespairloop } 200ac
do_wait time 1
do_action rotate right { 2 3 4 5 6 7 8 }
do_wait time 0.8
set_anim [Actor 6] runloop 0 2
set_vel [Actor 6] { 4 0 0 }
set_anim [Actor 7] runloop 0 2
set_vel [Actor 7] { 4 0 0 }
do_wait time 0.1
set_anim [Actor 8] runloop 0 2
set_vel [Actor 8] { 4 0 0 }
do_wait time 0.1
set_anim [Actor 5] runloop 0 2
set_vel [Actor 5] { 4 0 0 }
do_wait time 0.1
set_anim [Actor 4] runloop 0 2
set_vel [Actor 4] { 4 0 0 }
do_wait time 0.1
set_anim [Actor 3] runloop 0 2
set_vel [Actor 3] { 4 0 0 }
set_anim [Actor 2] runloop 0 2
set_vel [Actor 2] { 4 0 0 }
sq_wait 1
do_wait
sq_wait none
sq_camera move Cam03 0.65 0.35 -0.3 0.2
sq_actor actionlist 1 { { anim throndespairloopb } loop }
do_action anim throndespairend 1
do_wait time 2
start_fade 4 0
do_wait time 4.5
+sq_object delete all
+do_action beam Fenriszurueck 0
+do_wait time 2
+cancel_fade
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow lavawelt
#-----------------------------------------

