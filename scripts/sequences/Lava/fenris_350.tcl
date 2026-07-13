sq_text file Lava
adaptive_sound changethemenow s350
sq_audio open Fenris_350
sq_pen set Fenris [Getobjpos Fenris_Stuhl]
sq_pen set wech 0
sq_pen move wech { 50 0 0 }
do_wait
sq_wait none
sq_object summon Fenris_Drunk Fenris
set_roty [Object 0] 1.8
sq_actor find Fenris_Drunk
sq_object summon Zwerg Fenris
set_visibility [Object 1] 0
sq_actor find Zwerg
sq_actor focus 2 none
sq_pen set Fenriszurueck 0
sq_pen set Text01 0
sq_pen move Text01 { 4.4 -4.7 0 }
set_visibility [Actor 0] 0
#do_action beam wech 0
sq_pen move Fenris { -1 -4.5 0 }
sq_actor actionlist 1 { {anim fenrir.trinken_loop} loop }
do_action anim fenrir.trinken_loop 1
sq_color 1 {255 118 57}
sq_color 2 {255 118 57}
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
do_text 350a 1 { sittalkb sittalkc } 350a
sq_wait none
do_text 350b 1 { getupfast } 350b
do_wait time 2.8
set_visibility [Actor 1] 0
do_action beam wech 1
-sound play fe_schritt1 1
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt2 0.9
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt1 0.8
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt2 0.7
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt1 0.6
-sq_screenvibe steps
do_wait time 0.5
-sound play fe_schritt2 0.5
-sq_screenvibe steps
do_wait time 0.5
do_wait time 2
sq_wait 2
do_text 350c 2 NoAnim 350c { 40 20 } Off

#--------------------------------------- Endfassung!
#sq_pen move Fenriszurueck { 15 0 4 }
do_wait time 1
#+do_action beam Fenriszurueck 0
+sq_object delete all
+do_wait
+set_visibility [Actor 0] 1
+adaptive_sound changethemenow fenris
+cancel_fade




