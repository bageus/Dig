###kein Text, der einzige actor ist schon ge"funden"... no init
sq_text file Kristall
sq_audio open kri_135
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s135
#-----------------------------------------

sq_pen set von_hier_gucken TriggerPos
sq_pen set overshoulder TriggerPos
sq_pen move overshoulder {0 -0.2 0}
sq_pen set erster_blick TriggerPos
sq_pen move erster_blick {-15 0 0}
sq_pen set establishing TriggerPos
sq_pen move establishing {-15 -3 0}
sq_pen set establishing2 establishing
sq_pen move establishing2 {4 0 0}
sq_pen set hoeher erster_blick
sq_pen move hoeher {-10 -15 0}
sq_pen set nochhoeher hoeher
sq_pen move nochhoeher {-22 -25 0}

sq_pen set marker1 TriggerPos
sq_pen move marker1 {-22.3 0 0}
sq_pen set marker2 TriggerPos
sq_pen move marker2 {54 -23 0}
sq_pen set marker3 TriggerPos
sq_pen move marker3 {100.5 16 0}
sq_color 0 Wiggle1

sq_wait all
sq_camera selset inout
sq_camera move von_hier_gucken 0.9 0.1 0.5 0.3
do_wait time 3
do_action walk von_hier_gucken 0
do_action rotate erster_blick 0
sq_wait 0
do_action anim washface 0
sq_wait none
do_text 0135a 0 {{talkacnta} {showup} {cheer} {talkacnta}} Dasist
do_wait time 0.5
sq_camera fix erster_blick 1.2 0.12 -1.0
do_wait time 0.5
sq_camera move hoeher 1.4 -0.3 -1.0 0.25
do_wait time 4
sq_camera move nochhoeher 1.8 -0.37 0.58 0.2
do_wait time 7
sq_camera fix establishing 1.7 -0.6 0.8
do_wait time 0.8
do_action anim lookup 0
#sq_wait none
+set_anim [Actor 0] standfronthith 0 1
do_wait time 0.9
+set_anim [Actor 0] standfronthith 9 0
do_wait time 0.5
sq_camera fix overshoulder 0.75 0 -1.1
do_wait time 0.5
+set_anim [Actor 0] standfronthith 9 1
do_wait time 2;
sq_wait all
sq_camera fix establishing2 2.4 -0.1 0.2
do_wait time 0.5
+sq_camera move von_hier_gucken 1.0 -0.1 0.1
sound play equake7 1
-sq_screenvibe equake7
do_wait time 0.5
+start_fade 1 0
do_wait time 1
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker wigglesburg [parse_pos marker1]
+adaptive_sound marker wigglesburg [parse_pos marker2]
+adaptive_sound marker wigglesburg [parse_pos marker3]
+adaptive_sound changethemenow wigglesburg
#-----------------------------------------

