sq_text file Lava
adaptive_sound changethemenow s340
sq_audio open Fenris_340
sq_pen set Fenris [Getobjpos Fenris_Stuhl]
sq_pen set wech 0
sq_pen move wech { 50 0 0 }
do_wait
sq_wait none
sq_object summon Fenris_Drunk Fenris
set_roty [Object 0] 1.8
sq_actor find Fenris_Drunk
sq_object summon Zwerg Fenris 6
set_visibility [Object 1] 0
sq_actor find Zwerg 100 1 6
sq_actor focus 2 none
sq_pen set Fenriszurueck 0
sq_pen set Text01 0
sq_pen move Text01 { 4.4 -4.7 0 }
set_visibility [Actor 0] 0
#do_action beam wech 0
sq_pen move Fenris { -1 -4.5 0 }
sq_actor actionlist 1 { {anim drinkloop} loop }
do_action anim drinkloop 1
sq_camera fix Fenris 1.0 0.2 0.3
do_wait time 0.2
sq_actor actionlist 1 {}
sq_wait 1
do_action beam Text01 2
do_action anim drinkloop 1
do_action anim drinkloop 1
do_action anim drinkstopb 1
-sound play fe_schlag 1
-sq_screenvibe steps
do_action anim drinkstopa 1
sq_wait none
sq_color 1 {255 118 57}
sq_color 2 {255 118 57}
do_text 340a 1 { getupfast } 340a Auto
do_wait time 2.8
set_visibility [Actor 1] 0
do_action beam wech 1
-sound play fe_schritt2 1
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt1 0.9
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt2 0.85
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt1 0.80
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt2 0.75
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt1 0.70
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt2 0.70
-sq_screenvibe steps
do_wait time 0.6
-sound play fe_schritt1 0.65
-sq_screenvibe steps
do_wait time 0.6
sq_wait none
do_text 340b 2 NoAnim 340b { 60 20 } Off
do_wait time 2
-sound play fe_schritt2 0.65
-sq_screenvibe steps
do_wait time 0.3
-sound play fe_schritt1 0.60
-sq_screenvibe steps
do_wait time 0.3
-sound play fe_schritt2 0.60
-sq_screenvibe steps
do_wait time 0.6
sq_wait 2
do_text 340c 2 NoAnim 340c { 50 20 } Off
do_text 340d 2 NoAnim 340d { 50 20 } Off
do_wait time 1
-sound play fe_schritt1 0.65
-sq_screenvibe steps
do_wait time 0.7
-sound play fe_schritt2 0.65
-sq_screenvibe steps
do_wait time 1
-sound play fe_schritt1 0.65
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt2 0.65
-sq_screenvibe steps
sq_wait 2
do_wait time 0.6
do_text 340e 2 NoAnim 340e { 60 20 } Off
do_text 340f 2 NoAnim 340f { 50 20 } Off
#-----------------------------------------------> Endfassung!
#sq_pen move Fenriszurueck { 15 0 4 }
do_wait time 1

#+do_action beam Fenriszurueck 0
+del [Actor 1]
+del [Actor 2]
+set_visibility [Actor 0] 1
+adaptive_sound changethemenow fenris
+cancel_fade




