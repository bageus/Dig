#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow trolle
#-----------------------------------------

sq_text file Urwald
sq_audio open Urw_028b
sq_camera follow 0 1.1 0.1
+sq_actor find Troll 20 1
+sq_pen set Pos5 TriggerPos
+sq_pen move Pos5 {30 0 0}

do_wait camera
sq_wait all
do_action walk TriggerPos 0
+do_action rotate front 0
+call_method [Actor 1] destroy
do_text 025ab 0 { impatient } 028a
do_wait

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker trolle [parse_pos Pos5]
#-----------------------------------------

