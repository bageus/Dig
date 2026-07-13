sq_text file Lava
adaptive_sound changethemenow s360
adaptive_sound volfact 70
sq_audio open Fenris_360
sq_pen set Fenris [Getobjpos Fenris_Stuhl]
sq_pen set wech 0
sq_pen move wech { 50 0 0 }
sq_pen set zweit Fenris
sq_pen move zweit { 1 0 0 }
do_wait
sq_wait none
sq_object summon Fenris_Drunk Fenris
set_roty [Object 0] 1.8
sq_actor find Fenris_Drunk
sq_object summon Zwerg Fenris
set_visibility [Object 1] 0
sq_actor find Zwerg
sq_actor find Fenris_Krug
do_wait
call_method [Actor 3] hide
sq_pen set Fenriszurueck 0
sq_pen set Text01 0
sq_pen move Text01 { 4.4 -4.7 0 }
set_visibility [Actor 0] 0
#do_action beam wech 0
sq_pen set FenrisCam Fenris
sq_pen move FenrisCam { -1.5 -5 0 }
sq_actor actionlist 1 { {anim drinkloop} loop }
do_action anim drinkloop 1
sq_color 1 {255 118 57}
sq_color 2 {255 118 57}
sq_camera fix FenrisCam 1.5 0.2 0.3
do_wait time 0.2
sq_actor actionlist 1 {}
sq_wait 1
do_action beam Text01 2
do_action anim drinkloop 1
do_action anim drinkloop 1
do_action anim drinkstopb 1
-sound play fe_schlag 1
-sq_screenvibe steps
call_method [Actor 3] show
do_action anim drinkstopa 1
do_action anim getup 1
sq_object beam 0 zweit
#sq_camera fix Fenris 1.4 0.2 0.5
gametime factor 1.4
do_text 360a 1 { standtalk standtalk } 360a
gametime factor 1
do_action anim sitdown 1
sq_object beam 0 Fenris
do_action anim drinkstarta 1
call_method [Actor 3] hide
do_action anim drinkstartb 1
sq_wait none
sq_actor actionlist 1 { {anim drinkloop} loop }
do_action anim drinkloop 1
do_wait time 1
do_wait time 1
#+do_action beam Fenriszurueck 0
+sq_object delete all
+do_wait
call_method [Actor 3] show
+set_visibility [Actor 0] 1
+adaptive_sound volfact 100
+adaptive_sound changethemenow fenris
+cancel_fade




