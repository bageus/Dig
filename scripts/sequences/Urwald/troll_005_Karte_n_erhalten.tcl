#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow trolle
#-----------------------------------------

sq_text file Urwald
sq_audio open Urw_028
sq_wait none
+sq_actor find Troll 20 1
sq_camera fix 0 1.5 0 0.4

do_action anim standloopa 0
do_action rotate right 0
do_wait time 1

sq_pen set Pos1 TriggerPos
sq_pen set Pos2 TriggerPos
sq_pen set Pos3 TriggerPos
sq_pen set Pos4 TriggerPos
+sq_pen set Pos5 TriggerPos

sq_pen move Pos1 {13.975 0 0}
sq_pen move Pos2 {-0.25 0 0}
sq_pen move Pos3 {20.975 0 0}
sq_pen move Pos4 {12 0 0}
+sq_pen move Pos5 {30 0 0}
sq_camera addset Cam01 0 {{s 0.2 0.4} {s 1.0 0.2}}
sq_camera addset Cam02 0.2 {{s 0.5 0.4} {s 1.0 0.0}}

do_wait time 0.1
sq_wait 1
do_action beam Pos1 1
sq_wait none
do_action rotate left 1
sq_camera selset Cam01
#sq_camera move TriggerPos 2.05 0.0 0.0 0.07 SelfRot
sq_camera move 0 0.9 0.0 0.4
do_wait time 0.5
sq_wait none
+do_action anim scout 0
do_wait time 0.7
#sq_camera move P1 1.05 -0.2 0.669 0.3 SelfRot
sq_camera selset Cam02
sq_camera move Pos4 1.1 -0.1 0.6
do_wait camera
sq_wait 1
do_action walk Pos4 1
do_action rotate Pos3 1
sq_wait none
do_action walk Pos3 1
sq_wait 0
do_action walk Pos2 0
do_action rotate right 0
sq_wait none
do_text 025aa 0 Auto 028a
do_wait time 1.5
+sq_camera fix 0 1 -0.1 -0.5
do_wait time 0.1
+call_method [Actor 1] destroy
#sq_camera get
sq_wait 0
do_wait

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker trolle [parse_pos Pos5]
#-----------------------------------------

